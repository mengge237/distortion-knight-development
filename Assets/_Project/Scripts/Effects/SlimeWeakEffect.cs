using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 粘液虚弱效果：打出粘液系卡牌时对敌人施加虚弱
    /// </summary>
    [CreateAssetMenu(fileName = "SlimeWeakEffect", menuName = "MutationChess/Relic Effects/Slime Weak")]
    public class SlimeWeakEffect : CardEffect
    {
        [Header("粘液虚弱配置")]
        [Tooltip("施加虚弱的层数")]
        public int weakAmount = 1;

        [Tooltip("Boss遗物激活时额外增加的虚弱层数")]
        public int bossExtraWeak = 1;

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
                GameLogger.LogWarning("[SlimeWeak] 未找到目标敌人");
                return;
            }

            int totalWeak = weakAmount + (ConversionModifier.BossSlimeGlandActive ? bossExtraWeak : 0);

            enemy.AddBuff(new Buff
            {
                type = BuffType.Weak,
                amount = totalWeak,
                duration = weakDuration
            });

            GameLogger.Log($"[SlimeWeak] {playedCard.cardName} 造成 {totalWeak} 层虚弱" +
                (ConversionModifier.BossSlimeGlandActive ? "（Boss加成）" : ""));
        }
    }
}
