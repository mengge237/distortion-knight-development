using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 格挡与力量效果：同时获得格挡和力量
    /// </summary>
    [CreateAssetMenu(fileName = "BlockAndStrengthEffect", menuName = "MutationChess/Relic Effects/Block And Strength")]
    public class BlockAndStrengthEffect : CardEffect
    {
        [Tooltip("获得的格挡值")]
        public int blockAmount = 5;

        [Tooltip("获得的力量层数")]
        public int strengthAmount = 1;

        public override string GetDescription(Card card)
        {
            return $"获得 {blockAmount} 格挡和 {strengthAmount} 力量";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null || context.battleManager == null)
            {
                GameLogger.LogWarning("[BlockAndStrengthEffect] context 或 battleManager 为 null");
                return;
            }


            context.battleManager.PlayerBlock(blockAmount);


            PlayerData playerData = context.targetPlayer ?? context.battleManager.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[BlockAndStrengthEffect] playerData 为 null");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Strength, amount = strengthAmount, duration = -1 });
            GameLogger.Log($"[BlockAndStrengthEffect] 获得 {blockAmount} 点格挡 + {strengthAmount} 层力量");
        }
    }
}


