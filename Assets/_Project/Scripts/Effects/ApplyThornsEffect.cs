using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyThornsEffect", menuName = "MutationChess/Card Effects/Apply Thorns")]
    public class ApplyThornsEffect : CardEffect
    {
        [Tooltip("������ֵ������magicNumber>0ʱʹ�ÿ���ֵ")]
        public int thornsAmount = 3;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[ApplyThornsEffect] playerData Ϊ��");
                return;
            }

            int amount = thornsAmount;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                amount = context.sourceCard.magicNumber;
            }

            playerData.AddBuff(new Buff { type = BuffType.Thorns, amount = amount, duration = -1 });
            context.battleManager?.AddLog($"��һ�� {amount} �㾣�����ܻ�������");
            GameLogger.Log($"[ApplyThornsEffect] ���� +{amount}");
        }
    }
}
