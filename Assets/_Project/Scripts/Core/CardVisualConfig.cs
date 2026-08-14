using UnityEngine;

namespace MutationChess.Core
{
    public static class CardVisualConfig
    {
        public static Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Common: return new Color(0.63f, 0.63f, 0.63f);
                case CardRarity.Uncommon: return new Color(0.31f, 0.76f, 0.97f);
                case CardRarity.Rare: return new Color(1f, 0.84f, 0.31f);
                case CardRarity.Legendary: return new Color(1f, 0.35f, 0.15f);
                case CardRarity.Colorless: return new Color(0.82f, 0.82f, 0.88f);
                case CardRarity.Cursed: return new Color(0.45f, 0.1f, 0.55f); // 诅咒：深紫黑
                default: return Color.white;
            }
        }

        public static Color GetFactionColor(CardFaction faction)
        {
            switch (faction)
            {
                case CardFaction.Slime: return new Color(0f, 1f, 0.53f);
                case CardFaction.Reluctant: return new Color(0.8f, 0.4f, 1f);
                case CardFaction.None:
                default: return new Color(0.5f, 0.5f, 0.5f);
            }
        }

        public static Rect GetCropRect(CardName cardName)
        {
            return new Rect(0f, 0f, 1f, 1f);
        }
    }
}