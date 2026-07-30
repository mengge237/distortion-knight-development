using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    ///
    /// Boss
    /// </summary>
    [CreateAssetMenu(fileName = "FrostBonusBlockEffect", menuName = "MutationChess/Relic Effects/Frost Bonus Block")]
    public class FrostBonusBlockEffect : CardEffect
    {
        [Header("冰霜格挡配置")]
        [Tooltip("打出冰霜卡牌时给予的额外格挡值")]
        public int bonusBlock = 8;

        public override void Execute(CombatContext context)
        {
            GrantBonusBlock(context);
        }

        public override void Execute(EffectContext context)
        {
            GrantBonusBlock(context?.combat);
        }

        private void GrantBonusBlock(CombatContext context)
        {
            if (context == null || context.battleManager == null) return;

            Card playedCard = context.sourceCard;
            if (playedCard == null) return;

            bool isFrostCard = playedCard.HasTag(CardTag.Frost) || playedCard.faction == CardFaction.Frost;
            if (!isFrostCard) return;

            int effectiveBlock = bonusBlock;
            if (ConversionModifier.BossFrostHeartActive)
                effectiveBlock *= 2;

            context.battleManager.PlayerBlock(effectiveBlock);
            GameLogger.Log($"[FrostBonusBlock] {playedCard.cardName} +{effectiveBlock} 格挡{(ConversionModifier.BossFrostHeartActive ? " (Boss加倍)" : "")}");
        }
    }
}


