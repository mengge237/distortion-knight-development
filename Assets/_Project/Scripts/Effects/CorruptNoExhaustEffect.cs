using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "CorruptNoExhaustEffect", menuName = "MutationChess/Relic Effects/Corrupt No Exhaust")]
    public class CorruptNoExhaustEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            ActivateNoExhaust();
        }

        public override void Execute(EffectContext context)
        {
            ActivateNoExhaust();
        }

        private void ActivateNoExhaust()
        {
            ConversionModifier.CorruptNoExhaustPermanent = true;
            GameLogger.Log("[CorruptNoExhaust] ");
        }
    }
}


