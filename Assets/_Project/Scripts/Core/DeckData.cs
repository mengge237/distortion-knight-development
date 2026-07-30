using UnityEngine;
using System.Collections.Generic;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DeckData", menuName = "MutationChess/Deck Data")]
    public class DeckData : ScriptableObject
    {
        [Header("牌组基础信息")]
        public string deckName = "";

        [Header("卡牌列表")]
        [Tooltip("牌组中包含的卡牌名称列表")]
        public List<CardName> cardNames = new List<CardName>();

        [Header("统计信息")]
        [SerializeField] private int totalCards;

        public List<Card> GetAllCards()
        {
            List<Card> cards = new List<Card>();
            foreach (CardName name in cardNames)
            {
                Card card = CardData.CreateCard(name);
                if (card != null)
                    cards.Add(card);
            }
            return cards;
        }

        public List<Card> GetRandomCards(int count)
        {
            if (cardNames.Count == 0) return new List<Card>();

            List<Card> result = new List<Card>();
            List<CardName> tempPool = new List<CardName>(cardNames);

            for (int i = 0; i < count && tempPool.Count > 0; i++)
            {
                int index = Random.Range(0, tempPool.Count);
                CardName name = tempPool[index];
                Card card = CardData.CreateCard(name);
                if (card != null)
                    result.Add(card);
                tempPool.RemoveAt(index);
            }

            return result;
        }

        public List<Card> GetDeckCopy()
        {
            List<Card> copy = new List<Card>();
            foreach (CardName name in cardNames)
            {
                Card card = CardData.CreateCard(name);
                if (card != null)
                    copy.Add(card);
            }
            return copy;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            totalCards = cardNames.Count;
        }
#endif
    }
}