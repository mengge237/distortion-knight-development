using System;
using System.Collections.Generic;
using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    public enum PotionRarity
    {
        Common,
        Uncommon,
        Rare
    }

    [Serializable]
    public class Potion
    {
        public string potionId;
        public string potionName;
        public PotionRarity rarity;
        public string description;
        public Sprite icon;
        public int price;

        [NonSerialized]
        public List<CardEffect> effects = new List<CardEffect>();

        public Potion() { }

        public Potion(string id, string name, PotionRarity rarity, string description, int price)
        {
            this.potionId = id;
            this.potionName = name;
            this.rarity = rarity;
            this.description = description;
            this.price = price;
        }

        public void ExecuteEffects(CombatContext context)
        {
            if (effects == null) return;

            foreach (var effect in effects)
            {
                if (effect == null) continue;
                effect.Execute(context);
            }
        }

        public string GetRarityName()
        {
            switch (rarity)
            {
                case PotionRarity.Common: return "1";
                case PotionRarity.Uncommon: return "2";
                case PotionRarity.Rare: return "3";
                default: return "";
            }
        }
    }
}

