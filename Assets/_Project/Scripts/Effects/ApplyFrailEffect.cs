using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyFrailEffect", menuName = "MutationChess/Effects/Apply Frail")]
    public class ApplyFrailEffect : CardEffect
    {
        [Header("��������")]
        [SerializeField] private int frailAmount = 1;
        [SerializeField] private int duration = 2;

        public override string GetDescription(Card card)
        {
            return $"ʩ�� {frailAmount} �������{duration} �غϣ�";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            Buff buff = new Buff
            {
                type = BuffType.Frail,
                amount = frailAmount,
                duration = duration
            };

            if (context.targetEnemy != null)
            {
                context.targetEnemy.AddBuff(buff);
                context.battleManager?.AddLog($"��Ҷ� {context.targetEnemy.enemyName} ʩ�� {frailAmount} �����������{duration}�غϣ�");
            }
            else if (context.targetPlayer != null)
            {
                context.targetPlayer.AddBuff(buff);
                context.battleManager?.AddLog($"��ұ�ʩ�� {frailAmount} �����������{duration}�غϣ�");
            }
        }
    }
}