﻿using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 血量转化修正器，管理全局转化比例和临时覆盖
    /// 支持永久和临时两种修正方式，影响所有卡牌的血量转化
    /// - 永久降低转化比例，每次降低1点，例如3:2变为2:2
    /// - 临时覆盖转化比例，设置为指定值如1:1
    /// - 全卡牌血量转化开关，开启后所有卡牌按1:1转化
    /// 使用 ResetTemporary() 重置所有临时修正
    /// </summary>
    public static class ConversionModifier
    {
        // === 永久修正：减少基础转化比例，对所有卡牌生效 ===
        public static int PermanentBloodRateReduction = 0;
        public static int PermanentBlockRateReduction = 0;

        // === 临时修正：临时覆盖转化比例，可被重置 ===
        [Tooltip("临时覆盖血量转化比例，0=不覆盖，>0=覆盖为指定值")]
        public static int TemporaryBloodRateOverride = 0;

        [Tooltip("临时覆盖格挡转化比例，0=不覆盖，>0=覆盖为指定值")]
        public static int TemporaryBlockRateOverride = 0;

        // === 全局开关 ===
        public static bool BloodConversionForAll = false;
        public static bool BlockConversionForAll = false;

        // === 消耗相关 ===
        public static bool AllCardsNoExhaustThisTurn = false;  // 本回合所有卡牌不消耗
        public static bool CorruptNoExhaustPermanent = false; // 永久：腐化卡牌不消耗

        // === 标签效果 ===
        public static bool TagEffectDoubleTrigger = false;    // 标签效果双倍触发：开启后所有标签效果触发两次
        // === 暗影机制 ===
        // 暗影临时力量全局标记：为 true 时所有 isShadow=true 的 Strength buff 不会在回合开始时流失
        public static bool ShadowStrengthNoDecay = true;

        /// <summary>
        /// 获取生效的血量转化比例
        /// 优先级：临时覆盖 > 永久降低 > 基础值
        /// </summary>
        public static int GetEffectiveBloodRate(int baseRate)
        {
            if (TemporaryBloodRateOverride > 0) return TemporaryBloodRateOverride;
            return Mathf.Max(1, baseRate - PermanentBloodRateReduction);
        }

        /// <summary>
        /// 获取生效的格挡转化比例
        /// </summary>
        public static int GetEffectiveBlockRate(int baseRate)
        {
            if (TemporaryBlockRateOverride > 0) return TemporaryBlockRateOverride;
            return Mathf.Max(1, baseRate - PermanentBlockRateReduction);
        }

        /// <summary>
        /// 判断卡牌是否应被消耗，考虑全局开关和腐化标记
        /// </summary>
        public static bool ShouldExhaust(Card card)
        {
            if (card == null) return false;
            if (AllCardsNoExhaustThisTurn) return false;
            if (CorruptNoExhaustPermanent && card.HasTag(CardTag.Corrupt)) return false;
            return card.exhaust;
        }

        /// <summary>
    /// 重置所有临时修正，在回合结束时调用
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
        /// 重置所有修正，包括永久修正/临时覆盖和全局开关
        /// </summary>
        public static void ResetAll()
        {
            PermanentBloodRateReduction = 0;
            PermanentBlockRateReduction = 0;
            CorruptNoExhaustPermanent = false;
            ShadowStrengthNoDecay = true;
            ResetTemporary();
        }
    }
}
