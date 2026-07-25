using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyDebuffEffect", menuName = "MutationChess/Effects/Apply Debuff")]
    public class ApplyDebuffEffect : CardEffect
    {
        [Header("Debuff类型")]
        [SerializeField] private BuffType debuffType = BuffType.Vulnerability;
        [Header("层数")]
        [SerializeField] private int amount = 1;
        [Header("持续回合")]
        [SerializeField] private int duration = 2;

        public override void Execute(CombatContext context)
        {
            bool targetIsEnemy = context.targetEnemy != null;

            Buff buff = new Buff
            {
                type = debuffType,
                amount = amount,
                duration = duration
            };

            if (targetIsEnemy)
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
