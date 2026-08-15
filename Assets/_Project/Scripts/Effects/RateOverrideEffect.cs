using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 转化率覆盖效果（药水）：临时覆盖格挡或鲜血的转化率。
    /// 由效果合并从 BlockDiscountEffect / BloodDiscountEffect 合并而来
    /// （仅覆盖目标字段不同，其余逻辑完全一致）。
    /// </summary>
    [CreateAssetMenu(fileName = "RateOverrideEffect", menuName = "MutationChess/Potion Effects/Rate Override")]
    public class RateOverrideEffect : CardEffect
    {
        public enum RateTarget
        {
            Block = 0, // 格挡转化率
            Blood = 1, // 鲜血转化率
        }

        [Header("转化率覆盖配置")]
        [Tooltip("覆盖目标：格挡或鲜血转化率")]
        public RateTarget target = RateTarget.Block;

        [Tooltip("覆盖的转化率（值为1时转化率为2:1）")]
        public int overrideRate = 1;

        public override void Execute(CombatContext context)
        {
            ApplyOverride();
            if (context?.battleManager != null)
                context.battleManager.AddBattleLog(OverrideLogText());
        }

        public override void Execute(EffectContext context)
        {
            ApplyOverride();
        }

        private void ApplyOverride()
        {
            if (target == RateTarget.Blood)
                ConversionModifier.TemporaryBloodRateOverride = overrideRate;
            else
                ConversionModifier.TemporaryBlockRateOverride = overrideRate;
            GameLogger.Log($"[RateOverride] {OverrideLogText()}");
        }

        private string OverrideLogText()
        {
            string name = target == RateTarget.Block ? "格挡" : "鲜血";
            return $"下一回合{name}转化率改为（{overrideRate + 1}:1）";
        }
    }
}
