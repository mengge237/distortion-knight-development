using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 鲜血狂暴效果
    /// </summary>
    [CreateAssetMenu(fileName = "BloodRageEffect", menuName = "MutationChess/Potion Effects/Blood Rage")]
    public class BloodRageEffect : CardEffect
    {
        [Header("鲜血狂暴配置")]
        [Tooltip("默认鲜血转化率")]
        public int defaultBloodRate = 3;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.BloodConversionForAll = true;
            ConversionModifier.TemporaryBloodRateOverride = defaultBloodRate;
            GameLogger.Log($"[BloodRage] 玩家进入鲜血狂暴状态，所有卡牌均可进行鲜血之转化，转化率为（{defaultBloodRate}:1）");

            if (context?.battleManager != null)
                context.battleManager.AddBattleLog($"玩家进入鲜血狂暴状态，所有卡牌均可进行鲜血之转化，转化率为（{defaultBloodRate}:1）");
        }

        public override void Execute(EffectContext context)
        {
            ConversionModifier.BloodConversionForAll = true;
            ConversionModifier.TemporaryBloodRateOverride = defaultBloodRate;
            GameLogger.Log($"[BloodRage] 玩家进入鲜血狂暴状态，所有卡牌均可进行鲜血之转化，转化率为（{defaultBloodRate}:1）");

            if (context?.battleManager != null)
                context.battleManager.AddBattleLog($"玩家进入鲜血狂暴状态，所有卡牌均可进行鲜血之转化，转化率为（{defaultBloodRate}:1）");
        }
    }
}
