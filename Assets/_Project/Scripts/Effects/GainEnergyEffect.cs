using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "GainEnergyEffect", menuName = "MutationChess/Relic Effects/Gain Energy")]
    public class GainEnergyEffect : CardEffect
    {
        [Header("能量配置")]
        [Tooltip("每位获得的能量数")]
        public int energyGain = 1;

        [Header("弃牌配置")]
        [Tooltip("效果触发后随机弃牌的数量（0=不弃牌）")]
        public int discardRandomCount = 0;

        public override string GetDescription(Card card)
        {
            string desc = $"获得 {energyGain} 点能量";
            if (discardRandomCount > 0)
                desc += $"，随机弃 {discardRandomCount} 张手牌";
            return desc;
        }

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[GainEnergyEffect] HandManager 为空");
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
                string log = $"获得 {energyGain} 点能量";
                if (discardRandomCount > 0) log += $"，随机弃牌 {discardRandomCount} 张";
                context.battleManager.AddLog(log);
            }

            GameLogger.Log($"[GainEnergyEffect] 能量+{energyGain}" + (discardRandomCount > 0 ? $" 弃牌{discardRandomCount}" : ""));
        }

        public void ExecuteGainEnergy(BattleManager battleManager)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[GainEnergyEffect] HandManager 为空");
                return;
            }

            handManager.RestoreEnergy(energyGain);

            if (battleManager != null)
            {
                battleManager.AddLog($"获得 {energyGain} 点能量");
            }
        }
    }
}
