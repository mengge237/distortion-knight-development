using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 检视效果：查看抽牌堆顶部的若干张卡牌，并可按优先级排序
    /// </summary>
    [CreateAssetMenu(fileName = "InspectEffect", menuName = "MutationChess/Card Effects/Inspect")]
    public class InspectEffect : CardEffect
    {
        [Header("检视配置")]
        [Tooltip("检视抽牌堆顶部的卡牌数量")]
        [Min(1)]
        public int inspectCount = 3;

        [Tooltip("是否启用AI自动排序，将最优卡牌置顶")]
        public bool autoSortBestToTop = true;

        [Tooltip("卡牌类型优先级排序（越靠前优先级越高）")]
        public List<CardType> priorityOrder = new List<CardType>
        {
            CardType.Attack,
            CardType.Defense,
            CardType.Skill,
            CardType.Power
        };

        public override string GetDescription(Card card)
        {
            int count = (card != null && card.magicNumber > 0) ? card.magicNumber : inspectCount;
            return $"查看抽牌堆顶 {count} 张牌";
        }

        public override void Execute(CombatContext context)
        {
            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[InspectEffect] HandManager 为空");
                return;
            }

            List<Card> drawPile = handManager.GetDrawPile();
            if (drawPile.Count == 0)
            {
                GameLogger.Log("[InspectEffect] 抽牌堆为空，无可检视的卡牌");
                return;
            }

            int actualCount = Mathf.Min(inspectCount, drawPile.Count);
            List<Card> topCards = new List<Card>();


            for (int i = 0; i < actualCount; i++)
            {
                topCards.Add(drawPile[i]);
            }


            GameLogger.Log($"[InspectEffect] 检视顶部 {actualCount} 张卡牌：");
            for (int i = 0; i < topCards.Count; i++)
            {
                GameLogger.Log($"  [{i + 1}] {topCards[i].cardName} ({topCards[i].cardType})");
            }

            if (autoSortBestToTop)
            {

                topCards.Sort((a, b) =>
                {
                    int priorityA = priorityOrder.IndexOf(a.cardType);
                    int priorityB = priorityOrder.IndexOf(b.cardType);
                    if (priorityA < 0) priorityA = int.MaxValue;
                    if (priorityB < 0) priorityB = int.MaxValue;
                    return priorityA.CompareTo(priorityB);
                });

                GameLogger.Log("[InspectEffect] 排序后的卡牌顺序：");
                for (int i = 0; i < topCards.Count; i++)
                {
                    GameLogger.Log($"  [{i + 1}] {topCards[i].cardName}");
                }


                for (int i = 0; i < actualCount; i++)
                {
                    drawPile[i] = topCards[i];
                }
            }

            handManager.UpdatePileCountUI();
        }
    }
}


