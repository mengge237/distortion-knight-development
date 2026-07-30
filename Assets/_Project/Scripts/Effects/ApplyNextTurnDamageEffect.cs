using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyNextTurnDamage", menuName = "MutationChess/Effects/Apply Next Turn Damage")]
    public class ApplyNextTurnDamageEffect : CardEffect
    {
        [Header("")]
        [Tooltip("(magicNumber>0)")]
        public int defaultDamage = 12;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;
            if (context.targetEnemy == null || context.sourceCard == null) return;
            int damage = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : defaultDamage;
            context.targetEnemy.AddDelayedDamage(damage);
        }
    }
}
