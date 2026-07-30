using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BossSlimeGlandEffect", menuName = "MutationChess/Relic Effects/Boss/Slime Gland")]
    public class BossSlimeGlandEffect : CardEffect
    {
        [Tooltip("")]
        public int slimePerTurn = 3;

        [Tooltip("debuff")]
        public int debuffStacks = 1;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.BossSlimeGlandActive = true;
            GameLogger.Log($"[BossSlimeGland] ");
        }

        public override void Execute(EffectContext context)
        {
            Execute(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }
    }
}
