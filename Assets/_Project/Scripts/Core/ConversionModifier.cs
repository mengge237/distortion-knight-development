using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 转换修正器，管理鲜血/格挡转换率的全局修正与Boss遗物激活状态
    /// 包含永久减免、临时覆盖、全卡牌转换启用等修正
    /// 使用 ResetTemporary() 在每回合结束时重置临时修正
    /// 使用 ResetAll() 在战斗开始时重置所有修正
    /// </summary>
    public static class ConversionModifier
    {
        // === 永久转换率减免 ===
        public static int PermanentBloodRateReduction = 0;
        public static int PermanentBlockRateReduction = 0;

        // === 临时转换率覆盖 ===
        [Tooltip("临时鲜血转换率覆盖（0=不覆盖，>0=使用该值作为转换率）")]
        public static int TemporaryBloodRateOverride = 0;

        [Tooltip("临时格挡转换率覆盖（0=不覆盖，>0=使用该值作为转换率）")]
        public static int TemporaryBlockRateOverride = 0;

        // === 全卡牌转换启用 ===
        public static bool BloodConversionForAll = false;
        public static bool BlockConversionForAll = false;

        // === 消耗规则修改 ===
        public static bool AllCardsNoExhaustThisTurn = false;  // 本回合所有卡牌不消耗
        public static bool CorruptNoExhaustPermanent = false; // 腐化标签卡牌永久不消耗

        // === 标签效果修改 ===
        public static bool TagEffectDoubleTrigger = false;    // 标签效果双倍触发
        // === 暗影系列修正 ===
        // 暗影力量不衰减（默认启用）
        public static bool ShadowStrengthNoDecay = true;

        // === Boss遗物激活状态 ===
        public static bool BossBloodVeinActive = false;      // 血脉
        public static bool BossFrostHeartActive = false;     // 寒霜之心
        public static bool BossCorruptLiverActive = false;   // 腐化之肝
        public static bool BossSlimeGlandActive = false;     // 粘液腺体
        public static bool BossReluctantChainActive = false; // 不舍之链
        public static bool BossMemoryLensActive = false;     // 记忆透镜
        public static bool BossAcidicCoreActive = false;     // 酸性核心
        public static bool BossPhantomMaskActive = false;    // 幻影面具

        // === 深渊/幻影减免 ===
        public static int AbyssThresholdReduction = 0;       // 深渊阈值减免
        public static int PhantomExtraReduction = 0;         // 幻影额外减免
        public static bool PhantomReductionActive = false;   // 幻影减伤是否已在本场触发

        /// <summary>
        /// 当前幻影减伤值（未触发或非战斗时为 0）
        /// </summary>
        public static int GetPhantomReduction()
        {
            if (!PhantomReductionActive) return 0;
            int baseValue = BossPhantomMaskActive ? 8 : 5;
            return baseValue + PhantomExtraReduction;
        }

        // === Boss计数器 ===
        public static int TurnCounterForMemoryLens = 0;      // 记忆透镜回合计数器
        public static int CardsPlayedThisBattle = 0;         // 本场战斗打出的卡牌数
        public static int AttackCardsPlayedThisBattle = 0;   // 本场战斗打出的攻击卡数

        /// <summary>
        /// 获取有效鲜血转换率
        /// </summary>
        public static int GetEffectiveBloodRate(int baseRate)
        {
            if (TemporaryBloodRateOverride > 0) return TemporaryBloodRateOverride;
            return Mathf.Max(1, baseRate - PermanentBloodRateReduction);
        }

        /// <summary>
        /// 获取有效格挡转换率
        /// </summary>
        public static int GetEffectiveBlockRate(int baseRate)
        {
            if (TemporaryBlockRateOverride > 0) return TemporaryBlockRateOverride;
            return Mathf.Max(1, baseRate - PermanentBlockRateReduction);
        }

        /// <summary>
        /// 判断卡牌是否应该消耗
        /// </summary>
        public static bool ShouldExhaust(Card card)
        {
            if (card == null) return false;
            if (AllCardsNoExhaustThisTurn) return false;
            if (CorruptNoExhaustPermanent && card.HasTag(CardTag.Corrupt)) return false;
            return card.exhaust;
        }

        /// <summary>
        /// 重置临时修正（每回合开始时调用）
        /// 注意：CardsPlayedThisBattle/AttackCardsPlayedThisBattle 是"本场战斗"计数，
        /// 不在此处清零（由 ResetAll 在战斗开始前清零）。
        /// </summary>
        public static void ResetTemporary()
        {
            TemporaryBloodRateOverride = 0;
            TemporaryBlockRateOverride = 0;
            BloodConversionForAll = false;
            BlockConversionForAll = false;
            AllCardsNoExhaustThisTurn = false;
            TagEffectDoubleTrigger = false;
        }

        /// <summary>
        /// 重置所有修正（战斗开始时调用，需在 RelicManager.OnBattleStart 之前，
        /// 之后由各遗物效果的 BattleStart 触发器重新建立本场状态）
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
            PhantomReductionActive = false;
            TurnCounterForMemoryLens = 0;
            CardsPlayedThisBattle = 0;
            AttackCardsPlayedThisBattle = 0;
            ResetTemporary();
        }
    }
}
