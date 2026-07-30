using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ModifyCost", menuName = "MutationChess/Effects/Modify Cost")]
    public class ModifyCostEffect : CardEffect
    {
        public int costModifier = -1;
        public bool applyToAllHand = false;

        public override string GetDescription(Card card)
        {
            string modText = costModifier >= 0 ? $"+{costModifier}" : $"{costModifier}";
            string targetText = applyToAllHand ? "ȫ������" : "������";
            return $"{targetText}����{modText}";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;
            if (context.sourceCard == null) return;

            if (applyToAllHand)
            {
                var handManager = UI.HandManager.Instance;
                if (handManager != null)
                {
                    var handCards = handManager.GetHandCards();
                    foreach (var card in handCards)
                    {
                        if (card != null)
                        {
                            card.cost = Mathf.Max(0, card.cost + costModifier);
                        }
                    }
                    GameLogger.Log($"[ModifyCostEffect] �޸� {handCards.Count} �����Ʒ��ã�����ֵ��{costModifier}");
                    handManager.UpdateHandUI();
                }
            }
            else
            {
                context.sourceCard.cost = Mathf.Max(0, context.sourceCard.cost + costModifier);
            }
        }
    }
}
