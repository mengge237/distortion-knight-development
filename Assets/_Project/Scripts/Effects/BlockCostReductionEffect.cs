using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 格挡消耗降低效果：降低格挡转化率
    /// </summary>
    [CreateAssetMenu(fileName = "BlockCostReductionEffect", menuName = "MutationChess/Relic Effects/Block Cost Reduction")]
    public class BlockCostReductionEffect : CardEffect
    {
        [Header("格挡消耗降低配置")]
        [Tooltip("格挡转化率降低数值（例如从 5 降至 3）")]
        public int rateReduction = 2;

        [System.NonSerialized]
        private bool appliedThisBattle = false;

        public override void Execute(CombatContext context)
        {
            ApplyReduction();
        }

        public override void Execute(EffectContext context)
        {
            ApplyReduction();
        }

        public override void ResetForBattle()
        {
            appliedThisBattle = false;
        }

        private void ApplyReduction()
        {
            // 本效果以 CalculateCardCost 触发，每次费用计算都会执行：
            // 必须每场战斗只叠加一次，否则减免值随费用计算次数无限膨胀
            if (appliedThisBattle) return;
            appliedThisBattle = true;

            ConversionModifier.PermanentBlockRateReduction += rateReduction;
            GameLogger.Log($"[BlockCostReduction] 格挡转化率降低 {rateReduction}，当前累计：{ConversionModifier.PermanentBlockRateReduction}");
        }
    }
}


