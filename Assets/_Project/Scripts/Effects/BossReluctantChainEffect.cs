using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BossReluctantChainEffect", menuName = "MutationChess/Relic Effects/Boss/Reluctant Chain")]
    public class BossReluctantChainEffect : CardEffect
    {
        [Tooltip("")]
        public int hpOnExhaustDraw = 1;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.BossReluctantChainActive = true;
            GameLogger.Log($"[BossReluctantChain] ");
        }

        public override void Execute(EffectContext context)
        {
            Execute(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }
    }
}
