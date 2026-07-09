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
            else
            {
                Debug.LogWarning($"卡牌 {template.cardName} 没有配置效果！");
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

            // 注意：文件名是 effectId（如 "DealDamage"），类型是 CardEffect
            string effectPath = $"Effects/{effectId}";
            effect = Resources.Load<CardEffect>(effectPath);

            if (effect == null)
            {
                Debug.LogWarning($"路径1加载失败: {effectPath}，尝试其他路径...");

                effect = Resources.Load<CardEffect>($"CardEffects/{effectId}");
            }

            if (effect == null)
            {
                Debug.LogWarning($"路径2加载失败: CardEffects/{effectId}，尝试直接加载...");

                effect = Resources.Load<CardEffect>(effectId);
            }

            if (effect != null)
            {
                effectCache[effectId] = effect;
            }
            else
            {
                Debug.LogError($"★★★ 无法加载效果: {effectId} ★★★");
                Debug.LogError($"请确保效果资产在 Resources/Effects/ 目录下，文件名为 {effectId}");
                Debug.LogError($"例如: Resources/Effects/DealDamage.asset");
            }

            return effect;
        }

        public static List<CardName> GetAllCardNames()
        {
            return new List<CardName>(Templates.Keys);
        }
    }

    public struct CardTemplate
    {
        public string cardName;
        public int cost;
        public int damage;      // 攻击伤害，非攻击卡牌为0
        public int block;       // 格挡值，非防御卡牌为0
        public int magicNumber; // 魔法数字，用于各种效果
        public CardType cardType;
        public CardRarity rarity;
        public string description;
        public List<string> effectIds;
        public string cardId;
        public string cardArtPath;
    }
}