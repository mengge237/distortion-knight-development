using UnityEngine;
using MutationChess.Battle;
using MutationChess.UI;
using System.Collections.Generic;

namespace MutationChess.Core
{
    /// <summary>
    /// 腐化力量 / 暗影效果：消耗手牌中的腐化(Corrupt)标签卡，每消耗一张获得对应力量。
    /// 默认可消耗数量：magicNumber=消耗上限(3)，每消耗1张腐化卡获得1点力量。
    /// 触发方式：打牌或暗影等时段由 Execute(CombatContext) 执行核心逻辑。
    /// </summary>
    [CreateAssetMenu(fileName = "CorruptPowerEffect", menuName = "MutationChess/Relic Effects/Corrupt Heart")]
    public class CorruptPowerEffect : CardEffect
    {
        [Header("腐化力量配置")]
        [Tooltip("每消耗的腐化卡可提供的力量值")]
        [Min(1)]
        public int strengthPerCard = 1;

        [Tooltip("单次消耗上限。0=不限制全部消耗，若 magicNumber>0 则使用 magicNumber")]
        [Min(0)]
        public int maxExhaustPerUse = 0;

        public override string GetDescription(Card card)
        {
            int limit = (card != null && card.magicNumber > 0) ? card.magicNumber : maxExhaustPerUse;
            string limitText = limit > 0 ? $"（上限 {limit} 张）" : "";
            return $"消耗手牌腐化卡，每张获得 {strengthPerCard} 点力量{limitText}";
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
                GameLogger.Log("[CorruptPowerEffect] 手牌无腐化卡可消耗");
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
                $"[CorruptPowerEffect] 消耗 {toExhaust.Count} 张腐化卡，获得 {strengthGain} 点力量" +
                $"（{strengthPerCard}点/张，上限{(limit > 0 ? limit.ToString() : "全部")}）");
        }
    }
}
