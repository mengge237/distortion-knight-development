using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>





    /// </summary>
    [CreateAssetMenu(fileName = "TagComboResonanceEffect", menuName = "MutationChess/Effects/Tag Combo Resonance")]
    public class TagComboResonanceEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public bool isPermanent = false;

        [Tooltip("")]
        public int temporaryDuration = 1;

        public override void Execute(CombatContext context)
        {
            ActivateResonance(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            ActivateResonance(context?.battleManager);
        }

        private void ActivateResonance(BattleManager battleManager)
        {
            ConversionModifier.TagEffectDoubleTrigger = true;
            GameLogger.Log("[TagComboResonance] ");

            if (battleManager != null)
                battleManager.AddBattleLog("");
        }
    }
}


