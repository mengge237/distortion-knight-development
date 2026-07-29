using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyDamageOverTime", menuName = "MutationChess/Effects/Apply Damage Over Time")]
    public class ApplyDamageOverTimeEffect : CardEffect
    {
        [Header("")]
        [Tooltip("(magicNumber>0)")]
        public int defaultPoison = 3;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;
            if (context.targetEnemy == null || context.sourceCard == null) return;
            int poisonCount = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : defaultPoison;
            context.targetEnemy.AddBuff(new Buff
            {
                type = BuffType.Poison,
                amount = poisonCount,
                duration = 999
            });
        }
    }
}
