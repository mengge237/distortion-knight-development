using UnityEngine;
using MutationChess.Battle;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 全能增益效果：同时赋予力量、敏捷、格挡和能量
    /// </summary>
    [CreateAssetMenu(fileName = "OmnipotentBuffEffect", menuName = "MutationChess/Relic Effects/Omnipotent Buff")]
    public class OmnipotentBuffEffect : CardEffect
    {
        [Tooltip("赋予的力量层数")]
        public int strengthAmount = 1;

        [Tooltip("赋予的敏捷层数")]
        public int dexterityAmount = 1;

        [Tooltip("赋予的格挡值")]
        public int blockAmount = 5;

        [Tooltip("恢复的能量数")]
        public int energyAmount = 1;

        public override string GetDescription(Card card)
        {
            return $"获得 {strengthAmount} 力量、{dexterityAmount} 敏捷、{blockAmount} 格挡和 {energyAmount} 能量";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null || context.battleManager == null)
            {
                GameLogger.LogWarning("[OmnipotentBuffEffect] context 或 battleManager 为 null");
                return;
            }


            PlayerData playerData = context.targetPlayer ?? context.battleManager.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[OmnipotentBuffEffect] playerData 为 null");
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
                GameLogger.LogWarning("[OmnipotentBuffEffect] HandManager 为 null");
            }

            GameLogger.Log($"[OmnipotentBuffEffect] 力量 {strengthAmount} + 敏捷 {dexterityAmount} + 格挡 {blockAmount} + 能量 {energyAmount}");
        }
    }
}


