using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    ///
    ///
    /// </summary>
    [CreateAssetMenu(fileName = "BloodCostReductionEffect", menuName = "MutationChess/Relic Effects/Blood Cost Reduction")]
    public class BloodCostReductionEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int costReduction = 1;

        public override void Execute(CombatContext context)
        {
            //
        }

        public override void Execute(EffectContext context)
        {
            if (context == null || context.trigger != EffectTrigger.CalculateCardCost) return;

            Card card = context.tag as Card;
            if (card == null) return;

            bool isBloodCard = card.HasTag(CardTag.Blood) || card.faction == CardFaction.Blood;
            if (!isBloodCard) return;

            context.finalValue = Mathf.Max(0, context.baseValue - costReduction);
            GameLogger.Log($"[BloodCostReduction] {card.cardName} {context.baseValue} -> {context.finalValue} (-{costReduction})");
        }
    }
}


