using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DrawCards", menuName = "MutationChess/Effects/Draw Cards")]
    public class DrawCardsEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager != null && context.sourceCard != null)
            {
                int drawCount = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : 2;
                handManager.DrawCards(drawCount);
            }
        }
    }
}