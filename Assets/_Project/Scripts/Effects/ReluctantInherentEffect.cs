using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    ///
    /// </summary>
    [CreateAssetMenu(fileName = "ReluctantInherentEffect", menuName = "MutationChess/Inherent/Reluctant")]
    public class ReluctantInherentEffect : InherentEffect
    {
        public override CardTag Tag => CardTag.Reluctant;

        [Tooltip("")]
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
                    GameLogger.Log($"[] z: {card.cardName}");
                    drawn++;
                    i--;
                }
            }

            if (drawn == 0)
                GameLogger.Log("[] ");
        }
    }
}
