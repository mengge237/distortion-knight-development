using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "BloodRageEffect", menuName = "MutationChess/Potion Effects/Blood Rage")]
    public class BloodRageEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int defaultBloodRate = 3;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.BloodConversionForAll = true;
            ConversionModifier.TemporaryBloodRateOverride = defaultBloodRate;
            GameLogger.Log($"[BloodRage]  {defaultBloodRate}=1");

            if (context?.battleManager != null)
                context.battleManager.AddBattleLog("");
        }

        public override void Execute(EffectContext context)
        {
            ConversionModifier.BloodConversionForAll = true;
            ConversionModifier.TemporaryBloodRateOverride = defaultBloodRate;
            GameLogger.Log($"[BloodRage]  {defaultBloodRate}=1");

            if (context?.battleManager != null)
                context.battleManager.AddBattleLog("");
        }
    }
}


