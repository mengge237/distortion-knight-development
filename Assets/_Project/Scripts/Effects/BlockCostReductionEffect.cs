using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "BlockCostReductionEffect", menuName = "MutationChess/Relic Effects/Block Cost Reduction")]
    public class BlockCostReductionEffect : CardEffect
    {
        [Header("")]
        [Tooltip("2 53")]
        public int rateReduction = 2;

        public override void Execute(CombatContext context)
        {
            ApplyReduction();
        }

        public override void Execute(EffectContext context)
        {
            ApplyReduction();
        }

        private void ApplyReduction()
        {
            ConversionModifier.PermanentBlockRateReduction += rateReduction;
            GameLogger.Log($"[BlockCostReduction]  {rateReduction}: {ConversionModifier.PermanentBlockRateReduction}");
        }
    }
}


