using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 斩杀效果：攻击时若目标敌人当前血量低于阈值，可直接一击秒杀。
    /// 触发方式：按 EffectTrigger.CalculateAttackDamage 值修改器挂入，
    /// 当 enemyHp &lt;= hpThreshold 时，将 finalValue 设为极大值（99999）以直接斩杀。
    /// 来源：胜利誓约剑(Synth_VictorySword)等配置获取。
    /// </summary>
    [CreateAssetMenu(fileName = "InstantKillEffect", menuName = "MutationChess/Relic Effects/Instant Kill")]
    public class InstantKillEffect : CardEffect
    {
        [Header("斩杀配置")]
        [Tooltip("斩杀血量阈值：敌人HP小于等于此值时触发秒杀")]
        [Min(1)]
        public int hpThreshold = 20;

        [Tooltip("每场战斗最多斩杀次数（防无限）")]
        [Min(1)]
        public int maxKillsPerBattle = 1;

        [System.NonSerialized]
        private int killsThisBattle = 0;

        public override void Execute(CombatContext context)
        {
            // CombatContext 无 trigger 字段，重置逻辑由 Execute(EffectContext) 的 BattleStart 分支处理
        }

        public override void Execute(EffectContext context)
        {
            if (context == null || context.combat?.targetEnemy == null) return;

            if (context.trigger == EffectTrigger.BattleStart)
            {
                ResetPerBattle();
                return;
            }

            if (killsThisBattle >= maxKillsPerBattle) return;
            Enemy enemy = context.combat.targetEnemy;
            if (enemy.currentHealth > hpThreshold) return;

            context.finalValue = 99999;
            killsThisBattle++;
            GameLogger.Log(
                $"[InstantKillEffect] 斩杀触发：{enemy.enemyName}(HP={enemy.currentHealth}&lt;={hpThreshold})" +
                $"，本场第 {killsThisBattle}/{maxKillsPerBattle} 次");
        }

        public void ResetPerBattle()
        {
            killsThisBattle = 0;
        }
    }
}
