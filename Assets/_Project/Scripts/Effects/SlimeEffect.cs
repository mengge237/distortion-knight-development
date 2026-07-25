using UnityEngine;
using MutationChess.UI;
using System.Collections.Generic;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "SlimeEffect", menuName = "MutationChess/Card Effects/Slime Effect")]
    public class SlimeEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context == null || context.sourceCard == null)
            {
                Debug.LogWarning("SlimeEffect: 上下文或卡牌为空");
                return;
            }

            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                Debug.LogWarning("SlimeEffect: HandManager 未找到");
                return;
            }

            Card playedCard = context.sourceCard;
            int playedCardIndex = -1;

            List<Card> handCards = handManager.GetHandCards();
            for (int i = 0; i < handCards.Count; i++)
            {
                if (handCards[i] == playedCard)
                {
                    playedCardIndex = i;
                    break;
                }
            }

            if (playedCardIndex < 0)
            {
                Debug.LogWarning("SlimeEffect: 找不到打出的卡牌");
                return;
            }

            List<Card> adjacentCards = new List<Card>();

            int leftIndex = playedCardIndex - 1;
            int rightIndex = playedCardIndex + 1;

            if (leftIndex >= 0 && leftIndex < handCards.Count)
            {
                adjacentCards.Add(handCards[leftIndex]);
            }

            if (rightIndex >= 0 && rightIndex < handCards.Count)
            {
                adjacentCards.Add(handCards[rightIndex]);
            }

            if (adjacentCards.Count == 0)
            {
                Debug.Log("SlimeEffect: 没有相邻卡牌");
                return;
            }

            foreach (var adjCard in adjacentCards)
            {
                if (adjCard != null && adjCard.HasKeyword("粘液"))
                {
                    Debug.Log($"SlimeEffect: 触发相邻卡牌 {adjCard.cardName} 的效果");

                    CombatContext tempContext = new CombatContext(
                        context.battleManager,
                        context.targetEnemy,
                        context.targetPlayer,
                        adjCard
                    );

                    adjCard.ExecuteEffects(tempContext);
                }
            }
        }
    }
}
