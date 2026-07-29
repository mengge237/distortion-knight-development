﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    public enum CardType
    {
        Attack,
        Defense,
        Skill,
        Power,
        Curse
    }

    public enum CardRarity
    {
        Common,     // 1级 普通
        Uncommon,   // 2级 非凡
        Rare,       // 3级 稀有
        Legendary,  // 4级 传说
        Colorless,  // 无色：跨系列通用中立卡
        Cursed      // 诅咒：负面卡，无法主动获得
    }

    public enum CardFaction
    {
        None,
        Slime,
        Reluctant,
        Blood,
        Frost,
        Shadow,
        Corrupt
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
        public bool isUpgraded = false;
        public bool exhaust = false;

        [Header("字段系统")]
        [Tooltip("卡牌拥有的字段列表，字段不一定带固有效果，但大部分联动卡牌会有")]
        public List<CardTag> tags = new List<CardTag>();

        [Header("鲜血字段：血量转换率")]
        [Tooltip("每点能量需要消耗的血量（如3表示3血=1能量）。0表示不使用血量转换")]
        [Min(0)]
        public int bloodPerEnergy = 0;

        [Header("寒霜字段：格挡转换率")]
        [Tooltip("每点能量需要消耗的格挡值（如5表示5格挡=1能量）。0表示不使用格挡转换")]
        [Min(0)]
        public int blockPerEnergy = 0;

        [NonSerialized]
        public List<CardEffect> effects = new List<CardEffect>();

        [NonSerialized]
        public List<InherentEffect> inherentEffects = new List<InherentEffect>();

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

        /// <summary>
        /// 判断卡牌是否拥有指定字段。
        /// </summary>
        public bool HasTag(CardTag tag)
        {
            if (tags == null || tags.Count == 0) return false;
            return tags.Contains(tag);
        }

        /// <summary>
        /// 添加字段。
        /// </summary>
        public void AddTag(CardTag tag)
        {
            if (tags == null) tags = new List<CardTag>();
            if (!tags.Contains(tag))
                tags.Add(tag);
        }

        /// <summary>
        /// 是否使用血量转换（鲜血字段，或血怒药水扩散）。
        /// </summary>
        public bool UsesBloodConversion => bloodPerEnergy > 0 || ConversionModifier.BloodConversionForAll;

        /// <summary>
        /// 是否使用格挡转换（寒霜字段，或冰封药水扩散）。
        /// </summary>
        public bool UsesBlockConversion => blockPerEnergy > 0 || ConversionModifier.BlockConversionForAll;

        /// <summary>
        /// 是否使用特殊费用转换（血量或格挡）。
        /// </summary>
        public bool UsesSpecialCost => UsesBloodConversion || UsesBlockConversion;

        /// <summary>
        /// 计算血量转换：先用能量抵扣，剩余需求按转换率折算成血量。
        /// 返回需要消耗的血量（0表示不需要）。
        /// 应用 ConversionModifier 修饰（遗物永久降低 / 药水临时覆盖）。
        /// effectiveCost 传入时用于费用减免后的实际费用。
        /// </summary>
        public int CalculateBloodCost(int currentEnergy, int? effectiveCost = null)
        {
            if (!UsesBloodConversion) return 0;
            int actualCost = effectiveCost ?? cost;
            int energyNeeded = Mathf.Max(0, actualCost - currentEnergy);
            int baseRate = bloodPerEnergy > 0 ? bloodPerEnergy : 3;
            int effectiveRate = ConversionModifier.GetEffectiveBloodRate(baseRate);
            return energyNeeded * effectiveRate;
        }

        /// <summary>
        /// 计算格挡转换：先用能量抵扣，剩余需求按转换率折算成格挡。
        /// 返回需要消耗的格挡值（0表示不需要）。
        /// 应用 ConversionModifier 修饰（遗物永久降低 / 药水临时覆盖）。
        /// effectiveCost 传入时用于费用减免后的实际费用。
        /// </summary>
        public int CalculateBlockCost(int currentEnergy, int? effectiveCost = null)
        {
            if (!UsesBlockConversion) return 0;
            int actualCost = effectiveCost ?? cost;
            int energyNeeded = Mathf.Max(0, actualCost - currentEnergy);
            int baseRate = blockPerEnergy > 0 ? blockPerEnergy : 5;
            int effectiveRate = ConversionModifier.GetEffectiveBlockRate(baseRate);
            return energyNeeded * effectiveRate;
        }

        /// <summary>
        /// 计算混合转换费用。
        /// 支付顺序：先算转换消耗 → 扣能量 → 扣血量 → 扣格挡。
        /// 返回 (bloodCost, blockCost) 元组。
        /// effectiveCost 传入时用于费用减免后的实际费用。
        /// </summary>
        public (int bloodCost, int blockCost) CalculateMixedConversionCosts(
            int currentEnergy, int currentHealth, int currentBlock, int? effectiveCost = null)
        {
            int actualCost = effectiveCost ?? cost;
            int energyShortfall = Mathf.Max(0, actualCost - currentEnergy);
            if (energyShortfall == 0) return (0, 0);

            int bloodCost = 0;
            int blockCost = 0;

            int baseBloodRate = bloodPerEnergy > 0 ? bloodPerEnergy : 3;
            int effectiveBloodRate = ConversionModifier.GetEffectiveBloodRate(baseBloodRate);
            int baseBlockRate = blockPerEnergy > 0 ? blockPerEnergy : 5;
            int effectiveBlockRate = ConversionModifier.GetEffectiveBlockRate(baseBlockRate);

            int remainingShortfall = energyShortfall;

            // 先扣血量（鲜血优先，符合项目规则：扣能量→扣血→扣格挡）
            if (UsesBloodConversion && remainingShortfall > 0)
            {
                int maxBloodEnergy = remainingShortfall;
                int bloodNeeded = maxBloodEnergy * effectiveBloodRate;
                if (bloodNeeded > currentHealth)
                {
                    maxBloodEnergy = Mathf.Max(0, currentHealth / effectiveBloodRate);
                    bloodNeeded = maxBloodEnergy * effectiveBloodRate;
                }
                bloodCost = bloodNeeded;
                remainingShortfall -= maxBloodEnergy;
            }

            // 再扣格挡（寒霜补足剩余缺口）
            if (UsesBlockConversion && remainingShortfall > 0)
            {
                int blockNeeded = remainingShortfall * effectiveBlockRate;
                if (blockNeeded > currentBlock)
                {
                    return (bloodCost, 0);
                }
                blockCost = blockNeeded;
            }

            return (bloodCost, blockCost);
        }

        /// <summary>
        /// 检查卡牌是否能支付费用（混合转换模式）。
        /// 顺序：能量 → 血量 → 格挡。
        /// effectiveCost 传入时用于费用减免后的实际费用。
        /// </summary>
        public bool CanPayWithMixedConversion(
            int currentEnergy, int currentHealth, int currentBlock, int? effectiveCost = null)
        {
            int actualCost = effectiveCost ?? cost;
            int energyShortfall = Mathf.Max(0, actualCost - currentEnergy);
            if (energyShortfall == 0) return true;

            int baseBloodRate = bloodPerEnergy > 0 ? bloodPerEnergy : 3;
            int effectiveBloodRate = ConversionModifier.GetEffectiveBloodRate(baseBloodRate);
            int baseBlockRate = blockPerEnergy > 0 ? blockPerEnergy : 5;
            int effectiveBlockRate = ConversionModifier.GetEffectiveBlockRate(baseBlockRate);

            int remainingShortfall = energyShortfall;

            // 先用血量（鲜血优先）
            if (UsesBloodConversion && remainingShortfall > 0)
            {
                int maxBloodEnergy = Mathf.Min(remainingShortfall, currentHealth / effectiveBloodRate);
                remainingShortfall -= maxBloodEnergy;
            }

            // 再用格挡（寒霜补足）
            if (UsesBlockConversion && remainingShortfall > 0)
            {
                int maxBlockEnergy = currentBlock / effectiveBlockRate;
                remainingShortfall -= maxBlockEnergy;
            }

            return remainingShortfall <= 0;
        }

        public bool HasKeyword(string keyword)
        {
            if (string.IsNullOrEmpty(cardName)) return false;
            return cardName.Contains(keyword);
        }

        public void Upgrade()
        {
            if (isUpgraded) return;
            if (cardType == CardType.Curse) return;

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
                case CardType.Curse:
                    desc = "诅咒卡：无法打出";
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

            // 显示字段标签
            if (tags != null && tags.Count > 0)
            {
                foreach (var tag in tags)
                {
                    string tagColor;
                    string tagName;
                    switch (tag)
                    {
                        case CardTag.Slime:
                            tagColor = "#00FF88"; tagName = "粘液"; break;
                        case CardTag.Reluctant:
                            tagColor = "#CC66FF"; tagName = "不舍"; break;
                        case CardTag.Blood:
                            tagColor = "#FF4444"; tagName = "鲜血"; break;
                        case CardTag.Frost:
                            tagColor = "#66CCFF"; tagName = "寒霜"; break;
                        case CardTag.Corrupt:
                            tagColor = "#9933CC"; tagName = "腐化"; break;
                        case CardTag.Shadow:
                            tagColor = "#666666"; tagName = "暗影"; break;
                        case CardTag.Curse:
                            tagColor = "#731A8B"; tagName = "诅咒"; break;
                        default: continue;
                    }
                    desc += $"\n<color={tagColor}>[{tagName}]</color>";
                }
            }

            // 显示特殊费用转换
            if (UsesBloodConversion)
                desc += $"\n<color=#FF4444>鲜血：每 {bloodPerEnergy} 血量代替 1 能量</color>";
            if (UsesBlockConversion)
                desc += $"\n<color=#66CCFF>寒霜：每 {blockPerEnergy} 格挡代替 1 能量</color>";

            // 旧的 faction 显示（保留兼容）
            if (faction == CardFaction.Slime && !HasTag(CardTag.Slime))
                desc += $"\n<color=#00FF88>粘液：打出时触发相邻卡牌效果</color>";

            if (faction == CardFaction.Reluctant && !HasTag(CardTag.Reluctant))
                desc += $"\n<color=#CC66FF>不舍：从牌库中抽一张不舍卡牌</color>";

            if (exhaust)
                desc += "\n<color=#FF6644>消耗</color>";

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
                case CardRarity.Common: return "1级";
                case CardRarity.Uncommon: return "2级";
                case CardRarity.Rare: return "3级";
                case CardRarity.Legendary: return "4级";
                case CardRarity.Colorless: return "无色";
                case CardRarity.Cursed: return "诅咒";
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
                case CardFaction.Blood: return "鲜血";
                case CardFaction.Frost: return "寒霜";
                case CardFaction.Shadow: return "暗影";
                case CardFaction.Corrupt: return "腐化";
                default: return "";
            }
        }

        public bool HasFaction() => faction != CardFaction.None;

        public void ExecuteEffects(CombatContext context)
        {
            if (effects.Count == 0 && inherentEffects.Count == 0)
            {
                GameLogger.LogWarning($"卡牌 {cardName} 没有效果列表");
                return;
            }

            var effectManager = EffectManager.Instance;
            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(context.battleManager);
                ctx.combat = context;
                effectManager.Trigger(EffectTrigger.CardPlayed, ctx);
            }

            // 先执行固有效果（字段的固有效果，如粘液触发相邻卡牌、不舍抽牌等）
            if (inherentEffects != null && inherentEffects.Count > 0)
            {
                // 联动共鸣：字段固有效果触发两次
                int triggerCount = ConversionModifier.TagEffectDoubleTrigger ? 2 : 1;

                foreach (var inherent in inherentEffects)
                {
                    if (inherent != null && inherent.ShouldApply(this))
                    {
                        for (int i = 0; i < triggerCount; i++)
                        {
                            GameLogger.Log($"[Card] {cardName} 触发固有效果: {inherent.GetType().Name}" + (triggerCount > 1 ? $" (第{i + 1}次)" : ""));
                            inherent.ApplyInherent(context);
                        }
                    }
                }
            }

            // 再执行普通效果（卡牌自身效果）
            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    effect.Execute(context);
                }
                else
                {
                    GameLogger.LogWarning("发现空效果引用");
                }
            }
        }
    }
}