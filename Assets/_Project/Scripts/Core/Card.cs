using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    // ==================== 卡牌类型枚举 ====================
    public enum CardType
    {
        Attack,
        Defense,
        Skill,
        Power
    }

    // ==================== 卡牌稀有度枚举 ====================
    public enum CardRarity
    {
        Common,     // 普通 - 灰色
        Uncommon,   // 罕见 - 蓝色
        Rare,       // 稀有 - 金色
        Mythic      // 神话 - 紫色
    }

    // ==================== 卡牌数据类 ====================
    [Serializable]
    public class Card
    {
        // ===== 核心数据 =====
        public string cardId;
        public string cardName;
        public int cost;
        public int damage;
        public int block;
        public int magicNumber;
        public CardType cardType;
        public CardRarity rarity;
        public Sprite cardArt;
        public string description;
        public string upgradeDescription;
        public bool isUpgraded = false;

        // ===== 效果列表 =====
        [NonSerialized]
        public List<CardEffect> effects = new List<CardEffect>();

        // ===== 构造函数 =====

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

        // ===== 卡牌升级 =====

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

        // ===== 描述生成 =====

        public void GenerateDescription()
        {
            string desc = "";
            switch (cardType)
            {
                case CardType.Attack:
                    desc = $"造成 {damage} 点伤害";
                    if (magicNumber > 0) desc += $"，抽 {magicNumber} 张牌";
                    break;
                case CardType.Defense:
                    desc = $"获得 {block} 点格挡";
                    if (magicNumber > 0) desc += $"，抽 {magicNumber} 张牌";
                    break;
                case CardType.Skill:
                    desc = magicNumber > 0 ? $"抽 {magicNumber} 张牌" : "效果未知";
                    break;
                case CardType.Power:
                    desc = magicNumber > 0 ? $"获得 {magicNumber} 层能力" : "获得能力";
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

            if (isUpgraded)
                desc += " (升级)";

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
                case CardRarity.Mythic: return "难寻";
                default: return "未知";
            }
        }

        // ===== 执行效果 =====

        public void ExecuteEffects(CombatContext context)
        {

            if (effects.Count == 0)
            {
                Debug.LogWarning($"卡牌 {cardName} 没有效果！请检查 CardData.CreateCard 是否正确加载了效果。");
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
                    Debug.LogWarning("卡牌效果为空！");
                }
            }
        }
    }
}