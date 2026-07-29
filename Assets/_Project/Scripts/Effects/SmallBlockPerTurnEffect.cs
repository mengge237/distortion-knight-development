using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "SmallBlockPerTurnEffect", menuName = "MutationChess/Relic Effects/Small Block Per Turn")]
    public class SmallBlockPerTurnEffect : CardEffect
    {
        [Tooltip("")]
        public int blockAmount = 3;

        public override void Execute(CombatContext context)
        {
            if (context == null || context.battleManager == null)
            {
                GameLogger.LogWarning("[SmallBlockPerTurnEffect] context  battleManager ");
                return;
            }


            context.battleManager.PlayerBlock(blockAmount);
            GameLogger.Log($"[SmallBlockPerTurnEffect]  {blockAmount} ");
        }
    }
}


