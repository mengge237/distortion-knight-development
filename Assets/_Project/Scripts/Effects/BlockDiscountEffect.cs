using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "BlockDiscountEffect", menuName = "MutationChess/Potion Effects/Block Discount")]
    public class BlockDiscountEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int overrideRate = 1;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.TemporaryBlockRateOverride = overrideRate;
            GameLogger.Log($"[BlockDiscount]  {overrideRate}=1");

            if (context?.battleManager != null)
                context.battleManager.AddBattleLog(" 1:1");
        }

        public override void Execute(EffectContext context)
        {
            ConversionModifier.TemporaryBlockRateOverride = overrideRate;
            GameLogger.Log($"[BlockDiscount]  {overrideRate}=1");
        }
    }
}


