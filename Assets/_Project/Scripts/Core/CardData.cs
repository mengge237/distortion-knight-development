using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    public static class CardData
    {
        private static readonly Dictionary<CardName, CardTemplate> Templates = new Dictionary<CardName, CardTemplate>
        {
            [CardName.攻击] = new CardTemplate
            {
                cardName = "攻击",
                cost = 1,
                damage = 6,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                description = "造成 6 点伤害",
                effectIds = new List<string> { "DealDamage" },
                cardArtPath = "CardArt/攻击",
            },

            [CardName.防御] = new CardTemplate
            {
                cardName = "防御",
                cost = 1,
                block = 5,
                cardType = CardType.Defense,
                rarity = CardRarity.Common,
                description = "获得 5 点格挡",
                effectIds = new List<string> { "ApplyBlock" },
                cardArtPath = "CardArt/防御",
            },

            [CardName.痛击] = new CardTemplate
            {
                cardName = "痛击",
                cost = 2,
                damage = 8,
                magicNumber = 2,
                cardType = CardType.Attack,
                rarity = CardRarity.Common,
                description = "造成 8 点伤害，施加 2 层易伤",
                effectIds = new List<string> { "DealDamage", "ApplyVulnerability" },
                cardArtPath = "CardArt/痛击",
            },

            [CardName.后发制人] = new CardTemplate
            {
                cardName = "后发制人",
                cost = 1,
                magicNumber = 12,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                description = "下回合造成 12 点伤害",
                effectIds = new List<string> { "DealDamageNextTurn" },
                cardArtPath = "CardArt/后发制人",
            },

            [CardName.暮光仪式] = new CardTemplate
            {
                cardName = "暮光仪式",
                cost = 2,
                magicNumber = 3,
                cardType = CardType.Power,
                rarity = CardRarity.Rare,
                description = "获得持续 3 回合的临时力量",
                effectIds = new List<string> { "ApplyTemporaryStrength" },
                cardArtPath = "CardArt/暮光仪式",
            },

            [CardName.预知仪式] = new CardTemplate
            {
                cardName = "预知仪式",
                cost = 1,
                block = 8,
                magicNumber = 1,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                description = "获得 8 点格挡，抽 1 张牌",
                effectIds = new List<string> { "ApplyBlock", "DrawCards" },
                cardArtPath = "CardArt/预知仪式",
            },

            [CardName.加固] = new CardTemplate
            {
                cardName = "加固",
                cost = 1,
                magicNumber = 3,
                cardType = CardType.Defense,
                rarity = CardRarity.Uncommon,
                description = "获得 3 点敏捷",
                effectIds = new List<string> { "ApplyDexterity" },
                cardArtPath = "CardArt/加固",
            },

            [CardName.粘液打击] = new CardTemplate
            {
                cardName = "粘液打击",
                cost = 1,
                damage = 5,
                cardType = CardType.Attack,
                rarity = CardRarity.Uncommon,
                description = "造成 5 点伤害，粘液：触发相邻卡牌效果",
                effectIds = new List<string> { "DealDamage", "SlimeEffect" },
                cardArtPath = "CardArt/粘液打击",
            },

            [CardName.粘液防御] = new CardTemplate
            {
                cardName = "粘液防御",
                cost = 1,
                block = 4,
                cardType = CardType.Defense,
                rarity = CardRarity.Uncommon,
                description = "获得 4 点格挡，粘液：触发相邻卡牌效果",
                effectIds = new List<string> { "ApplyBlock", "SlimeEffect" },
                cardArtPath = "CardArt/粘液防御",
            },

            [CardName.粘液附体] = new CardTemplate
            {
                cardName = "粘液附体",
                cost = 1,
                damage = 0,
                block = 0,
                magicNumber = 2,
                cardType = CardType.Skill,
                rarity = CardRarity.Rare,
                description = "抽 2 张牌，粘液：触发相邻卡牌效果",
                effectIds = new List<string> { "DrawCards", "SlimeEffect" },
                cardArtPath = "CardArt/粘液附体",
            },

            [CardName.不舍之念] = new CardTemplate
            {
                cardName = "不舍之念",
                cost = 1,
                damage = 0,
                block = 0,
                magicNumber = 0,
                cardType = CardType.Skill,
                rarity = CardRarity.Uncommon,
                description = "从牌库中抽一张不舍卡牌",
                effectIds = new List<string> { "ReluctantEffect" },
                cardArtPath = "CardArt/不舍之念",
            },

            [CardName.不舍连击] = new CardTemplate
            {
                cardName = "不舍连击",
                cost = 2,
                damage = 10,
                cardType = CardType.Attack,
                rarity = CardRarity.Rare,
                description = "造成 10 点伤害，从牌库中抽一张不舍卡牌",
                effectIds = new List<string> { "DealDamage", "ReluctantEffect" },
                cardArtPath = "CardArt/不舍连击",
            },
        };

        private static Dictionary<string, CardEffect> effectCache = new Dictionary<string, CardEffect>();

        public static CardTemplate GetTemplate(CardName cardName)
        {
            if (Templates.TryGetValue(cardName, out CardTemplate template))
                return template;

            Debug.LogError($"未找到卡牌模板: {cardName}");
            return default;
        }

        public static Card CreateCard(CardName cardName)
        {
            if (!Templates.TryGetValue(cardName, out CardTemplate template))
            {
                Debug.LogError($"未知卡牌名称: {cardName}");
                return null;
            }

            Card card = new Card(
                template.cardName,
                template.cardType == CardType.Attack ? template.damage : template.block,
                template.cardType,
                template.rarity,
                template.cost,
                template.magicNumber
            );

            card.description = template.description;
            card.cardId = template.cardId;

            if (!string.IsNullOrEmpty(template.cardArtPath))
            {
                Sprite originalSprite = Resources.Load<Sprite>(template.cardArtPath);
                if (originalSprite != null)
                {
                    card.cardArt = originalSprite;
                }
                else
                {
                    Debug.LogWarning($"未找到卡牌图片: {template.cardArtPath}");
                }
            }

            if (template.effectIds != null && template.effectIds.Count > 0)
            {
                foreach (string effectId in template.effectIds)
                {
                    CardEffect effect = LoadEffect(effectId);
                    if (effect != null)
                    {
                        card.effects.Add(effect);
                    }
                    else
                    {
                        Debug.LogError($"加载效果失败: {effectId} 到卡牌 {template.cardName}");
                    }
                }
            }

            return card;
        }

        private static CardEffect LoadEffect(string effectId)
        {
            if (effectCache.TryGetValue(effectId, out CardEffect cachedEffect))
            {
                return cachedEffect;
            }

            CardEffect effect = null;

            string effectPath = $"Effects/{effectId}";
            effect = Resources.Load<CardEffect>(effectPath);

            if (effect == null)
            {
                effect = Resources.Load<CardEffect>($"CardEffects/{effectId}");
            }

            if (effect == null)
            {
                effect = Resources.Load<CardEffect>(effectId);
            }

            if (effect != null)
            {
                effectCache[effectId] = effect;
            }
            else
            {
                Debug.LogError($"无法加载效果: {effectId}");
            }

            return effect;
        }

        public static List<CardName> GetAllCardNames()
        {
            return new List<CardName>(Templates.Keys);
        }
    }

    public class CardTemplate
    {
        public string cardName;
        public int cost;
        public int damage;
        public int block;
        public int magicNumber;
        public CardType cardType;
        public CardRarity rarity;
        public string description;
        public List<string> effectIds;
        public string cardId;
        public string cardArtPath;

        public CardTemplate()
        {
        }
    }
}
