using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "PotionBalanceConfig", menuName = "MutationChess/Config/Potion Balance Config")]
    public class PotionBalanceConfig : ScriptableObject
    {
        [System.Serializable]
        public class PotionBalanceEntry
        {
            [Header("基础信息")]
            public PotionName potionName;
            public string potionId;
            public PotionRarity rarity;
            [TextArea(2, 4)] public string designNotes;

            [Header("价格（需与 ShopConfig 一致）")]
            public int price;

            [Header("效果列表")]
            [Tooltip("该药水触发的 Effect 资源名称列表")]
            public List<string> effectIds = new List<string>();

            [Header("关联阵营")]
            [Tooltip("若该药水与某一阵营强相关则填入，否则保持 None")]
            public CardFaction relatedFaction = CardFaction.None;
        }

        [Header("所有药水配置项")]
        public List<PotionBalanceEntry> entries = new List<PotionBalanceEntry>();

        public PotionBalanceEntry GetEntry(PotionName potionName)
        {
            foreach (var entry in entries)
            {
                if (entry.potionName == potionName)
                    return entry;
            }
            return null;
        }

        public static PotionBalanceConfig CreateDefaultConfig()
        {
            var config = CreateInstance<PotionBalanceConfig>();

            // === 通用 ===

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.能量药水,
                potionId = "Potion_Energy",
                rarity = PotionRarity.Common,
                designNotes = "获得2点能量。参考 STS Energy Potion",
                price = 50,
                relatedFaction = CardFaction.None,
                effectIds = new List<string> { "GainEnergyEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.抽牌药水,
                potionId = "Potion_Draw",
                rarity = PotionRarity.Common,
                designNotes = "抽3张牌。参考 STS Swift Potion",
                price = 50,
                relatedFaction = CardFaction.None,
                effectIds = new List<string> { "DrawCardsEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.力量药水,
                potionId = "Potion_Strength",
                rarity = PotionRarity.Uncommon,
                designNotes = "获得2点力量。参考 STS Strength Potion",
                price = 75,
                relatedFaction = CardFaction.None,
                effectIds = new List<string> { "GainStrengthEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.盾药水,
                potionId = "Potion_Block",
                rarity = PotionRarity.Common,
                designNotes = "获得12点格挡。参考 STS Block Potion",
                price = 50,
                relatedFaction = CardFaction.None,
                effectIds = new List<string> { "ApplyBlockEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.回复药水,
                potionId = "Potion_Heal",
                rarity = PotionRarity.Common,
                designNotes = "回复8点生命。参考 STS Blood Potion",
                price = 50,
                relatedFaction = CardFaction.Blood,
                effectIds = new List<string> { "HealPlayerEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.净化药水,
                potionId = "Potion_Cleanse",
                rarity = PotionRarity.Uncommon,
                designNotes = "移除玩家身上所有 debuff（易伤/虚弱/脆弱等）。参考 STS Swift Potion + Orange Pellets",
                price = 75,
                relatedFaction = CardFaction.None,
                effectIds = new List<string> { "CleanseDebuffEffect" }
            });

            // === 鲜血 ===

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.血怒药水,
                potionId = "Potion_BloodRage",
                rarity = PotionRarity.Rare,
                designNotes = "本回合鲜血系卡牌伤害与效果大幅提升，代价为失去生命。属于高风险高回报药水。",
                price = 100,
                relatedFaction = CardFaction.Blood,
                effectIds = new List<string> { "BloodRageEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.纯血药水,
                potionId = "Potion_PureBlood",
                rarity = PotionRarity.Uncommon,
                designNotes = "本回合鲜血系卡牌的生命消耗减少（每1点=1格挡抵扣），并回复少量生命。偏防御向。",
                price = 75,
                relatedFaction = CardFaction.Blood,
                effectIds = new List<string> { "BloodDiscountEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.鲜血共鸣药水,
                potionId = "Potion_BloodResonance",
                rarity = PotionRarity.Rare,
                designNotes = "回复20点生命，下回合失去3点力量。参考 STS Regen Potion + 力量反噬机制。",
                price = 100,
                relatedFaction = CardFaction.Blood,
                effectIds = new List<string> { "HealPlayerEffect", "LoseStrengthNextTurnEffect" }
            });

            // === 寒霜 ===

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.冰封药水,
                potionId = "Potion_FrostLock",
                rarity = PotionRarity.Uncommon,
                designNotes = "本回合寒霜系卡牌的格挡消耗减少（每1点=1格挡抵扣），并提升格挡获取。",
                price = 75,
                relatedFaction = CardFaction.Frost,
                effectIds = new List<string> { "BlockDiscountEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.寒霜共鸣药水,
                potionId = "Potion_FrostResonance",
                rarity = PotionRarity.Rare,
                designNotes = "获得20点格挡，下回合失去3点敏捷。参考 STS Block Potion + 敏捷反噬机制。",
                price = 100,
                relatedFaction = CardFaction.Frost,
                effectIds = new List<string> { "ApplyBlockEffect", "LoseDexterityNextTurnEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.永冻药水,
                potionId = "Potion_Permafrost",
                rarity = PotionRarity.Rare,
                designNotes = "本回合受到的伤害大幅降低，但下回合无法获得新格挡。参考 STS Power Potion。",
                price = 100,
                relatedFaction = CardFaction.Frost,
                effectIds = new List<string> { "DamageReductionEffect", "BlockLockNextTurnEffect" }
            });

            // === 腐化 ===

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.腐化释放药水,
                potionId = "Potion_CorruptRelease",
                rarity = PotionRarity.Uncommon,
                designNotes = "立即触发手牌中所有腐化系卡牌的腐化效果，无需打出。偏爆发型药水。",
                price = 75,
                relatedFaction = CardFaction.Corrupt,
                effectIds = new List<string> { "CorruptReleaseEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.黑暗契约药水,
                potionId = "Potion_DarkPact",
                rarity = PotionRarity.Rare,
                designNotes = "消耗手牌换取能量，每消耗1张获得1点能量。参考 STS Dark Potion。",
                price = 100,
                relatedFaction = CardFaction.Corrupt,
                effectIds = new List<string> { "ExhaustHandForEnergyEffect" }
            });

            // === 系列联动 ===

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.粘液净化药水,
                potionId = "Potion_SlimeCleanse",
                rarity = PotionRarity.Uncommon,
                designNotes = "触发手牌中所有粘液系卡牌的粘滞效果，延缓敌人行动。",
                price = 75,
                relatedFaction = CardFaction.Slime,
                effectIds = new List<string> { "TriggerSlimeHandEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.不舍回响药水,
                potionId = "Potion_ReluctantEcho",
                rarity = PotionRarity.Uncommon,
                designNotes = "从弃牌堆随机取回2张不舍系卡牌到手牌。增强循环与续航。",
                price = 75,
                relatedFaction = CardFaction.Reluctant,
                effectIds = new List<string> { "DrawReluctantFromDiscardEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.联动共鸣药水,
                potionId = "Potion_ComboResonance",
                rarity = PotionRarity.Rare,
                designNotes = "本回合所有跨系列联动卡牌效果翻倍。鼓励多系列混搭卡组。",
                price = 100,
                relatedFaction = CardFaction.None,
                effectIds = new List<string> { "TagComboResonanceEffect" }
            });

            // === 工具 ===

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.发现药水,
                potionId = "Potion_Discover",
                rarity = PotionRarity.Uncommon,
                designNotes = "发现一张无色卡牌加入背包。参考 STS Colorless Potion。",
                price = 75,
                relatedFaction = CardFaction.None,
                effectIds = new List<string> { "DiscoverEffect" }
            });

            config.entries.Add(new PotionBalanceEntry
            {
                potionName = PotionName.洞察药水,
                potionId = "Potion_Inspect",
                rarity = PotionRarity.Common,
                designNotes = "查看牌堆顶5张牌并可选择调换顺序。参考 STS Tools of the Trade。",
                price = 50,
                relatedFaction = CardFaction.None,
                effectIds = new List<string> { "InspectEffect" }
            });

            return config;
        }
    }
}
