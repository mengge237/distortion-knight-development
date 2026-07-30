using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>



    /// </summary>
    [CreateAssetMenu(fileName = "CardBalanceConfig", menuName = "MutationChess/Config/Card Balance Config")]
    public class CardBalanceConfig : ScriptableObject
    {
        [System.Serializable]
        public class CardBalanceEntry
        {
            public CardName cardName;
            [TextArea(2, 4)] public string designNotes;

            [Header("基础数值")]
            public int cost = 1;
            public int damage = 0;
            public int block = 0;
            public int magicNumber = 0;
            public CardType cardType = CardType.Attack;
            public CardRarity rarity = CardRarity.Common;
            public bool exhaust = false;

            [Header("标签")]
            public List<CardTag> tags = new List<CardTag>();

            [Header("鲜血换能量机制")]
            [Tooltip("每点能量消耗的鲜血值，例如3=3滴血换1点能量（打出时消耗生命）")]
            public int bloodPerEnergy = 0;

            [Header("格挡换能量机制")]
            [Tooltip("每点能量消耗的格挡值，例如5=5点格挡换1点能量（打出时消耗格挡）")]
            public int blockPerEnergy = 0;

            [Header("效果ID")]
            public List<string> effectIds = new List<string>();
            public List<string> inherentEffectIds = new List<string>();

            [Header("无色卡牌设置")]
            [Tooltip("勾选后该卡牌将作为无色卡牌，可被任意职业/流派发现或获取")]
            public bool isColorless = false;
        }

        [Header("卡牌平衡配置列表")]
        public List<CardBalanceEntry> entries = new List<CardBalanceEntry>();

        /// <summary>

        /// </summary>
        public CardBalanceEntry GetEntry(CardName cardName)
        {
            foreach (var entry in entries)
            {
                if (entry.cardName == cardName)
                    return entry;
            }
            return null;
        }

        /// <summary>

        /// </summary>
        public static CardBalanceConfig CreateDefaultConfig()
        {
            var config = CreateInstance<CardBalanceConfig>();



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.攻击,
                designNotes = "基础攻击卡。1费造成6点伤害。对应 Slay the Spire Strike",
                cost = 1,
                damage = 6,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.防御,
                designNotes = "基础防御卡。1费获得5点格挡。对应 Slay the Spire Defend",
                cost = 1,
                block = 5,
                cardType = CardType.Defense,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "ApplyBlockEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.痛击,
                designNotes = "2费造成8点伤害+1层易伤。对应 Slay the Spire Pommel Strike/Bash",
                cost = 2,
                damage = 8,
                magicNumber = 1,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "DealDamageEffect", "ApplyVulnerabilityEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.加固,
                designNotes = "1费获得3点敏捷（永久）。对应 Slay the Spire Footwork",
                cost = 1,
                magicNumber = 3,
                cardType = CardType.Defense,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "ApplyDexterityEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.暮光仪式,
                designNotes = "1费造成8点伤害，当HP<=3时伤害翻倍(8->16)，高风险高回报卡牌",
                cost = 1,
                damage = 8,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.预知仪式,
                designNotes = "1费抽2张牌。对应 Slay the Spire Acrobatics/Pommel Strike 类效果",
                cost = 1,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "DrawCardsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.后发制人,
                designNotes = "0费检视抽牌堆顶部3张牌并排序。对应 Slay the Spire Foresight/Scry",
                cost = 0,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "InspectEffect" }
            });



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.粘液打击,
                designNotes = "粘液流派基础攻击。1费造成5点伤害。带粘液固有标签",
                cost = 1,
                damage = 5,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { CardTag.Slime },
                inherentEffectIds = new List<string> { "SlimeInherent" },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.粘液防御,
                designNotes = "粘液流派防御。1费获得4点格挡+施加1层虚弱",
                cost = 1,
                block = 4,
                magicNumber = 1,
                cardType = CardType.Defense,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { CardTag.Slime },
                inherentEffectIds = new List<string> { "SlimeInherent" },
                effectIds = new List<string> { "ApplyBlockEffect", "ApplyWeakEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.粘液附体,
                designNotes = "2费对所有敌人造成3点伤害(AoE)。粘液群体攻击",
                cost = 2,
                damage = 3,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Slime },
                inherentEffectIds = new List<string> { "SlimeInherent" },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.粘液喷射,
                designNotes = "1费造成3点伤害+施加2层虚弱。粘液削弱卡",
                cost = 1,
                damage = 3,
                magicNumber = 2,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { CardTag.Slime },
                inherentEffectIds = new List<string> { "SlimeInherent" },
                effectIds = new List<string> { "DealDamageEffect", "ApplyWeakEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.粘液陷阱,
                designNotes = "0费下回合造成5点伤害。粘液延迟攻击陷阱",
                cost = 0,
                damage = 5,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Slime },
                inherentEffectIds = new List<string> { "SlimeInherent" },
                effectIds = new List<string> { "DealDamageNextTurnEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.粘液分裂,
                designNotes = "1费抽1张牌。粘液过牌卡",
                cost = 1,
                magicNumber = 1,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Slime },
                inherentEffectIds = new List<string> { "SlimeInherent" },
                effectIds = new List<string> { "DrawCardsEffect" }
            });



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.腐化之触,
                designNotes = "腐化流派防御。1费获得6点格挡。带腐化标签",
                cost = 1,
                block = 6,
                cardType = CardType.Defense,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { CardTag.Corrupt },
                inherentEffectIds = new List<string> { },
                effectIds = new List<string> { "ApplyBlockEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.腐化蔓延,
                designNotes = "腐化流派攻击。1费造成7点伤害。腐化蔓延效果",
                cost = 1,
                damage = 7,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { CardTag.Corrupt },
                inherentEffectIds = new List<string> { },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.血瀑,
                designNotes = "鲜血流派。1费获得2点临时力量（回合结束消失），鲜血爆发特性",
                cost = 1,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Blood },
                inherentEffectIds = new List<string> { },
                effectIds = new List<string> { "ApplyTemporaryStrengthEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.嗜血仪式,
                designNotes = "鲜血流派。0费从弃牌堆抽1张不舍标签卡。嗜血回收卡",
                cost = 0,
                magicNumber = 1,
                cardType = CardType.Skill,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { CardTag.Blood },
                inherentEffectIds = new List<string> { },
                effectIds = new List<string> { "DrawReluctantFromDiscardEffect" }
            });



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.血池,
                designNotes = "鲜血流派攻击。1费造成9点伤害。高伤单卡",
                cost = 1,
                damage = 9,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { CardTag.Blood },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.鲜血献祭,
                designNotes = "鲜血流派攻击。1费造成7点伤害+施加1层易伤",
                cost = 1,
                damage = 7,
                magicNumber = 1,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { CardTag.Blood },
                effectIds = new List<string> { "DealDamageEffect", "ApplyVulnerabilityEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.血怒,
                designNotes = "鲜血流派攻击。2费造成14点伤害。高费高伤终结技",
                cost = 2,
                damage = 14,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Blood },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.血腥撕裂,
                designNotes = "鲜血流派技能。1费获得8点格挡+施加1层虚弱",
                cost = 1,
                block = 8,
                magicNumber = 1,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Blood },
                effectIds = new List<string> { "ApplyBlockEffect", "ApplyWeakEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.寒枪,
                designNotes = "寒霜流派。0费抽2张牌。寒霜过牌卡",
                cost = 0,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { CardTag.Frost },
                effectIds = new List<string> { "DrawCardsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.霜甲,
                designNotes = "寒霜流派能力。1费获得3点力量（永久）。寒霜成长卡",
                cost = 1,
                magicNumber = 3,
                cardType = CardType.Power,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { CardTag.Frost },
                effectIds = new List<string> { "GainStrengthEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.寒霜反击,
                designNotes = "寒霜流派攻击。2费造成20点伤害。寒霜重击终结技",
                cost = 2,
                damage = 20,
                cardType = CardType.Attack,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { CardTag.Frost },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.冰封,
                designNotes = "寒霜流派攻击。1费造成6点伤害+抽1张牌。攻守兼备",
                cost = 1,
                damage = 6,
                magicNumber = 1,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Frost },
                effectIds = new List<string> { "DealDamageEffect", "DrawCardsEffect" }
            });



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.寒冰壁垒,
                designNotes = "寒霜流派攻击。2费造成18点伤害，3滴血换1点能量（消耗生命），回复3点生命",
                cost = 2,
                damage = 18,
                magicNumber = 3,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Frost },
                bloodPerEnergy = 3,
                effectIds = new List<string> { "DealDamageEffect", "HealPlayerEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.冰霜之锤,
                designNotes = "寒霜流派技能。1费获得2点力量，4滴血换1点能量（消耗生命），回复2点生命",
                cost = 1,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { CardTag.Frost },
                bloodPerEnergy = 4,
                effectIds = new List<string> { "GainStrengthEffect", "HealPlayerEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.腐化,
                designNotes = "腐化流派攻击。0费造成8点伤害，5滴血换1点能量（消耗生命），回复2点生命",
                cost = 0,
                damage = 8,
                magicNumber = 2,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { CardTag.Corrupt },
                bloodPerEnergy = 5,
                effectIds = new List<string> { "DealDamageEffect", "HealPlayerEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.腐蚀打击,
                designNotes = "技能卡。1费治疗6点生命+抽2张牌，3滴血换1点能量（消耗生命），腐蚀回复过牌",
                cost = 1,
                magicNumber = 6,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { },
                bloodPerEnergy = 3,
                effectIds = new List<string> { "HealPlayerEffect", "DrawCardsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.腐化释放,
                designNotes = "腐化流派攻击。2费造成12点伤害+回复4点生命，3滴血换1点能量（消耗生命），攻防一体",
                cost = 2,
                damage = 12,
                magicNumber = 4,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Corrupt },
                bloodPerEnergy = 3,
                effectIds = new List<string> { "DealDamageEffect", "HealPlayerEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.暗影腐化,
                designNotes = "腐化流派大招。3费造成25点伤害+回复6点生命，3滴血换1点能量（消耗生命），终极爆发",
                cost = 3,
                damage = 25,
                magicNumber = 6,
                cardType = CardType.Attack,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { CardTag.Corrupt },
                bloodPerEnergy = 3,
                effectIds = new List<string> { "DealDamageEffect", "HealPlayerEffect" }
            });



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.腐化吞噬,
                designNotes = "腐化流派攻击。2费造成12点伤害，5点格挡换1点能量（消耗格挡），吞噬反击",
                cost = 2,
                damage = 12,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Corrupt },
                blockPerEnergy = 5,
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.不舍之盾,
                designNotes = "不舍流派防御。2费获得15点格挡，4点格挡换1点能量（消耗格挡），不舍之盾",
                cost = 2,
                block = 15,
                cardType = CardType.Defense,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { CardTag.Reluctant },
                blockPerEnergy = 4,
                effectIds = new List<string> { "ApplyBlockEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.回响打击,
                designNotes = "防御卡。3费获得20点格挡+3层反伤，6点格挡换1点能量（消耗格挡），回响反击",
                cost = 3,
                block = 20,
                magicNumber = 3,
                cardType = CardType.Defense,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { },
                blockPerEnergy = 6,
                effectIds = new List<string> { "ApplyBlockEffect", "ApplyThornsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.执念,
                designNotes = "防御卡。1费获得10点格挡，5点格挡换1点能量（消耗格挡），执念固守",
                cost = 1,
                block = 10,
                cardType = CardType.Defense,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { },
                blockPerEnergy = 5,
                effectIds = new List<string> { "ApplyBlockEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.轮回,
                designNotes = "防御卡。2费获得18点格挡，4点格挡换1点能量（消耗格挡），轮回循环",
                cost = 2,
                block = 18,
                cardType = CardType.Defense,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { },
                blockPerEnergy = 4,
                effectIds = new List<string> { "ApplyBlockEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.暗影突袭,
                designNotes = "暗影流派攻击。2费造成15点伤害+5点格挡，5点格挡换1点能量（消耗格挡），攻守兼备",
                cost = 2,
                damage = 15,
                block = 5,
                cardType = CardType.Attack,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { CardTag.Shadow },
                blockPerEnergy = 5,
                effectIds = new List<string> { "DealDamageEffect", "ApplyBlockEffect" }
            });



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.影刃,
                designNotes = "暗影流派技能。1费消耗最多3张腐化卡，每张获得1点力量",
                cost = 1,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Shadow },
                effectIds = new List<string> { "CorruptPowerEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.暗袭,
                designNotes = "暗影流派攻击。1费造成10点伤害。暗影突袭型单卡",
                cost = 1,
                damage = 10,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { CardTag.Shadow },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.暗影迷雾,
                designNotes = "暗影流派技能。1费使本回合所有卡牌不消耗（腐化释放），暗影迷雾掩护",
                cost = 1,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { CardTag.Shadow },
                effectIds = new List<string> { "CorruptReleaseEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.幻影,
                designNotes = "暗影流派技能。1费随机按标签加入2张卡到牌组（RandomByTag），幻影召唤",
                cost = 1,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { CardTag.Shadow },
                effectIds = new List<string> { "AddCardToDeckEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.暗影蓄力,
                designNotes = "暗影流派大招。3费造成20点伤害+抽2张牌，蓄力终结技",
                cost = 3,
                damage = 20,
                magicNumber = 2,
                cardType = CardType.Attack,
                rarity = CardRarity.Legendary,
                tags = new List<CardTag> { CardTag.Shadow },
                effectIds = new List<string> { "DealDamageEffect", "DrawCardsEffect" }
            });



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.暗影爆发,
                designNotes = "暗影流派技能。2费施加虚弱+易伤+减力量三层debuff（magicNumber=3）",
                cost = 2,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { CardTag.Shadow },
                inherentEffectIds = new List<string> { },
                effectIds = new List<string> { "ApplyWeakEffect", "ApplyVulnerabilityEffect", "ReduceStrengthEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.影舞,
                designNotes = "暗影流派攻击。2费造成16点伤害+10点格挡，5点格挡换1点能量（消耗格挡），影舞攻守",
                cost = 2,
                damage = 16,
                block = 10,
                cardType = CardType.Attack,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { CardTag.Shadow },
                blockPerEnergy = 5,
                effectIds = new List<string> { "DealDamageEffect", "ApplyBlockEffect" }
            });



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.粘腻爱意,
                designNotes = "联动卡。0费检视3张牌。粘液与不舍联动",
                cost = 0,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "InspectEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.霜影斩,
                designNotes = "联动卡。1费发现3选1。寒霜与暗影联动",
                cost = 1,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                effectIds = new List<string> { "DiscoverEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.预知,
                designNotes = "预知卡。1费造成10点伤害+礼物触发1次（magicNumber=1）",
                cost = 1,
                damage = 10,
                magicNumber = 1,
                cardType = CardType.Attack,
                rarity = CardRarity.Rare,
                effectIds = new List<string> { "GiftEffect", "DealDamageEffect" }
            });



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.探索,
                designNotes = "无色卡。0费宝藏效果2次（magicNumber=2），探索收益卡",
                cost = 0,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                isColorless = true,
                effectIds = new List<string> { "TreasureEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.礼物之力,
                designNotes = "无色卡。1费获得1点能量+抽2张牌（magicNumber=2），过牌增益卡",
                cost = 1,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                isColorless = true,
                effectIds = new List<string> { "GainEnergyEffect", "DrawCardsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.宝藏,
                designNotes = "无色卡。1费发现3选1（magicNumber=3），宝藏收益卡",
                cost = 1,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Rare,
                isColorless = true,
                effectIds = new List<string> { "DiscoverEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.冥想,
                designNotes = "无色卡。0费获得2点力量+2点敏捷（magicNumber=2），冥想成长卡",
                cost = 0,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Rare,
                isColorless = true,
                effectIds = new List<string> { "GainStrengthEffect", "ApplyDexterityEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.神秘卷轴,
                designNotes = "无色卡。2费治疗6点生命（magicNumber=6）+6点格挡（block=6），神秘防御回复",
                cost = 2,
                block = 6,
                magicNumber = 6,
                cardType = CardType.Skill,
                rarity = CardRarity.Legendary,
                isColorless = true,
                effectIds = new List<string> { "HealPlayerEffect", "ApplyBlockEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.古老符文,
                designNotes = "无色卡。2费减伤50%（magicNumber=50）+抽3张牌，古老符文防护过牌",
                cost = 2,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Legendary,
                isColorless = true,
                effectIds = new List<string> { "DamageReductionEffect", "DrawCardsEffect" }
            });

            // ============================================================

            // ============================================================
            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.圣物,
                designNotes = "通用攻击卡。1费造成8点伤害，连击3张+4伤害加成，圣物系列基础攻击",
                cost = 1,
                damage = 8,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.深渊之眼,
                designNotes = "通用技能。0费回复4HP。深渊之眼治疗卡",
                cost = 0,
                magicNumber = 4,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "HealPlayerEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.破阵,
                designNotes = "通用技能。1费获得6点格挡+抽1张牌。破阵过牌防御",
                cost = 1,
                block = 6,
                magicNumber = 1,
                cardType = CardType.Skill,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "ApplyBlockEffect", "DrawCardsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.回春,
                designNotes = "通用技能。0费下回合+2点能量。回春蓄能卡",
                cost = 0,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "GainEnergyNextTurnEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.镇魂,
                designNotes = "通用攻击。2费造成14点伤害，消耗。镇魂一击",
                cost = 2,
                damage = 14,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                exhaust = true,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.蓄势,
                designNotes = "通用技能。1费减伤-3点。蓄势防御卡",
                cost = 1,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "DamageReductionEffect" }
            });

            // ============================================================

            // ============================================================
            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.斩缘,
                designNotes = "通用攻击。2费造成12点伤害，消耗。斩缘一击",
                cost = 2,
                damage = 12,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                exhaust = true,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.灵动,
                designNotes = "通用技能。1费获得5点格挡+抽1张牌。灵动过牌",
                cost = 1,
                block = 5,
                magicNumber = 1,
                cardType = CardType.Skill,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "ApplyBlockEffect", "DrawCardsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.惊雷,
                designNotes = "通用攻击。3费造成20点伤害。惊雷重击",
                cost = 3,
                damage = 20,
                cardType = CardType.Attack,
                rarity = CardRarity.Rare,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.守心,
                designNotes = "通用技能。1费回复5HP+5点格挡。守心防御回复",
                cost = 1,
                block = 5,
                magicNumber = 5,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "HealPlayerEffect", "ApplyBlockEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.破军,
                designNotes = "通用技能。0费下回合+1能量+抽1张牌。破军蓄势过牌",
                cost = 0,
                magicNumber = 1,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "GainEnergyNextTurnEffect", "DrawCardsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.归元,
                designNotes = "通用防御。2费获得14点格挡。归元防御卡",
                cost = 2,
                block = 14,
                cardType = CardType.Defense,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "ApplyBlockEffect" }
            });

            // ============================================================

            // ============================================================
            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.凝神,
                designNotes = "诅咒卡。0费不可打出，每回合结束-1HP（衰减）",
                cost = 0,
                cardType = CardType.Curse,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "CurseDecayEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.玄甲,
                designNotes = "诅咒卡。0费，手牌上限-1（迷雾），降低每回合可持卡数",
                cost = 0,
                cardType = CardType.Curse,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "CurseFogEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.诅咒_衰败,
                designNotes = "诅咒卡（衰败）。0费，每回合抽牌数-1（锁链）",
                cost = 0,
                cardType = CardType.Curse,
                rarity = CardRarity.Cursed,
                tags = new List<CardTag> { CardTag.Curse },
                effectIds = new List<string> { "CurseChainsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.诅咒_迷雾,
                designNotes = "诅咒卡（迷雾）。0费，每打出一张卡-1HP（噬命）",
                cost = 0,
                cardType = CardType.Curse,
                rarity = CardRarity.Cursed,
                tags = new List<CardTag> { CardTag.Curse },
                effectIds = new List<string> { "CurseDevourEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.诅咒_枷锁,
                designNotes = "诅咒卡（枷锁）。0费，每回合开始-1能量（虚空）",
                cost = 0,
                cardType = CardType.Curse,
                rarity = CardRarity.Cursed,
                tags = new List<CardTag> { CardTag.Curse },
                effectIds = new List<string> { "CurseVoidEffect" }
            });

            return config;
        }
    }
}



