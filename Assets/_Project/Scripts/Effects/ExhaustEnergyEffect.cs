using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "ExhaustEnergyEffect", menuName = "MutationChess/Relic Effects/Exhaust Energy")]
    public class ExhaustEnergyEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int energyGain = 1;

        [Tooltip("")]
        public int triggersPerTurn = 1;

        [System.NonSerialized]
        private int triggersThisTurn = 0;

        public override void Execute(CombatContext context)
        {
            TryGrantEnergy(context);
        }

        public override void Execute(EffectContext context)
        {
            if (context != null && context.trigger == EffectTrigger.PlayerTurnStart)
            {
                ResetTurnCount();
                return;
            }
            TryGrantEnergy(context?.combat);
        }

        private void TryGrantEnergy(CombatContext context)
        {
            if (context == null) return;

            if (triggersThisTurn >= triggersPerTurn) return;

            triggersThisTurn++;

            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.RestoreEnergy(energyGain);
                GameLogger.Log($"[ExhaustEnergy]  +{energyGain} ({triggersThisTurn}/{triggersPerTurn})");
            }
        }

        public void ResetTurnCount()
        {
            triggersThisTurn = 0;
        }
    }
}


