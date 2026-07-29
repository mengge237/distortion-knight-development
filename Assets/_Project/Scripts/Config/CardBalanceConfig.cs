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

            [Header("???????")]
            public int cost = 1;
            public int damage = 0;
            public int block = 0;
            public int magicNumber = 0;
            public CardType cardType = CardType.Attack;
            public CardRarity rarity = CardRarity.Common;
            public bool exhaust = false;

            [Header("?????")]
            public List<CardTag> tags = new List<CardTag>();

            [Header("?????????????")]
            [Tooltip("????????????????????3=3???1??????")]
            public int bloodPerEnergy = 0;

            [Header("????????????")]
            [Tooltip("??????????????????5=5????1??????")]
            public int blockPerEnergy = 0;

            [Header("???ID")]
            public List<string> effectIds = new List<string>();
            public List<string> inherentEffectIds = new List<string>();

            [Header("???????")]
            [Tooltip("???????????????????????????????????????")]
            public bool isColorless = false;
        }

        [Header("????????????")]
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
                designNotes = "?????????????1??6???????? Slay the Spire Strike",
                cost = 1,
                damage = 6,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.防御,
                designNotes = "?????????????1??5??????? Slay the Spire Defend",
                cost = 1,
                block = 5,
                cardType = CardType.Defense,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "ApplyBlockEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.痛击,
                designNotes = "2??8???+1?????????? Slay the Spire Pommel Strike/Bash",
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
                designNotes = "1????3???????????????? Slay the Spire Footwork",
                cost = 1,
                magicNumber = 3,
                cardType = CardType.Defense,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "ApplyDexterityEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.暮光仪式,
                designNotes = "1??????????<=3????????(8->16)??????????????",
                cost = 1,
                damage = 8,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                effectIds = new List<string> { "DealDamageEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.预知仪式,
                designNotes = "1???2???????? Slay the Spire Acrobatics/Pommel Strike ????",
                cost = 1,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "DrawCardsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.后发制人,
                designNotes = "0???????3???????? Slay the Spire Foresight/Scry",
                cost = 0,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "InspectEffect" }
            });



            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.粘液打击,
                designNotes = "???????????1??5?????????????????",
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
                designNotes = "???????????1??4??+???1??????",
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
                designNotes = "2?????????????3???(AoE)????????????",
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
                designNotes = "1??3???+2?????????????????????",
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
                designNotes = "0????????????5????????????????",
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
                designNotes = "1???1?????????????????",
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
                designNotes = "?????????????1??6?????????????",
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
                designNotes = "?????????????1??7??????????????????",
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
                designNotes = "?????????????1????2????????????2??????????????",
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
                designNotes = "?????????????0?????????1????????????????",
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
                designNotes = "????????????1??9????????????",
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
                designNotes = "????????????1??7???+????1??",
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
                designNotes = "????????????2??14????????????",
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
                designNotes = "????????????1????8??+????1??",
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
                designNotes = "????????????0???2????????????",
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
                designNotes = "????????????1????3????????????????",
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
                designNotes = "????????????2??20???????????????????",
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
                designNotes = "????????????1??6???+??1????????????",
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
                designNotes = "????????????2??18?????3???1????????????????3???????",
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
                designNotes = "????????????1????2??????4???1?????????2???????",
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
                designNotes = "????????????0??8?????5???1???????????????2???????",
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
                designNotes = "????????????1????6?????+??2?????3???1???????????????????",
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
                designNotes = "????????????2??12???+???4???????3???1????????????????",
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
                designNotes = "???????????????3??25???+???6???????3???1????????????",
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
                designNotes = "????????????2??12?????5????1?????????????",
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
                designNotes = "????????????2??15????4????1???????????",
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
                designNotes = "????????????3??20??+3??????6????1???????????????",
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
                designNotes = "????????????1??10????5????1???????????????????",
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
                designNotes = "????????????2??18????4????1???????????????",
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
                designNotes = "????????????2??15???+5????5????1?????????????",
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
                designNotes = "?????????????1????3???????????",
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
                designNotes = "?????????????1??10???+??????????????",
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
                designNotes = "?????????????1?????????????2???????????????",
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
                designNotes = "?????????????1????????????2???????(RandomByTag)?????????",
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
                designNotes = "?????????????3??20???+??2????????????????????????",
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
                designNotes = "??????????+?????????2??????+????+??????(magicNumber=3)",
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
                designNotes = "???????????+????????2??16???+10??????????????",
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
                designNotes = "????0???????3????",
                cost = 0,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Common,
                effectIds = new List<string> { "InspectEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.霜影斩,
                designNotes = "????1?????3?1",
                cost = 1,
                magicNumber = 3,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                effectIds = new List<string> { "DiscoverEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.预知,
                designNotes = "????1??10???+??????1????????(magicNumber=1)",
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
                designNotes = "?????0????2???????(magicNumber=2)????????????",
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
                designNotes = "?????1????1????+??2????(magicNumber=2)??????????",
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
                designNotes = "?????1?????3?1(magicNumber=3)????????????",
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
                designNotes = "?????0????2????+2???(magicNumber=2)???????????",
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
                designNotes = "?????2????6?????(magicNumber=6)+6??(block=6)???????????????",
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
                designNotes = "?????2??????50%????(magicNumber=50)+??3??????????????",
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
                designNotes = "????????1??8????????????3????+4???",
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
                designNotes = "???????0????4HP??????????????",
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
                designNotes = "???????1????6??+??1????",
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
                designNotes = "???????0??????+2???????????????",
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
                designNotes = "????????2??14???+????????????",
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
                designNotes = "???????1????????????-3?????????",
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
                designNotes = "????????2??12???+??????????????",
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
                designNotes = "???????1????5??+??1?????????????",
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
                designNotes = "????????3??20????????????",
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
                designNotes = "???????1????5HP+???5???????????",
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
                designNotes = "???????0??????+1????+??1??????????????",
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
                designNotes = "????????2????14????????????",
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
                designNotes = "?????????????????????????????-1HP",
                cost = 0,
                cardType = CardType.Curse,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "CurseDecayEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.玄甲,
                designNotes = "??????????????????????-1?????????_?????",
                cost = 0,
                cardType = CardType.Curse,
                rarity = CardRarity.Common,
                tags = new List<CardTag> { },
                effectIds = new List<string> { "CurseFogEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.诅咒_衰败,
                designNotes = "?????????????????????-1",
                cost = 0,
                cardType = CardType.Curse,
                rarity = CardRarity.Cursed,
                tags = new List<CardTag> { CardTag.Curse },
                effectIds = new List<string> { "CurseChainsEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.诅咒_迷雾,
                designNotes = "????????????????????????-1HP",
                cost = 0,
                cardType = CardType.Curse,
                rarity = CardRarity.Cursed,
                tags = new List<CardTag> { CardTag.Curse },
                effectIds = new List<string> { "CurseDevourEffect" }
            });

            config.entries.Add(new CardBalanceEntry
            {
                cardName = CardName.诅咒_枷锁,
                designNotes = "???????????????????????????????",
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



