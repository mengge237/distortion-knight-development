using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    [Serializable]
    public class RelicEffectEntry
    {
        [Tooltip("效果ID，对应 Resources/Effects 下的效果资源名（如 TempStrength3）")]
        public string effectId;
        [Tooltip("效果触发时机")]
        public EffectTrigger trigger;
        [Tooltip("效果参数1")]
        public float value1 = 0f;
        [Tooltip("效果参数2")]
        public float value2 = 0f;
    }

    /// <summary>
    /// 强度分级：在稀有度体系之上再度划分获得概率。
    /// Normal = 数值型效果（伤害/回复/金币等数值增益），按稀有度正常获得；
    /// Mechanic = 机制级强力效果（改变游戏机制，如诅咒免疫/反转、牌库操纵、能量引擎），
    /// 掉落概率大幅降低且不进商店出售。
    /// </summary>
    public enum PowerTier
    {
        Normal = 0,   // 数值级
        Mechanic = 1  // 机制级
    }

    /// <summary>强度分级掉落权重（机制级大幅降权）。</summary>
    public static class PowerTierWeights
    {
        public const float Normal = 1f;
        public const float Mechanic = 0.1f;
    }

    [CreateAssetMenu(fileName = "RelicDataAsset", menuName = "MutationChess/Relic Data Asset")]
    public class RelicDataAsset : ScriptableObject
    {
        [Header("遗物基础信息")]
        public string relicId;
        public string relicName;
        public RelicRarity rarity;
        public CardFaction faction;

        [Header("强度分级")]
        [Tooltip("机制级强力效果（改变机制而非数值）：掉落概率大幅降低且不进商店")]
        public PowerTier powerTier = PowerTier.Normal;

        [Header("基础效果")]
        [Tooltip("基础效果列表，遗物持有时始终激活")]
        public List<RelicEffectEntry> baseEffectIds = new List<RelicEffectEntry>();

        [Header("隐藏效果激活器")]
        [Tooltip("Boss遗物ID，持有后激活本遗物的隐藏效果（如 Boss_BloodVein 血脉）")]
        public string hiddenActivatorRelicId = "";

        [Header("隐藏效果")]
        [Tooltip("当对应的Boss激活器遗物持有时激活的隐藏效果列表")]
        public List<RelicEffectEntry> hiddenEffectIds = new List<RelicEffectEntry>();

        [Header("兼容字段")]
        [Tooltip("旧版遗物效果列表，兼容性保留，优先使用 baseEffectIds")]
        public List<RelicEffectEntry> relicEffects = new List<RelicEffectEntry>();

        [Header("遗物描述")]
        [TextArea(2, 4)]
        public string description;

        [Header("经济属性")]
        [Tooltip("价格区间：1层150、2层250、3层285-350、特殊层320-400、Boss层350+")]
        public int price = 150;

        [Header("遗物类型标记")]
        public bool isShopRelic = false;
        public bool isBossRelic = false;
        public bool isStartingRelic = false;
        public bool isSynthesisTarget = false;

        [Header("诅咒")]
        [Tooltip("诅咒遗物（困难度系统发放，负面效果，不进商店/掉落池）")]
        public bool isCurse = false;

        [Header("阵营解锁")]
        [Tooltip("是否为Boss阵营解锁遗物")]
        public bool isFactionUnlocker = false;
        public CardFaction unlockedFaction = CardFaction.None;

        [Header("资源路径")]
        public string iconPath;

        [Header("图鉴")]
        [Tooltip("图鉴稳定编号（1001-1999），由 Tools/分配图鉴ID 自动分配，勿手动修改")]
        public int codexId;

        /// <summary>
        /// 获取当前激活的效果列表
        /// </summary>
        /// <param name="hasActivator">是否持有对应的Boss激活器遗物</param>
        /// <returns>激活的效果列表</returns>
        public List<RelicEffectEntry> GetActiveEffects(bool hasActivator)
        {
            List<RelicEffectEntry> result = new List<RelicEffectEntry>();

            if (baseEffectIds != null && baseEffectIds.Count > 0)
                result.AddRange(baseEffectIds);
            else if (relicEffects != null && relicEffects.Count > 0)
                result.AddRange(relicEffects);

            if (hasActivator && hiddenEffectIds != null && hiddenEffectIds.Count > 0)
                result.AddRange(hiddenEffectIds);

            return result;
        }

        private void OnValidate()
        {
            // 兼容旧版：如果 relicEffects 有数据但 baseEffectIds 为空，则迁移
            if ((relicEffects != null && relicEffects.Count > 0) &&
                (baseEffectIds == null || baseEffectIds.Count == 0))
            {
                baseEffectIds = new List<RelicEffectEntry>(relicEffects);
            }
        }
    }
}
