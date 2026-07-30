using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "GainStrengthEffect", menuName = "MutationChess/Relic Effects/Gain Strength")]
    public class GainStrengthEffect : CardEffect
    {
        [Header("��������")]
        [Tooltip("��õ�������ֵ")]
        public int strengthAmount = 2;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[GainStrengthEffect] playerData Ϊ��");
                return;
            }

            playerData.AddBuff(new Buff
            {
                type = BuffType.Strength,
                amount = strengthAmount,
                duration = -1
            });

            context.battleManager?.AddLog($"��һ�� {strengthAmount} ��������������������");
            GameLogger.Log($"[GainStrengthEffect] ��� {strengthAmount} ������");
        }
    }
}
