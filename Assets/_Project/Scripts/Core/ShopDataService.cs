using System.Collections.Generic;
using MutationChess.Battle;
using MutationChess.Core;
using UnityEngine;
using System.Linq;

namespace MutationChess.Core
{
    public enum ShopItemType
    {
        ColoredCard,
        ColorlessCard,
        Relic,
        Potion,
        CardRemoval
    }

    public class ShopItem
    {
        public ShopItemType type;
        public object item;
        public int basePrice;
        public int finalPrice;
        public bool isOnSale;
        public bool isSold;
        public bool isShopRelic;

        public string Name
        {
            get
            {
                switch (type)
                {
                    case ShopItemType.ColoredCard:
                    case ShopItemType.ColorlessCard:
                        return (item as Card)?.cardName ?? "未知卡牌";
                    case ShopItemType.Relic:
                        return (item as Relic)?.relicName ?? "未知遗物";
                    case ShopItemType.Potion:
                        return (item as Potion)?.potionName ?? "未知药水";
                    case ShopItemType.CardRemoval:
                        return "移除卡牌";
                    default:
                        return "未知";
                }
            }
        }

        public string Description
        {
            get
            {
                switch (type)
                {
                    case ShopItemType.ColoredCard:
                    case ShopItemType.ColorlessCard:
                        return (item as Card)?.GetDescription() ?? "";
                    case ShopItemType.Relic:
                        return (item as Relic)?.description ?? "";
                    case ShopItemType.Potion:
                        return (item as Potion)?.description ?? "";
                    case ShopItemType.CardRemoval:
                        return "从牌组中永久移除一张卡牌。";
                    default:
                        return "";
                }
            }
        }
    }

    public class ShopDataService : MonoBehaviour
    {
        private static ShopDataService _instance;
        public static ShopDataService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<ShopDataService>();
                return _instance;
            }
        }

        [SerializeField] private ShopConfig config;

        private int totalRemovalsThisRun = 0;
        private float discountMultiplier = 1f;
        private bool courierOwned = false;


        private HashSet<string> seenRelicIds = new HashSet<string>();
        private List<PotionDataAsset> potionAssetCache;

        public int TotalRemovals => totalRemovalsThisRun;

        public event System.Action OnShopConfigChanged;

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
                config = Resources.Load<ShopConfig>("ShopConfig");
                if (config == null)
                    GameLogger.LogWarning("[ShopDataService] ShopConfig not found, please create Resources/ShopConfig.asset");
            }
        }

        public void SetDiscountMultiplier(float multiplier)
        {
            discountMultiplier = Mathf.Max(0.1f, multiplier);
            OnShopConfigChanged?.Invoke();
        }

        public void SetCourierOwned(bool owned)
        {
            courierOwned = owned;
        }

        public void ResetForNewRun()
        {
            totalRemovalsThisRun = 0;
            seenRelicIds.Clear();
            potionAssetCache = null;
        }

        public List<ShopItem> GenerateShopContents()
        {
            List<ShopItem> items = new List<ShopItem>();

            items.AddRange(GenerateColoredCards());
            items.AddRange(GenerateColorlessCards());
            items.AddRange(GenerateRelics());
            items.AddRange(GeneratePotions());
            items.Add(GenerateCardRemoval());

            ApplySaleDiscounts(items);

            return items;
        }

        private List<ShopItem> GenerateColoredCards()
        {
            List<ShopItem> items = new List<ShopItem>();
            if (config == null) return items;

            var unlockService = FactionUnlockService.Instance;
            var poolConfig = Battle.RewardPoolManager.Config;

            int count = config.coloredCardCount.Random;
            int commonCount = Mathf.RoundToInt(count * 0.4f);
            int uncommonCount = Mathf.RoundToInt(count * 0.4f);
            int rareCount = count - commonCount - uncommonCount;

            var commonCards = poolConfig.GetColoredCardsByRarity(CardRarity.Common)
                .Where(a => IsCardAvailable(a, unlockService))
                .ToList();
            var uncommonCards = poolConfig.GetColoredCardsByRarity(CardRarity.Uncommon)
                .Where(a => IsCardAvailable(a, unlockService))
                .ToList();
            var rareCards = poolConfig.GetColoredCardsByRarity(CardRarity.Rare)
                .Where(a => IsCardAvailable(a, unlockService))
                .ToList();

            for (int i = 0; i < commonCount && commonCards.Count > 0; i++)
            {
                int idx = Random.Range(0, commonCards.Count);
                var asset = commonCards[idx];
                commonCards.RemoveAt(idx);
                var card = CreateCardFromAsset(asset);
                if (card != null)
                    items.Add(CreateShopItem(card, ShopItemType.ColoredCard));
            }

            for (int i = 0; i < uncommonCount && uncommonCards.Count > 0; i++)
            {
                int idx = Random.Range(0, uncommonCards.Count);
                var asset = uncommonCards[idx];
                uncommonCards.RemoveAt(idx);
                var card = CreateCardFromAsset(asset);
                if (card != null)
                    items.Add(CreateShopItem(card, ShopItemType.ColoredCard));
            }

            for (int i = 0; i < rareCount && rareCards.Count > 0; i++)
            {
                int idx = Random.Range(0, rareCards.Count);
                var asset = rareCards[idx];
                rareCards.RemoveAt(idx);
                var card = CreateCardFromAsset(asset);
                if (card != null)
                    items.Add(CreateShopItem(card, ShopItemType.ColoredCard));
            }

            return items;
        }

        private bool IsCardAvailable(CardDataAsset asset, FactionUnlockService unlockService)
        {
            if (asset == null) return false;
            if (asset.faction == CardFaction.None) return true;
            if (!asset.isFactionLocked) return true;
            if (unlockService == null) return true;
            return unlockService.IsFactionUnlocked(asset.faction);
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

        private ShopItem CreateShopItem(Card card, ShopItemType type)
        {
            if (card == null) return null;

            ShopItem item = new ShopItem
            {
                type = type,
                item = card,
                basePrice = config.GetCardPrice(card.rarity, type == ShopItemType.ColorlessCard),
                finalPrice = 0
            };
            item.finalPrice = ApplyDiscount(item.basePrice);
            return item;
        }

        private List<ShopItem> GenerateColorlessCards()
        {
            List<ShopItem> items = new List<ShopItem>();
            if (config == null) return items;

            var unlockService = FactionUnlockService.Instance;
            var poolConfig = Battle.RewardPoolManager.Config;

            var colorlessUncommon = poolConfig.GetColorlessCardsByRarity(CardRarity.Uncommon)
                .Where(a => IsCardAvailable(a, unlockService))
                .ToList();
            var colorlessRare = poolConfig.GetColorlessCardsByRarity(CardRarity.Rare)
                .Where(a => IsCardAvailable(a, unlockService))
                .ToList();

            if (colorlessUncommon.Count > 0)
            {
                var asset = colorlessUncommon[Random.Range(0, colorlessUncommon.Count)];
                var card = CreateCardFromAsset(asset);
                if (card != null)
                    items.Add(CreateShopItem(card, ShopItemType.ColorlessCard));
            }

            if (colorlessRare.Count > 0)
            {
                var asset = colorlessRare[Random.Range(0, colorlessRare.Count)];
                var card = CreateCardFromAsset(asset);
                if (card != null)
                    items.Add(CreateShopItem(card, ShopItemType.ColorlessCard));
            }

            return items;
        }

        private List<ShopItem> GenerateRelics()
        {
            List<ShopItem> items = new List<ShopItem>();
            if (config == null) return items;

            int count = config.relicCount.Random;

            var relicManager = RelicManager.Instance;
            if (relicManager == null) return items;


            var nonShopAssets = relicManager.LoadNonShopRelicAssets()
                .Where(a => !relicManager.HasRelic(a.relicId) && !seenRelicIds.Contains(a.relicId))
                .ToList();
            var shopAssets = relicManager.LoadShopRelicAssets()
                .Where(a => !relicManager.HasRelic(a.relicId) && !seenRelicIds.Contains(a.relicId))
                .ToList();


            if (nonShopAssets.Count == 0 && shopAssets.Count == 0)
            {
                GameLogger.Log("[ShopDataService] 遗物池已耗尽，重置已见遗物列表");
                seenRelicIds.Clear();
                nonShopAssets = relicManager.LoadNonShopRelicAssets()
                    .Where(a => !relicManager.HasRelic(a.relicId))
                    .ToList();
                shopAssets = relicManager.LoadShopRelicAssets()
                    .Where(a => !relicManager.HasRelic(a.relicId))
                    .ToList();
            }

            int nonShopCount = Mathf.Min(count - 1, nonShopAssets.Count);
            int shopCount = shopAssets.Count > 0 ? 1 : 0;

            for (int i = 0; i < nonShopCount && nonShopAssets.Count > 0; i++)
            {
                int index = Random.Range(0, nonShopAssets.Count);
                var chosen = nonShopAssets[index];
                nonShopAssets.RemoveAt(index);
                seenRelicIds.Add(chosen.relicId);

                Relic relic = relicManager.CreateRelicFromAsset(chosen);
                if (relic == null) continue;

                int price = relic.price > 0 ? relic.price : config.GetRelicPrice(relic.rarity);

                ShopItem item = new ShopItem
                {
                    type = ShopItemType.Relic,
                    item = relic,
                    basePrice = price,
                    finalPrice = 0,
                    isShopRelic = false
                };
                item.finalPrice = ApplyDiscount(item.basePrice);
                items.Add(item);
            }

            if (config.guaranteeShopRelic && shopCount > 0 && shopAssets.Count > 0)
            {
                int index = Random.Range(0, shopAssets.Count);
                var chosen = shopAssets[index];
                seenRelicIds.Add(chosen.relicId);

                Relic relic = relicManager.CreateRelicFromAsset(chosen);
                if (relic != null)
                {
                    int price = relic.price > 0 ? relic.price : config.GetRelicPrice(relic.rarity);

                    ShopItem item = new ShopItem
                    {
                        type = ShopItemType.Relic,
                        item = relic,
                        basePrice = price,
                        finalPrice = 0,
                        isShopRelic = true
                    };
                    item.finalPrice = ApplyDiscount(item.basePrice);
                    items.Add(item);
                }
            }

            return items;
        }

        private List<ShopItem> GeneratePotions()
        {
            List<ShopItem> items = new List<ShopItem>();
            if (config == null) return items;

            int count = config.potionCount.Random;

            var potionPool = LoadPotionAssets();
            if (potionPool == null || potionPool.Count == 0) return items;

            List<PotionDataAsset> available = new List<PotionDataAsset>(potionPool);

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int index = Random.Range(0, available.Count);
                var asset = available[index];
                available.RemoveAt(index);

                Potion potion = CreatePotionFromAsset(asset);
                if (potion == null) continue;

                ShopItem item = new ShopItem
                {
                    type = ShopItemType.Potion,
                    item = potion,
                    basePrice = config.GetPotionPrice(potion.rarity),
                    finalPrice = 0
                };
                item.finalPrice = ApplyDiscount(item.basePrice);
                items.Add(item);
            }

            return items;
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
                        GameLogger.LogWarning($"[ShopDataService] 药水效果加载失败：{effectId}");
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

        private ShopItem GenerateCardRemoval()
        {
            if (config == null) return null;

            int cost = config.GetRemovalCost(totalRemovalsThisRun);

            ShopItem item = new ShopItem
            {
                type = ShopItemType.CardRemoval,
                item = null,
                basePrice = cost,
                finalPrice = cost
            };

            return item;
        }

        private void ApplySaleDiscounts(List<ShopItem> items)
        {
            if (config == null) return;

            var cardItems = items
                .Where(i => i.type == ShopItemType.ColoredCard || i.type == ShopItemType.ColorlessCard)
                .Where(i => !i.isSold)
                .ToList();

            int saleCount = Mathf.Min(config.saleCardCount, cardItems.Count);

            for (int i = 0; i < saleCount && cardItems.Count > 0; i++)
            {
                int index = Random.Range(0, cardItems.Count);
                var item = cardItems[index];
                item.isOnSale = true;
                item.finalPrice = Mathf.RoundToInt(item.basePrice * config.saleDiscount);
                cardItems.RemoveAt(index);
            }
        }

        private int ApplyDiscount(int basePrice)
        {
            return Mathf.RoundToInt(basePrice * discountMultiplier);
        }

        public void OnRemovalPurchased()
        {
            totalRemovalsThisRun++;
        }

        public int GetCurrentRemovalCost()
        {
            if (config == null) return 75;
            return config.GetRemovalCost(totalRemovalsThisRun);
        }

        public void OnItemPurchased(ShopItem item)
        {
            if (item.type == ShopItemType.CardRemoval)
            {
                OnRemovalPurchased();
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}


