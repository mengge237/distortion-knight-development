using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    ///
    ///
    /// AbyssEvery4AttackEffect 
    /// </summary>
    [CreateAssetMenu(fileName = "AbyssReduceThresholdEffect", menuName = "MutationChess/Relic Effects/Abyss Reduce Threshold")]
    public class AbyssReduceThresholdEffect : CardEffect
    {
        [Tooltip("")]
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
            GameLogger.Log($"[AbyssReduceThreshold] {reduce}{ConversionModifier.AbyssThresholdReduction}");
        }
    }
}
