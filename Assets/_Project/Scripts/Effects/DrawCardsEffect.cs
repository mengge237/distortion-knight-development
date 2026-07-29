using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DrawCards", menuName = "MutationChess/Effects/Draw Cards")]
    public class DrawCardsEffect : CardEffect
    {
        [Header("")]
        [Tooltip("0magicNumber")]
        public int drawCount = 1;

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager != null && context.sourceCard != null)
            {
                int actualDraw = drawCount > 0 ? drawCount : (context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : 1);
                handManager.DrawCards(actualDraw);
            }
        }
    }
}