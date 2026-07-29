using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>





    /// </summary>
    [CreateAssetMenu(fileName = "SlimeEnergyEffect", menuName = "MutationChess/Relic Effects/Slime Energy")]
    public class SlimeEnergyEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int energyGain = 1;

        public override void Execute(CombatContext context)
        {
            TryGrantEnergy(context);
        }

        public override void Execute(EffectContext context)
        {
            TryGrantEnergy(context?.combat);
        }

        private void TryGrantEnergy(CombatContext context)
        {
            if (context == null || context.sourceCard == null) return;


            bool isSlimeCard = context.sourceCard.HasTag(CardTag.Slime)
                || context.sourceCard.faction == CardFaction.Slime;
            if (!isSlimeCard) return;


            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.RestoreEnergy(energyGain);
                GameLogger.Log($"[SlimeEnergy]  {context.sourceCard.cardName}  +{energyGain}");
            }
        }
    }
}


