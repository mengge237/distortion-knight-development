using System;
using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "MutationChess/Shop Config")]
    public class ShopConfig : ScriptableObject
    {
        [Header("卡牌商品数量")]
        public IntRange coloredCardCount = new IntRange(5, 5);
        public IntRange colorlessCardCount = new IntRange(2, 2);

        [Header("卡牌价格")]
        public IntRange commonCardPrice = new IntRange(40, 65);
        public IntRange uncommonCardPrice = new IntRange(60, 95);
        public IntRange rareCardPrice = new IntRange(120, 180);
        public IntRange legendaryCardPrice = new IntRange(220, 320);
        public IntRange colorlessCommonPrice = new IntRange(70, 100);
        public IntRange colorlessUncommonPrice = new IntRange(100, 140);
        public IntRange colorlessRarePrice = new IntRange(200, 280);
        public IntRange colorlessLegendaryPrice = new IntRange(320, 450);

        [Header("卡牌类型比例")]
        [Range(0f, 1f)] public float attackCardRatio = 0.4f;
        [Range(0f, 1f)] public float skillCardRatio = 0.4f;
        [Range(0f, 1f)] public float powerCardRatio = 0.2f;

        [Header("遗物商品数量")]
        public IntRange relicCount = new IntRange(3, 3);
        public bool guaranteeShopRelic = true;

        [Header("遗物价格")]
        public IntRange commonRelicPrice = new IntRange(130, 180);
        public IntRange uncommonRelicPrice = new IntRange(220, 290);
        public IntRange rareRelicPrice = new IntRange(260, 350);
        public IntRange legendaryRelicPrice = new IntRange(280, 380);
        public IntRange shopRelicPrice = new IntRange(130, 180);
        public IntRange factionUnlockRelicPrice = new IntRange(300, 420);

        [Header("药水商品数量")]
        public IntRange potionCount = new IntRange(3, 3);

        [Header("药水价格")]
        public IntRange commonPotionPrice = new IntRange(40, 65);
        public IntRange uncommonPotionPrice = new IntRange(65, 90);
        public IntRange rarePotionPrice = new IntRange(85, 120);

        [Header("卡牌移除服务")]
        public int baseRemovalCost = 70;
        public int removalCostIncrease = 25;
        public int maxRemovalCost = 200;

        [Header("打折")]
        [Range(0f, 1f)] public float saleDiscount = 0.5f;
        [Range(0, 100)] public int saleCardCount = 1;

        [Header("难度价格倍率")]
        [Range(0f, 0.5f)] public float ascensionPriceMultiplier = 0.1f;

        public int GetRemovalCost(int timesRemoved)
        {
            int cost = baseRemovalCost + timesRemoved * removalCostIncrease;
            return Math.Min(cost, maxRemovalCost);
        }

        public int GetCardPrice(CardRarity rarity, bool isColorless = false)
        {
            IntRange range;
            if (isColorless)
            {
                switch (rarity)
                {
                    case CardRarity.Common: range = colorlessCommonPrice; break;
                    case CardRarity.Uncommon: range = colorlessUncommonPrice; break;
                    case CardRarity.Rare: range = colorlessRarePrice; break;
                    case CardRarity.Legendary: range = colorlessLegendaryPrice; break;
                    case CardRarity.Colorless: range = colorlessCommonPrice; break;
                    case CardRarity.Cursed: return 0;
                    default: range = colorlessCommonPrice; break;
                }
            }
            else
            {
                switch (rarity)
                {
                    case CardRarity.Common: range = commonCardPrice; break;
                    case CardRarity.Uncommon: range = uncommonCardPrice; break;
                    case CardRarity.Rare: range = rareCardPrice; break;
                    case CardRarity.Legendary: range = legendaryCardPrice; break;
                    case CardRarity.Colorless: range = commonCardPrice; break;
                    case CardRarity.Cursed: return 0;
                    default: range = commonCardPrice; break;
                }
            }
            return UnityEngine.Random.Range(range.min, range.max + 1);
        }

        public int GetRelicPrice(RelicRarity rarity)
        {
            IntRange range;
            switch (rarity)
            {
                case RelicRarity.Common: range = commonRelicPrice; break;
                case RelicRarity.Starting: range = uncommonRelicPrice; break;
                case RelicRarity.Rare: range = rareRelicPrice; break;
                case RelicRarity.Legendary: range = legendaryRelicPrice; break;
                case RelicRarity.Special: range = factionUnlockRelicPrice; break;
                default: range = commonRelicPrice; break;
            }
            return UnityEngine.Random.Range(range.min, range.max + 1);
        }

        public int GetPotionPrice(PotionRarity rarity)
        {
            IntRange range;
            switch (rarity)
            {
                case PotionRarity.Common: range = commonPotionPrice; break;
                case PotionRarity.Uncommon: range = uncommonPotionPrice; break;
                case PotionRarity.Rare: range = rarePotionPrice; break;
                default: range = commonPotionPrice; break;
            }
            return UnityEngine.Random.Range(range.min, range.max + 1);
        }
    }

    [Serializable]
    public struct IntRange
    {
        public int min;
        public int max;

        public IntRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public int Random => UnityEngine.Random.Range(min, max + 1);
    }
}