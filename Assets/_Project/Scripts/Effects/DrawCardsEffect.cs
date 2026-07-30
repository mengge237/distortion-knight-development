using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DrawCards", menuName = "MutationChess/Effects/Draw Cards")]
    public class DrawCardsEffect : CardEffect
    {
        [Header("抽牌配置")]
        [Tooltip("默认抽牌数量，为0时使用卡牌magicNumber")]
        public int drawCount = 1;

        public override string GetDescription(Card card)
        {
            int actualDraw = drawCount > 0 ? drawCount : (card != null && card.magicNumber > 0 ? card.magicNumber : 1);
            return $"抽 {actualDraw} 张牌";
        }

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager != null && context.sourceCard != null)
            {
                int actualDraw = drawCount > 0 ? drawCount : (context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : 1);
                handManager.DrawCards(actualDraw);
                context.battleManager?.AddLog($"抽了 {actualDraw} 张牌");
            }
        }
    }
}
