using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DealDamageNextTurn", menuName = "MutationChess/Effects/Deal Damage Next Turn")]
    public class DealDamageNextTurnEffect : CardEffect
    {
        // �� Card �� magicNumber �� damage ��ȡ�ӳ��˺�ֵ

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