using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyWeakEffect", menuName = "MutationChess/Effects/Apply Weak")]
    public class ApplyWeakEffect : CardEffect
    {
        [Header("虚弱配置")]
        [SerializeField] private int weakAmount = 1;
        [SerializeField] private int duration = 2;

        public override string GetDescription(Card card)
        {
            return $"施加 {weakAmount} 层虚弱（{duration} 回合）";
        }

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
                context.battleManager?.AddLog($"玩家对 {context.targetEnemy.enemyName} 施加 {weakAmount} 层虚弱（{duration}回合）");
            }
            else if (context.targetPlayer != null)
            {
                context.targetPlayer.AddBuff(buff);
                context.battleManager?.AddLog($"玩家被施加 {weakAmount} 层虚弱（{duration}回合）");
            }
        }
    }
}
