﻿using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>

    /// ?????????????? CalculateBlock??????????0??
    /// ???? duration ????????????
    /// ???? EffectManager.RegisterValueModifier ???????????
    /// </summary>
    [CreateAssetMenu(fileName = "BlockLockNextTurnEffect", menuName = "MutationChess/Potion Effects/Block Lock Next Turn")]
    public class BlockLockNextTurnEffect : CardEffect
    {
        [Header("??????")]
        [Tooltip("???????????2=?????+?????")]
        public int duration = 2;

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
            ApplyBlockLock(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            ApplyBlockLock(context?.battleManager);
        }

        private void ApplyBlockLock(BattleManager battleManager)
        {
            if (isActive)
            {
                remainingTurns = Mathf.Max(remainingTurns, duration);
                GameLogger.Log($"[BlockLock]  {remainingTurns}");
                return;
            }

            isActive = true;
            remainingTurns = duration;

            var effectManager = EffectManager.Instance;
            if (effectManager == null)
            {
                GameLogger.LogWarning("[BlockLock] EffectManager ");
                return;
            }


            modifierRef = (ctx, baseValue) =>
            {
                if (!isActive) return baseValue;
                GameLogger.Log($"[BlockLock] : {baseValue}  0");
                return 0;
            };

            effectManager.RegisterValueModifier(EffectTrigger.CalculateBlock, modifierRef);


            turnEndHandlerRef = (ctx) => OnTurnEnd();
            effectManager.Register(EffectTrigger.PlayerTurnEnd, turnEndHandlerRef);

            GameLogger.Log($"[BlockLock]  {remainingTurns} ");

            if (battleManager != null)
                battleManager.AddBattleLog($"{duration} ");
        }

        /// <summary>


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
                        effectManager.UnregisterValueModifier(EffectTrigger.CalculateBlock, modifierRef);
                    if (turnEndHandlerRef != null)
                        effectManager.Unregister(EffectTrigger.PlayerTurnEnd, turnEndHandlerRef);
                }
                isActive = false;
                modifierRef = null;
                turnEndHandlerRef = null;
                GameLogger.Log("[BlockLock] ");
            }
        }
    }
}


