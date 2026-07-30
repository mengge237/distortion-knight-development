using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "AbyssEvery4AttackEffect", menuName = "MutationChess/Relic Effects/Abyss Every 4 Attack")]
    public class AbyssEvery4AttackEffect : CardEffect
    {
        [Tooltip("")]
        public int threshold = 4;

        [Tooltip("")]
        public float dmgMultiplier = 2f;

        public override void Execute(CombatContext context) { }

        public override void Execute(EffectContext context)
        {
            if (context == null) return;

            if (context.trigger == EffectTrigger.CardPlayed)
            {
                Card playedCard = context.tag as Card;
                if (playedCard != null && playedCard.cardType == CardType.Attack)
                {
                    ConversionModifier.AttackCardsPlayedThisBattle += Mathf.RoundToInt(context.floatValue > 0 ? context.floatValue : 1);
                }
                return;
            }

            if (context.trigger != EffectTrigger.CalculateAttackDamage) return;

            int effectiveThreshold = threshold - (ConversionModifier.BossMemoryLensActive ? 1 : 0) - ConversionModifier.AbyssThresholdReduction;
            if (ConversionModifier.AttackCardsPlayedThisBattle < effectiveThreshold) return;

            context.finalValue = Mathf.RoundToInt(context.baseValue * dmgMultiplier);
            GameLogger.Log($"[AbyssEvery4Attack] {effectiveThreshold}x{dmgMultiplier}: {context.baseValue} -> {context.finalValue}");
        }
    }
}
