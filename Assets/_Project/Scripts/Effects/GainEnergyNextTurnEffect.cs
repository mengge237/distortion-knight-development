using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "GainEnergyNextTurnEffect", menuName = "MutationChess/Effects/Gain Energy Next Turn")]
    public class GainEnergyNextTurnEffect : CardEffect
    {
        [Tooltip("")]
        public int energyGain = 1;

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[GainEnergyNextTurnEffect] HandManager ");
                return;
            }

            handManager.AddPendingNextTurnEnergy(energyGain);
            GameLogger.Log($"[GainEnergyNextTurnEffect]  {energyGain} ");
        }
    }
}


