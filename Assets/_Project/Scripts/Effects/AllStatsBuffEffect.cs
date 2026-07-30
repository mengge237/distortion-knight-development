using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 全属性增益效果：同时获得力量、敏捷和格挡
    /// </summary>
    [CreateAssetMenu(fileName = "AllStatsBuffEffect", menuName = "MutationChess/Relic Effects/All Stats Buff")]
    public class AllStatsBuffEffect : CardEffect
    {
        [Tooltip("获得的力量值")]
        public int strengthAmount = 1;

        [Tooltip("获得的敏捷值")]
        public int dexterityAmount = 1;

        [Tooltip("获得的格挡值")]
        public int blockAmount = 10;

        public override string GetDescription(Card card)
        {
            return $"获得 {strengthAmount} 力量、{dexterityAmount} 敏捷和 {blockAmount} 格挡";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null || context.battleManager == null)
            {
                GameLogger.LogWarning("[AllStatsBuffEffect] context 或 battleManager 为空");
                return;
            }


            PlayerData playerData = context.targetPlayer ?? context.battleManager.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[AllStatsBuffEffect] playerData 为空");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Strength, amount = strengthAmount, duration = -1 });
            playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = dexterityAmount, duration = -1 });
            context.battleManager.PlayerBlock(blockAmount);

            GameLogger.Log($"[AllStatsBuffEffect] 力量+{strengthAmount} 敏捷+{dexterityAmount} 格挡+{blockAmount}");
        }
    }
}


