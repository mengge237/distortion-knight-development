using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 鲜血折扣效果
    /// </summary>
    [CreateAssetMenu(fileName = "BloodDiscountEffect", menuName = "MutationChess/Potion Effects/Blood Discount")]
    public class BloodDiscountEffect : CardEffect
    {
        [Header("鲜血折扣配置")]
        [Tooltip("覆盖的鲜血转化率（值为1时转化率为2:1）")]
        public int overrideRate = 1;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.TemporaryBloodRateOverride = overrideRate;
            GameLogger.Log($"[BloodDiscount] 下一回合鲜血转化率改为（{overrideRate + 1}:1）");

            if (context?.battleManager != null)
                context.battleManager.AddBattleLog($"下一回合鲜血转化率改为（{overrideRate + 1}:1）");
        }

        public override void Execute(EffectContext context)
        {
            ConversionModifier.TemporaryBloodRateOverride = overrideRate;
            GameLogger.Log($"[BloodDiscount] 下一回合鲜血转化率改为（{overrideRate + 1}:1）");
        }
    }
}
