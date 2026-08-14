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
        Power,
        Curse
    }

    public enum CardRarity
    {
        Common,     // 1星 普通
        Uncommon,   // 2星 罕见
        Rare,       // 3星 稀有
        Legendary,  // 4星 传说
        Colorless,  // 5星 无色
        Cursed      // 诅咒
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

        [Header("卡牌标签")]
        [Tooltip("卡牌标签列表，用于标签联动与效果触发")]
        public List<CardTag> tags = new List<CardTag>();

        [Header("鲜血转换")]
        [Tooltip("鲜血换能量比率（3=3滴血换1点能量，0=使用默认值）")]
        [Min(0)]
        public int bloodPerEnergy = 0;

        [Header("格挡转换")]
        [Tooltip("格挡换能量比率（5=5点格挡换1点能量，0=使用默认值）")]
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
        /// 检查卡牌是否拥有指定标签
        /// </summary>
        public bool HasTag(CardTag tag)
        {
            if (tags == null || tags.Count == 0) return false;
            return tags.Contains(tag);
        }

        /// <summary>
        /// 添加卡牌标签
        /// </summary>
        public void AddTag(CardTag tag)
        {
            if (tags == null) tags = new List<CardTag>();
            if (!tags.Contains(tag))
                tags.Add(tag);
        }

        /// <summary>
        /// 是否使用鲜血转换
        /// </summary>
        public bool UsesBloodConversion => bloodPerEnergy > 0 || ConversionModifier.BloodConversionForAll;

        /// <summary>
        /// 是否使用格挡转换
        /// </summary>
        public bool UsesBlockConversion => blockPerEnergy > 0 || ConversionModifier.BlockConversionForAll;

        /// <summary>
        /// 是否使用特殊费用
        /// </summary>
        public bool UsesSpecialCost => UsesBloodConversion || UsesBlockConversion;

        /// <summary>
        /// 计算鲜血转换费用
        /// ConversionModifier 修正比率，effectiveCost 指定有效费用
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
        /// 计算格挡转换费用
        /// ConversionModifier 修正比率，effectiveCost 指定有效费用
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
        /// 计算混合转换费用（鲜血+格挡）
        /// effectiveCost 指定有效费用
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

            // 优先用鲜血转换
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

            // 再用格挡转换
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
        /// 检查是否能用混合转换支付
        /// effectiveCost 指定有效费用
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

            // 鲜血转换
            if (UsesBloodConversion && remainingShortfall > 0)
            {
                int maxBloodEnergy = Mathf.Min(remainingShortfall, currentHealth / effectiveBloodRate);
                remainingShortfall -= maxBloodEnergy;
            }

            // 格挡转换
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
                    if (damage > 0) desc = $"造成 {damage} 点伤害";
                    if (magicNumber > 0)
                    {
                        if (!string.IsNullOrEmpty(desc)) desc += "，";
                        desc += $"额外效果值 {magicNumber}";
                    }
                    break;
                case CardType.Defense:
                    if (block > 0) desc = $"获得 {block} 点格挡";
                    if (magicNumber > 0)
                    {
                        if (!string.IsNullOrEmpty(desc)) desc += "，";
                        desc += $"获得 {magicNumber} 点力量";
                    }
                    break;
                case CardType.Skill:
                    desc = magicNumber > 0 ? $"技能效果值：{magicNumber}" : "";
                    break;
                case CardType.Power:
                    desc = magicNumber > 0 ? $"能力效果值：{magicNumber}" : "";
                    break;
                case CardType.Curse:
                    desc = "";
                    break;
            }

            if (effects.Count > 0)
            {
                foreach (var effect in effects)
                {
                    if (effect == null) continue;
                    string effectDesc = effect.GetDescription(this);
                    if (!string.IsNullOrEmpty(effectDesc))
                    {
                        if (!string.IsNullOrEmpty(desc)) desc += "\n";
                        desc += effectDesc;
                    }
                }
            }

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

            if (UsesBloodConversion)
                desc += $"\n<color=#FF4444>可消耗 {bloodPerEnergy} 点生命代替 1 点费用</color>";
            if (UsesBlockConversion)
                desc += $"\n<color=#66CCFF>可消耗 {blockPerEnergy} 点格挡代替 1 点费用</color>";

            if (faction == CardFaction.Slime && !HasTag(CardTag.Slime))
                desc += $"\n<color=#00FF88>[粘液阵营]</color>";

            if (faction == CardFaction.Reluctant && !HasTag(CardTag.Reluctant))
                desc += $"\n<color=#CC66FF>[不舍阵营]</color>";

            if (exhaust)
                desc += "\n<color=#FF6644>消耗（使用后从牌组中移除）</color>";

            if (isUpgraded)
                desc += "（已强化）";

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
                case CardRarity.Legendary: return "传说";
                case CardRarity.Colorless: return "无色";
                case CardRarity.Cursed: return "诅咒";
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

        /// <summary>
        /// 本卡在手中时最后一次记录的手牌索引（出牌前由 HandManager 写入）。
        /// 供移除出牌后的相邻牌判定（史莱姆系列效果）使用。
        /// </summary>
        [System.NonSerialized]
        public int lastHandIndex = -1;

        public void ExecuteEffects(CombatContext context)
        {
            var bm = context.battleManager;
            bm?.AddLog($"玩家打出卡牌【{cardName}】");

            if (effects.Count == 0 && inherentEffects.Count == 0)
            {
                GameLogger.LogWarning($"{cardName} 未配置任何效果");
                return;
            }

            var effectManager = EffectManager.Instance;
            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(context.battleManager);
                ctx.combat = context;
                ctx.tag = this;
                effectManager.Trigger(EffectTrigger.CardPlayed, ctx);

                ConversionModifier.CardsPlayedThisBattle++;
                if (cardType == CardType.Attack)
                    ConversionModifier.AttackCardsPlayedThisBattle++;

                effectManager.Trigger(EffectTrigger.AfterCardsPlayed, ctx);
            }

            if (inherentEffects != null && inherentEffects.Count > 0)
            {
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

            foreach (var effect in effects)
            {
                if (effect != null)
                {
                    effect.Execute(context);
                }
                else
                {
                    GameLogger.LogWarning($"{cardName} 存在空的效果引用");
                }
            }
        }
    }
}
