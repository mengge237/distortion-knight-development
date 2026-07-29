using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    ///
    ///
    /// </summary>
    [CreateAssetMenu(fileName = "DamageReductionEffect", menuName = "MutationChess/Potion Effects/Damage Reduction")]
    public class DamageReductionEffect : CardEffect
    {
        [Header("")]
        [Tooltip("0.5=")]
        [Range(0.1f, 0.9f)]
        public float damageReduction = 0.5f;

        [Tooltip("")]
        public int duration = 1;

        //
        [System.NonSerialized]
        private bool isActive = false;

        [System.NonSerialized]
        private int remainingTurns = 0;

        [System.NonSerialized]
        private System.Func<EffectContext, int, int> modifierRef;

        [System.NonSerialized]
        private System.Action<EffectContext> turnEndHandlerRef;

        public override void Execute(CombatContext context)
        {
            ActivateReduction(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            ActivateReduction(context?.battleManager);
        }

        private void ActivateReduction(BattleManager battleManager)
        {
            if (isActive)
            {
                remainingTurns = Mathf.Max(remainingTurns, duration);
                GameLogger.Log($"[DamageReduction] : {remainingTurns}");
                return;
            }

            isActive = true;
            remainingTurns = duration;


            var effectManager = EffectManager.Instance;
            if (effectManager == null)
            {
                GameLogger.LogWarning("[DamageReduction] EffectManager ");
                return;
            }

            modifierRef = (ctx, baseValue) =>
            {
                if (!isActive) return baseValue;
                int reduced = Mathf.RoundToInt(baseValue * (1f - damageReduction));
                GameLogger.Log($"[DamageReduction] : {baseValue} ?? {reduced}");
                return reduced;
            };

            effectManager.RegisterValueModifier(EffectTrigger.CalculatePlayerDamage, modifierRef);


            turnEndHandlerRef = (ctx) => OnTurnEnd();
            effectManager.Register(EffectTrigger.PlayerTurnEnd, turnEndHandlerRef);

            GameLogger.Log($"[DamageReduction] {damageReduction * 100}%{remainingTurns} ");

            if (battleManager != null)
                battleManager.AddBattleLog($"{damageReduction * 100}%");
        }

        /// <summary>
        ///
        /// </summary>
        public void OnTurnEnd()
        {
            if (!isActive) return;

            remainingTurns--;
            if (remainingTurns <= 0)
            {
                var effectManager = EffectManager.Instance;
                if (effectManager != null)
                {
                    if (modifierRef != null)
                        effectManager.UnregisterValueModifier(EffectTrigger.CalculatePlayerDamage, modifierRef);
                    if (turnEndHandlerRef != null)
                        effectManager.Unregister(EffectTrigger.PlayerTurnEnd, turnEndHandlerRef);
                }
                isActive = false;
                modifierRef = null;
                turnEndHandlerRef = null;
                GameLogger.Log("[DamageReduction] ");
            }
        }
    }
}
