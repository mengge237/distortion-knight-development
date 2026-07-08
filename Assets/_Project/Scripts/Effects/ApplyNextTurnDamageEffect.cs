using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyNextTurnDamage", menuName = "MutationChess/Effects/Apply Next Turn Damage")]
    public class ApplyNextTurnDamageEffect : CardEffect
    {
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