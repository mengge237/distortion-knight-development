using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyFrailEffect", menuName = "MutationChess/Effects/Apply Frail")]
    public class ApplyFrailEffect : CardEffect
    {
        [Header("脆弱层数")]
        [SerializeField] private int frailAmount = 1;
        [Header("持续回合")]
        [SerializeField] private int duration = 2;

        public override void Execute(CombatContext context)
        {
            Buff buff = new Buff
            {
                type = BuffType.Frail,
                amount = frailAmount,
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
