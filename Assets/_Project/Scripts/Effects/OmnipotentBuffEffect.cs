using UnityEngine;
using MutationChess.Battle;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "OmnipotentBuffEffect", menuName = "MutationChess/Relic Effects/Omnipotent Buff")]
    public class OmnipotentBuffEffect : CardEffect
    {
        [Tooltip("")]
        public int strengthAmount = 1;

        [Tooltip("")]
        public int dexterityAmount = 1;

        [Tooltip("")]
        public int blockAmount = 5;

        [Tooltip("")]
        public int energyAmount = 1;

        public override void Execute(CombatContext context)
        {
            if (context == null || context.battleManager == null)
            {
                GameLogger.LogWarning("[OmnipotentBuffEffect] context  battleManager ");
                return;
            }


            PlayerData playerData = context.targetPlayer ?? context.battleManager.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[OmnipotentBuffEffect] playerData ");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Strength, amount = strengthAmount, duration = -1 });
            playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = dexterityAmount, duration = -1 });
            context.battleManager.PlayerBlock(blockAmount);


            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.RestoreEnergy(energyAmount);
            }
            else
            {
                GameLogger.LogWarning("[OmnipotentBuffEffect] HandManager ");
            }

            GameLogger.Log($"[OmnipotentBuffEffect]  {strengthAmount}  + {dexterityAmount}  + {blockAmount}  + {energyAmount} ");
        }
    }
}


