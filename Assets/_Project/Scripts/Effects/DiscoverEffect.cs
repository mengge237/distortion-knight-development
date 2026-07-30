using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// ����Ч�������ƶѻ򿨳��������ȡ N �ſ��ƹ����ѡ��
    /// ѡ�еĿ��ƽ��������ƣ����࿨�ƽ�������
    /// </summary>
    [CreateAssetMenu(fileName = "DiscoverEffect", menuName = "MutationChess/Card Effects/Discover")]
    public class DiscoverEffect : CardEffect
    {
        public enum DiscoverSource
        {
            DrawPile,           // �ӳ��ƶѷ���
            DiscardPile,        // �����ƶѷ���
            ByTag,              // ����ǩ���˿��Ƴ�
            ByFaction,          // ����Ӫ���˿��Ƴ�
            ByRarity,           // ��ϡ�жȹ��˿��Ƴ�
            AllCards,           // �����п��Ƴط���
        }

        [Header("��������")]
        [Tooltip("���ֿ��Ƶ���Դ����")]
        public DiscoverSource source = DiscoverSource.ByTag;

        [Tooltip("���ֵĿ�������")]
        [Min(1)]
        public int discoverCount = 3;

        [Tooltip("�������ƵĿ�������")]
        [Min(1)]
        public int cardsToHand = 1;

        [Header("������")]
        [Tooltip("����ǩ���ˣ�source=ByTag ʱ��Ч")]
        public CardTag filterTag = CardTag.None;

        [Tooltip("����Ӫ���ˣ�source=ByFaction ʱ��Ч")]
        public CardFaction filterFaction = CardFaction.None;

        [Tooltip("��ϡ�жȹ��ˣ�source=ByRarity ʱ��Ч")]
        public CardRarity filterRarity = CardRarity.Common;

        [Tooltip("�Ƿ��ų��������")]
        public bool excludeSelf = true;

        public override string GetDescription(Card card)
        {
            int actualDiscover = (card != null && card.magicNumber > 0) ? card.magicNumber : discoverCount;
            return $"���� {actualDiscover} �ſ��ƣ�ѡ {cardsToHand} �ż�������";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[DiscoverEffect] HandManager ������");
                return;
            }

            List<Card> pool = BuildDiscoverPool(context);
            if (pool.Count == 0)
            {
                GameLogger.Log("[DiscoverEffect] ���Ƴ�Ϊ��");
                return;
            }

            // �����ȡ N �ź�ѡ����
            List<Card> candidates = new List<Card>(pool);
            ShuffleList(candidates);
            int actualCount = Mathf.Min(discoverCount, candidates.Count);

            GameLogger.Log($"[DiscoverEffect] �� {pool.Count} �ſ����г�ȡ {actualCount} ��");

            // ��ǰ cardsToHand �ż�������
            // ���࿨�ƿ�ѡ������Ż��ƶ�
            int toHand = Mathf.Min(cardsToHand, actualCount);
            for (int i = 0; i < toHand; i++)
            {
                Card selected = candidates[i];
                if (selected != null)
                {
                    handManager.AddCardToHand(selected);
                    GameLogger.Log($"[DiscoverEffect] �����Ƽ�������: {selected.cardName}");
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

            // �ų�����
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
