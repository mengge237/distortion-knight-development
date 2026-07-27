using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    public enum CardType
    {
        Attack,
        Defense,
        Skill,
        Power
    }

    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Mythic
    }

    public enum CardFaction
    {
        None,
        Slime,
        Reluctant,
    }

    [Serializable]
    public class Card
    {
        public string cardId;
        public string cardName;
        public int cost;
        public int damage;
        public int block;
        public int magicNumber;
        public CardType cardType;
        public CardRarity rarity;
        public CardFaction faction;
        public Sprite cardArt;
        public string description;
        public string upgradeDescription;
        public bool isUpgraded = false;

        [NonSerialized]
        public List<CardEffect> effects = new List<CardEffect>();

        public Card()
        {
            cardId = Guid.NewGuid().ToString();
        }

        public Card(string name, int value, CardType type, CardRarity rarity, int cost = 1) : this()
        {
            cardName = name;
            this.cost = cost;
            this.cardType = type;
            this.rarity = rarity;
            this.magicNumber = 0;

            if (type == CardType.Attack)
                damage = value;
            else if (type == CardType.Defense)
                block = value;

            GenerateDescription();
        }

        public Card(string name, int value, CardType type, CardRarity rarity, int cost, int magicNumber)
            : this(name, value, type, rarity, cost)
        {
            this.magicNumber = magicNumber;
            GenerateDescription();
        }

        public bool HasKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(cardName)) return false;
            return cardName.Contains(keyword);
        }

        public void Upgrade()
        {
            if (isUpgraded) return;

            if (cardType == CardType.Attack)
                damage += 3;
            else if (cardType == CardType.Defense)
                block += 3;
            else if (cardType == CardType.Skill && magicNumber > 0)
                magicNumber += 1;
            else if (cardType == CardType.Power)
                cost = Mathf.Max(0, cost - 1);

            isUpgraded = true;
            GenerateDescription();
        }

        public void GenerateDescription()
        {
            string desc = "";
            switch (cardType)
            {
                case CardType.Attack:
                    desc = $"造成 {damage} 点伤害";
                    if (magicNumber > 0) desc += $" 施加 {magicNumber} 层易伤";
                    break;
                case CardType.Defense:
                    desc = $"获得 {block} 点格挡";
                    if (magicNumber > 0) desc += $" 抽 {magicNumber} 张牌";
                    break;
                case CardType.Skill:
                    desc = magicNumber > 0 ? $"抽 {magicNumber} 张牌" : "效果未定义";
                    break;
                case CardType.Power:
                    desc = magicNumber > 0 ? $"持续 {magicNumber} 回合" : "效果未定义";
                    break;
            }

            if (effects.Count > 0)
            {
                foreach (var effect in effects)
                {
                    if (effect != null && !string.IsNullOrEmpty(effect.effectDescription))
                        desc += $"\n{effect.effectDescription}";
                }
            }

            if (faction == CardFaction.Slime)
                desc += $"\n<color=#00FF88>粘液：打出时触发相邻卡牌效果</color>";

            if (faction == CardFaction.Reluctant)
                desc += $"\n<color=#CC66FF>不舍：从牌库中抽一张不舍卡牌</color>";

            if (isUpgraded)
                desc += " (已升级)";

            description = desc;
        }

        public string GetDescription() => description;

        public Color GetRarityColor() => CardVisualConfig.GetRarityColor(rarity);

        public string GetRarityName()
        {
            switch (rarity)
            {
                case CardRarity.Common: return "普通";
                case CardRarity.Uncommon: return "罕见";
                case CardRarity.Rare: return "稀有";
                case CardRarity.Mythic: return "神话";
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

        public void ExecuteEffects(CombatContext context)
        {
            if (effects.Count == 0)
            {
                Debug.LogWarning($"卡牌 {cardName} 没有效果列表");
                return;
            }

            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    effect.Execute(context);
                }
                else
                {
                    Debug.LogWarning("发现空效果引用");
                }
            }
        }
    }
}
