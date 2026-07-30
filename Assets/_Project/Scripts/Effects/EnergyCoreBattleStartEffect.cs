using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "EnergyCoreBattleStartEffect", menuName = "MutationChess/Relic Effects/Energy Core")]
    public class EnergyCoreBattleStartEffect : CardEffect
    {
        [Tooltip("")]
        public int energy = 2;

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.RestoreEnergy(energy);
                GameLogger.Log($"[EnergyCore] +{energy} ");
            }
        }
    }
}
