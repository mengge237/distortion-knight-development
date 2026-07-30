using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyDebuffEffect", menuName = "MutationChess/Effects/Apply Debuff")]
    public class ApplyDebuffEffect : CardEffect
    {
        [Header("Debuff")]
        [SerializeField] private BuffType debuffType = BuffType.Vulnerability;

        [Header("数值配置")]
        [SerializeField] private int amount = 1;

        [Header("持续回合")]
        [SerializeField] private int duration = 2;

        public override string GetDescription(Card card)
        {
            return $"施加 {amount} 层减益（{duration} 回合）";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

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
