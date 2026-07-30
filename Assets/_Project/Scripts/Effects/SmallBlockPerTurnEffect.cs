using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "SmallBlockPerTurnEffect", menuName = "MutationChess/Relic Effects/Small Block Per Turn")]
    public class SmallBlockPerTurnEffect : CardEffect
    {
        [Tooltip("每回合获得的格挡值")]
        public int blockAmount = 3;

        public override void Execute(CombatContext context)
        {
            if (context == null || context.battleManager == null)
            {
                GameLogger.LogWarning("[SmallBlockPerTurnEffect] context 或 battleManager 为空");
                return;
            }


            context.battleManager.PlayerBlock(blockAmount);
            GameLogger.Log($"[SmallBlockPerTurnEffect] 获得格挡 {blockAmount} 点");
        }
    }
}


