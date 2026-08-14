using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 勉强卡牌格挡加成效果
    /// 当打出带有 Reluctant 标签的卡牌时给予额外格挡
    /// Boss 加成：格挡值翻倍（base 2 -> 4）
    /// </summary>
    [CreateAssetMenu(fileName = "ReluctantBlockBonusEffect", menuName = "MutationChess/Relic Effects/Reluctant Block Bonus")]
    public class ReluctantBlockBonusEffect : CardEffect
    {
        [Header("格挡配置")]
        [Tooltip("打出勉强卡牌时给予的格挡值")]
        public int blockAmount = 2;

        public override void Execute(CombatContext context)
        {
            GrantReluctantBlock(context);
        }

        public override void Execute(EffectContext context)
        {
            GrantReluctantBlock(context?.combat);
        }

        private void GrantReluctantBlock(CombatContext context)
        {
            if (context == null || context.battleManager == null) return;

            Card playedCard = context.sourceCard;
            if (playedCard == null || !playedCard.HasTag(CardTag.Reluctant)) return;

            int effectiveBlock = blockAmount;
            if (ConversionModifier.BossReluctantChainActive)
                effectiveBlock = blockAmount * 2;

            context.battleManager.PlayerBlock(effectiveBlock);
            GameLogger.Log($"[ReluctantBlock] {playedCard.cardName} 赋予格挡 {effectiveBlock} 点{(ConversionModifier.BossReluctantChainActive ? " (Boss加倍)" : "")}");
        }
    }
}
