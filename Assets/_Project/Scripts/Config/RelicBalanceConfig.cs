using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 遗物平衡配置文件
    /// 
    /// 隐藏效果机制（重要）：
    /// - 部分字段系列遗物一开始只显示"基础效果"
    /// - 当玩家获得对应系列的"Boss遗物"（稀有度=Special，通常是Boss掉落）后，
    ///   该系列所有带 hiddenActivatorRelicId 的遗物会额外激活 hiddenEffectIds 中的效果
    /// - 例如：血护符 基础是+8最大HP，拥有 boss遗物"鲜血脉络"后，额外激活"鲜血卡费用-1"
    /// </summary>
    [CreateAssetMenu(fileName = "RelicBalanceConfig", menuName = "MutationChess/Config/Relic Balance Config")]
    public class RelicBalanceConfig : ScriptableObject
    {
        [System.Serializable]
        public class RelicBalanceEntry
        {
            [Header("遗物基础信息")]
            public string relicId;
            public string relicName;
            public RelicRarity rarity;
            public CardFaction faction;
            [TextArea(2, 4)] public string designNotes;

            [Header("价格")]
            public int price;

            [Header("基础效果（始终激活，玩家一直能看到）")]
            public List<RelicEffectEntry> baseEffectIds = new List<RelicEffectEntry>();

            [Header("隐藏效果激活条件：拥有此Boss遗物ID时才激活隐藏效果")]
            [Tooltip("留空表示没有隐藏效果")]
            public string hiddenActivatorRelicId = "";

            [Header("隐藏效果（拥有Activator后激活，获得Activator前不显示）")]
            public List<RelicEffectEntry> hiddenEffectIds = new List<RelicEffectEntry>();

            [Header("标记")]
            public bool isShopRelic = false;       // 商店专属
            public bool isBossRelic = false;       // Boss掉落，字段核心（激活隐藏效果）
            public bool isStartingRelic = false;   // 起始遗物
            public bool isSynthesisTarget = false; // 合成目标（剑碎片等）
        }

        [Header("全局遗物平衡条目")]
        public List<RelicBalanceEntry> entries = new List<RelicBalanceEntry>();

        public RelicBalanceEntry GetEntry(string relicId)
        {
            foreach (var entry in entries)
            {
                if (entry.relicId == relicId)
                    return entry;
            }
            return null;
        }

        public static RelicBalanceConfig CreateDefaultConfig()
        {
            var config = CreateInstance<RelicBalanceConfig>();

            // ============================================================
            // 一、起始遗物
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Starter_BurningHeart,
                relicName = "燃烧之心",
                rarity = RelicRarity.Starting,
                faction = CardFaction.None,
                designNotes = "起始遗物：战斗胜利时回复6HP",
                price = 0,
                isStartingRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "HealOnVictoryEffect", trigger = EffectTrigger.Victory, value1 = 6f }
                }
            });

            // ============================================================
            // 二、Boss核心遗物（Special稀有度，只能Boss掉落，负责激活系列隐藏效果）
            // 旧版解锁遗物融入新系统，共6个字段系列 -> 6个Boss遗物
            // ============================================================

            // --- 1. 鲜血 Boss 遗物 ---
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Boss_BloodVein,
                relicName = "鲜血脉络",
                rarity = RelicRarity.Starting,
                faction = CardFaction.Blood,
                designNotes = "【Boss核心仅Boss掉落】最大生命-5；战斗开始时，每有2点最大生命+1层力量（激活鲜血系列隐藏效果）",
                price = 350,
                isBossRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "BossBloodVeinEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            // --- 2. 寒霜 Boss 遗物（新增补全） ---
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Boss_FrostHeart,
                relicName = "寒霜之心",
                rarity = RelicRarity.Starting,
                faction = CardFaction.Frost,
                designNotes = "【Boss核心仅Boss掉落】战斗开始获得20格挡；打出寒霜牌返还1能量（激活寒霜系列隐藏效果）",
                price = 350,
                isBossRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ApplyBlockEffect", trigger = EffectTrigger.BattleStart, value1 = 20f },
                    new RelicEffectEntry { effectId = "FrostCardEnergyRefundEffect", trigger = EffectTrigger.CardPlayed }
                }
            });

            // --- 3. 腐化 Boss 遗物 ---
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Boss_CorruptLiver,
                relicName = "腐化肝脏",
                rarity = RelicRarity.Starting,
                faction = CardFaction.Corrupt,
                designNotes = "【Boss核心仅Boss掉落】卡牌消耗时往牌堆添加1张随机腐化牌；击败敌人永久+1伤害（激活腐化系列隐藏效果）",
                price = 350,
                isBossRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "BossCorruptLiverEffect", trigger = EffectTrigger.CardExhausted },
                    new RelicEffectEntry { effectId = "ShadowCardBonusDamageEffect", trigger = EffectTrigger.EnemyDeath, value1 = 1f }
                }
            });

            // --- 4. 粘液 Boss 遗物 ---
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Boss_SlimeGland,
                relicName = "粘液腺体",
                rarity = RelicRarity.Starting,
                faction = CardFaction.Slime,
                designNotes = "【Boss核心仅Boss掉落】战斗开始额外抽1张牌；粘液牌施加虚弱层数+1（激活粘液系列隐藏效果）",
                price = 350,
                isBossRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "DrawCardsEffect", trigger = EffectTrigger.BattleStart, value1 = 1f },
                    new RelicEffectEntry { effectId = "SlimeWeakEffect", trigger = EffectTrigger.CardPlayed, value1 = 1f }
                }
            });

            // --- 5. 不舍 Boss 遗物（新增补全） ---
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Boss_ReluctantChain,
                relicName = "不舍锁链",
                rarity = RelicRarity.Starting,
                faction = CardFaction.Reluctant,
                designNotes = "【Boss核心仅Boss掉落】战斗开始额外抽1张不舍牌；打出不舍牌时格挡+2（激活不舍系列隐藏效果）",
                price = 350,
                isBossRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "DrawReluctantFromDiscardEffect", trigger = EffectTrigger.BattleStart, value1 = 1f },
                    new RelicEffectEntry { effectId = "ReluctantBlockBonusEffect", trigger = EffectTrigger.CardPlayed, value1 = 2f }
                }
            });

            // --- 6. 暗影 Boss 遗物（记忆晶状体+负面效果） ---
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Boss_MemoryLens,
                relicName = "记忆晶状体",
                rarity = RelicRarity.Starting,
                faction = CardFaction.Shadow,
                designNotes = "【Boss核心仅Boss掉落】战斗开始获得15格挡；暗影牌伤害+3；【负面】每4回合结束减少1点敏捷（激活暗影系列隐藏效果）",
                price = 350,
                isBossRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ApplyBlockEffect", trigger = EffectTrigger.BattleStart, value1 = 15f },
                    new RelicEffectEntry { effectId = "ShadowCardBonusDamageEffect", trigger = EffectTrigger.CalculateAttackDamage },
                    new RelicEffectEntry { effectId = "Every4TurnsLoseDexEffect", trigger = EffectTrigger.TurnEnd }
                }
            });

            // ============================================================
            // 三、鲜血系列（带隐藏效果：拥有鲜血脉络时激活费用-1）
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Blood_BloodCharm,
                relicName = "血护符",
                rarity = RelicRarity.Rare, // 稀有度调高：Common → Rare
                faction = CardFaction.Blood,
                designNotes = "基础：+8最大生命值【拥有鲜血脉络后：鲜血卡牌费用-1】",
                price = 285,
                hiddenActivatorRelicId = RelicIds.Boss_BloodVein,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "MaxHealthSmallEffect", trigger = EffectTrigger.BattleStart, value1 = 8f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "BloodCostReductionEffect", trigger = EffectTrigger.CalculateCardCost }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Blood_VampireFang,
                relicName = "吸血獠牙",
                rarity = RelicRarity.Common, // 稀有度调低：Rare → Common
                faction = CardFaction.Blood,
                designNotes = "基础：打出攻击牌回复1HP【拥有鲜血脉络后：回复量+1（共2HP）】",
                price = 150,
                hiddenActivatorRelicId = RelicIds.Boss_BloodVein,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "AttackHeal1Effect", trigger = EffectTrigger.CardPlayed, value1 = 1f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "AttackHeal1Effect", trigger = EffectTrigger.CardPlayed, value1 = 1f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Blood_BloodPact,
                relicName = "血契",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.Blood,
                designNotes = "基础：战斗开始永久+2力量【拥有鲜血脉络后：改为+3力量但-5最大生命】",
                price = 300,
                hiddenActivatorRelicId = RelicIds.Boss_BloodVein,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "GainStrengthBattleStartEffect", trigger = EffectTrigger.BattleStart, value1 = 2f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "BloodPactStrengthEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Blood_CrimsonAltar,
                relicName = "血祭坛",
                rarity = RelicRarity.Rare,
                faction = CardFaction.Blood,
                designNotes = "基础：战斗开始失3血获得2力量【拥有鲜血脉络后：失血-1（失2血获得3力量）】",
                price = 285,
                hiddenActivatorRelicId = RelicIds.Boss_BloodVein,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "BloodAltarEffect", trigger = EffectTrigger.BattleStart }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "BloodAltarBoostedEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            // ============================================================
            // 四、寒霜系列（隐藏效果由寒霜之心激活）
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Frost_IceCrystal,
                relicName = "冰晶符",
                rarity = RelicRarity.Common,
                faction = CardFaction.Frost,
                designNotes = "基础：战斗开始获得5格挡【拥有寒霜之心后：格挡卡牌费用-1】",
                price = 150,
                hiddenActivatorRelicId = RelicIds.Boss_FrostHeart,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ApplyBlockEffect", trigger = EffectTrigger.BattleStart, value1 = 5f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "BlockCostReductionEffect", trigger = EffectTrigger.CalculateCardCost }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Frost_Permafrost,
                relicName = "永冻土",
                rarity = RelicRarity.Rare,
                faction = CardFaction.Frost,
                designNotes = "基础：战斗开始获得15格挡（一次性，非每回合）【拥有寒霜之心后：+10格挡】",
                price = 285,
                hiddenActivatorRelicId = RelicIds.Boss_FrostHeart,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "FrostPermafrostEffect", trigger = EffectTrigger.BattleStart }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ApplyBlockEffect", trigger = EffectTrigger.BattleStart, value1 = 10f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Frost_FrostGiant,
                relicName = "霜巨人",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.Frost,
                designNotes = "基础：战斗开始+1敏捷；打出寒霜牌获得8格挡（加强版，去除了隐藏效果依赖）",
                price = 300,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "GainDexterityBattleStartEffect", trigger = EffectTrigger.BattleStart, value1 = 1f },
                    new RelicEffectEntry { effectId = "FrostBonusBlockEffect", trigger = EffectTrigger.CardPlayed, value1 = 8f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Frost_Snowflake,
                relicName = "雪符",
                rarity = RelicRarity.Common,
                faction = CardFaction.Frost,
                designNotes = "基础：战斗开始获得10格挡【拥有寒霜之心后：敏捷+1】",
                price = 150,
                hiddenActivatorRelicId = RelicIds.Boss_FrostHeart,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "FrostSnowflakeEffect", trigger = EffectTrigger.BattleStart }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "GainDexterityBattleStartEffect", trigger = EffectTrigger.BattleStart, value1 = 1f }
                }
            });

            // ============================================================
            // 五、腐化系列（隐藏效果由腐化肝脏激活）
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Corrupt_DarkTome,
                relicName = "暗法典",
                rarity = RelicRarity.Common,
                faction = CardFaction.Corrupt,
                designNotes = "基础：卡牌消耗时回复1点能量【拥有腐化肝脏后：额外抽1张牌】",
                price = 150,
                hiddenActivatorRelicId = RelicIds.Boss_CorruptLiver,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ExhaustEnergyEffect", trigger = EffectTrigger.CardExhausted }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ExhaustDrawEffect", trigger = EffectTrigger.CardExhausted, value1 = 1f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Corrupt_Necronomicon,
                relicName = "邪典遗书",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.Corrupt,
                designNotes = "基础：卡牌消耗时抽1张牌【拥有腐化肝脏后：消耗时回复1能量】",
                price = 350,
                hiddenActivatorRelicId = RelicIds.Boss_CorruptLiver,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ExhaustDrawEffect", trigger = EffectTrigger.CardExhausted }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ExhaustEnergyEffect", trigger = EffectTrigger.CardExhausted }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Corrupt_DeadBranch,
                relicName = "枯枝",
                rarity = RelicRarity.Rare,
                faction = CardFaction.Corrupt,
                designNotes = "基础：卡牌消耗时往牌堆加1张腐化卡【拥有腐化肝脏后：加2张】",
                price = 285,
                hiddenActivatorRelicId = RelicIds.Boss_CorruptLiver,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "CorruptAddCardEffect", trigger = EffectTrigger.CardExhausted, value1 = 1f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "CorruptAddCardEffect", trigger = EffectTrigger.CardExhausted, value1 = 1f }
                }
            });

            // ============================================================
            // 六、粘液系列（隐藏效果由粘液腺体激活）
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Slime_SlimeHeart,
                relicName = "粘液核",
                rarity = RelicRarity.Rare,
                faction = CardFaction.Slime,
                designNotes = "基础：打出粘液牌时回复1点能量【拥有粘液腺体后：额外回复1格挡】",
                price = 285,
                hiddenActivatorRelicId = RelicIds.Boss_SlimeGland,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "SlimeEnergyEffect", trigger = EffectTrigger.CardPlayed }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ReluctantBlockBonusEffect", trigger = EffectTrigger.CardPlayed, value1 = 1f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Slime_AcidicCore,
                relicName = "酸核",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.Slime,
                designNotes = "基础：战斗开始对所有敌人施加3层虚弱+3层脆弱+3层易伤（3种debuff各持续3回合）【拥有粘液腺体后：改为各4层】",
                price = 300,
                hiddenActivatorRelicId = RelicIds.Boss_SlimeGland,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "AcidicCoreDebuff3StacksEffect", trigger = EffectTrigger.BattleStart, value1 = 3f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "AcidicCoreBoostedEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Slime_StickyGlove,
                relicName = "粘液手套",
                rarity = RelicRarity.Common,
                faction = CardFaction.Slime,
                designNotes = "基础：打出粘液牌施加1层虚弱【拥有粘液腺体后：虚弱+1】",
                price = 150,
                hiddenActivatorRelicId = RelicIds.Boss_SlimeGland,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "SlimeWeakEffect", trigger = EffectTrigger.CardPlayed, value1 = 1f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "SlimeWeakEffect", trigger = EffectTrigger.CardPlayed, value1 = 1f }
                }
            });

            // ============================================================
            // 七、不舍系列（隐藏效果由不舍锁链激活）
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Reluctant_EchoRing,
                relicName = "回响戒",
                rarity = RelicRarity.Rare,
                faction = CardFaction.Reluctant,
                designNotes = "基础：打出不舍牌时抽1张牌【拥有不舍锁链后：额外+1抽牌】",
                price = 285,
                hiddenActivatorRelicId = RelicIds.Boss_ReluctantChain,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ReluctantBonusDrawEffect", trigger = EffectTrigger.CardPlayed, value1 = 1f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ReluctantBonusDrawEffect", trigger = EffectTrigger.CardPlayed, value1 = 1f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Reluctant_Nostalgia,
                relicName = "怀旧链",
                rarity = RelicRarity.Common,
                faction = CardFaction.Reluctant,
                designNotes = "基础：打出不舍牌时获得2格挡【拥有不舍锁链后：格挡+2（共4）】",
                price = 150,
                hiddenActivatorRelicId = RelicIds.Boss_ReluctantChain,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ReluctantBlockBonusEffect", trigger = EffectTrigger.CardPlayed, value1 = 2f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ReluctantBlockBonusEffect", trigger = EffectTrigger.CardPlayed, value1 = 2f }
                }
            });

            // ============================================================
            // 八、暗影系列（概率→计数触发；隐藏效果由记忆晶状体激活）
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Shadow_Cloak,
                relicName = "暗影斗篷",
                rarity = RelicRarity.Common,
                faction = CardFaction.Shadow,
                designNotes = "基础：战斗开始获得10格挡【拥有记忆晶状体后：额外获得1敏捷】",
                price = 150,
                hiddenActivatorRelicId = RelicIds.Boss_MemoryLens,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ApplyBlockEffect", trigger = EffectTrigger.BattleStart, value1 = 10f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "GainDexterityBattleStartEffect", trigger = EffectTrigger.BattleStart, value1 = 1f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Shadow_PhantomMask,
                relicName = "幻影面具",
                rarity = RelicRarity.Rare,
                faction = CardFaction.Shadow,
                designNotes = "基础：打出5张牌后，本回合受到伤害-5（原25%概率闪避→计数触发）【拥有记忆晶状体后：减伤+3（共-8）】",
                price = 285,
                hiddenActivatorRelicId = RelicIds.Boss_MemoryLens,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "PhantomAfter5CardsEffect", trigger = EffectTrigger.AfterCardsPlayed, value1 = 5f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "PhantomAfter5CardsBoostEffect", trigger = EffectTrigger.AfterCardsPlayed }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Shadow_AbyssGaze,
                relicName = "深渊凝视",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.Shadow,
                designNotes = "基础：打出4张攻击牌后，下一张攻击牌伤害翻倍（原15%暴击→计数触发）【拥有记忆晶状体后：改为3张攻击牌就触发】",
                price = 320,
                hiddenActivatorRelicId = RelicIds.Boss_MemoryLens,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "AbyssEvery4AttackEffect", trigger = EffectTrigger.CalculateAttackDamage, value1 = 4f }
                },
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "AbyssReduceThresholdEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            // ============================================================
            // 九、通用遗物（无隐藏效果，或有单独机制）
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_IronRing,
                relicName = "铁戒指",
                rarity = RelicRarity.Common,
                faction = CardFaction.None,
                designNotes = "战斗开始获得1力量（一次性）",
                price = 150,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "GainStrengthBattleStartEffect", trigger = EffectTrigger.BattleStart, value1 = 1f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_BronzeShield,
                relicName = "铜盾",
                rarity = RelicRarity.Common,
                faction = CardFaction.None,
                designNotes = "战斗开始获得1敏捷（一次性）",
                price = 150,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "GainDexterityBattleStartEffect", trigger = EffectTrigger.BattleStart, value1 = 1f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_LeatherArmor,
                relicName = "皮甲",
                rarity = RelicRarity.Common,
                faction = CardFaction.None,
                designNotes = "战斗开始获得10格挡（原每回合+3→一次性）",
                price = 150,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ApplyBlockEffect", trigger = EffectTrigger.BattleStart, value1 = 10f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_WarriorBelt,
                relicName = "战士腰带",
                rarity = RelicRarity.Common,
                faction = CardFaction.None,
                designNotes = "最大生命值+15",
                price = 150,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "MaxHealthSmallEffect", trigger = EffectTrigger.BattleStart, value1 = 15f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_SwiftBoots,
                relicName = "疾行靴",
                rarity = RelicRarity.Rare,
                faction = CardFaction.None,
                designNotes = "战斗开始额外抽2张牌（原每回合+1→一次性2张）",
                price = 285,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "DrawCardsEffect", trigger = EffectTrigger.BattleStart, value1 = 2f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_PowerPendant,
                relicName = "力量坠饰",
                rarity = RelicRarity.Rare,
                faction = CardFaction.None,
                designNotes = "战斗开始获得2力量",
                price = 285,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "GainStrength2BattleStartEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_IronWill,
                relicName = "钢铁意志",
                rarity = RelicRarity.Rare,
                faction = CardFaction.None,
                designNotes = "战斗开始获得8格挡+1力量（原每回合→一次性）",
                price = 285,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "BlockAndStrengthEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_GoldenChalice,
                relicName = "金杯",
                rarity = RelicRarity.Rare,
                faction = CardFaction.None,
                designNotes = "战斗胜利获得12金币（原回血12→改为金币）；黄金王国·金觉醒后额外+18金币",
                price = 285,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "Gain12GoldOnVictoryEffect", trigger = EffectTrigger.Victory, value1 = 12f }
                },
                hiddenActivatorRelicId = RelicIds.Gold_GoldenKingdom_Gold,
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "Gain12GoldOnVictoryEffect", trigger = EffectTrigger.Victory, value1 = 18f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_BattleStandard,
                relicName = "战旗",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.None,
                designNotes = "战斗开始+1力量、+1敏捷、+10格挡",
                price = 320,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "AllStatsBuffEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_PhoenixFeather,
                relicName = "凤凰羽毛",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.None,
                designNotes = "致命伤害时回50%生命（每场战斗1次）",
                price = 350,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "PhoenixReviveEffect", trigger = EffectTrigger.CalculatePlayerDamage }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_TitanHeart,
                relicName = "泰坦核",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.None,
                designNotes = "战斗开始获得1点敏捷，持续3回合（原每回合+2→平衡削弱）",
                price = 320,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "TempDexterity3TurnsEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_EternalFlame,
                relicName = "永焰",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.None,
                designNotes = "战斗开始+2力量+2敏捷+10格挡+2能量（原每回合持续→削弱为战斗开始一次性）",
                price = 380,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "EternalFlameBattleStartEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            // ============================================================
            // 十、联动遗物（降为Legendary）
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Combo_ResonanceStone,
                relicName = "共鸣石",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.None,
                designNotes = "打出同系列联动卡时效果增强",
                price = 350,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "TagComboResonanceEffect", trigger = EffectTrigger.CardPlayed }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Combo_ChessMaster,
                relicName = "棋王冠",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.None,
                designNotes = "【联动效果】每打出6张牌后获得1点本局力量；同时最大生命值+20",
                price = 350,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "MaxHealthEffect", trigger = EffectTrigger.BattleStart, value1 = 20f },
                    new RelicEffectEntry { effectId = "ChessMasterEvery6CardsStrengthEffect", trigger = EffectTrigger.AfterCardsPlayed, value1 = 6f }
                }
            });

            // ============================================================
            // 十一、商店遗物（去每回合）
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Shop_EnergyCore,
                relicName = "能核",
                rarity = RelicRarity.Legendary, // 稀有度调高：Rare → Legendary
                faction = CardFaction.None,
                designNotes = "战斗开始+2能量",
                price = 325,
                isShopRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "EnergyCoreBattleStartEffect", trigger = EffectTrigger.BattleStart }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Shop_DrawingPad,
                relicName = "画板",
                rarity = RelicRarity.Legendary, // 稀有度调高：Rare → Legendary
                faction = CardFaction.None,
                designNotes = "战斗开始额外抽2张牌",
                price = 325,
                isShopRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "DrawingPadDraw2Effect", trigger = EffectTrigger.BattleStart }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Shop_TreasureChest,
                relicName = "宝箱",
                rarity = RelicRarity.Rare,
                faction = CardFaction.None,
                designNotes = "战胜精英敌人时额外多获得一组卡牌奖励（非精英不生效）",
                price = 325,
                isShopRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "EliteVictoryExtraCardGroupEffect", trigger = EffectTrigger.Victory }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Shop_GoldenIdol,
                relicName = "金像",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.None,
                designNotes = "击杀敌人额外+20%金币；黄金王国·银觉醒后倍率提升至+40%",
                price = 350,
                isShopRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "GoldBonusEffect", trigger = EffectTrigger.EnemyDeath }
                },
                hiddenActivatorRelicId = RelicIds.Gold_GoldenKingdom_Silver,
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "GoldBonusEffect", trigger = EffectTrigger.EnemyDeath, value1 = 0.4f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Shop_RestockTalisman,
                relicName = "补货符",
                rarity = RelicRarity.Rare,
                faction = CardFaction.None,
                designNotes = "商店不会卖空：购买后商品自动补货",
                price = 300,
                isShopRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ShopRestockEffect", trigger = EffectTrigger.Passive }
                }
            });

            // ============================================================
            // 十二、合成系统遗物（剑碎片→剑柄→剑核心）
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Synth_SwordShard,
                relicName = "剑碎片",
                rarity = RelicRarity.Common,
                faction = CardFaction.None,
                designNotes = "攻击时额外造成2点伤害（可与剑柄碎片合成剑核）",
                price = 150,
                isSynthesisTarget = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "ShadowCardBonusDamageEffect", trigger = EffectTrigger.CalculateAttackDamage, value1 = 2f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Synth_HiltShard,
                relicName = "剑柄碎片",
                rarity = RelicRarity.Common,
                faction = CardFaction.None,
                designNotes = "获得时最大生命值+5（可与剑碎片合成剑核）",
                price = 150,
                isSynthesisTarget = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "MaxHealthSmallEffect", trigger = EffectTrigger.BattleStart, value1 = 5f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Synth_SwordCore,
                relicName = "剑核",
                rarity = RelicRarity.Rare,
                faction = CardFaction.None,
                designNotes = "合成获得：永久+2力量（剑碎片+剑柄碎片合成）",
                price = 0,
                isSynthesisTarget = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "GainStrengthBattleStartEffect", trigger = EffectTrigger.BattleStart, value1 = 2f }
                }
            });

            // ============================================================
            // 十三、其他通用
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_PiggyBank,
                relicName = "储蓄罐",
                rarity = RelicRarity.Common,
                faction = CardFaction.None,
                designNotes = "每次战斗胜利时获得当前金币的10%；黄金王国·金觉醒后提升至15%",
                price = 150,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "VictoryGoldPercentEffect", trigger = EffectTrigger.Victory }
                },
                hiddenActivatorRelicId = RelicIds.Gold_GoldenKingdom_Gold,
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "VictoryGoldPercentEffect", trigger = EffectTrigger.Victory, value1 = 0.15f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Generic_VictorySword,
                relicName = "胜利誓约剑",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.None,
                designNotes = "每场战斗1次：斩杀血量低于20的敌人",
                price = 350,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "InstantKillEffect", trigger = EffectTrigger.CalculateAttackDamage }
                }
            });

            // ============================================================
            // 十四、黄金王国系列（贪婪遗物：与 Boss 遗物同级的机制型遗物）
            //   金·黄金王国：随时间获得（第3层起胜利概率降临，一局至多一次）
            //   银·黄金王国：商店获得
            //   金银互为隐藏效果激活者：双持后各自额外激活+25%当前金币/胜利（合计+50%），
            //   并强化金杯/金像/储蓄罐/罗盘/星图/寻宝针等一切金币相关遗物
            // ============================================================
            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Gold_GoldenKingdom_Gold,
                relicName = "黄金王国·金",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.None,
                designNotes = "【贪婪遗物·时间获取】胜利时获得当前金币10%；与黄金王国·银双持后额外+25%",
                price = 0,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "VictoryGoldPercentEffect", trigger = EffectTrigger.Victory, value1 = 0.10f }
                },
                hiddenActivatorRelicId = RelicIds.Gold_GoldenKingdom_Silver,
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "VictoryGoldPercentEffect", trigger = EffectTrigger.Victory, value1 = 0.25f }
                }
            });

            config.entries.Add(new RelicBalanceEntry
            {
                relicId = RelicIds.Gold_GoldenKingdom_Silver,
                relicName = "黄金王国·银",
                rarity = RelicRarity.Legendary,
                faction = CardFaction.None,
                designNotes = "【贪婪遗物·商店获取】胜利时获得15金币；与黄金王国·金双持后额外+25%当前金币",
                price = 380,
                isShopRelic = true,
                baseEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "Gain12GoldOnVictoryEffect", trigger = EffectTrigger.Victory, value1 = 15f }
                },
                hiddenActivatorRelicId = RelicIds.Gold_GoldenKingdom_Gold,
                hiddenEffectIds = new List<RelicEffectEntry>
                {
                    new RelicEffectEntry { effectId = "VictoryGoldPercentEffect", trigger = EffectTrigger.Victory, value1 = 0.25f }
                }
            });

            return config;
        }
    }
}


