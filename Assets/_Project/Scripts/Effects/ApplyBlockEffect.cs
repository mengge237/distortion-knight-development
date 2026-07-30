using UnityEngine;
using MutationChess.Core;
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
                GameLogger.LogError("ApplyBlockEffect: battleManager Ϊ��");
                return;
            }

            if (context.sourceCard == null)
            {
                GameLogger.LogError("ApplyBlockEffect: sourceCard Ϊ��");
                return;
            }

            int block = context.sourceCard.block;
            context.battleManager.PlayerBlock(block);
        }
    }
}
