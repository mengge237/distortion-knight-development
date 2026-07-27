using System;
using UnityEngine;

namespace MutationChess.Core
{
    public enum RelicRarity
    {
        Special,    // 特殊遗物（条件获取）
        Common,     // 普通
        Rare,       // 稀有
        Legendary,  // 传说
        Mythic      // 神话
    }

    public enum RelicEffectType
    {
        None,                   // 无特殊效果
        BonusDamage,            // 攻击时额外造成伤害
        OncePerBattleAttackBoost, // 每场战斗限1次 +攻击力
        VictoryGoldPercent,     // 战斗胜利时获得金币百分比
        InstantKill,            // 直接击败敌人（使用后消失）
        HealPercentEachTurn,    // 每回合开始时回复最大生命值百分比
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
        public RelicEffectType effectType = RelicEffectType.None;
        public float effectValue;  // 效果数值（BonusDamage=额外伤害值, VictoryGoldPercent=百分比(0.01=1%), OncePerBattleAttackBoost=攻击力加成）

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
                case RelicRarity.Special: return "特殊";
                case RelicRarity.Common: return "普通";
                case RelicRarity.Rare: return "稀有";
                case RelicRarity.Legendary: return "传说";
                case RelicRarity.Mythic: return "神话";
                default: return "未知";
            }
        }

        public string GetFactionName()
        {
            switch (faction)
            {
                case CardFaction.None: return "";
                case CardFaction.Slime: return "粘液";
                case CardFaction.Reluctant: return "不舍";
                default: return "";
            }
        }

        public bool HasFaction() => faction != CardFaction.None;
    }
}
