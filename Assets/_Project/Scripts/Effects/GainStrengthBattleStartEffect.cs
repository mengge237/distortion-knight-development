using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "GainStrengthBattleStartEffect", menuName = "MutationChess/Relic Effects/Gain Strength Battle Start")]
    public class GainStrengthBattleStartEffect : CardEffect
    {
        [Tooltip("战斗开始时获得的力量值")]
        public int strengthAmount = 1;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;


            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[GainStrengthBattleStartEffect] playerData 为空");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Strength, amount = strengthAmount, duration = -1 });
            GameLogger.Log($"[GainStrengthBattleStartEffect] 获得力量 {strengthAmount} 点");
        }
    }
}


