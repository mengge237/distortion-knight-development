using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 游戏全局配置（生命、金币、能量、伤害倍率、掉落概率等）
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MutationChess/Config/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("玩家属性")]
        [Tooltip("玩家最大生命值")]
        public int maxHealth = 100;

        [Tooltip("玩家初始金币")]
        public int startingGold = 99;

        [Tooltip("药水携带上限")]
        public int maxPotions = 3;

        [Header("手牌与能量")]
        [Tooltip("手牌上限")]
        public int maxHandSize = 10;

        [Tooltip("每回合抽卡数")]
        public int cardsPerTurn = 5;

        [Tooltip("每回合最大能量")]
        public int maxEnergy = 3;

        [Tooltip("初始手牌数")]
        public int startingHandSize = 5;

        [Header("伤害倍率")]
        [Tooltip("力量伤害倍率")]
        public float strengthDamageMultiplier = 1.0f;

        [Tooltip("敏捷格挡倍率")]
        public float dexterityBlockMultiplier = 1.0f;

        [Tooltip("虚弱伤害倍率")]
        public float weakDamageMultiplier = 0.75f;

        [Tooltip("易伤伤害倍率")]
        public float vulnerabilityDamageMultiplier = 1.5f;

        [Header("转换默认值")]
        [Tooltip("3点血量 = 1能量")]
        public int defaultBloodPerEnergy = 3;

        [Tooltip("5点格挡 = 1能量")]
        public int defaultBlockPerEnergy = 5;

        [Header("地图层数")]
        [Tooltip("最大楼层数")]
        public int maxFloor = 3;

        [Header("地图与掉落")]
        [Tooltip("每层金币加成15% = 0.15")]
        public float goldBonusPerFloor = 0.15f;

        [Tooltip("每层稀有掉落加成10% = 0.10")]
        public float rareDropBonusPerFloor = 0.10f;

        [Tooltip("每层遗物稀有度加成")]
        public float relicRarityBonusPerFloor = 0.15f;

        [Header("遗物稀有度概率")]
        [Tooltip("普通遗物基础概率")]
        public float relicCommonBaseChance = 0.72f;
        [Tooltip("稀有遗物基础概率")]
        public float relicRareBaseChance = 0.20f;
        [Tooltip("传说遗物基础概率")]
        public float relicLegendaryBaseChance = 0.08f;

        [Header("药水掉落")]
        [Tooltip("前期药水掉落概率")]
        [Range(0, 1)]
        public float earlyPotionDropChance = 0.45f;

        [Tooltip("后期药水掉落概率")]
        [Range(0, 1)]
        public float latePotionDropChance = 0.10f;

        [Tooltip("普通敌人药水倍率")]
        public float normalEnemyPotionMultiplier = 1.0f;

        [Tooltip("精英敌人药水倍率")]
        public float eliteEnemyPotionMultiplier = 1.5f;

        [Tooltip("Boss掉落药水倍率，0=Boss不掉药水")]
        public float bossEnemyPotionMultiplier = 0.0f;


        private static GameConfig _instance;

        /// <summary>
        /// 单例实例，从 Resources/GameConfig 加载，不存在则创建默认实例
        /// </summary>
        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfig>(ResourcePaths.GameConfig);
                    if (_instance == null)
                    {
                        _instance = CreateInstance<GameConfig>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 重置单例，下次访问时重新加载
        /// </summary>
        public static void Reload()
        {
            _instance = null;
        }
    }
}
