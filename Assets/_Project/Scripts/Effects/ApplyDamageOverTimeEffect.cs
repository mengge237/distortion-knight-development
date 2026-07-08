using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyDamageOverTime", menuName = "MutationChess/Effects/Apply Damage Over Time")]
    public class ApplyDamageOverTimeEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context.targetEnemy != null && context.sourceCard != null)
            {
                int poisonCount = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : 3;
                context.targetEnemy.AddBuff(new Buff { type = BuffType.Poison, amount = poisonCount, duration = 999 });
            }
        }
    }
}