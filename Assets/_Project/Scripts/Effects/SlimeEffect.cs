using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using System.Collections.Generic;

namespace MutationChess.Core
{
    /// <summary>
    /// 粘液效果：打出粘液系卡牌时触发相邻粘液卡牌的效果
    /// </summary>
    [CreateAssetMenu(fileName = "SlimeEffect", menuName = "MutationChess/Card Effects/Slime Effect")]
    public class SlimeEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context == null || context.sourceCard == null)
            {
                GameLogger.LogWarning("SlimeEffect: context 或 sourceCard 为空");
                return;
            }

            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("SlimeEffect: HandManager 为空");
                return;
            }



            Card playedCard = context.sourceCard;
            List<Card> handCards = handManager.GetHandCards();
            int playedIndex = handCards.IndexOf(playedCard);
            if (playedIndex < 0)
            {
                GameLogger.LogWarning("SlimeEffect: 未在手牌中找到打出的卡牌");
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
                        GameLogger.Log($"SlimeEffect: 触发相邻卡牌 {adj.cardName}（偏移 {offset}）");
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