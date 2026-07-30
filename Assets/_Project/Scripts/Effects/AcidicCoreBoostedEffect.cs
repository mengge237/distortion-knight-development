using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "AcidicCoreBoostedEffect", menuName = "MutationChess/Relic Effects/Acidic Core Boost")]
    public class AcidicCoreBoostedEffect : CardEffect
    {
        [Tooltip("额外施加的debuff层数")]
        public int extraStacks = 1;

        public override void Execute(CombatContext context)
        {
            ApplyExtraDebuffs(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            ApplyExtraDebuffs(context?.battleManager);
        }

        private void ApplyExtraDebuffs(BattleManager bm)
        {
            if (bm == null) return;

            var enemy = bm.GetCurrentEnemy();
            if (enemy == null) return;

            enemy.AddBuff(new Buff { type = BuffType.Weak, amount = extraStacks, duration = 999 });
            enemy.AddBuff(new Buff { type = BuffType.Frail, amount = extraStacks, duration = 999 });
            enemy.AddBuff(new Buff { type = BuffType.Vulnerability, amount = extraStacks, duration = 999 });

            bm.AddLog($"酸性核心增强：对 {enemy.enemyName} 额外施加 {extraStacks} 层虚弱、脆弱、易伤");
            GameLogger.Log($"[AcidicCoreBoosted] 额外施加 {extraStacks} 层 debuff");
        }
    }
}
