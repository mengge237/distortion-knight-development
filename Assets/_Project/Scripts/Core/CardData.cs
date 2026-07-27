using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    public static class CardData
    {
        private static Dictionary<string, CardDataAsset> assetCache = new Dictionary<string, CardDataAsset>();
        private static Dictionary<string, CardEffect> effectCache = new Dictionary<string, CardEffect>();

        private static CardDataAsset LoadAsset(CardName cardName)
        {
            string key = cardName.ToString();
            if (assetCache.TryGetValue(key, out CardDataAsset cached))
                return cached;

            string path = $"Cards/{key}";
            CardDataAsset asset = Resources.Load<CardDataAsset>(path);

            if (asset != null)
            {
                assetCache[key] = asset;
                Debug.Log($"[CardData] 载入卡牌资产: {path} | cardArtPath={asset.cardArtPath ?? "null"} | effects={asset.effectIds?.Count ?? 0}");
            }
            else
            {
                Debug.LogError($"[CardData] 无法加载卡牌资产: {path}，请在 Unity 中确认 .asset 文件存在并已 Reimport");
            }

            return asset;
        }

        public static CardDataAsset GetTemplate(CardName cardName)
        {
            return LoadAsset(cardName);
        }

        public static Card CreateCard(CardName cardName)
        {
            CardDataAsset asset = LoadAsset(cardName);
            if (asset == null)
            {
                Debug.LogError($"未找到卡牌: {cardName}");
                return null;
            }

            Card card = new Card(
                asset.cardName,
                asset.cardType == CardType.Attack ? asset.damage : asset.block,
                asset.cardType,
                asset.rarity,
                asset.cost,
                asset.magicNumber
            );

            card.description = asset.description;
            card.faction = asset.faction;

            if (!string.IsNullOrEmpty(asset.cardArtPath))
            {
                Sprite originalSprite = Resources.Load<Sprite>(asset.cardArtPath);
                if (originalSprite != null)
                {
                    card.cardArt = originalSprite;
                    Debug.Log($"[CardData] 卡牌图片加载成功: {asset.cardArtPath}");
                }
                else
                {
                    Debug.LogWarning($"[CardData] 未找到卡牌图片: {asset.cardArtPath}，请确认 Resources/CardArt/ 中有对应图片");
                }
            }
            else
            {
                Debug.LogWarning($"[CardData] 卡牌 {asset.cardName} 的 cardArtPath 为空");
            }

            if (asset.effectIds != null && asset.effectIds.Count > 0)
            {
                foreach (string effectId in asset.effectIds)
                {
                    CardEffect effect = LoadEffect(effectId);
                    if (effect != null)
                    {
                        card.effects.Add(effect);
                    }
                    else
                    {
                        Debug.LogError($"无法创建效果: {effectId} 用于卡牌 {asset.cardName}");
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
                Debug.LogError($"加载效果资源失败: {effectId}");
            }

            return effect;
        }

        public static List<CardName> GetAllCardNames()
        {
            return new List<CardName>((CardName[])Enum.GetValues(typeof(CardName)));
        }
    }
}
