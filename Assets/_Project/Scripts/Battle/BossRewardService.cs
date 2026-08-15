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
                    GameLogger.Log("[BossRewardService] 未找到 Resources/BossRewardConfig.asset，将使用内置回退奖励池（阵营解锁遗物自动从 Resources/Relics 加载）");
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

            // 配置缺失时使用默认金币与内置回退池（不再直接空手而归）
            result.goldAmount = config != null
                ? Random.Range(config.minGold, config.maxGold + 1)
                : 100;

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

            // 额外遗物：配置池优先，无配置时使用内置回退池（稀有/传说、无阵营协同）
            List<RelicDataAsset> bonusPool = GetBonusRelicPool();
            if (bonusPool.Count > 0)
            {
                var available = bonusPool
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

            // 阵营卡牌仅由配置提供（无配置时跳过）
            if (config != null && config.factionCardRewards != null && config.factionCardRewards.Count > 0)
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

        /// <summary>额外Boss遗物池：配置优先；无配置时回退到 Resources 中的稀有/传说常规遗物。</summary>
        private List<RelicDataAsset> GetBonusRelicPool()
        {
            if (config != null && config.bonusRelics != null && config.bonusRelics.Count > 0)
                return config.bonusRelics.Where(r => r != null).ToList();

            List<RelicDataAsset> fallback = new List<RelicDataAsset>();
            RelicDataAsset[] allRelics = Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics);
            foreach (var relic in allRelics)
            {
                if (relic == null) continue;
                if (relic.isBossRelic || relic.isStartingRelic || relic.isFactionUnlocker || relic.isShopRelic || relic.isCurse) continue;
                if (relic.faction != CardFaction.None) continue;
                if (relic.rarity != RelicRarity.Rare && relic.rarity != RelicRarity.Legendary) continue;
                fallback.Add(relic);
            }
            return fallback;
        }

        /// <summary>
        /// 生成 Boss 遗物选择面板的选项（不重复取样）：
        /// 主池为「未拥有且阵营未解锁」的阵营解锁遗物，不足时用额外Boss遗物池补足。
        /// 用于战胜 Boss 后优先弹出的三选一遗物面板。
        /// </summary>
        public List<Relic> GenerateBossRelicChoices(int count)
        {
            List<Relic> choices = new List<Relic>();
            if (count <= 0) return choices;

            var relicManager = RelicManager.Instance;
            if (relicManager == null) return choices;

            List<RelicDataAsset> pool = new List<RelicDataAsset>();

            // 主池：阵营解锁遗物（未拥有、阵营未解锁）
            foreach (var unlock in LoadAllFactionUnlockRelics())
            {
                if (unlock == null || !unlock.isFactionUnlocker) continue;
                if (IsFactionAlreadyUnlocked(unlock.unlockedFaction)) continue;
                if (IsRelicAlreadyOwned(unlock.relicId)) continue;
                pool.Add(unlock);
            }

            // 补充池：额外 Boss 遗物（未拥有）
            foreach (var bonus in GetBonusRelicPool())
            {
                if (bonus == null || bonus.isFactionUnlocker) continue;
                if (IsRelicAlreadyOwned(bonus.relicId)) continue;
                pool.Add(bonus);
            }

            // 随机取 count 个不重复
            while (choices.Count < count && pool.Count > 0)
            {
                int idx = Random.Range(0, pool.Count);
                Relic relic = relicManager.CreateRelicFromAsset(pool[idx]);
                pool.RemoveAt(idx);
                if (relic != null)
                {
                    if (debugMode)
                        GameLogger.Log($"[BossRewardService] Boss遗物选项：{relic.relicName}");
                    choices.Add(relic);
                }
            }
            return choices;
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


