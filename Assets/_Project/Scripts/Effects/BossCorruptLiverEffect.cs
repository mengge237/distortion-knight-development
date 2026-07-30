using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BossCorruptLiverEffect", menuName = "MutationChess/Relic Effects/Boss/Corrupt Liver")]
    public class BossCorruptLiverEffect : CardEffect
    {
        [Tooltip("消耗腐化系卡牌时恢复的能量")]
        public int energyOnExhaust = 1;

        [Tooltip("消耗腐化系卡牌时抽牌的数量")]
        public int drawOnExhaust = 1;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.BossCorruptLiverActive = true;
            ConversionModifier.CorruptNoExhaustPermanent = false;
            GameLogger.Log("[BossCorruptLiver] 腐化之肝激活，强化腐化系卡牌效果");
        }

        public override void Execute(EffectContext context)
        {
            Execute(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }
    }
}
