using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "CorruptAddCardEffect", menuName = "MutationChess/Relic Effects/Corrupt Add Card")]
    public class CorruptAddCardEffect : CardEffect
    {
        [Header("腐化卡牌配置")]
        [Tooltip("战斗开始时添加腐化卡牌的额外数量")]
        [Min(1)]
        public int count = 1;

        [Tooltip("Boss战下额外添加的腐化卡牌数量")]
        public int bossExtraCount = 1;

        public override string GetDescription(Card card)
        {
            return $"将 {count} 张腐化卡加入手牌";
        }

        public override void Execute(CombatContext context)
        {
            AddCorruptCardsToHand(context);
        }

        public override void Execute(EffectContext context)
        {
            AddCorruptCardsToHand(context.combat);
        }

        private void AddCorruptCardsToHand(CombatContext combat)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogError("[CorruptAddCard] HandManager 为空");
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
                GameLogger.LogWarning("[CorruptAddCard] 未找到任何腐化标签卡牌");
                return;
            }

            int totalCount = count + (ConversionModifier.BossCorruptLiverActive ? bossExtraCount : 0);
            List<string> addedNames = new List<string>();

            for (int i = 0; i < totalCount; i++)
            {
                CardName pick = candidates[Random.Range(0, candidates.Count)];
                Card newCard = CardData.CreateCard(pick);
                if (newCard != null)
                {
                    handManager.AddCardToHand(newCard);
                    addedNames.Add(newCard.cardName);
                    GameLogger.Log($"[CorruptAddCard] 添加腐化卡: {newCard.cardName}");
                }
            }

            if (addedNames.Count > 0)
            {
                combat?.battleManager?.AddLog($"腐化之力生效，手中加入了 {addedNames.Count} 张腐化卡牌：{string.Join("、", addedNames)}");
            }
        }
    }
}
