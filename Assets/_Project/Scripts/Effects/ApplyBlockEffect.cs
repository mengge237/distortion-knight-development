using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyBlock", menuName = "MutationChess/Effects/Apply Block")]
    public class ApplyBlockEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context.battleManager == null)
            {
                Debug.LogError("ApplyBlockEffect: battleManager Îª¿Õ£¡");
                return;
            }

            if (context.sourceCard == null)
            {
                Debug.LogError("ApplyBlockEffect: sourceCard Îª¿Õ£¡");
                return;
            }

            int block = context.sourceCard.block;
            context.battleManager.PlayerBlock(block);
        }
    }
}