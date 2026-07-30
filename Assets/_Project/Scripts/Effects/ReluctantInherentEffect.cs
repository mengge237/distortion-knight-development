using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 不舍固有效果：开局从抽牌堆抽取不舍系卡牌到手牌
    /// </summary>
    [CreateAssetMenu(fileName = "ReluctantInherentEffect", menuName = "MutationChess/Inherent/Reluctant")]
    public class ReluctantInherentEffect : InherentEffect
    {
        public override CardTag Tag => CardTag.Reluctant;

        [Tooltip("开局抽取的不舍系卡牌数量")]
        public int drawCount = 1;

        public override void ApplyInherent(CombatContext context)
        {
            var handManager = UI.HandManager.Instance;
            if (handManager == null) return;

            var drawPile = handManager.GetDrawPile();
            int drawn = 0;

            for (int i = 0; i < drawPile.Count && drawn < drawCount; i++)
            {
                if (drawPile[i] != null && drawPile[i].HasTag(CardTag.Reluctant))
                {
                    Card card = drawPile[i];
                    handManager.RemoveCardFromDrawPile(i);
                    handManager.AddCardToHand(card);
                    GameLogger.Log($"[ReluctantInherent] 抽取不舍卡牌：{card.cardName}");
                    drawn++;
                    i--;
                }
            }

            if (drawn == 0)
                GameLogger.Log("[ReluctantInherent] 抽牌堆中无不舍系卡牌");
        }
    }
}
