using UnityEngine;
using MutationChess.UI;
using MutationChess.Battle;
using System.Collections.Generic;

namespace MutationChess.Core
{
    /// <summary>
    /// 腐化力量 / 影刃效果：消耗手牌中的腐化(Corrupt)标签卡，每消耗一张获得对应力量。
    /// 默认卡牌配置：magicNumber=消耗上限(3)，每次消耗1张腐化卡获得1点力量。
    /// 触发方式：玩家主动打出影刃等卡时，Execute(CombatContext) 执行消耗逻辑并加力量。
    /// </summary>
    [CreateAssetMenu(fileName = "CorruptPowerEffect", menuName = "MutationChess/Relic Effects/Corrupt Heart")]
    public class CorruptPowerEffect : CardEffect
    {
        [Header("力量配置")]
        [Tooltip("每张消耗的腐化卡提供的力量点数")]
        [Min(1)]
        public int strengthPerCard = 1;

        [Tooltip("单次最大消耗数量（0=消耗全部），若卡牌 magicNumber>0 则优先用 magicNumber")]
        [Min(0)]
        public int maxExhaustPerUse = 0;

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
                GameLogger.Log("[CorruptPowerEffect] 手牌中无腐化卡，跳过");
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
                $"[CorruptPowerEffect] 消耗 {toExhaust.Count} 张腐化卡，获得 {strengthGain} 力量" +
                $"（{strengthPerCard}力量/卡，上限{(limit > 0 ? limit.ToString() : "全部")}）");
        }
    }
}
