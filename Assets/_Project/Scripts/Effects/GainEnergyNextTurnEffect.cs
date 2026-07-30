using UnityEngine;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "GainEnergyNextTurnEffect", menuName = "MutationChess/Effects/Gain Energy Next Turn")]
    public class GainEnergyNextTurnEffect : CardEffect
    {
        [Tooltip("下回合获得的能量数")]
        public int energyGain = 1;

        public override string GetDescription(Card card)
        {
            return $"下回合额外获得 {energyGain} 点能量";
        }

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[GainEnergyNextTurnEffect] HandManager 为空");
                return;
            }

            handManager.AddPendingNextTurnEnergy(energyGain);
            context.battleManager?.AddLog($"效果生效，下回合开始时将获得 {energyGain} 点能量");
            GameLogger.Log($"[GainEnergyNextTurnEffect] 下回合能量+{energyGain}");
        }
    }
}
