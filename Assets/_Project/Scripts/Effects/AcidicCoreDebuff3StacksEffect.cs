using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "AcidicCoreDebuff3StacksEffect", menuName = "MutationChess/Relic Effects/Acidic Core 3 Stacks")]
    public class AcidicCoreDebuff3StacksEffect : CardEffect
    {
        [Tooltip("")]
        public int weak = 3;

        [Tooltip("")]
        public int frail = 3;

        [Tooltip("")]
        public int vulnerable = 3;

        [Tooltip("Boss")]
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
            int totalWeak = weak + extra;
            int totalFrail = frail + extra;
            int totalVulnerable = vulnerable + extra;

            var enemy = bm.GetCurrentEnemy();
            if (enemy == null) return;

            enemy.AddBuff(new Buff { type = BuffType.Weak, amount = totalWeak, duration = 999 });
            enemy.AddBuff(new Buff { type = BuffType.Frail, amount = totalFrail, duration = 999 });
            enemy.AddBuff(new Buff { type = BuffType.Vulnerability, amount = totalVulnerable, duration = 999 });

            GameLogger.Log($"[AcidicCore] {totalWeak}/ {totalFrail}/ {totalVulnerable}" +
                (ConversionModifier.BossAcidicCoreActive ? " (Boss加成)" : ""));
        }
    }
}
