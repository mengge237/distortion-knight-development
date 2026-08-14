using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 格挡锁定效果
    /// 下回合玩家无法获得格挡
    /// 通过 EffectManager.RegisterValueModifier 注册修饰器将格挡值置为 0
    /// </summary>
    [CreateAssetMenu(fileName = "BlockLockNextTurnEffect", menuName = "MutationChess/Potion Effects/Block Lock Next Turn")]
    public class BlockLockNextTurnEffect : CardEffect
    {
        [Header("格挡锁定配置")]
        [Tooltip("2=+")]
        public int duration = 2;

        [System.NonSerialized]
        private bool isActive = false;

        [System.NonSerialized]
        private int remainingTurns = 0;

        [System.NonSerialized]
        private System.Func<EffectContext, int, int> modifierRef;

        [System.NonSerialized]
        private System.Action<EffectContext> turnEndHandlerRef;

        [System.NonSerialized]
        private bool skipFirstDecrement = false;

        [System.NonSerialized]
        private bool battleResetHookRegistered = false;

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
            var effectManager = EffectManager.Instance;
            if (effectManager == null)
            {
                // 先判空再置 isActive，避免 EffectManager 缺失时陷入"已激活但未注册"的卡死状态
                GameLogger.LogWarning("[BlockLock] EffectManager 为空");
                return;
            }

            if (isActive)
            {
                remainingTurns = Mathf.Max(remainingTurns, duration);
                GameLogger.Log($"[BlockLock] 已激活，剩余回合: {remainingTurns}");
                return;
            }

            EnsureBattleResetHook(effectManager);

            modifierRef = (ctx, baseValue) =>
            {
                if (!isActive) return baseValue;
                GameLogger.Log($"[BlockLock] 锁定格挡: {baseValue} -> 0");
                return 0;
            };

            effectManager.RegisterValueModifier(EffectTrigger.CalculateBlock, modifierRef);


            turnEndHandlerRef = (ctx) => OnTurnEnd();
            effectManager.Register(EffectTrigger.PlayerTurnEnd, turnEndHandlerRef);

            isActive = true;
            remainingTurns = duration;
            // 激活当回合的 PlayerTurnEnd 不消耗持续时间，否则锁在生效前就提前过期
            skipFirstDecrement = true;

            GameLogger.Log($"[BlockLock] 玩家被施加封印，{remainingTurns}回合内无法获得格挡");

            if (battleManager != null)
                battleManager.AddBattleLog($"玩家被施加封印，{duration}回合内无法获得格挡");
        }

        /// <summary>
        /// 战斗开始时重置状态（药水效果不经过 RelicManager，需要自己挂 BattleStart 钩子）
        /// </summary>
        private void EnsureBattleResetHook(EffectManager effectManager)
        {
            if (battleResetHookRegistered) return;
            battleResetHookRegistered = true;
            effectManager.Register(EffectTrigger.BattleStart, ctx => ResetState());
        }

        private void ResetState()
        {
            isActive = false;
            remainingTurns = 0;
            skipFirstDecrement = false;
            modifierRef = null;
            turnEndHandlerRef = null;
        }

        public override void ResetForBattle()
        {
            ResetState();
        }

        /// <summary>
        /// 玩家回合结束时处理（跳过激活当回合的第一次，之后每次消耗 1 回合）
        /// </summary>
        public void OnTurnEnd()
        {
            if (!isActive) return;

            if (skipFirstDecrement)
            {
                skipFirstDecrement = false;
                return;
            }

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
                GameLogger.Log("[BlockLock] 封印解除");
            }
        }
    }
}
