using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 粘液虚弱遗物效果：打出粘液标签卡牌时，对当前敌人施加虚弱。
    /// 触发时机：CardPlayed（context.combat.sourceCard 为打出的卡牌）。
    /// 仅当 sourceCard 拥有 Slime 标签时触发，对 targetEnemy 施加 Weak buff。
    /// </summary>
    [CreateAssetMenu(fileName = "SlimeWeakEffect", menuName = "MutationChess/Relic Effects/Slime Weak")]
    public class SlimeWeakEffect : CardEffect
    {
        [Header("粘液虚弱")]
        [Tooltip("施加的虚弱层数")]
        public int weakAmount = 1;

        [Tooltip("虚弱持续回合数")]
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
                GameLogger.LogWarning("[SlimeWeak] 没有目标敌人");
                return;
            }

            enemy.AddBuff(new Buff
            {
                type = BuffType.Weak,
                amount = weakAmount,
                duration = weakDuration
            });

            GameLogger.Log($"[SlimeWeak] 粘液卡 {playedCard.cardName} 使敌人获得 {weakAmount} 层虚弱");
        }
    }
}
