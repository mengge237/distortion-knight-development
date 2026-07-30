using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BossCorruptLiverEffect", menuName = "MutationChess/Relic Effects/Boss/Corrupt Liver")]
    public class BossCorruptLiverEffect : CardEffect
    {
        [Tooltip("")]
        public int energyOnExhaust = 1;

        [Tooltip("")]
        public int drawOnExhaust = 1;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.BossCorruptLiverActive = true;
            ConversionModifier.CorruptNoExhaustPermanent = false;
            GameLogger.Log($"[BossCorruptLiver] ");
        }

        public override void Execute(EffectContext context)
        {
            Execute(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }
    }
}
