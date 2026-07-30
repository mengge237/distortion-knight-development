using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyDexterity", menuName = "MutationChess/Effects/Apply Dexterity")]
    public class ApplyDexterityEffect : CardEffect
    {
        [Header("�������")]
        [Tooltip("Ĭ�������ֵ(magicNumber>0ʱʹ�ÿ���ֵ)")]
        public int defaultAmount = 3;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            int amount = (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                ? context.sourceCard.magicNumber : defaultAmount;

            var buff = new Buff { type = BuffType.Dexterity, amount = amount, duration = 999 };

            if (context.targetPlayer != null)
            {
                context.targetPlayer.AddBuff(buff);
                context.battleManager?.AddLog($"��һ�� {amount} ����ݣ���������");
            }
            else if (context.targetEnemy != null)
            {
                context.targetEnemy.AddBuff(buff);
                context.battleManager?.AddLog($"{context.targetEnemy.enemyName} ��� {amount} �����");
            }
        }
    }
}
