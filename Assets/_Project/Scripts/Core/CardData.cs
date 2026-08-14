using System;
using MutationChess.Core;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    public static class CardData
    {
        private static Dictionary<string, CardDataAsset> assetCache = new Dictionary<string, CardDataAsset>();
        private static Dictionary<string, CardEffect> effectCache = new Dictionary<string, CardEffect>();
        private static Dictionary<string, InherentEffect> inherentEffectCache = new Dictionary<string, InherentEffect>();
        private static bool allCardsLoaded = false;

        /// <summary>
        ///
        /// </summary>
        private static void LoadAllCards()
        {
            if (allCardsLoaded) return;

            CardDataAsset[] allAssets = Resources.LoadAll<CardDataAsset>("Cards");
            foreach (var asset in allAssets)
            {
                if (asset != null && !string.IsNullOrEmpty(asset.cardName))
                {
                    assetCache[asset.cardName] = asset;
                }
            }

            allCardsLoaded = true;
            GameLogger.Log($"[CardData] 已加载 {allAssets.Length} 张卡牌资产");
        }

        private static CardDataAsset LoadAsset(CardName cardName)
        {
            string key = cardName.ToString();
            if (assetCache.TryGetValue(key, out CardDataAsset cached))
                return cached;

            //
            LoadAllCards();

            if (assetCache.TryGetValue(key, out CardDataAsset loaded))
                return loaded;

            //
            allCardsLoaded = false;
            assetCache.Clear();
            LoadAllCards();

            if (assetCache.TryGetValue(key, out CardDataAsset retry))
                return retry;

            GameLogger.LogError($"[CardData] 未找到卡牌: {key}，请检查 Cards 目录下的 .asset 文件");
            return null;
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
                GameLogger.LogError($"[CardData] 无法创建卡牌: {cardName}");
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

            //
            if (asset.tags != null && asset.tags.Count > 0)
            {
                foreach (var tag in asset.tags)
                {
                    card.AddTag(tag);
                }
            }

            //
            card.bloodPerEnergy = asset.bloodPerEnergy;
            card.blockPerEnergy = asset.blockPerEnergy;

            //
            card.exhaust = asset.exhaust;
            if (card.HasTag(CardTag.Corrupt))
            {
                card.exhaust = true;
            }

            if (!string.IsNullOrEmpty(asset.cardArtPath))
            {
                Sprite originalSprite = Resources.Load<Sprite>(asset.cardArtPath);
                if (originalSprite != null)
                {
                    card.cardArt = originalSprite;
                }
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
                        GameLogger.LogError($"[CardData] 效果加载失败: {effectId}, 卡牌: {asset.cardName}");
                    }
                }
            }


            if (asset.inherentEffectIds != null && asset.inherentEffectIds.Count > 0)
            {
                foreach (string inherentId in asset.inherentEffectIds)
                {
                    InherentEffect inherent = LoadInherentEffect(inherentId);
                    if (inherent != null)
                    {
                        card.inherentEffects.Add(inherent);
                    }
                    else
                    {
                        GameLogger.LogError($"[CardData] 固有效果加载失败: {inherentId}, 卡牌: {asset.cardName}");
                    }
                }
            }


            InjectDefaultInherentEffects(card);

            card.GenerateDescription();

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
                GameLogger.LogError($"[CardData] 未找到效果: {effectId}");
            }

            return effect;
        }

        private static InherentEffect LoadInherentEffect(string effectId)
        {
            if (inherentEffectCache.TryGetValue(effectId, out InherentEffect cached))
            {
                return cached;
            }

            InherentEffect inherent = null;

            inherent = Resources.Load<InherentEffect>($"InherentEffects/{effectId}");
            if (inherent == null)
                inherent = Resources.Load<InherentEffect>($"Effects/{effectId}");
            if (inherent == null)
                inherent = Resources.Load<InherentEffect>(effectId);

            if (inherent != null)
            {
                inherentEffectCache[effectId] = inherent;
            }
            else
            {
                GameLogger.LogError($"[CardData] 未找到固有效果: {effectId}");
            }

            return inherent;
        }

        /// <summary>
        ///
        /// </summary>
        private static void InjectDefaultInherentEffects(Card card)
        {
            if (card == null) return;

            //
            var existingTags = new HashSet<CardTag>();
            if (card.inherentEffects != null)
            {
                foreach (var inherent in card.inherentEffects)
                {
                    if (inherent != null)
                        existingTags.Add(inherent.Tag);
                }
            }


            if (card.HasTag(CardTag.Slime) && !existingTags.Contains(CardTag.Slime))
            {
                var inherent = LoadInherentEffect("SlimeInherentEffect");
                if (inherent != null) card.inherentEffects.Add(inherent);
            }

            if (card.HasTag(CardTag.Reluctant) && !existingTags.Contains(CardTag.Reluctant))
            {
                var inherent = LoadInherentEffect("ReluctantInherentEffect");
                if (inherent != null) card.inherentEffects.Add(inherent);
            }


        }

        public static List<CardName> GetAllCardNames()
        {
            return new List<CardName>((CardName[])Enum.GetValues(typeof(CardName)));
        }
    }
}
