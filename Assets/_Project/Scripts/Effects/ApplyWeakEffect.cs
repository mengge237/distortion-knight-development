using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyWeakEffect", menuName = "MutationChess/Effects/Apply Weak")]
    public class ApplyWeakEffect : CardEffect
    {
        [Header("")]
        [SerializeField] private int weakAmount = 1;
        [Header("")]
        [SerializeField] private int duration = 2;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            Buff buff = new Buff
            {
                type = BuffType.Weak,
                amount = weakAmount,
                duration = duration
            };

            if (context.targetEnemy != null)
            {
                context.targetEnemy.AddBuff(buff);
            }
            else if (context.targetPlayer != null)
            {
                context.targetPlayer.AddBuff(buff);
            }
        }
    }
}
