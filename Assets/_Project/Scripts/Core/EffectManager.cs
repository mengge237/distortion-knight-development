using System;
using MutationChess.Battle;
using MutationChess.Core;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    public enum EffectTrigger
    {
        BattleStart,
        BattleEnd,
        PlayerTurnStart,
        PlayerTurnEnd,
        TurnEnd,
        PlayerAttack,
        CalculateAttackDamage,
        CalculateBlock,
        CardPlayed,
        AfterCardsPlayed,
        CardExhausted,
        CalculateCardCost,
        Victory,
        Defeat,
        EnemyDeath,
        CalculatePotionDropChance,
        CalculatePlayerDamage,
        Passive,
    }

    public class EffectContext
    {
        public Battle.BattleManager battleManager;
        public CombatContext combat;
        public Relic relic;
        public int baseValue;
        public int finalValue;
        public float floatValue;
        public string stringValue;
        public object tag;
        public EffectTrigger trigger;

        public EffectContext() { }

        public EffectContext(Battle.BattleManager bm)
        {
            battleManager = bm;
        }
    }

    public class EffectManager : MonoBehaviour
    {
        private static EffectManager _instance;
        public static EffectManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<EffectManager>();
                return _instance;
            }
        }

        private Dictionary<EffectTrigger, List<Action<EffectContext>>> handlers
            = new Dictionary<EffectTrigger, List<Action<EffectContext>>>();

        // 数值修正器签名(context, currentValue) => newValue
        private Dictionary<EffectTrigger, List<Func<EffectContext, int, int>>> valueModifiers
            = new Dictionary<EffectTrigger, List<Func<EffectContext, int, int>>>();

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        public void Register(EffectTrigger trigger, Action<EffectContext> handler)
        {
            if (!handlers.ContainsKey(trigger))
                handlers[trigger] = new List<Action<EffectContext>>();
            handlers[trigger].Add(handler);
        }

        public void Unregister(EffectTrigger trigger, Action<EffectContext> handler)
        {
            if (handlers.ContainsKey(trigger))
                handlers[trigger].Remove(handler);
        }

        public void RegisterValueModifier(EffectTrigger trigger, Func<EffectContext, int, int> modifier)
        {
            if (!valueModifiers.ContainsKey(trigger))
                valueModifiers[trigger] = new List<Func<EffectContext, int, int>>();
            valueModifiers[trigger].Add(modifier);
        }

        public void UnregisterValueModifier(EffectTrigger trigger, Func<EffectContext, int, int> modifier)
        {
            if (valueModifiers.ContainsKey(trigger))
                valueModifiers[trigger].Remove(modifier);
        }

        public void Trigger(EffectTrigger trigger, EffectContext context = null)
        {
            context = context ?? new EffectContext();
            context.trigger = trigger;
            EnsureCombat(context);
            if (!handlers.ContainsKey(trigger)) return;

            // 快照遍历：处理器可能在执行中注销自己（如 DamageReductionEffect.OnTurnEnd），
            // 直接索引原列表会跳过后续元素或越界
            var list = new List<Action<EffectContext>>(handlers[trigger]);
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    list[i]?.Invoke(context);
                }
                catch (Exception e)
                {
                    GameLogger.LogError($"[EffectManager] 触发 {trigger} 时发生错误: {e.Message}\n{e.StackTrace}");
                }
            }
        }

        public int CalculateModifiedValue(EffectTrigger trigger, EffectContext context, int baseValue)
        {
            context = context ?? new EffectContext();
            context.trigger = trigger;
            context.baseValue = baseValue;
            context.finalValue = baseValue;
            EnsureCombat(context);

            if (!valueModifiers.ContainsKey(trigger))
                return baseValue;

            // 快照遍历（同 Trigger 的原因）
            var list = new List<Func<EffectContext, int, int>>(valueModifiers[trigger]);
            int result = baseValue;
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    result = list[i](context, result);
                }
                catch (Exception e)
                {
                    GameLogger.LogError($"[EffectManager] 计算 {trigger} 修正值时出错: {e.Message}\n{e.StackTrace}");
                }
            }
            context.finalValue = result;
            return result;
        }

        /// <summary>
        /// 调用点大多只创建 EffectContext(battleManager)，combat 常为 null。
        /// 这里补全 combat，让依赖 combat.targetEnemy/sourceCard/targetPlayer 的效果可用。
        /// </summary>
        private static void EnsureCombat(EffectContext context)
        {
            if (context == null || context.combat != null) return;
            var bm = context.battleManager;
            if (bm == null) return;

            context.combat = new CombatContext(
                bm,
                bm.GetCurrentEnemy(),
                PlayerDataManager.Instance != null ? PlayerDataManager.Instance.GetPlayerData() : null,
                context.tag as Card);
        }

        public void ClearAll()
        {
            handlers.Clear();
            valueModifiers.Clear();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
