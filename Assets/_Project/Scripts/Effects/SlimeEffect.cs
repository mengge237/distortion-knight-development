using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using System.Collections.Generic;

namespace MutationChess.Core
{
    /// <summary>
    /// 史莱姆联动防递归守卫：相邻触发链中已在执行效果的卡牌不再重复触发，
    /// 防止 A 触发 B、B 又触发 A 的无限递归（TriggerSlimeHandEffect 触发全手牌时尤其危险）
    /// </summary>
    public static class SlimeTriggerGuard
    {
        private static readonly HashSet<Card> TriggeringCards = new HashSet<Card>();

        /// <summary>
        /// 药水"触发手牌中所有粘液卡牌"执行期间置为 true，
        /// 抑制相邻联动链（只执行卡牌自身效果，避免 N² 级重复触发）
        /// </summary>
        public static bool SuppressAdjacency = false;

        /// <summary>尝试进入触发链，返回 false 表示该卡已在链上（应跳过）</summary>
        public static bool TryEnter(Card card) => card != null && TriggeringCards.Add(card);

        public static void Exit(Card card)
        {
            if (card != null) TriggeringCards.Remove(card);
        }
    }

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

            // 药水全手牌触发期间抑制相邻联动（只执行卡牌自身效果）
            if (SlimeTriggerGuard.SuppressAdjacency) return;

            if (!SlimeTriggerGuard.TryEnter(playedCard))
            {
                GameLogger.Log($"SlimeEffect: {playedCard.cardName} 已在触发链中，跳过（防递归）");
                return;
            }

            try
            {
                List<Card> handCards = handManager.GetHandCards();
                bool removed = ResolvePlayedIndex(handCards, playedCard, out int playedIndex);
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
                    int idx = GetAdjacentIndex(playedIndex, offset, removed);
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
            finally
            {
                SlimeTriggerGuard.Exit(playedCard);
            }
        }

        /// <summary>
        /// 解析打出卡牌在手牌中的索引。
        /// 出牌后卡牌已被 HandManager 移除（IndexOf 返回 -1），此时退回出牌前记录
        /// 的 lastHandIndex，并标记 removed=true 以修正右邻偏移。
        /// </summary>
        internal static bool ResolvePlayedIndex(List<Card> handCards, Card playedCard, out int playedIndex)
        {
            playedIndex = handCards.IndexOf(playedCard);
            if (playedIndex >= 0) return false;

            playedIndex = playedCard != null ? playedCard.lastHandIndex : -1;
            return true;
        }

        /// <summary>
        /// 计算相邻牌的实际手牌索引。
        /// 卡牌已移除时，右邻牌向左移了一格：偏移 +1 对应当前索引 playedIndex。
        /// </summary>
        internal static int GetAdjacentIndex(int playedIndex, int offset, bool removed)
        {
            if (!removed || offset < 0) return playedIndex + offset;
            return playedIndex + offset - 1;
        }
    }
}