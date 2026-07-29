using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "GainStrength2BattleStartEffect", menuName = "MutationChess/Relic Effects/Gain Strength 2 Battle Start")]
    public class GainStrength2BattleStartEffect : CardEffect
    {
        [Tooltip("")]
        public int strengthAmount = 2;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;


            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[GainStrength2BattleStartEffect] playerData ");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Strength, amount = strengthAmount, duration = -1 });
            GameLogger.Log($"[GainStrength2BattleStartEffect]  {strengthAmount} ");
        }
    }
}


