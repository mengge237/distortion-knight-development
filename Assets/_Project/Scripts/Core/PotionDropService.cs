using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 药水掉落服务：根据楼层、节点进度、敌人类型计算药水掉落概率与稀有度。
    /// 楼层越高，掉落概率越低，稀有度越高；普通敌人最低，精英居中，Boss 通过 BossRewardService 单独处理。
    /// 楼层与节点进度共同影响最终概率，可通过 EffectManager.CalculatePotionDropChance 修改掉率。
    /// </summary>
    public class PotionDropService : MonoBehaviour
    {
        private static PotionDropService _instance;
        public static PotionDropService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<PotionDropService>();
                return _instance;
            }
        }

        [Header("=== 基础掉落概率 ===")]
        [Tooltip("游戏开始时的药水掉率（楼层1节点0）")]
        [Range(0f, 1f)]
        [SerializeField] private float baseDropChance = 0.45f;

        [Tooltip("游戏结束时的药水掉率（楼层3节点末）")]
        [Range(0f, 1f)]
        [SerializeField] private float endDropChance = 0.10f;

        [Header("=== 药水稀有度权重 ===")]
        [Tooltip("前期稀有度药水权重：Common / Uncommon / Rare")]
        [SerializeField] private Vector3 earlyRarityWeights = new Vector3(0.70f, 0.25f, 0.05f);

        [Tooltip("后期稀有度药水权重：Common / Uncommon / Rare")]
        [SerializeField] private Vector3 lateRarityWeights = new Vector3(0.30f, 0.40f, 0.30f);

        [Header("=== 敌人类型掉落倍率 ===")]
        [Tooltip("普通敌人药水掉落倍率")]
        [Range(0f, 2f)]
        [SerializeField] private float normalEnemyMultiplier = 1.0f;

        [Tooltip("精英敌人药水掉落倍率")]
        [Range(0f, 2f)]
        [SerializeField] private float eliteEnemyMultiplier = 1.5f;

        [Tooltip("Boss药水掉落倍率，通常为 0（Boss 走独立奖励逻辑）")]
        [Range(0f, 2f)]
        [SerializeField] private float bossEnemyMultiplier = 0f;

        private List<PotionDataAsset> potionAssetCache;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// 计算当前药水掉率，综合考虑楼层和节点进度的影响。
        /// 楼层与节点进度交叉影响：totalProgress = (floor-1 + nodeProgress) / maxFloor
        /// 掉率从 baseDropChance 线性插值到 endDropChance。
        /// </summary>
        public float CalculateBaseDropChance(int currentFloor, int maxFloor, float nodeProgress)
        {
            // 全局进度 0~1：楼层+节点交叉推进
            float floorProgress = Mathf.Clamp01((float)(currentFloor - 1) / Mathf.Max(1, maxFloor - 1));
            float totalProgress = Mathf.Clamp01((floorProgress + nodeProgress) / 2f);

            // 线性插值：前期 -> 后期
            float dropChance = Mathf.Lerp(baseDropChance, endDropChance, totalProgress);

            // 触发遗物修改掉率
            var effectManager = EffectManager.Instance;
            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext();
                ctx.floatValue = dropChance;
                effectManager.Trigger(EffectTrigger.CalculatePotionDropChance, ctx);
                dropChance = ctx.floatValue;
            }

            return Mathf.Clamp01(dropChance);
        }

        /// <summary>
        /// 尝试掉落一个药水。返回 null 表示未掉落。
        /// </summary>
        public Potion TryDropPotion(EnemyType enemyType, int currentFloor, int maxFloor, float nodeProgress)
        {
            float baseChance = CalculateBaseDropChance(currentFloor, maxFloor, nodeProgress);

            // 应用的敌人倍率
            float multiplier = GetEnemyMultiplier(enemyType);
            float finalChance = Mathf.Clamp01(baseChance * multiplier);

            bool dropped = UnityEngine.Random.value <= finalChance;
            GameLogger.Log($"[PotionDropService] 敌人:{enemyType} 楼层:{currentFloor}/{maxFloor} 节点进度:{nodeProgress:F2} 基础:{baseChance:P0} 倍率:{multiplier:F2} 最终:{finalChance:P0} 掉落:{dropped}");

            if (!dropped) return null;

            return GeneratePotion(currentFloor, maxFloor, nodeProgress);
        }

        /// <summary>
        /// 强制生成一瓶药水，用于特殊奖励（如 Boss 奖励）。
        /// </summary>
        public Potion GeneratePotion(int currentFloor, int maxFloor, float nodeProgress)
        {
            var pool = LoadPotionAssets();
            if (pool == null || pool.Count == 0)
            {
                GameLogger.LogWarning("[PotionDropService] 药水池为空");
                return null;
            }

            // 计算全局进度与稀有度权重
            float floorProgress = Mathf.Clamp01((float)(currentFloor - 1) / Mathf.Max(1, maxFloor - 1));
            float totalProgress = Mathf.Clamp01((floorProgress + nodeProgress) / 2f);

            // 稀有度权重插值
            Vector3 weights = Vector3.Lerp(earlyRarityWeights, lateRarityWeights, totalProgress);
            float commonW = Mathf.Max(0f, weights.x);
            float uncommonW = Mathf.Max(0f, weights.y);
            float rareW = Mathf.Max(0f, weights.z);
            float totalW = commonW + uncommonW + rareW;

            if (totalW <= 0f)
            {
                commonW = 1f;
                totalW = 1f;
            }

            // 按稀有度分池
            var common = pool.Where(a => a.rarity == PotionRarity.Common).ToList();
            var uncommon = pool.Where(a => a.rarity == PotionRarity.Uncommon).ToList();
            var rare = pool.Where(a => a.rarity == PotionRarity.Rare).ToList();

            // 按权重选择稀有度
            float roll = UnityEngine.Random.value * totalW;
            PotionRarity chosenRarity;
            if (roll < commonW)
                chosenRarity = PotionRarity.Common;
            else if (roll < commonW + uncommonW)
                chosenRarity = PotionRarity.Uncommon;
            else
                chosenRarity = PotionRarity.Rare;

            // 按对应稀有度取出对应池
            List<PotionDataAsset> targetPool;
            switch (chosenRarity)
            {
                case PotionRarity.Uncommon: targetPool = uncommon; break;
                case PotionRarity.Rare: targetPool = rare; break;
                default: targetPool = common; break;
            }


            if (targetPool.Count == 0)
            {
                if (common.Count > 0) targetPool = common;
                else if (uncommon.Count > 0) targetPool = uncommon;
                else targetPool = rare;
            }

            if (targetPool.Count == 0)
            {
                GameLogger.LogWarning("[PotionDropService] 无可用药水");
                return null;
            }

            var asset = targetPool[UnityEngine.Random.Range(0, targetPool.Count)];
            Potion potion = CreatePotionFromAsset(asset);

            GameLogger.Log($"[PotionDropService] 生成药水: {potion?.potionName} ({chosenRarity}) 进度:{totalProgress:F2}");
            return potion;
        }

        private float GetEnemyMultiplier(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.Elite: return eliteEnemyMultiplier;
                case EnemyType.Boss: return bossEnemyMultiplier;
                default: return normalEnemyMultiplier;
            }
        }

        private List<PotionDataAsset> LoadPotionAssets()
        {
            if (potionAssetCache != null) return potionAssetCache;

            PotionDataAsset[] allAssets = Resources.LoadAll<PotionDataAsset>("Potions");
            potionAssetCache = new List<PotionDataAsset>(allAssets);
            return potionAssetCache;
        }

        private Potion CreatePotionFromAsset(PotionDataAsset asset)
        {
            if (asset == null) return null;

            Potion potion = new Potion(
                asset.potionId,
                asset.potionName,
                asset.rarity,
                asset.description,
                asset.price
            );

            if (asset.effectIds != null && asset.effectIds.Count > 0)
            {
                foreach (string effectId in asset.effectIds)
                {
                    CardEffect effect = LoadPotionEffect(effectId);
                    if (effect != null)
                    {
                        potion.effects.Add(effect);
                    }
                    else
                    {
                        GameLogger.LogWarning($"[PotionDropService] 无法加载药水效果: {effectId}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(asset.iconPath))
            {
                potion.icon = Resources.Load<Sprite>(asset.iconPath);
            }

            return potion;
        }

        private CardEffect LoadPotionEffect(string effectId)
        {
            string effectPath = $"Effects/{effectId}";
            CardEffect effect = Resources.Load<CardEffect>(effectPath);

            if (effect == null)
                effect = Resources.Load<CardEffect>($"CardEffects/{effectId}");

            if (effect == null)
                effect = Resources.Load<CardEffect>(effectId);

            return effect;
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
