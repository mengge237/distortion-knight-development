using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ReduceStrengthEffect", menuName = "MutationChess/Card Effects/Reduce Strength")]
    public class ReduceStrengthEffect : CardEffect
    {
        [Tooltip("������������ֵ������magicNumber>0ʱʹ�ÿ���ֵ")]
        public int reduceAmount = 3;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            Enemy enemy = context.targetEnemy ?? context.battleManager?.GetCurrentEnemy();
            if (enemy == null)
            {
                GameLogger.LogWarning("[ReduceStrengthEffect] û��Ŀ�����");
                return;
            }

            int amount = reduceAmount;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                amount = context.sourceCard.magicNumber;
            }

            enemy.AddBuff(new Buff { type = BuffType.Strength, amount = -amount, duration = -1 });
            context.battleManager?.AddLog($"{enemy.enemyName} ���� {amount} ������");
            GameLogger.Log($"[ReduceStrengthEffect] {enemy.enemyName} ���� -{amount}");
        }
    }
}
