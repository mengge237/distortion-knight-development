using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyWeakEffect", menuName = "MutationChess/Effects/Apply Weak")]
    public class ApplyWeakEffect : CardEffect
    {
        [Header("弱化层数")]
        [SerializeField] private int weakAmount = 1;
        [Header("持续回合")]
        [SerializeField] private int duration = 2;

        public override void Execute(CombatContext context)
        {
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
