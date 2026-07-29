using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "ApplyThornsEffect", menuName = "MutationChess/Card Effects/Apply Thorns")]
    public class ApplyThornsEffect : CardEffect
    {
        [Tooltip("")]
        public int thornsAmount = 3;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;


            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[ApplyThornsEffect] playerData ");
                return;
            }


            int amount = thornsAmount;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                amount = context.sourceCard.magicNumber;
            }


            playerData.AddBuff(new Buff { type = BuffType.Thorns, amount = amount, duration = -1 });
            GameLogger.Log($"[ApplyThornsEffect]  {amount} 㷴");
        }
    }
}


