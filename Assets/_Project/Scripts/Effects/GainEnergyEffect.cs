using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "GainEnergyEffect", menuName = "MutationChess/Relic Effects/Gain Energy")]
    public class GainEnergyEffect : CardEffect
    {
        [Header("��������")]
        [Tooltip("���λ�õ���������")]
        public int energyGain = 1;

        [Header("��������")]
        [Tooltip("Ч���������������������0=�����ƣ�")]
        public int discardRandomCount = 0;

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[GainEnergyEffect] HandManager Ϊ��");
                return;
            }

            handManager.RestoreEnergy(energyGain);

            for (int i = 0; i < discardRandomCount; i++)
            {
                var handCards = handManager.GetHandCards();
                if (handCards.Count == 0) break;

                int index = Random.Range(0, handCards.Count);
                handManager.DiscardCard(handCards[index]);
            }

            if (context?.battleManager != null)
            {
                string log = $"��һ�� {energyGain} ������";
                if (discardRandomCount > 0) log += $"��������� {discardRandomCount} ����";
                context.battleManager.AddLog(log);
            }

            GameLogger.Log($"[GainEnergyEffect] ����+{energyGain}" + (discardRandomCount > 0 ? $" ����{discardRandomCount}" : ""));
        }

        public void ExecuteGainEnergy(BattleManager battleManager)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[GainEnergyEffect] HandManager Ϊ��");
                return;
            }

            handManager.RestoreEnergy(energyGain);

            if (battleManager != null)
            {
                battleManager.AddLog($"��һ�� {energyGain} ������");
            }
        }
    }
}
