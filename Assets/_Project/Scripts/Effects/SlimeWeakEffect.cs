using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    ///
    ///
    /// 
    /// </summary>
    [CreateAssetMenu(fileName = "SlimeWeakEffect", menuName = "MutationChess/Relic Effects/Slime Weak")]
    public class SlimeWeakEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int weakAmount = 1;

        [Tooltip("Boss")]
        public int bossExtraWeak = 1;

        [Tooltip("")]
        public int weakDuration = 3;

        public override void Execute(CombatContext context)
        {
            ApplySlimeWeak(context);
        }

        public override void Execute(EffectContext context)
        {
            ApplySlimeWeak(context?.combat);
        }

        private void ApplySlimeWeak(CombatContext context)
        {
            if (context == null) return;

            Card playedCard = context.sourceCard;
            if (playedCard == null || !playedCard.HasTag(CardTag.Slime)) return;

            Enemy enemy = context.targetEnemy ?? context.battleManager?.GetCurrentEnemy();
            if (enemy == null)
            {
                GameLogger.LogWarning("[SlimeWeak] ");
                return;
            }

            int totalWeak = weakAmount + (ConversionModifier.BossSlimeGlandActive ? bossExtraWeak : 0);

            enemy.AddBuff(new Buff
            {
                type = BuffType.Weak,
                amount = totalWeak,
                duration = weakDuration
            });

            GameLogger.Log($"[SlimeWeak] {playedCard.cardName} {totalWeak} " +
                (ConversionModifier.BossSlimeGlandActive ? " (Boss加成)" : ""));
        }
    }
}
