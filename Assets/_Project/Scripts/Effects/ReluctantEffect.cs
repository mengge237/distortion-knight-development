using UnityEngine;
using MutationChess.UI;
using System.Collections.Generic;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ReluctantEffect", menuName = "MutationChess/Card Effects/Reluctant Effect")]
    public class ReluctantEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                Debug.LogWarning("ReluctantEffect: HandManager 未找到");
                return;
            }

            Card reluctantCard = null;
            int reluctantIndex = -1;

            List<Card> drawPile = handManager.GetDrawPile();

            for (int i = 0; i < drawPile.Count; i++)
            {
                if (drawPile[i] != null && drawPile[i].HasKeyword("不舍"))
                {
                    reluctantCard = drawPile[i];
                    reluctantIndex = i;
                    break;
                }
            }

            if (reluctantCard != null && reluctantIndex >= 0)
            {
                handManager.RemoveCardFromDrawPile(reluctantIndex);
                handManager.AddCardToHand(reluctantCard);
                Debug.Log($"ReluctantEffect: 从牌库中抽到了 {reluctantCard.cardName}");
            }
            else
            {
                Debug.Log("ReluctantEffect: 牌库中没有具有'不舍'关键字的卡牌");
            }
        }
    }
}
