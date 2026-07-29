using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "BloodCostReductionEffect", menuName = "MutationChess/Relic Effects/Blood Cost Reduction")]
    public class BloodCostReductionEffect : CardEffect
    {
        [Header("")]
        [Tooltip("1 32")]
        public int rateReduction = 1;

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
            ConversionModifier.PermanentBloodRateReduction += rateReduction;
            GameLogger.Log($"[BloodCostReduction]  {rateReduction}: {ConversionModifier.PermanentBloodRateReduction}");
        }
    }
}


