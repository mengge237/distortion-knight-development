using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>





    /// </summary>
    [CreateAssetMenu(fileName = "ReluctantBonusDrawEffect", menuName = "MutationChess/Relic Effects/Reluctant Bonus Draw")]
    public class ReluctantBonusDrawEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int bonusDraw = 1;

        public override void Execute(CombatContext context)
        {
            TryBonusDraw(context);
        }

        public override void Execute(EffectContext context)
        {
            TryBonusDraw(context?.combat);
        }

        private void TryBonusDraw(CombatContext context)
        {
            if (context == null || context.sourceCard == null) return;


            bool isReluctantCard = context.sourceCard.HasTag(CardTag.Reluctant)
                || context.sourceCard.faction == CardFaction.Reluctant;
            if (!isReluctantCard) return;


            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.DrawCards(bonusDraw);
                GameLogger.Log($"[ReluctantBonusDraw]  {context.sourceCard.cardName}  +{bonusDraw}");
            }
        }
    }
}


