using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyShadowStrength", menuName = "MutationChess/Effects/Apply Shadow Strength")]
    public class ApplyShadowStrengthEffect : CardEffect
    {
        [Header("��Ӱ��������")]
        [Tooltip("��Ӱ������ֵ������magicNumber>0ʱʹ�ÿ���ֵ")]
        public int strengthAmount = 2;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            if (context.targetPlayer != null)
            {
                int amount = strengthAmount;
                if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                {
                    amount = context.sourceCard.magicNumber;
                }

                var buff = new Buff
                {
                    type = BuffType.Strength,
                    amount = amount,
                    duration = -1,
                    isShadow = true
                };
                context.targetPlayer.AddBuff(buff);
                context.battleManager?.AddLog($"��һ�� {amount} �㰵Ӱ�������ɱ���Ӱ�������ģ�");
                GameLogger.Log($"[ApplyShadowStrength] ��һ�ð�Ӱ���� +{amount}");
            }
            else if (context.targetEnemy != null)
            {
                int amount = strengthAmount;
                if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                {
                    amount = context.sourceCard.magicNumber;
                }
                context.targetEnemy.AddBuff(new Buff
                {
                    type = BuffType.Strength,
                    amount = amount,
                    duration = -1,
                    isShadow = true
                });
                context.battleManager?.AddLog($"{context.targetEnemy.enemyName} ��� {amount} �㰵Ӱ����");
            }
        }

        public void ApplyToPlayer(PlayerData player, int amount)
        {
            if (player == null) return;
            var buff = new Buff
            {
                type = BuffType.Strength,
                amount = amount,
                duration = -1,
                isShadow = true
            };
            player.AddBuff(buff);
        }
    }
}
