using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using System.Collections.Generic;

namespace MutationChess.Core
{
    /// <summary>



    /// </summary>
    [CreateAssetMenu(fileName = "SlimeEffect", menuName = "MutationChess/Card Effects/Slime Effect")]
    public class SlimeEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context == null || context.sourceCard == null)
            {
                GameLogger.LogWarning("SlimeEffect: ");
                return;
            }

            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("SlimeEffect: HandManager ");
                return;
            }



            Card playedCard = context.sourceCard;
            List<Card> handCards = handManager.GetHandCards();
            int playedIndex = handCards.IndexOf(playedCard);
            if (playedIndex < 0)
            {
                GameLogger.LogWarning("SlimeEffect: ");
                return;
            }

            int range = SlimeExpandEffect.SlimeTriggerRange > 0
                ? SlimeExpandEffect.SlimeTriggerRange : 1;

            for (int offset = -range; offset <= range; offset++)
            {
                if (offset == 0) continue;
                int idx = playedIndex + offset;
                if (idx >= 0 && idx < handCards.Count)
                {
                    Card adj = handCards[idx];
                    if (adj != null && (adj.HasTag(CardTag.Slime) || adj.faction == CardFaction.Slime))
                    {
                        GameLogger.Log($"SlimeEffect:  {adj.cardName} ( {offset})");
                        CombatContext adjCtx = new CombatContext(
                            context.battleManager,
                            context.targetEnemy,
                            context.targetPlayer,
                            adj
                        );
                        adj.ExecuteEffects(adjCtx);
                    }
                }
            }
        }
    }
}


