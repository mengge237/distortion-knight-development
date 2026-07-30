using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "GainStrength2BattleStartEffect", menuName = "MutationChess/Relic Effects/Gain Strength 2 Battle Start")]
    public class GainStrength2BattleStartEffect : CardEffect
    {
        [Tooltip("战斗开始时获得的力量值")]
        public int strengthAmount = 2;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;


            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[GainStrength2BattleStartEffect] playerData 为空");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Strength, amount = strengthAmount, duration = -1 });
            GameLogger.Log($"[GainStrength2BattleStartEffect] 获得力量 {strengthAmount} 点");
        }
    }
}


