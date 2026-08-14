using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "PhantomAfter5CardsEffect", menuName = "MutationChess/Relic Effects/Phantom After 5 Cards")]
    public class PhantomAfter5CardsEffect : CardEffect
    {
        [Tooltip("触发减伤所需的出牌数量阈值")]
        public int cardThreshold = 5;

        [Tooltip("触发后减少的伤害值")]
        public int dmgReduction = 5;

        [Tooltip("Boss遗物激活时的减伤值")]
        public int bossDmgReduction = 8;

        public override void Execute(CombatContext context) { }

        public override void Execute(EffectContext context)
        {
            if (context == null || context.trigger != EffectTrigger.AfterCardsPlayed) return;

            // CardsPlayedThisBattle 已由 Card.ExecuteEffects 统一 +1，此处不再自增（否则每张牌计 2 次）
            if (ConversionModifier.PhantomReductionActive) return;
            if (ConversionModifier.CardsPlayedThisBattle < cardThreshold) return;

            ConversionModifier.PhantomReductionActive = true;
            int effectiveReduction = GetActiveReduction();
            GameLogger.Log($"[PhantomAfter5Cards] 出牌达 {cardThreshold} 张，触发减伤 {effectiveReduction}");
        }

        public int GetActiveReduction()
        {
            if (!ConversionModifier.PhantomReductionActive) return 0;
            int baseValue = ConversionModifier.BossPhantomMaskActive ? bossDmgReduction : dmgReduction;
            return baseValue + ConversionModifier.PhantomExtraReduction;
        }

        public override void ResetForBattle()
        {
            ConversionModifier.PhantomReductionActive = false;
        }
    }
}
