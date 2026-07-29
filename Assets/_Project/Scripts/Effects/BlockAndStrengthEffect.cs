using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "BlockAndStrengthEffect", menuName = "MutationChess/Relic Effects/Block And Strength")]
    public class BlockAndStrengthEffect : CardEffect
    {
        [Tooltip("")]
        public int blockAmount = 5;

        [Tooltip("")]
        public int strengthAmount = 1;

        public override void Execute(CombatContext context)
        {
            if (context == null || context.battleManager == null)
            {
                GameLogger.LogWarning("[BlockAndStrengthEffect] context  battleManager ");
                return;
            }


            context.battleManager.PlayerBlock(blockAmount);


            PlayerData playerData = context.targetPlayer ?? context.battleManager.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[BlockAndStrengthEffect] playerData ");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Strength, amount = strengthAmount, duration = -1 });
            GameLogger.Log($"[BlockAndStrengthEffect]  {blockAmount}  + {strengthAmount} ");
        }
    }
}


