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
                GameLogger.Log($"[BlockLock] 已激活，剩余回合: {remainingTurns}");
                return;
            }

            isActive = true;
            remainingTurns = duration;

            var effectManager = EffectManager.Instance;
            if (effectManager == null)
            {
                GameLogger.LogWarning("[BlockLock] EffectManager 为空");
                return;
            }


            modifierRef = (ctx, baseValue) =>
            {
                if (!isActive) return baseValue;
                GameLogger.Log($"[BlockLock] 锁定格挡: {baseValue} -> 0");
                return 0;
            };

            effectManager.RegisterValueModifier(EffectTrigger.CalculateBlock, modifierRef);


            turnEndHandlerRef = (ctx) => OnTurnEnd();
            effectManager.Register(EffectTrigger.PlayerTurnEnd, turnEndHandlerRef);

            GameLogger.Log($"[BlockLock] 玩家被施加封印，{remainingTurns}回合内无法获得格挡");

            if (battleManager != null)
                battleManager.AddBattleLog($"玩家被施加封印，{duration}回合内无法获得格挡");
        }

        /// <summary>
        /// 回合结束时处理
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
                GameLogger.Log("[BlockLock] 封印解除");
            }
        }
    }
}
