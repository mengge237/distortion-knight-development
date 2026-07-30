using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 消耗抽牌效果
    /// Boss 加成：抽牌数 +2（翻倍）
    /// </summary>
    [CreateAssetMenu(fileName = "ExhaustDrawEffect", menuName = "MutationChess/Relic Effects/Exhaust Draw")]
    public class ExhaustDrawEffect : CardEffect
    {
        [Header("抽牌配置")]
        [Tooltip("消耗时抽牌的数量")]
        public int drawCount = 1;

        public override string GetDescription(Card card)
        {
            return $"消耗 1 张手牌，抽 {drawCount} 张牌";
        }

        public override void Execute(CombatContext context)
        {
            TryDrawCard(context);
        }

        public override void Execute(EffectContext context)
        {
            TryDrawCard(context?.combat);
        }

        private void TryDrawCard(CombatContext context)
        {
            if (context == null) return;

            int effectiveDraw = drawCount;
            if (ConversionModifier.BossCorruptLiverActive)
                effectiveDraw = drawCount * 2;

            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.DrawCards(effectiveDraw);
                GameLogger.Log($"[ExhaustDraw] 抽牌 {effectiveDraw} 张 {(ConversionModifier.BossCorruptLiverActive ? "(Boss加倍)" : "")}");
            }
        }
    }
}
