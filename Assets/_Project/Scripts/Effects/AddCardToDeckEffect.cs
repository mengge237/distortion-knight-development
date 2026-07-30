using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 添加卡牌到牌组效果
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

        [Header("卡牌选择模式")]
        [Tooltip("卡牌选择方式")]
        public CardSelectionMode selectionMode = CardSelectionMode.SpecificName;

        [Header("指定卡牌名")]
        [Tooltip("要添加的卡牌名称（CardName 枚举值）")]
        public string cardNameToAdd = "";

        [Header("类型筛选")]
        [Tooltip("按类型随机时的筛选类型")]
        public CardType filterCardType = CardType.Attack;

        [Header("标签筛选")]
        [Tooltip("按标签随机时的筛选标签")]
        public CardTag filterTag = CardTag.Corrupt;

        [Header("稀有度筛选")]
        [Tooltip("按稀有度随机时的筛选稀有度")]
        public CardRarity filterRarity = CardRarity.Common;

        [Header("数量配置")]
        [Tooltip("添加卡牌的数量")]
        [Min(1)]
        public int count = 1;

        [Tooltip("卡牌添加的位置")]
        public AddLocation location = AddLocation.DrawPileRandom;

        [Tooltip("是否在描述中显示卡牌名称")]
        public bool showCardNameInDescription = true;

        public override void Execute(CombatContext context)
        {
            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[AddCardToDeckEffect] HandManager 为空");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Card newCard = CreateCardByMode(context);
                if (newCard == null)
                {
                    GameLogger.LogError($"[AddCardToDeckEffect] 创建卡牌失败（模式: {selectionMode}）");
                    continue;
                }

                AddCardToLocation(handManager, newCard);
                GameLogger.Log($"[AddCardToDeckEffect] 添加卡牌: {newCard.cardName} -> {location}");
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
                GameLogger.LogError("[AddCardToDeckEffect] SelfCopy 需要 sourceCard");
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
                GameLogger.LogError($"[AddCardToDeckEffect] 无法解析卡牌名: {cardNameToAdd}");
                return null;
            }

            Card card = CardData.CreateCard(parsedName);
            if (card == null)
            {
                GameLogger.LogError($"[AddCardToDeckEffect] 创建卡牌失败: {cardNameToAdd}");
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
                GameLogger.LogWarning($"[AddCardToDeckEffect] 未找到类型为 {filterCardType} 的卡牌");
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
                GameLogger.LogWarning($"[AddCardToDeckEffect] 未找到标签为 {filterTag} 的卡牌");
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
                GameLogger.LogWarning($"[AddCardToDeckEffect] 未找到稀有度为 {filterRarity} 的卡牌");
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
