using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyDebuff", menuName = "MutationChess/Effects/Apply Debuff")]
    public class ApplyDebuffEffect : CardEffect
    {
        public BuffType debuffType = BuffType.Vulnerability;
        public int defaultAmount = 1;
        public int defaultDuration = 2;

        public override void Execute(CombatContext context)
        {
            if (context.targetEnemy != null && context.sourceCard != null)
            {
                int amount = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : defaultAmount;
                int duration = defaultDuration;
                context.targetEnemy.AddBuff(new Buff { type = debuffType, amount = amount, duration = duration });
            }
        }
    }
}