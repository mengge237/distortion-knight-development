using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>



    /// </summary>
    [CreateAssetMenu(fileName = "AddCardToDeckEffect", menuName = "MutationChess/Card Effects/Add Card To Deck")]
    public class AddCardToDeckEffect : CardEffect
    {
        public enum AddLocation
        {
            DrawPileTop,
            DrawPileBottom,
            DrawPileRandom,
            DiscardPile,
            Hand,
        }

        public enum CardSelectionMode
        {
            SpecificName,
            SelfCopy,
            RandomByType,
            RandomByTag,
            RandomByRarity,
        }

        [Header("")]
        [Tooltip("")]
        public CardSelectionMode selectionMode = CardSelectionMode.SpecificName;

        [Header("")]
        [Tooltip("CardName ")]
        public string cardNameToAdd = "";

        [Header("")]
        [Tooltip("")]
        public CardType filterCardType = CardType.Attack;

        [Header("")]
        [Tooltip("")]
        public CardTag filterTag = CardTag.Corrupt;

        [Header("")]
        [Tooltip("")]
        public CardRarity filterRarity = CardRarity.Common;

        [Header("")]
        [Tooltip("")]
        [Min(1)]
        public int count = 1;

        [Tooltip("")]
        public AddLocation location = AddLocation.DrawPileRandom;

        [Tooltip("")]
        public bool showCardNameInDescription = true;

        public override void Execute(CombatContext context)
        {
            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[AddCardToDeckEffect] HandManager ");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Card newCard = CreateCardByMode(context);
                if (newCard == null)
                {
                    GameLogger.LogError($"[AddCardToDeckEffect]  (: {selectionMode})");
                    continue;
                }

                AddCardToLocation(handManager, newCard);
                GameLogger.Log($"[AddCardToDeckEffect] : {newCard.cardName} -> {location}");
            }

            handManager.UpdatePileCountUI();
        }

        private Card CreateCardByMode(CombatContext context)
        {
            switch (selectionMode)
            {
                case CardSelectionMode.SelfCopy:
                    return CreateSelfCopy(context);

                case CardSelectionMode.SpecificName:
                    return CreateByName();

                case CardSelectionMode.RandomByType:
                    return CreateRandomByType();

                case CardSelectionMode.RandomByTag:
                    return CreateRandomByTag();

                case CardSelectionMode.RandomByRarity:
                    return CreateRandomByRarity();

                default:
                    return CreateByName();
            }
        }

        private Card CreateSelfCopy(CombatContext context)
        {
            if (context?.sourceCard == null)
            {
                GameLogger.LogError("[AddCardToDeckEffect] SelfCopysourceCard");
                return null;
            }

            if (!System.Enum.TryParse<CardName>(context.sourceCard.cardName, out CardName parsedName))
            {
                GameLogger.LogError($"[AddCardToDeckEffect] SelfCopy 无法解析卡牌名: {context.sourceCard.cardName}");
                return null;
            }

            var cardData = CardData.GetTemplate(parsedName);
            if (cardData != null)
            {
                return CardData.CreateCard(parsedName);
            }

            return null;
        }

        private Card CreateByName()
        {
            if (!System.Enum.TryParse<CardName>(cardNameToAdd, out CardName parsedName))
            {
                GameLogger.LogError($"[AddCardToDeckEffect] : {cardNameToAdd}");
                return null;
            }

            Card card = CardData.CreateCard(parsedName);
            if (card == null)
            {
                GameLogger.LogError($"[AddCardToDeckEffect] : {cardNameToAdd}");
            }
            return card;
        }

        private Card CreateRandomByType()
        {
            var allNames = CardData.GetAllCardNames();
            List<CardName> candidates = new List<CardName>();

            foreach (var name in allNames)
            {
                var template = CardData.GetTemplate(name);
                if (template != null && template.cardType == filterCardType)
                {
                    candidates.Add(name);
                }
            }

            if (candidates.Count == 0)
            {
                GameLogger.LogWarning($"[AddCardToDeckEffect]  {filterCardType} ");
                return null;
            }

            CardName randomName = candidates[Random.Range(0, candidates.Count)];
            return CardData.CreateCard(randomName);
        }

        private Card CreateRandomByTag()
        {
            var allNames = CardData.GetAllCardNames();
            List<CardName> candidates = new List<CardName>();

            foreach (var name in allNames)
            {
                var template = CardData.GetTemplate(name);
                if (template != null && template.tags != null && template.tags.Contains(filterTag))
                {
                    candidates.Add(name);
                }
            }

            if (candidates.Count == 0)
            {
                GameLogger.LogWarning($"[AddCardToDeckEffect]  {filterTag} ");
                return null;
            }

            CardName randomName = candidates[Random.Range(0, candidates.Count)];
            return CardData.CreateCard(randomName);
        }

        private Card CreateRandomByRarity()
        {
            var allNames = CardData.GetAllCardNames();
            List<CardName> candidates = new List<CardName>();

            foreach (var name in allNames)
            {
                var template = CardData.GetTemplate(name);
                if (template != null && template.rarity == filterRarity)
                {
                    candidates.Add(name);
                }
            }

            if (candidates.Count == 0)
            {
                GameLogger.LogWarning($"[AddCardToDeckEffect]  {filterRarity} ");
                return null;
            }

            CardName randomName = candidates[Random.Range(0, candidates.Count)];
            return CardData.CreateCard(randomName);
        }

        private void AddCardToLocation(HandManager handManager, Card card)
        {
            switch (location)
            {
                case AddLocation.DrawPileTop:
                    handManager.AddToDrawPileTop(card);
                    break;

                case AddLocation.DrawPileBottom:
                    handManager.AddToDrawPileBottom(card);
                    break;

                case AddLocation.DrawPileRandom:
                    handManager.AddToDrawPileRandom(card);
                    break;

                case AddLocation.DiscardPile:
                    handManager.AddToDiscardPile(card);
                    break;

                case AddLocation.Hand:
                    handManager.AddCardToHand(card);
                    break;
            }
        }
    }
}


