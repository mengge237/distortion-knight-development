using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "AllStatsBuffEffect", menuName = "MutationChess/Relic Effects/All Stats Buff")]
    public class AllStatsBuffEffect : CardEffect
    {
        [Tooltip("")]
        public int strengthAmount = 1;

        [Tooltip("")]
        public int dexterityAmount = 1;

        [Tooltip("")]
        public int blockAmount = 10;

        public override void Execute(CombatContext context)
        {
            if (context == null || context.battleManager == null)
            {
                GameLogger.LogWarning("[AllStatsBuffEffect] context  battleManager ");
                return;
            }


            PlayerData playerData = context.targetPlayer ?? context.battleManager.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[AllStatsBuffEffect] playerData ");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Strength, amount = strengthAmount, duration = -1 });
            playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = dexterityAmount, duration = -1 });
            context.battleManager.PlayerBlock(blockAmount);

            GameLogger.Log($"[AllStatsBuffEffect]  {strengthAmount}  + {dexterityAmount}  + {blockAmount} ");
        }
    }
}


