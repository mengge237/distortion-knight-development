using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "InspectEffect", menuName = "MutationChess/Card Effects/Inspect")]
    public class InspectEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        [Min(1)]
        public int inspectCount = 3;

        [Tooltip("AI")]
        public bool autoSortBestToTop = true;

        [Tooltip("")]
        public List<CardType> priorityOrder = new List<CardType>
        {
            CardType.Attack,
            CardType.Defense,
            CardType.Skill,
            CardType.Power
        };

        public override void Execute(CombatContext context)
        {
            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[InspectEffect] HandManager ");
                return;
            }

            List<Card> drawPile = handManager.GetDrawPile();
            if (drawPile.Count == 0)
            {
                GameLogger.Log("[InspectEffect] ");
                return;
            }

            int actualCount = Mathf.Min(inspectCount, drawPile.Count);
            List<Card> topCards = new List<Card>();


            for (int i = 0; i < actualCount; i++)
            {
                topCards.Add(drawPile[i]);
            }


            GameLogger.Log($"[InspectEffect]  {actualCount} :");
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

                GameLogger.Log("[InspectEffect] :");
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


