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
        [Header("")]
        [Tooltip("")]
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
            GameLogger.Log($"[FrostBonusBlock] {playedCard.cardName} +{effectiveBlock} ??{(ConversionModifier.BossFrostHeartActive ? "??Boss" : "")}");
        }
    }
}


