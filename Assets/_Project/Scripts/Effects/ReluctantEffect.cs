using UnityEngine;
using MutationChess.UI;
using System.Collections.Generic;

namespace MutationChess.Core
{
    /// <summary>



    /// </summary>
    [CreateAssetMenu(fileName = "ReluctantEffect", menuName = "MutationChess/Card Effects/Reluctant Effect")]
    public class ReluctantEffect : CardEffect
    {
        [Tooltip("鿨")]
        public int drawCount = 1;

        public override void Execute(CombatContext context)
        {
            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("ReluctantEffect: HandManager ");
                return;
            }


            List<Card> drawPile = handManager.GetDrawPile();
            int drawn = 0;

            for (int i = 0; i < drawPile.Count && drawn < drawCount; i++)
            {
                Card c = drawPile[i];
                if (c != null && (c.HasTag(CardTag.Reluctant) || c.faction == CardFaction.Reluctant))
                {
                    handManager.RemoveCardFromDrawPile(i);
                    handManager.AddCardToHand(c);
                    GameLogger.Log($"ReluctantEffect: 鵽 {c.cardName}");
                    drawn++;
                    i--;
                }
            }

            if (drawn == 0)
                GameLogger.Log("ReluctantEffect: ");
        }
    }
}


