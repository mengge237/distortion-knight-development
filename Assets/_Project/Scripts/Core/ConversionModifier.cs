using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    ///
    ///
    /// - 13:22:2
    /// - 1:1
    /// - 1:1
    /// ResetTemporary() 
    /// </summary>
    public static class ConversionModifier
    {
        // === ===
        public static int PermanentBloodRateReduction = 0;
        public static int PermanentBlockRateReduction = 0;

        // === ===
        [Tooltip("0=>0=")]
        public static int TemporaryBloodRateOverride = 0;

        [Tooltip("0=>0=")]
        public static int TemporaryBlockRateOverride = 0;

        // === ===
        public static bool BloodConversionForAll = false;
        public static bool BlockConversionForAll = false;

        // === ===
        public static bool AllCardsNoExhaustThisTurn = false;  //
        public static bool CorruptNoExhaustPermanent = false; //

        // === ===
        public static bool TagEffectDoubleTrigger = false;    //
        // === ===
        //
        public static bool ShadowStrengthNoDecay = true;

        // === Boss===
        public static bool BossBloodVeinActive = false;      //
        public static bool BossFrostHeartActive = false;     //
        public static bool BossCorruptLiverActive = false;   //
        public static bool BossSlimeGlandActive = false;     //
        public static bool BossReluctantChainActive = false; //
        public static bool BossMemoryLensActive = false;     //
        public static bool BossAcidicCoreActive = false;     //
        public static bool BossPhantomMaskActive = false;    //

        // === ===
        public static int AbyssThresholdReduction = 0;       //
        public static int PhantomExtraReduction = 0;         //

        // === Boss ===
        public static int TurnCounterForMemoryLens = 0;      //
        public static int CardsPlayedThisBattle = 0;         //
        public static int AttackCardsPlayedThisBattle = 0;   //

        /// <summary>
        ///
        ///
        /// </summary>
        public static int GetEffectiveBloodRate(int baseRate)
        {
            if (TemporaryBloodRateOverride > 0) return TemporaryBloodRateOverride;
            return Mathf.Max(1, baseRate - PermanentBloodRateReduction);
        }

        /// <summary>
        ///
        /// </summary>
        public static int GetEffectiveBlockRate(int baseRate)
        {
            if (TemporaryBlockRateOverride > 0) return TemporaryBlockRateOverride;
            return Mathf.Max(1, baseRate - PermanentBlockRateReduction);
        }

        /// <summary>
        ///
        /// </summary>
        public static bool ShouldExhaust(Card card)
        {
            if (card == null) return false;
            if (AllCardsNoExhaustThisTurn) return false;
            if (CorruptNoExhaustPermanent && card.HasTag(CardTag.Corrupt)) return false;
            return card.exhaust;
        }

        /// <summary>
    ///
        /// </summary>
        public static void ResetTemporary()
        {
            TemporaryBloodRateOverride = 0;
            TemporaryBlockRateOverride = 0;
            BloodConversionForAll = false;
            BlockConversionForAll = false;
            AllCardsNoExhaustThisTurn = false;
            TagEffectDoubleTrigger = false;
            CardsPlayedThisBattle = 0;
            AttackCardsPlayedThisBattle = 0;
        }

        /// <summary>
        ///
        /// </summary>
        public static void ResetAll()
        {
            PermanentBloodRateReduction = 0;
            PermanentBlockRateReduction = 0;
            CorruptNoExhaustPermanent = false;
            ShadowStrengthNoDecay = true;
            BossBloodVeinActive = false;
            BossFrostHeartActive = false;
            BossCorruptLiverActive = false;
            BossSlimeGlandActive = false;
            BossReluctantChainActive = false;
            BossMemoryLensActive = false;
            BossAcidicCoreActive = false;
            BossPhantomMaskActive = false;
            AbyssThresholdReduction = 0;
            PhantomExtraReduction = 0;
            TurnCounterForMemoryLens = 0;
            ResetTemporary();
        }
    }
}
