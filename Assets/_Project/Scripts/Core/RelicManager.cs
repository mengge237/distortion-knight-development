using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MutationChess.Core
{
    public class RelicManager : MonoBehaviour
    {
        private static RelicManager _instance;
        public static RelicManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<RelicManager>();
                return _instance;
            }
        }

        [Header("=== 掉落 & 商店生成配置 ===")]
        [SerializeField] [Range(0f, 1f)] private float normalRelicDropChance = 0.35f;

        private List<Relic> ownedRelics = new List<Relic>();
        private HashSet<string> relicsUsedThisBattle = new HashSet<string>();

        public event System.Action OnRelicsChanged;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        // ==================== 遗物收集 ====================

        public void AddRelic(Relic relic)
        {
            if (relic == null) return;
            ownedRelics.Add(relic);
            Debug.Log($"[RelicManager] 获得遗物: {relic.relicName} ({relic.GetRarityName()})");
            OnRelicsChanged?.Invoke();
        }

        public void RemoveRelic(string relicId)
        {
            Relic relic = ownedRelics.FirstOrDefault(r => r.relicId == relicId);
            if (relic != null)
            {
                ownedRelics.Remove(relic);
                Debug.Log($"[RelicManager] 移除遗物: {relic.relicName}");
                OnRelicsChanged?.Invoke();
            }
        }

        public bool HasRelic(string relicId)
        {
            return ownedRelics.Any(r => r.relicId == relicId);
        }

        public Relic GetRelic(string relicId)
        {
            return ownedRelics.FirstOrDefault(r => r.relicId == relicId);
        }

        public List<Relic> GetAllRelics()
        {
            return new List<Relic>(ownedRelics);
        }

        public int Count => ownedRelics.Count;

        // ==================== 战斗回合重置 ====================

        /// <summary>每场战斗开始时调用，重置限次效果追踪</summary>
        public void OnBattleStart()
        {
            relicsUsedThisBattle.Clear();
        }

        // ==================== 遗物效果查询 ====================

        /// <summary>获取所有BonusDamage遗物效果的总伤害加成</summary>
        public int GetBonusDamage()
        {
            int bonus = 0;
            foreach (var relic in ownedRelics)
            {
                if (relic.effectType == RelicEffectType.BonusDamage)
                    bonus += Mathf.RoundToInt(relic.effectValue);
            }
            return bonus;
        }

        /// <summary>获取每场战斗限1次的攻击力加成（铁戒指等），使用后标记已使用</summary>
        public float TryGetOncePerBattleAttackBoost()
        {
            float bonus = 0;
            foreach (var relic in ownedRelics)
            {
                if (relic.effectType == RelicEffectType.OncePerBattleAttackBoost
                    && !relicsUsedThisBattle.Contains(relic.relicId))
                {
                    relicsUsedThisBattle.Add(relic.relicId);
                    bonus += relic.effectValue;
                    Debug.Log($"[RelicManager] 触发每场限1次效果: {relic.relicName} (+{relic.effectValue})");
                }
            }
            return bonus;
        }

        /// <summary>获取所有VictoryGoldPercent遗物的总百分比</summary>
        public float GetVictoryGoldPercent()
        {
            float percent = 0;
            foreach (var relic in ownedRelics)
            {
                if (relic.effectType == RelicEffectType.VictoryGoldPercent)
                    percent += relic.effectValue;
            }
            return percent;
        }

        /// <summary>检查是否有秒杀遗物，返回遗物ID</summary>
        public string GetInstantKillRelicId()
        {
            foreach (var relic in ownedRelics)
            {
                if (relic.effectType == RelicEffectType.InstantKill)
                    return relic.relicId;
            }
            return null;
        }

        /// <summary>获取所有HealPercentEachTurn遗物的总回复百分比</summary>
        public float GetHealPercentEachTurn()
        {
            float percent = 0;
            foreach (var relic in ownedRelics)
            {
                if (relic.effectType == RelicEffectType.HealPercentEachTurn)
                    percent += relic.effectValue;
            }
            return percent;
        }

        // ==================== 商店 ====================

        /// <summary>
        /// 加载所有可获得的遗物数据资产（排除 Special 稀有度）
        /// </summary>
        public List<RelicDataAsset> LoadAllObtainableRelicAssets()
        {
            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>("Relics");
            List<RelicDataAsset> result = new List<RelicDataAsset>();

            foreach (var asset in allAssets)
            {
                if (asset.rarity != RelicRarity.Special)
                    result.Add(asset);
            }
            return result;
        }

        /// <summary>
        /// 从 Asset 数据创建 Relic 实例并加载图标
        /// </summary>
        public Relic CreateRelicFromAsset(RelicDataAsset asset)
        {
            if (asset == null) return null;

            Relic relic = new Relic(
                asset.relicId,
                asset.relicName,
                asset.rarity,
                asset.faction,
                asset.description,
                asset.price
            );

            relic.effectType = asset.effectType;
            relic.effectValue = asset.effectValue;

            if (!string.IsNullOrEmpty(asset.iconPath))
            {
                relic.icon = Resources.Load<Sprite>(asset.iconPath);
                if (relic.icon == null)
                    Debug.LogWarning($"[RelicManager] 未找到遗物图标: {asset.iconPath}");
            }

            return relic;
        }

        /// <summary>
        /// 从池中随机选择一个未拥有的遗物
        /// </summary>
        public Relic GenerateRandomRelic(List<RelicDataAsset> pool)
        {
            if (pool == null || pool.Count == 0) return null;

            List<RelicDataAsset> available = pool
                .Where(a => !HasRelic(a.relicId))
                .ToList();

            if (available.Count == 0) return null;

            RelicDataAsset chosen = available[Random.Range(0, available.Count)];
            return CreateRelicFromAsset(chosen);
        }

        /// <summary>
        /// 随机生成 N 个不重复的遗物
        /// </summary>
        public List<Relic> GenerateRandomRelics(List<RelicDataAsset> pool, int count)
        {
            List<Relic> result = new List<Relic>();
            List<RelicDataAsset> available = pool
                .Where(a => !HasRelic(a.relicId))
                .ToList();

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int index = Random.Range(0, available.Count);
                result.Add(CreateRelicFromAsset(available[index]));
                available.RemoveAt(index);
            }

            return result;
        }

        // ==================== 掉落掉落 ====================

        /// <summary>
        /// 普通怪物遗物掉落（按概率判定）
        /// </summary>
        public Relic TryNormalMonsterRelicDrop()
        {
            if (Random.value <= normalRelicDropChance)
            {
                var pool = LoadAllObtainableRelicAssets();
                Relic relic = GenerateRandomRelic(pool);
                if (relic != null)
                    Debug.Log($"[RelicManager] 普通怪物掉落遗物: {relic.relicName}");
                return relic;
            }
            return null;
        }

        /// <summary>
        /// 精英怪物遗物掉落（必定掉落）
        /// </summary>
        public Relic GetEliteMonsterRelicDrop()
        {
            var pool = LoadAllObtainableRelicAssets();
            Relic relic = GenerateRandomRelic(pool);
            if (relic != null)
                Debug.Log($"[RelicManager] 精英怪物掉落遗物: {relic.relicName}");
            return relic;
        }

        /// <summary>
        /// 生成 N 个商店遗物
        /// </summary>
        public List<Relic> GenerateShopRelics(int count = 5)
        {
            var pool = LoadAllObtainableRelicAssets();
            return GenerateRandomRelics(pool, count);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
