using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ExhaustCard", menuName = "MutationChess/Effects/Exhaust Card")]
    public class ExhaustCardEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context.sourceCard == null) return;
            //
            //
            context.sourceCard.exhaust = true;
            GameLogger.Log($"[ExhaustCardEffect] {context.sourceCard.cardName} ");
        }
    }
}