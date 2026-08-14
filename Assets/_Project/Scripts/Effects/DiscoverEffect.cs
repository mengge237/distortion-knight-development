using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 发现效果：从卡牌池或抽牌堆中随机抽取 N 张卡牌供玩家选择。
    /// 选中的卡牌加入手牌，其余卡牌返回牌堆。
    /// </summary>
    [CreateAssetMenu(fileName = "DiscoverEffect", menuName = "MutationChess/Card Effects/Discover")]
    public class DiscoverEffect : CardEffect
    {
        public enum DiscoverSource
        {
            DrawPile,           // 从抽牌堆发现
            DiscardPile,        // 从弃牌堆发现
            ByTag,              // 按标签过滤卡牌池
            ByFaction,          // 按阵营过滤卡牌池
            ByRarity,           // 按稀有度过滤卡牌池
            AllCards,           // 从所有卡牌池发现
        }

        [Header("发现配置")]
        [Tooltip("发现卡牌的来源类型")]
        public DiscoverSource source = DiscoverSource.ByTag;

        [Tooltip("发现的候选卡牌数量")]
        [Min(1)]
        public int discoverCount = 3;

        [Tooltip("加入手牌的卡牌数量")]
        [Min(1)]
        public int cardsToHand = 1;

        [Header("过滤器")]
        [Tooltip("按标签过滤（source=ByTag 时有效）")]
        public CardTag filterTag = CardTag.None;

        [Tooltip("按阵营过滤（source=ByFaction 时有效）")]
        public CardFaction filterFaction = CardFaction.None;

        [Tooltip("按稀有度过滤（source=ByRarity 时有效）")]
        public CardRarity filterRarity = CardRarity.Common;

        [Tooltip("是否排除当前卡牌")]
        public bool excludeSelf = true;

        public override string GetDescription(Card card)
        {
            int actualDiscover = (card != null && card.magicNumber > 0) ? card.magicNumber : discoverCount;
            return $"发现 {actualDiscover} 张卡牌，选 {cardsToHand} 张加入手牌";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[DiscoverEffect] HandManager 不存在");
                return;
            }

            List<Card> pool = BuildDiscoverPool(context);
            if (pool.Count == 0)
            {
                GameLogger.Log("[DiscoverEffect] 卡牌池为空");
                return;
            }

            // 随机抽取 N 张候选卡牌
            List<Card> candidates = new List<Card>(pool);
            ShuffleList(candidates);
            int actualCount = Mathf.Min(discoverCount, candidates.Count);

            GameLogger.Log($"[DiscoverEffect] 从 {pool.Count} 张卡牌中抽取 {actualCount} 张");

            // 取前 cardsToHand 张加入手牌
            // 其余卡牌可选回弃牌堆
            int toHand = Mathf.Min(cardsToHand, actualCount);
            for (int i = 0; i < toHand; i++)
            {
                Card selected = candidates[i];
                if (selected != null)
                {
                    handManager.AddCardToHand(selected);
                    GameLogger.Log($"[DiscoverEffect] 发现卡牌加入手牌: {selected.cardName}");
                }
            }
        }

        private List<Card> BuildDiscoverPool(CombatContext context)
        {
            List<Card> pool = new List<Card>();
            HandManager handManager = HandManager.Instance;
            if (handManager == null) return pool;

            switch (source)
            {
                case DiscoverSource.DrawPile:
                    pool.AddRange(handManager.GetDrawPile());
                    break;

                case DiscoverSource.DiscardPile:
                    pool.AddRange(handManager.GetDiscardPile());
                    break;

                case DiscoverSource.ByTag:
                    pool = GetAllCardsByTag(filterTag);
                    break;

                case DiscoverSource.ByFaction:
                    pool = GetAllCardsByFaction(filterFaction);
                    break;

                case DiscoverSource.ByRarity:
                    pool = GetAllCardsByRarity(filterRarity);
                    break;

                case DiscoverSource.AllCards:
                    pool = GetAllCardsFromPoolConfig();
                    break;
            }

            // 排除自身
            if (excludeSelf && context?.sourceCard != null)
            {
                pool.RemoveAll(c => c == context.sourceCard);
            }

            return pool;
        }

        private List<Card> GetAllCardsByTag(CardTag tag)
        {
            var allCards = GetAllCardsFromPoolConfig();
            return allCards.Where(c => c != null && c.HasTag(tag)).ToList();
        }

        private List<Card> GetAllCardsByFaction(CardFaction faction)
        {
            var allCards = GetAllCardsFromPoolConfig();
            return allCards.Where(c => c != null && c.faction == faction).ToList();
        }

        private List<Card> GetAllCardsByRarity(CardRarity rarity)
        {
            var allCards = GetAllCardsFromPoolConfig();
            return allCards.Where(c => c != null && c.rarity == rarity).ToList();
        }

        private List<Card> GetAllCardsFromPoolConfig()
        {
            List<Card> result = new List<Card>();
            var poolConfig = RewardPoolManager.Config;
            if (poolConfig == null || poolConfig.allCards == null) return result;

            foreach (var asset in poolConfig.allCards)
            {
                if (asset == null) continue;

                Card card = new Card(
                    asset.cardName,
                    asset.cardType == CardType.Attack ? asset.damage : asset.block,
                    asset.cardType,
                    asset.rarity,
                    asset.cost,
                    asset.magicNumber
                );
                card.faction = asset.faction;
                if (asset.tags != null)
                {
                    foreach (var t in asset.tags) card.AddTag(t);
                }
                card.bloodPerEnergy = asset.bloodPerEnergy;
                card.blockPerEnergy = asset.blockPerEnergy;
                result.Add(card);
            }

            return result;
        }

        private void ShuffleList(List<Card> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Card temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }
    }
}
