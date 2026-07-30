using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyTemporaryStrength", menuName = "MutationChess/Effects/Apply Temporary Strength")]
    public class ApplyTemporaryStrengthEffect : CardEffect
    {
        [Header("��ʱ��������")]
        [Tooltip("��õ���ʱ������ֵ")]
        public int strengthAmount = 2;

        [Tooltip("�����غ���(-1��ʾս��������)������magicNumber>0ʱʹ�ÿ���ֵ��Ϊ�����غ�")]
        public int duration = -1;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            int amount = strengthAmount;
            int dur = duration;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                dur = context.sourceCard.magicNumber;
            }

            if (context.targetPlayer != null)
            {
                var buff = new Buff { type = BuffType.Strength, amount = amount, duration = dur };
                context.targetPlayer.AddBuff(buff);
                string durText = dur < 0 ? "����" : $"{dur}�غ�";
                context.battleManager?.AddLog($"��һ�� {amount} ����ʱ������{durText}��");
            }
            else if (context.targetEnemy != null)
            {
                context.targetEnemy.AddBuff(new Buff { type = BuffType.Strength, amount = amount, duration = dur });
                string durText = dur < 0 ? "����" : $"{dur}�غ�";
                context.battleManager?.AddLog($"{context.targetEnemy.enemyName} ��� {amount} ����ʱ������{durText}��");
            }
        }

        public void ApplyToPlayer(PlayerData player, int amount, int duration)
        {
            if (player == null) return;
            var buff = new Buff
            {
                type = BuffType.Strength,
                amount = amount,
                duration = duration
            };
            player.AddBuff(buff);
        }
    }
}
