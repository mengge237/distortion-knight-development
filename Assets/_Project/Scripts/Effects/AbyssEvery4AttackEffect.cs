using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "AbyssEvery4AttackEffect", menuName = "MutationChess/Relic Effects/Abyss Every 4 Attack")]
    public class AbyssEvery4AttackEffect : CardEffect
    {
        [Tooltip("触发伤害加成所需的攻击牌数量阈值")]
        public int threshold = 4;

        [Tooltip("触发时的伤害倍率")]
        public float dmgMultiplier = 2f;

        public override void Execute(CombatContext context) { }

        public override void Execute(EffectContext context)
        {
            if (context == null) return;

            // AttackCardsPlayedThisBattle 已由 Card.ExecuteEffects 统一计数，这里只做取模判定
            if (context.trigger != EffectTrigger.CalculateAttackDamage) return;

            int effectiveThreshold = threshold - (ConversionModifier.BossMemoryLensActive ? 1 : 0) - ConversionModifier.AbyssThresholdReduction;
            effectiveThreshold = Mathf.Max(1, effectiveThreshold);

            // 第 threshold 次、2*threshold 次……攻击时翻倍（取模消耗计数）
            int attacks = ConversionModifier.AttackCardsPlayedThisBattle;
            if (attacks <= 0 || attacks % effectiveThreshold != 0) return;

            context.finalValue = Mathf.RoundToInt(context.baseValue * dmgMultiplier);
            GameLogger.Log($"[AbyssEvery4Attack] 第 {attacks} 次攻击触发，伤害倍率 {dmgMultiplier}：{context.baseValue} -> {context.finalValue}");
        }
    }
}
