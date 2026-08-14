using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 冰霜雪花效果
    /// Boss 加成：格挡消耗 -1
    /// </summary>
    [CreateAssetMenu(fileName = "FrostSnowflakeEffect", menuName = "MutationChess/Relic Effects/Frost Snowflake")]
    public class FrostSnowflakeEffect : CardEffect
    {
        [Tooltip("战斗开始时获得的格挡值")]
        public int startBlock = 5;

        [Tooltip("Boss 加成时格挡消耗减少值")]
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
                GameLogger.Log($"[FrostSnowflake] Boss 加成：格挡消耗 -{bossBlockCostReduction}，当前总减免 {ConversionModifier.PermanentBlockRateReduction}");
            }
        }
    }
}
