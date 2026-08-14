using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 粘液固有效果：打出粘液系卡牌时触发相邻粘液卡牌的效果
    /// </summary>
    [CreateAssetMenu(fileName = "SlimeInherentEffect", menuName = "MutationChess/Inherent/Slime")]
    public class SlimeInherentEffect : InherentEffect
    {
        public override CardTag Tag => CardTag.Slime;

        public override void ApplyInherent(CombatContext context)
        {
            if (context?.sourceCard == null) return;

            var handManager = UI.HandManager.Instance;
            if (handManager == null) return;

            // 药水全手牌触发期间抑制相邻联动（只执行卡牌自身效果）
            if (SlimeTriggerGuard.SuppressAdjacency) return;

            if (!SlimeTriggerGuard.TryEnter(context.sourceCard))
            {
                GameLogger.Log($"[史莱姆联动] {context.sourceCard.cardName} 已在触发链中，跳过（防递归）");
                return;
            }

            try
            {
                var handCards = handManager.GetHandCards();
                bool removed = SlimeEffect.ResolvePlayedIndex(handCards, context.sourceCard, out int playedIndex);
                if (playedIndex < 0) return;

                int range = SlimeExpandEffect.SlimeTriggerRange > 0
                    ? SlimeExpandEffect.SlimeTriggerRange : 1;

                for (int offset = -range; offset <= range; offset++)
                {
                    if (offset == 0) continue;
                    int idx = SlimeEffect.GetAdjacentIndex(playedIndex, offset, removed);
                    if (idx >= 0 && idx < handCards.Count)
                    {
                        Card adj = handCards[idx];
                        if (adj != null && adj.HasTag(CardTag.Slime))
                        {
                            GameLogger.Log($"[史莱姆联动] 触发相邻: {adj.cardName} ({offset})");
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
            finally
            {
                SlimeTriggerGuard.Exit(context.sourceCard);
            }
        }
    }
}