using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    ///
    ///
    /// PhantomAfter5CardsEffect.GetActiveReduction() 
    /// </summary>
    [CreateAssetMenu(fileName = "PhantomAfter5CardsBoostEffect", menuName = "MutationChess/Relic Effects/Phantom Boost")]
    public class PhantomAfter5CardsBoostEffect : CardEffect
    {
        [Tooltip("")]
        public int extraReduction = 3;

        public override void Execute(CombatContext context)
        {
            ApplyBoost();
        }

        public override void Execute(EffectContext context)
        {
            ApplyBoost();
        }

        private void ApplyBoost()
        {
            ConversionModifier.PhantomExtraReduction += extraReduction;
            GameLogger.Log($"[PhantomBoost] +{extraReduction}{ConversionModifier.PhantomExtraReduction}");
        }
    }
}
