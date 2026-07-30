using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 腐化不消耗效果：腐化系卡牌不再被消耗
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
            GameLogger.Log("[CorruptNoExhaust] 腐化系卡牌不再被消耗");
        }
    }
}