using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 腐化增卡遗物效果：消耗卡牌时随机往手牌加入一张腐化标签卡牌。
    /// 触发时机：CardExhausted。
    /// 从 CardData.GetAllCardNames() 中筛选拥有 Corrupt 标签的模板，随机选一张创建并加入手牌。
    /// 不依赖 context.combat，仅依赖 HandManager 与 CardData。
    /// </summary>
    [CreateAssetMenu(fileName = "CorruptAddCardEffect", menuName = "MutationChess/Relic Effects/Corrupt Add Card")]
    public class CorruptAddCardEffect : CardEffect
    {
        [Header("腐化增卡")]
        [Tooltip("每次触发加入手牌的腐化卡数量")]
        [Min(1)]
        public int count = 1;

        public override void Execute(CombatContext context)
        {
            AddCorruptCardsToHand();
        }

        public override void Execute(EffectContext context)
        {
            AddCorruptCardsToHand();
        }

        private void AddCorruptCardsToHand()
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogError("[CorruptAddCard] HandManager 为空！");
                return;
            }

            List<CardName> candidates = new List<CardName>();
            foreach (var name in CardData.GetAllCardNames())
            {
                var template = CardData.GetTemplate(name);
                if (template != null && template.tags != null && template.tags.Contains(CardTag.Corrupt))
                {
                    candidates.Add(name);
                }
            }

            if (candidates.Count == 0)
            {
                GameLogger.LogWarning("[CorruptAddCard] 卡池中没有腐化标签卡牌");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                CardName pick = candidates[Random.Range(0, candidates.Count)];
                Card newCard = CardData.CreateCard(pick);
                if (newCard != null)
                {
                    handManager.AddCardToHand(newCard);
                    GameLogger.Log($"[CorruptAddCard] 随机加入腐化卡: {newCard.cardName}");
                }
            }
        }
    }
}
