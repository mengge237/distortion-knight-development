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
        Colorless,  //
        Cursed      //
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

        [Header("")]
        [Tooltip("")]
        public List<CardTag> tags = new List<CardTag>();

        [Header("")]
        [Tooltip("33?=10")]
        [Min(0)]
        public int bloodPerEnergy = 0;

        [Header("")]
        [Tooltip("55??=10")]
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
        ///
        /// </summary>
        public bool HasTag(CardTag tag)
        {
            if (tags == null || tags.Count == 0) return false;
            return tags.Contains(tag);
        }

        /// <summary>
        ///
        /// </summary>
        public void AddTag(CardTag tag)
        {
            if (tags == null) tags = new List<CardTag>();
            if (!tags.Contains(tag))
                tags.Add(tag);
        }

        /// <summary>
        ///
        /// </summary>
        public bool UsesBloodConversion => bloodPerEnergy > 0 || ConversionModifier.BloodConversionForAll;

        /// <summary>
        ///
        /// </summary>
        public bool UsesBlockConversion => blockPerEnergy > 0 || ConversionModifier.BlockConversionForAll;

        /// <summary>
        ///
        /// </summary>
        public bool UsesSpecialCost => UsesBloodConversion || UsesBlockConversion;

        /// <summary>
        ///
        ///
        /// ConversionModifier / 
        /// effectiveCost 
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
        ///
        ///
        /// ConversionModifier / 
        /// effectiveCost 
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
        ///
        ///
        /// 
        /// effectiveCost 
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

            //
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

            //
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
        ///
        ///
        /// effectiveCost 
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

            //
            if (UsesBloodConversion && remainingShortfall > 0)
            {
                int maxBloodEnergy = Mathf.Min(remainingShortfall, currentHealth / effectiveBloodRate);
                remainingShortfall -= maxBloodEnergy;
            }

            //
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
                    desc = $"{damage} ";
                    if (magicNumber > 0) desc += $" {magicNumber} ";
                    break;
                case CardType.Defense:
                    desc = $"Block {block}";
                    if (magicNumber > 0) desc += $" + Gain {magicNumber} Strength";
                    break;
                case CardType.Skill:
                    desc = magicNumber > 0 ? $"Skill {magicNumber} " : "";
                    break;
                case CardType.Power:
                    desc = magicNumber > 0 ? $"{magicNumber} " : "";
                    break;
                case CardType.Curse:
                    desc = "";
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

            //
            if (tags != null && tags.Count > 0)
            {
                foreach (var tag in tags)
                {
                    string tagColor;
                    string tagName;
                    switch (tag)
                    {
                        case CardTag.Slime:
                            tagColor = "#00FF88"; tagName = "Slime"; break;
                        case CardTag.Reluctant:
                            tagColor = "#CC66FF"; tagName = "Reluctant"; break;
                        case CardTag.Blood:
                            tagColor = "#FF4444"; tagName = "Blood"; break;
                        case CardTag.Frost:
                            tagColor = "#66CCFF"; tagName = "Frost"; break;
                        case CardTag.Corrupt:
                            tagColor = "#9933CC"; tagName = "Corrupt"; break;
                        case CardTag.Shadow:
                            tagColor = "#666666"; tagName = "Shadow"; break;
                        case CardTag.Curse:
                            tagColor = "#731A8B"; tagName = "Curse"; break;
                        default: continue;
                    }
                    desc += $"\n<color={tagColor}>[{tagName}]</color>";
                }
            }

            //
            if (UsesBloodConversion)
                desc += $"\n<color=#FF4444>{bloodPerEnergy} 1 </color>";
            if (UsesBlockConversion)
                desc += $"\n<color=#66CCFF>{blockPerEnergy} 1 </color>";

            // faction 
            if (faction == CardFaction.Slime && !HasTag(CardTag.Slime))
                desc += $"\n<color=#00FF88></color>";

            if (faction == CardFaction.Reluctant && !HasTag(CardTag.Reluctant))
                desc += $"\n<color=#CC66FF></color>";

            if (exhaust)
                desc += "\n<color=#FF6644>Exhaust</color>";

            if (isUpgraded)
                desc += " ()";

            description = desc;
        }

        public string GetDescription() => description;

        public Color GetRarityColor() => CardVisualConfig.GetRarityColor(rarity);

        public string GetRarityName()
        {
            switch (rarity)
            {
                case CardRarity.Common: return "Common";
                case CardRarity.Uncommon: return "Uncommon";
                case CardRarity.Rare: return "Rare";
                case CardRarity.Legendary: return "Legendary";
                case CardRarity.Colorless: return "Colorless";
                case CardRarity.Cursed: return "Cursed";
                default: return "Unknown";
            }
        }

        public string GetFactionName()
        {
            switch (faction)
            {
                case CardFaction.None: return "";
                case CardFaction.Slime: return "Slime";
                case CardFaction.Reluctant: return "Reluctant";
                case CardFaction.Blood: return "Blood";
                case CardFaction.Frost: return "Frost";
                case CardFaction.Shadow: return "Shadow";
                case CardFaction.Corrupt: return "Corrupt";
                default: return "";
            }
        }

        public bool HasFaction() => faction != CardFaction.None;

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