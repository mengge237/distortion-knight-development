using UnityEngine;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "GainEnergyNextTurnEffect", menuName = "MutationChess/Effects/Gain Energy Next Turn")]
    public class GainEnergyNextTurnEffect : CardEffect
    {
        [Tooltip("�»غϻ�õ���������")]
        public int energyGain = 1;

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[GainEnergyNextTurnEffect] HandManager Ϊ��");
                return;
            }

            handManager.AddPendingNextTurnEnergy(energyGain);
            context.battleManager?.AddLog($"Ч����Ч���»غϿ�ʼʱ�������� {energyGain} ������");
            GameLogger.Log($"[GainEnergyNextTurnEffect] �»غ�����+{energyGain}");
        }
    }
}
