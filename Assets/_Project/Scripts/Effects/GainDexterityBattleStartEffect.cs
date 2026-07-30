using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "GainDexterityBattleStartEffect", menuName = "MutationChess/Relic Effects/Gain Dexterity Battle Start")]
    public class GainDexterityBattleStartEffect : CardEffect
    {
        [Tooltip("战斗开始时获得的敏捷值")]
        public int dexterityAmount = 1;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;


            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[GainDexterityBattleStartEffect] playerData 为空");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = dexterityAmount, duration = -1 });
            GameLogger.Log($"[GainDexterityBattleStartEffect] 获得敏捷 {dexterityAmount} 点");
        }
    }
}


