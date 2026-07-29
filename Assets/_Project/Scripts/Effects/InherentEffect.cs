using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 卡牌字段（Tag）枚举。
    /// 字段代表卡牌的系列属性，大部分系列卡牌会同时拥有多个字段。
    /// 字段不一定带有固有特效，但大部分系列卡牌会触发对应字段的固有效果。
    /// </summary>
    public enum CardTag
    {
        None = 0,
        Slime = 1,       // 粘液：相邻卡牌触发效果
        Reluctant = 2,   // 不舍：从牌库中抽一张不舍卡牌
        Blood = 3,       // 鲜血：血量转换支持费用
        Frost = 4,       // 寒霜：格挡转换支持费用
        Corrupt = 5,     // 腐化：消耗类卡牌
        Shadow = 6,      // 暗影：暗影系列卡牌
        Curse = 7,       // 诅咒：负面卡牌，无法主动获得
    }

    /// <summary>
    /// 固有效果基类。
    /// 字段的固有效果不一定是基础效果，可能联动获得。
    /// 例如：不舍 = DrawCardsEffect(1) + 不舍字段；粘液 = 相邻卡牌触发效果。
    /// 对于鲜血和寒霜的费用转换不需要固有特效，由 Card 字段和 HandManager 直接处理。
    /// </summary>
    public abstract class InherentEffect : CardEffect
    {
        /// <summary>
        /// 此固有效果对应的字段。
        /// </summary>
        public abstract CardTag Tag { get; }

        /// <summary>
        /// 卡牌被打出时应该应用固有效果。
        /// </summary>
        public abstract void ApplyInherent(CombatContext context);

        public override void Execute(CombatContext context)
        {
            ApplyInherent(context);
        }

        /// <summary>
        /// 判断卡牌是否应该应用此固有效果（拥有对应字段）。
        /// </summary>
        public bool ShouldApply(Card card)
        {
            if (card == null) return false;
            return card.HasTag(Tag);
        }
    }

    /// <summary>
    /// 粘液字段固有效果：打出时触发相邻粘液牌的效果。
    /// 此效果比较特殊，无法直接附加给效果，需要单独实现。
    /// 支持 SlimeExpandEffect 的扩展（范围默认1，可扩展为2）。
    /// </summary>
    [CreateAssetMenu(fileName = "SlimeInherent", menuName = "MutationChess/Inherent/Slime")]
    public class SlimeInherentEffect : InherentEffect
    {
        public override CardTag Tag => CardTag.Slime;

        public override void ApplyInherent(CombatContext context)
        {
            if (context?.sourceCard == null) return;

            var handManager = UI.HandManager.Instance;
            if (handManager == null) return;

            var handCards = handManager.GetHandCards();
            int playedIndex = handCards.IndexOf(context.sourceCard);
            if (playedIndex < 0) return;

            // 获取 SlimeExpandEffect 设置的范围（默认1，可扩展为2）
            int range = SlimeExpandEffect.SlimeTriggerRange > 0
                ? SlimeExpandEffect.SlimeTriggerRange : 1;

            // 触发左右各 range 张粘液牌
            for (int offset = -range; offset <= range; offset++)
            {
                if (offset == 0) continue;  // 跳过自身
                int idx = playedIndex + offset;
                if (idx >= 0 && idx < handCards.Count)
                {
                    Card adj = handCards[idx];
                    if (adj != null && adj.HasTag(CardTag.Slime))
                    {
                        GameLogger.Log($"[粘液] 触发相邻粘液牌: {adj.cardName} (偏移 {offset})");
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

    /// <summary>
    /// 不舍字段固有效果：从牌库抽一张不舍卡牌。
    /// 本质是 DrawCardsEffect 的变体，只是过滤指定字段。
    /// </summary>
    [CreateAssetMenu(fileName = "ReluctantInherent", menuName = "MutationChess/Inherent/Reluctant")]
    public class ReluctantInherentEffect : InherentEffect
    {
        public override CardTag Tag => CardTag.Reluctant;

        [Tooltip("抽卡数量")]
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
                    GameLogger.Log($"[不舍] 从牌库抽到: {card.cardName}");
                    drawn++;
                    i--; // 索引修正
                }
            }

            if (drawn == 0)
                GameLogger.Log("[不舍] 牌库里没有不舍卡牌");
        }
    }
}
