﻿using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 不舍费用减免遗物效果：不舍标签卡牌的费用降低。
    /// 触发时机：CalculateCardCost（值修改器，通过修改 context.finalValue 生效）。
    /// 仅当 context.combat.sourceCard 拥有 Reluctant 标签时生效。
    /// Execute(CombatContext) 不适用该值修改流程，保留为空实现。
    /// </summary>
    [CreateAssetMenu(fileName = "ReluctantCostReductionEffect", menuName = "MutationChess/Relic Effects/Reluctant Cost Reduction")]
    public class ReluctantCostReductionEffect : CardEffect
    {
        [Header("费用减免")]
        [Tooltip("不舍卡费用降低的数值")]
        [Min(0)]
        public int costReduction = 1;

        public override void Execute(CombatContext context)
        {
            // 费用减免为值修改器效果，仅通过 Execute(EffectContext) 处理
        }

        public override void Execute(EffectContext context)
        {
            ApplyCostReduction(context, CardTag.Reluctant);
        }

        private void ApplyCostReduction(EffectContext context, CardTag requiredTag)
        {
            if (context == null) return;

            Card target = context.combat?.sourceCard;
            if (target == null || !target.HasTag(requiredTag)) return;

            context.finalValue = Mathf.Max(0, context.baseValue - costReduction);
            GameLogger.Log($"[ReluctantCostReduction] 卡牌 {target.cardName} 费用 {context.baseValue} -> {context.finalValue}");
        }
    }
}
