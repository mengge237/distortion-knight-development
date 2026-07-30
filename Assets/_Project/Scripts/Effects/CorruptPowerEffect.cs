using UnityEngine;
using MutationChess.UI;
using MutationChess.Battle;
using System.Collections.Generic;

namespace MutationChess.Core
{
    /// <summary>
    /// �������� / Ӱ��Ч�������������еĸ���(Corrupt)��ǩ����ÿ����һ�Ż�ö�Ӧ������
    /// Ĭ�Ͽ������ã�magicNumber=��������(3)��ÿ������1�Ÿ��������1��������
    /// ������ʽ������������Ӱ�еȿ�ʱ��Execute(CombatContext) ִ�������߼�����������
    /// </summary>
    [CreateAssetMenu(fileName = "CorruptPowerEffect", menuName = "MutationChess/Relic Effects/Corrupt Heart")]
    public class CorruptPowerEffect : CardEffect
    {
        [Header("��������")]
        [Tooltip("ÿ�����ĵĸ������ṩ����������")]
        [Min(1)]
        public int strengthPerCard = 1;

        [Tooltip("�����������������0=����ȫ����������� magicNumber>0 �������� magicNumber")]
        [Min(0)]
        public int maxExhaustPerUse = 0;

        public override string GetDescription(Card card)
        {
            int limit = (card != null && card.magicNumber > 0) ? card.magicNumber : maxExhaustPerUse;
            string limitText = limit > 0 ? $"����� {limit} �ţ�" : "";
            return $"�������Ƹ�������ÿ�Ż�� {strengthPerCard} ����{limitText}";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            HandManager hm = HandManager.Instance;
            if (hm == null) return;

            PlayerData player = context.targetPlayer;
            if (player == null)
            {
                var dm = PlayerDataManager.Instance;
                if (dm != null) player = dm.GetPlayerData();
            }
            if (player == null) return;

            int limit = (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                ? context.sourceCard.magicNumber
                : maxExhaustPerUse;

            List<Card> hand = hm.GetHandCards();
            List<Card> toExhaust = new List<Card>();
            for (int i = 0; i < hand.Count; i++)
            {
                Card c = hand[i];
                if (c == null || c == context.sourceCard) continue;
                if (!c.HasTag(CardTag.Corrupt)) continue;
                if (limit > 0 && toExhaust.Count >= limit) break;
                toExhaust.Add(c);
            }

            if (toExhaust.Count == 0)
            {
                GameLogger.Log("[CorruptPowerEffect] �������޸�����������");
                return;
            }

            int strengthGain = toExhaust.Count * strengthPerCard;
            var buff = new Buff
            {
                type = BuffType.Strength,
                amount = strengthGain,
                duration = -1
            };
            player.AddBuff(buff);

            for (int i = 0; i < toExhaust.Count; i++)
                hm.AddToExhaustPile(toExhaust[i]);

            GameLogger.Log(
                $"[CorruptPowerEffect] ���� {toExhaust.Count} �Ÿ���������� {strengthGain} ����" +
                $"��{strengthPerCard}����/��������{(limit > 0 ? limit.ToString() : "ȫ��")}��");
        }
    }
}
