using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 获得能量效果，每回合获得一点能量。
    /// 类似不舍系列的效果。
    /// </summary>
    [CreateAssetMenu(fileName = "GainEnergyEffect", menuName = "MutationChess/Relic Effects/Gain Energy")]
    public class GainEnergyEffect : CardEffect
    {
        [Header("能量效果")]
        [Tooltip("获得的能量点数")]
        public int energyGain = 1;

        [Header("负面效果")]
        [Tooltip("每回合丢弃随机卡牌数")]
        public int discardRandomCount = 1;

        public override void Execute(CombatContext context)
        {
            // 本效果由效果系统处理，不需要直接执行
        }

        public void ExecuteGainEnergy(BattleManager battleManager)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[GainEnergyEffect] HandManager 未找到");
                return;
            }

            handManager.RestoreEnergy(energyGain);

            if (battleManager != null)
            {
                battleManager.AddBattleLog($"能量药水: 恢复 {energyGain} 点能量");
            }
        }
    }
}
