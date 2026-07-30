using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 格挡折扣效果
    /// 临时修改格挡转化率
    /// </summary>
    [CreateAssetMenu(fileName = "BlockDiscountEffect", menuName = "MutationChess/Potion Effects/Block Discount")]
    public class BlockDiscountEffect : CardEffect
    {
        [Header("格挡折扣配置")]
        [Tooltip("覆盖的格挡转化率（值为1时转化率为2:1）")]
        public int overrideRate = 1;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.TemporaryBlockRateOverride = overrideRate;
            GameLogger.Log($"[BlockDiscount] 下一回合格挡转化率改为（{overrideRate + 1}:1）");

            if (context?.battleManager != null)
                context.battleManager.AddBattleLog($"下一回合格挡转化率改为（{overrideRate + 1}:1）");
        }

        public override void Execute(EffectContext context)
        {
            ConversionModifier.TemporaryBlockRateOverride = overrideRate;
            GameLogger.Log($"[BlockDiscount] 下一回合格挡转化率改为（{overrideRate + 1}:1）");
        }
    }
}
