using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DealDamageNextTurn", menuName = "MutationChess/Effects/Deal Damage Next Turn")]
    public class DealDamageNextTurnEffect : CardEffect
    {
        // 从 Card 的 magicNumber 或 damage 读取延迟伤害值

        public override void Execute(CombatContext context)
        {
            if (context.targetEnemy != null && context.sourceCard != null)
            {
                int damage = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : 12;
                context.targetEnemy.AddDelayedDamage(damage);
            }
        }
    }
}