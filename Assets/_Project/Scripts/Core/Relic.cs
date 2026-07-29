using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    public enum RelicRarity
    {
        Starting,
        Common,
        Rare,
        Legendary,
        Special
    }

    [Serializable]
    public class RelicEffectInstance
    {
        public CardEffect effect;
        public EffectTrigger trigger;
    }

    [Serializable]
    public class Relic
    {
        public string relicId;
        public string relicName;
        public RelicRarity rarity;
        public CardFaction faction;
        public string description;
        public Sprite icon;
        public int price;

        [NonSerialized]
        public List<RelicEffectInstance> relicEffects = new List<RelicEffectInstance>();

        public Relic() { }

        public Relic(string relicId, string relicName, RelicRarity rarity, CardFaction faction,
                      string description, int price)
        {
            this.relicId = relicId;
            this.relicName = relicName;
            this.rarity = rarity;
            this.faction = faction;
            this.description = description;
            this.price = price;
        }

        public string GetRarityName()
        {
            switch (rarity)
            {
                case RelicRarity.Starting: return "";
                case RelicRarity.Common: return "";
                case RelicRarity.Rare: return "";
                case RelicRarity.Legendary: return "";
                case RelicRarity.Special: return "";
                default: return "";
            }
        }

        public string GetFactionName()
        {
            switch (faction)
            {
                case CardFaction.None: return "";
                case CardFaction.Slime: return "";
                case CardFaction.Reluctant: return "";
                case CardFaction.Blood: return "";
                case CardFaction.Frost: return "";
                case CardFaction.Shadow: return "";
                case CardFaction.Corrupt: return "";
                default: return "";
            }
        }

        public bool HasFaction() => faction != CardFaction.None;
    }
}


