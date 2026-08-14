using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 深渊阈值降低效果：降低深渊触发所需的攻击牌数量
    /// 影响 AbyssEvery4AttackEffect 的触发阈值
    /// </summary>
    [CreateAssetMenu(fileName = "AbyssReduceThresholdEffect", menuName = "MutationChess/Relic Effects/Abyss Reduce Threshold")]
    public class AbyssReduceThresholdEffect : CardEffect
    {
        [Tooltip("深渊触发阈值降低的数量")]
        public int reduce = 1;

        public override void Execute(CombatContext context)
        {
            ApplyThresholdReduction();
        }

        public override void Execute(EffectContext context)
        {
            ApplyThresholdReduction();
        }

        private void ApplyThresholdReduction()
        {
            ConversionModifier.AbyssThresholdReduction += reduce;
            GameLogger.Log($"[AbyssReduceThreshold] 阈值降低 {reduce}，当前累计：{ConversionModifier.AbyssThresholdReduction}");
        }
    }
}
