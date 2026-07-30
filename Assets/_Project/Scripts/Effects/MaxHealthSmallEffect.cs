using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "MaxHealthSmallEffect", menuName = "MutationChess/Relic Effects/Max Health Small")]
    public class MaxHealthSmallEffect : CardEffect
    {
        [Tooltip("增加的最大生命值")]
        public int healthBonus = 15;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;


            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[MaxHealthSmallEffect] playerData 为空");
                return;
            }


            playerData.maxHealth += healthBonus;
            playerData.currentHealth += healthBonus;
            GameLogger.Log($"[MaxHealthSmallEffect] 最大生命+{healthBonus}，当前: {playerData.currentHealth}/{playerData.maxHealth}");
        }
    }
}


