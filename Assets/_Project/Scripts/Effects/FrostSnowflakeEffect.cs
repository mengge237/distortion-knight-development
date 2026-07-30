using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    ///
    /// Boss-1??
    /// </summary>
    [CreateAssetMenu(fileName = "FrostSnowflakeEffect", menuName = "MutationChess/Relic Effects/Frost Snowflake")]
    public class FrostSnowflakeEffect : CardEffect
    {
        [Tooltip("")]
        public int startBlock = 5;

        [Tooltip("Boss")]
        public int bossBlockCostReduction = 1;

        public override void Execute(CombatContext context)
        {
            ApplySnowflake(context);
        }

        public override void Execute(EffectContext context)
        {
            ApplySnowflake(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }

        private void ApplySnowflake(CombatContext context)
        {
            if (context == null || context.battleManager == null) return;
            context.battleManager.PlayerBlock(startBlock);
            GameLogger.Log($"[FrostSnowflake] 获得 {startBlock} 格挡");

            if (ConversionModifier.BossFrostHeartActive)
            {
                ConversionModifier.PermanentBlockRateReduction += bossBlockCostReduction;
                GameLogger.Log($"[FrostSnowflake] Boss-{bossBlockCostReduction}{ConversionModifier.PermanentBlockRateReduction}");
            }
        }
    }
}
