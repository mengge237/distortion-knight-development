using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    ///
    ///
    /// Execute(CombatContext) 
    /// </summary>
    [CreateAssetMenu(fileName = "CriticalStrikeEffect", menuName = "MutationChess/Relic Effects/Critical Strike")]
    public class CriticalStrikeEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        [Range(0f, 1f)]
        public float criticalChance = 0.15f;

        [Tooltip("")]
        public float damageMultiplier = 2f;

        public override void Execute(CombatContext context)
        {
            //
        }

        public override void Execute(EffectContext context)
        {
            if (context == null) return;
            if (context.trigger != EffectTrigger.CalculateAttackDamage) return;

            if (Random.value < criticalChance)
            {
                int critDamage = Mathf.RoundToInt(context.baseValue * damageMultiplier);
                context.finalValue = critDamage;
                GameLogger.Log($"[CriticalStrike] {context.baseValue} -> {critDamage}");
            }
        }
    }
}
