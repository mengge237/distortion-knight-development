using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "PhantomAfter5CardsEffect", menuName = "MutationChess/Relic Effects/Phantom After 5 Cards")]
    public class PhantomAfter5CardsEffect : CardEffect
    {
        [Tooltip("")]
        public int cardThreshold = 5;

        [Tooltip("")]
        public int dmgReduction = 5;

        [Tooltip("Boss")]
        public int bossDmgReduction = 8;

        private bool triggeredThisTurn = false;

        public override void Execute(CombatContext context) { }

        public override void Execute(EffectContext context)
        {
            if (context == null || context.trigger != EffectTrigger.AfterCardsPlayed) return;

            ConversionModifier.CardsPlayedThisBattle++;

            if (triggeredThisTurn) return;
            if (ConversionModifier.CardsPlayedThisBattle < cardThreshold) return;

            triggeredThisTurn = true;
            int effectiveReduction = GetActiveReduction();
            GameLogger.Log($"[PhantomAfter5Cards] {cardThreshold}+{effectiveReduction}");
        }

        public int GetActiveReduction()
        {
            if (!triggeredThisTurn) return 0;
            int baseValue = ConversionModifier.BossPhantomMaskActive ? bossDmgReduction : dmgReduction;
            return baseValue + ConversionModifier.PhantomExtraReduction;
        }

        public void ResetForNewTurn() { triggeredThisTurn = false; }
    }
}
