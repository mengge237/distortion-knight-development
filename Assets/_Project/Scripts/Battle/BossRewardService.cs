using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MutationChess.Core;

namespace MutationChess.Battle
{
    public class BossRewardService : MonoBehaviour
    {
        private static BossRewardService _instance;
        public static BossRewardService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<BossRewardService>();
                return _instance;
            }
        }

        [Header("Boss奖励配置")]
        [SerializeField] private BossRewardConfig config;

        [Header("调试设置")]
        [SerializeField] private bool debugMode = true;

        public BossRewardConfig Config => config;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        void Start()
        {
            if (config == null)
            {
                config = Resources.Load<BossRewardConfig>(ResourcePaths.BossRewardConfig);
                if (config == null && debugMode)
                    GameLogger.LogWarning("[BossRewardService] BossRewardConfig not found, please create Resources/BossRewardConfig.asset");
            }
        }

        public struct BossRewardResult
        {
            public int goldAmount;
            public Relic factionUnlockRelic;
            public Relic bonusRelic;
            public Card factionCard;
        }

        public BossRewardResult GenerateBossRewards()
        {
            BossRewardResult result = new BossRewardResult();

            if (config == null)
            {
                result.goldAmount = 100;
                return result;
            }

            result.goldAmount = Random.Range(config.minGold, config.maxGold + 1);


            List<RelicDataAsset> allUnlockRelics = LoadAllFactionUnlockRelics();

            if (allUnlockRelics.Count > 0)
            {
                var availableUnlockers = allUnlockRelics
                    .Where(r => r != null && r.isFactionUnlocker)
                    .Where(r => !IsFactionAlreadyUnlocked(r.unlockedFaction))
                    .Where(r => !IsRelicAlreadyOwned(r.relicId))
                    .ToList();

                if (availableUnlockers.Count > 0)
                {
                    var asset = availableUnlockers[Random.Range(0, availableUnlockers.Count)];
                    var relicManager = RelicManager.Instance;
                    if (relicManager != null)
                    {
                        result.factionUnlockRelic = relicManager.CreateRelicFromAsset(asset);
                        if (debugMode)
                            GameLogger.Log($"[BossRewardService] Boss掉落阵营解锁遗物：{asset.relicName}（阵营：{asset.unlockedFaction}）");
                    }
                }
                else if (debugMode)
                {
                    GameLogger.Log("[BossRewardService] 无可用的阵营解锁遗物");
                }
            }

            if (config.bonusRelics != null && config.bonusRelics.Count > 0)
            {
                var available = config.bonusRelics
                    .Where(r => r != null && !r.isFactionUnlocker)
                    .Where(r => !IsRelicAlreadyOwned(r.relicId))
                    .ToList();

                if (available.Count > 0)
                {
                    var asset = available[Random.Range(0, available.Count)];
                    var relicManager = RelicManager.Instance;
                    if (relicManager != null)
                    {
                        result.bonusRelic = relicManager.CreateRelicFromAsset(asset);
                        if (debugMode)
                            GameLogger.Log($"[BossRewardService] Boss掉落额外遗物：{asset.relicName}");
                    }
                }
            }

            if (config.factionCardRewards != null && config.factionCardRewards.Count > 0)
            {
                var unlockService = FactionUnlockService.Instance;
                if (unlockService != null)
                {
                    var unlockedFactionCards = config.factionCardRewards
                        .Where(c => c != null)
                        .Where(c => unlockService.IsFactionUnlocked(c.faction))
                        .ToList();

                    if (unlockedFactionCards.Count > 0)
                    {
                        var asset = unlockedFactionCards[Random.Range(0, unlockedFactionCards.Count)];
                        var card = CreateCardFromAsset(asset);
                        if (card != null)
                        {
                            result.factionCard = card;
                            if (debugMode)
                                GameLogger.Log($"[BossRewardService] Boss掉落阵营卡牌：{asset.cardName}（阵营：{asset.faction}）");
                        }
                    }
                }
            }

            return result;
        }

        private List<RelicDataAsset> LoadAllFactionUnlockRelics()
        {
            List<RelicDataAsset> unlockRelics = new List<RelicDataAsset>();


            if (config != null && config.factionUnlockRelics != null && config.factionUnlockRelics.Count > 0)
            {
                unlockRelics.AddRange(config.factionUnlockRelics);
            }


            RelicDataAsset[] allRelics = Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics);
            foreach (var relic in allRelics)
            {
                if (relic != null && relic.isFactionUnlocker)
                {
                    if (!unlockRelics.Any(r => r.relicId == relic.relicId))
                    {
                        unlockRelics.Add(relic);
                    }
                }
            }

            return unlockRelics;
        }

        private bool IsFactionAlreadyUnlocked(CardFaction faction)
        {
            if (faction == CardFaction.None) return true;
            var unlockService = FactionUnlockService.Instance;
            if (unlockService == null) return false;
            return unlockService.IsFactionUnlocked(faction);
        }

        private bool IsRelicAlreadyOwned(string relicId)
        {
            if (string.IsNullOrEmpty(relicId)) return true;
            var relicManager = RelicManager.Instance;
            if (relicManager == null) return false;
            return relicManager.HasRelic(relicId);
        }

        private Card CreateCardFromAsset(CardDataAsset asset)
        {
            if (asset == null) return null;
            CardName cardName;
            if (System.Enum.TryParse(asset.name, out cardName))
            {
                return CardData.CreateCard(cardName);
            }
            return null;
        }

        public void OnFactionUnlockRelicClaimed(Relic relic)
        {
            if (relic == null) return;

            var unlockService = FactionUnlockService.Instance;
            if (unlockService != null && relic.faction != CardFaction.None)
            {
                unlockService.UnlockFaction(relic.faction);
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}


