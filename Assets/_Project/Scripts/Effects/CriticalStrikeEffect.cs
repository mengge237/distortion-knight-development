using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 暴击遗物效果：造成攻击伤害时按概率触发暴击，造成倍率伤害。
    /// 触发时机：CalculateAttackDamage（值修改器，通过修改 context.finalValue 生效）。
    /// Execute(CombatContext) 不适用该值修改流程，保留为空实现。
    /// </summary>
    [CreateAssetMenu(fileName = "CriticalStrikeEffect", menuName = "MutationChess/Relic Effects/Critical Strike")]
    public class CriticalStrikeEffect : CardEffect
    {
        [Header("暴击参数")]
        [Tooltip("暴击触发概率")]
        [Range(0f, 1f)]
        public float criticalChance = 0.15f;

        [Tooltip("暴击伤害倍率")]
        public float damageMultiplier = 2f;

        public override void Execute(CombatContext context)
        {
            // 暴击为值修改器效果，仅通过 Execute(EffectContext) 处理
        }

        public override void Execute(EffectContext context)
        {
            if (context == null) return;

            if (Random.value < criticalChance)
            {
                int critDamage = Mathf.RoundToInt(context.baseValue * damageMultiplier);
                context.finalValue = critDamage;
                GameLogger.Log($"[CriticalStrike] 暴击！基础 {context.baseValue} -> 暴击 {critDamage}");
            }
        }
    }
}
