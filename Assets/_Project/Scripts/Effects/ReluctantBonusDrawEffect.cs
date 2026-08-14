using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 勉强卡牌额外抽牌效果
    /// Boss 加成：抽牌数 +2（翻倍）
    /// </summary>
    [CreateAssetMenu(fileName = "ReluctantBonusDrawEffect", menuName = "MutationChess/Relic Effects/Reluctant Bonus Draw")]
    public class ReluctantBonusDrawEffect : CardEffect
    {
        [Header("抽牌配置")]
        [Tooltip("打出勉强卡牌时额外抽牌的数量")]
        public int bonusDraw = 1;

        public override void Execute(CombatContext context)
        {
            TryBonusDraw(context);
        }

        public override void Execute(EffectContext context)
        {
            TryBonusDraw(context?.combat);
        }

        private void TryBonusDraw(CombatContext context)
        {
            if (context == null || context.sourceCard == null) return;

            bool isReluctantCard = context.sourceCard.HasTag(CardTag.Reluctant)
                || context.sourceCard.faction == CardFaction.Reluctant;

            if (!isReluctantCard) return;

            int effectiveDraw = bonusDraw;
            if (ConversionModifier.BossReluctantChainActive)
                effectiveDraw = bonusDraw * 2;

            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.DrawCards(effectiveDraw);
                GameLogger.Log($"[ReluctantBonusDraw] {context.sourceCard.cardName} 额外抽牌 {effectiveDraw} 张{(ConversionModifier.BossReluctantChainActive ? " (Boss加倍)" : "")}");
            }
        }
    }
}
