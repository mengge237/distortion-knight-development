using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 三重减益效果：对当前敌人施加虚弱/脆弱/易伤各 N 层。
    /// 由效果合并从 AcidicCoreDebuff3StacksEffect / AcidicCoreBoostedEffect 合并而来
    /// （仅各减益层数数值不同，逻辑完全一致；Boss 酸性核心激活时各额外 +bossExtraStacks 层）。
    /// </summary>
    [CreateAssetMenu(fileName = "TripleDebuffEffect", menuName = "MutationChess/Relic Effects/Triple Debuff")]
    public class TripleDebuffEffect : CardEffect
    {
        [Tooltip("施加虚弱的层数")]
        public int weak = 3;

        [Tooltip("施加脆弱的层数")]
        public int frail = 3;

        [Tooltip("施加易伤的层数")]
        public int vulnerable = 3;

        [Tooltip("Boss酸性核心激活时各额外施加的层数")]
        public int bossExtraStacks = 1;

        public override void Execute(CombatContext context)
        {
            ApplyDebuffs(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            ApplyDebuffs(context?.battleManager);
        }

        private void ApplyDebuffs(BattleManager bm)
        {
            if (bm == null) return;

            int extra = ConversionModifier.BossAcidicCoreActive ? bossExtraStacks : 0;

            var enemy = bm.GetCurrentEnemy();
            if (enemy == null) return;

            enemy.AddBuff(new Buff { type = BuffType.Weak, amount = weak + extra, duration = 999 });
            enemy.AddBuff(new Buff { type = BuffType.Frail, amount = frail + extra, duration = 999 });
            enemy.AddBuff(new Buff { type = BuffType.Vulnerability, amount = vulnerable + extra, duration = 999 });

            bm.AddLog($"酸性核心：对 {enemy.enemyName} 施加 {weak + extra} 虚弱 / {frail + extra} 脆弱 / {vulnerable + extra} 易伤");
            GameLogger.Log($"[TripleDebuff] {weak + extra}/{frail + extra}/{vulnerable + extra}" +
                (ConversionModifier.BossAcidicCoreActive ? " (Boss加成)" : ""));
        }
    }
}
