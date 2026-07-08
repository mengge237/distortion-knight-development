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
                default: return Color.white;
            }
        }

        public static Rect GetCropRect(CardName cardName)
        {
            return new Rect(0f, 0f, 1f, 1f);
        }
    }
}