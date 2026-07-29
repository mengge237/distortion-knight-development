using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DiscardRandomCard", menuName = "MutationChess/Effects/Discard Random Card")]
    public class DiscardRandomCardEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager == null || handManager.GetHandSize() == 0) return;

            var handCards = handManager.GetHandCards();
            if (handCards.Count > 0)
            {
                int index = Random.Range(0, handCards.Count);
                Card cardToDiscard = handCards[index];

                handManager.DiscardCard(cardToDiscard);
            }
        }
    }
}