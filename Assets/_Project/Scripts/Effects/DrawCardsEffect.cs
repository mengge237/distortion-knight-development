using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 抽牌效果（兼容卡牌与遗物两种触发来源，无需 sourceCard）。
    /// 由 EffectMergeMigration 工具合并了 DrawingPadDraw2Effect / ExhaustDrawEffect。
    /// </summary>
    [CreateAssetMenu(fileName = "DrawCards", menuName = "MutationChess/Effects/Draw Cards")]
    public class DrawCardsEffect : CardEffect
    {
        [Header("抽牌配置")]
        [Tooltip("默认抽牌数量，为0时使用卡牌magicNumber")]
        public int drawCount = 1;

        [Header("Boss 加成")]
        [Tooltip("Boss腐化之肝激活时抽牌翻倍")]
        public bool bossDouble = false;

        public override string GetDescription(Card card)
        {
            int actualDraw = drawCount > 0 ? drawCount : (card != null && card.magicNumber > 0 ? card.magicNumber : 1);
            return $"抽 {actualDraw} 张牌";
        }

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[DrawCards] HandManager 为空");
                return;
            }

            int actualDraw = drawCount;
            if (actualDraw <= 0 && context.sourceCard != null && context.sourceCard.magicNumber > 0)
                actualDraw = context.sourceCard.magicNumber;
            if (actualDraw <= 0)
                actualDraw = 1;

            if (bossDouble && ConversionModifier.BossCorruptLiverActive)
                actualDraw *= 2;

            handManager.DrawCards(actualDraw);
            context.battleManager?.AddLog($"抽了 {actualDraw} 张牌");
            GameLogger.Log($"[DrawCards] 抽牌 {actualDraw} 张{(bossDouble && ConversionModifier.BossCorruptLiverActive ? " (Boss加倍)" : "")}");
        }
    }
}
