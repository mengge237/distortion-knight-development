using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "GainStrengthEffect", menuName = "MutationChess/Relic Effects/Gain Strength")]
    public class GainStrengthEffect : CardEffect
    {
        [Header("力量配置")]
        [Tooltip("获得的力量数值")]
        public int strengthAmount = 2;

        public override string GetDescription(Card card)
        {
            int amount = (card != null && card.magicNumber > 0) ? card.magicNumber : strengthAmount;
            return $"获得 {amount} 点力量";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[GainStrengthEffect] playerData 为空");
                return;
            }

            playerData.AddBuff(new Buff
            {
                type = BuffType.Strength,
                amount = strengthAmount,
                duration = -1
            });

            context.battleManager?.AddLog($"获得 {strengthAmount} 点力量（永久生效）");
            GameLogger.Log($"[GainStrengthEffect] 力量 +{strengthAmount} 点");
        }
    }
}
