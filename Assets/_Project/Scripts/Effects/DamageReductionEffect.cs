using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 伤害减免效果
    /// 通过注册伤害修饰器按比例减少玩家受到的伤害
    /// </summary>
    [CreateAssetMenu(fileName = "DamageReductionEffect", menuName = "MutationChess/Potion Effects/Damage Reduction")]
    public class DamageReductionEffect : CardEffect
    {
        [Header("减伤配置")]
        [Tooltip("0.5=减伤50%")]
        [Range(0.1f, 0.9f)]
        public float damageReduction = 0.5f;

        [Tooltip("减伤持续回合数")]
        public int duration = 1;

        // 运行时状态
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

        public override string GetDescription(Card card)
        {
            int percent = Mathf.RoundToInt(damageReduction * 100f);
            return $"{duration} 回合受到伤害减 {percent}%";
        }

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
            var effectManager = EffectManager.Instance;
            if (effectManager == null)
            {
                // 注意：先判空再置 isActive，避免 EffectManager 缺失时陷入"已激活但未注册"的卡死状态
                GameLogger.LogWarning("[DamageReduction] EffectManager 为空");
                return;
            }

            if (isActive)
            {
                remainingTurns = Mathf.Max(remainingTurns, duration);
                GameLogger.Log($"[DamageReduction] 已激活，剩余回合: {remainingTurns}");
                return;
            }

            EnsureBattleResetHook(effectManager);

            modifierRef = (ctx, baseValue) =>
            {
                if (!isActive) return baseValue;
                int reduced = Mathf.RoundToInt(baseValue * (1f - damageReduction));
                GameLogger.Log($"[DamageReduction] 原始伤害: {baseValue} 减为 {reduced}");
                return reduced;
            };

            effectManager.RegisterValueModifier(EffectTrigger.CalculatePlayerDamage, modifierRef);


            turnEndHandlerRef = (ctx) => OnTurnEnd();
            effectManager.Register(EffectTrigger.PlayerTurnEnd, turnEndHandlerRef);

            isActive = true;
            remainingTurns = duration;
            // 激活当回合的 PlayerTurnEnd 不消耗持续时间，否则 duration=1 时
            // 敌人在同回合结束后的攻击完全吃不到减伤
            skipFirstDecrement = true;

            GameLogger.Log($"[DamageReduction] {damageReduction * 100}%伤害减免，持续{remainingTurns}个敌人回合");

            if (battleManager != null)
                battleManager.AddBattleLog($"{damageReduction * 100}%伤害减免，持续{duration}回合");
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
                        effectManager.UnregisterValueModifier(EffectTrigger.CalculatePlayerDamage, modifierRef);
                    if (turnEndHandlerRef != null)
                        effectManager.Unregister(EffectTrigger.PlayerTurnEnd, turnEndHandlerRef);
                }
                isActive = false;
                modifierRef = null;
                turnEndHandlerRef = null;
                GameLogger.Log("[DamageReduction] 减伤结束");
            }
        }
    }
}
