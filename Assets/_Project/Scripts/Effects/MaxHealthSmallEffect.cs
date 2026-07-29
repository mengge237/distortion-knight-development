using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "MaxHealthSmallEffect", menuName = "MutationChess/Relic Effects/Max Health Small")]
    public class MaxHealthSmallEffect : CardEffect
    {
        [Tooltip("")]
        public int healthBonus = 15;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;


            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[MaxHealthSmallEffect] playerData ");
                return;
            }


            playerData.maxHealth += healthBonus;
            playerData.currentHealth += healthBonus;
            GameLogger.Log($"[MaxHealthSmallEffect]  +{healthBonus}: {playerData.currentHealth}/{playerData.maxHealth}");
        }
    }
}


