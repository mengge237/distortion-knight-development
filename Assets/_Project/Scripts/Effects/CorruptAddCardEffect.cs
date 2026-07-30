using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "CorruptAddCardEffect", menuName = "MutationChess/Relic Effects/Corrupt Add Card")]
    public class CorruptAddCardEffect : CardEffect
    {
        [Header("�����ӿ�����")]
        [Tooltip("ս����ʼʱ��ӵ����Ƶĸ�����������")]
        [Min(1)]
        public int count = 1;

        [Tooltip("Bossս�¶�����ӵĸ�����������")]
        public int bossExtraCount = 1;

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
                GameLogger.LogError("[CorruptAddCard] HandManager Ϊ��");
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
                GameLogger.LogWarning("[CorruptAddCard] δ�ҵ��κθ�����ǩ����");
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
                    GameLogger.Log($"[CorruptAddCard] ��Ӹ�����: {newCard.cardName}");
                }
            }

            if (addedNames.Count > 0)
            {
                combat?.battleManager?.AddLog($"����֮����Ч�������м����� {addedNames.Count} �Ÿ������ƣ�{string.Join("��", addedNames)}");
            }
        }
    }
}
