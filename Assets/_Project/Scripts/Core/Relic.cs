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
                case RelicRarity.Starting: return "初始";
                case RelicRarity.Common: return "普通";
                case RelicRarity.Rare: return "稀有";
                case RelicRarity.Legendary: return "传说";
                case RelicRarity.Special: return "特殊";
                default: return "未知";
            }
        }

        public string GetFactionName()
        {
            switch (faction)
            {
                case CardFaction.None: return "无阵营";
                case CardFaction.Slime: return "粘液";
                case CardFaction.Reluctant: return "不舍";
                case CardFaction.Blood: return "鲜血";
                case CardFaction.Frost: return "寒霜";
                case CardFaction.Shadow: return "暗影";
                case CardFaction.Corrupt: return "腐化";
                default: return "未知";
            }
        }

        public bool HasFaction() => faction != CardFaction.None;
    }
}


