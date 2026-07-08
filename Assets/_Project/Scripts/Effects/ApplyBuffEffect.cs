using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyBuff", menuName = "MutationChess/Effects/Apply Buff")]
    public class ApplyBuffEffect : CardEffect
    {
        public BuffType buffType = BuffType.Strength;
        public int defaultAmount = 2;
        public int defaultDuration = 3;

        public override void Execute(CombatContext context)
        {
            if (context.targetEnemy != null && context.sourceCard != null)
            {
                int amount = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : defaultAmount;
                int duration = defaultDuration;
                context.targetEnemy.AddBuff(new Buff { type = buffType, amount = amount, duration = duration });
            }
        }
    }
}