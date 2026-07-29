using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    ///
    ///
    /// </summary>
    [CreateAssetMenu(fileName = "GainEnergyEffect", menuName = "MutationChess/Relic Effects/Gain Energy")]
    public class GainEnergyEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int energyGain = 1;

        [Header("")]
        [Tooltip("")]
        public int discardRandomCount = 1;

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[GainEnergyEffect] HandManager ");
                return;
            }

            //
            handManager.RestoreEnergy(energyGain);

            //
            for (int i = 0; i < discardRandomCount; i++)
            {
                var handCards = handManager.GetHandCards();
                if (handCards.Count == 0) break;

                int index = Random.Range(0, handCards.Count);
                handManager.DiscardCard(handCards[index]);
            }

            if (context?.battleManager != null)
            {
                context.battleManager.AddBattleLog($": +{energyGain}" +
                    (discardRandomCount > 0 ? $": {discardRandomCount}" : ""));
            }

            GameLogger.Log($"[GainEnergyEffect] {energyGain} " +
                (discardRandomCount > 0 ? $"{discardRandomCount} " : ""));
        }

        public void ExecuteGainEnergy(BattleManager battleManager)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[GainEnergyEffect] HandManager ");
                return;
            }

            handManager.RestoreEnergy(energyGain);

            if (battleManager != null)
            {
                battleManager.AddBattleLog($": {energyGain} ");
            }
        }
    }
}
