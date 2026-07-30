using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    ///
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MutationChess/Config/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("")]
        [Tooltip("")]
        public int maxHealth = 100;

        [Tooltip("")]
        public int startingGold = 99;

        [Tooltip("")]
        public int maxPotions = 3;

        [Header("")]
        [Tooltip("")]
        public int maxHandSize = 10;

        [Tooltip("")]
        public int cardsPerTurn = 5;

        [Tooltip("")]
        public int maxEnergy = 3;

        [Tooltip("")]
        public int startingHandSize = 5;

        [Header("")]
        [Tooltip("")]
        public float strengthDamageMultiplier = 1.0f;

        [Tooltip("")]
        public float dexterityBlockMultiplier = 1.0f;

        [Tooltip("")]
        public float weakDamageMultiplier = 0.75f;

        [Tooltip("")]
        public float vulnerabilityDamageMultiplier = 1.5f;

        [Header("")]
        [Tooltip("3点血量 = 1能量")]
        public int defaultBloodPerEnergy = 3;

        [Tooltip("5点格挡 = 1能量")]
        public int defaultBlockPerEnergy = 5;

        [Header("")]
        [Tooltip("")]
        public int maxFloor = 3;

        [Tooltip("每层金币加成15% = 0.15")]
        public float goldBonusPerFloor = 0.15f;

        [Tooltip("每层稀有掉落加成10% = 0.10")]
        public float rareDropBonusPerFloor = 0.10f;

        [Tooltip("")]
        public float relicRarityBonusPerFloor = 0.15f;

        [Header("")]
        [Tooltip("Common")]
        public float relicCommonBaseChance = 0.72f;
        [Tooltip("Rare")]
        public float relicRareBaseChance = 0.20f;
        [Tooltip("Legendary")]
        public float relicLegendaryBaseChance = 0.08f;

        [Header("")]
        [Tooltip("")]
        [Range(0, 1)]
        public float earlyPotionDropChance = 0.45f;

        [Tooltip("")]
        [Range(0, 1)]
        public float latePotionDropChance = 0.10f;

        [Tooltip("")]
        public float normalEnemyPotionMultiplier = 1.0f;

        [Tooltip("")]
        public float eliteEnemyPotionMultiplier = 1.5f;

        [Tooltip("Boss掉落药水倍率，0=Boss不掉药水")]
        public float bossEnemyPotionMultiplier = 0.0f;


        private static GameConfig _instance;

        /// <summary>
        ///
        /// </summary>
        public static GameConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<GameConfig>("GameConfig");
                    if (_instance == null)
                    {
                        _instance = CreateInstance<GameConfig>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public static void Reload()
        {
            _instance = null;
        }
    }
}
