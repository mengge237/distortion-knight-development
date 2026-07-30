using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 幻影减伤增强效果：额外增加幻影触发后的减伤值
    /// 影响 PhantomAfter5CardsEffect.GetActiveReduction() 的返回值
    /// </summary>
    [CreateAssetMenu(fileName = "PhantomAfter5CardsBoostEffect", menuName = "MutationChess/Relic Effects/Phantom Boost")]
    public class PhantomAfter5CardsBoostEffect : CardEffect
    {
        [Tooltip("额外增加的减伤值")]
        public int extraReduction = 3;

        public override void Execute(CombatContext context)
        {
            ApplyBoost();
        }

        public override void Execute(EffectContext context)
        {
            ApplyBoost();
        }

        private void ApplyBoost()
        {
            ConversionModifier.PhantomExtraReduction += extraReduction;
            GameLogger.Log($"[PhantomBoost] 额外减伤 +{extraReduction}，当前累计：{ConversionModifier.PhantomExtraReduction}");
        }
    }
}
