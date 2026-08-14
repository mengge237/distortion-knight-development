using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BossReluctantChainEffect", menuName = "MutationChess/Relic Effects/Boss/Reluctant Chain")]
    public class BossReluctantChainEffect : CardEffect
    {
        [Tooltip("消耗不舍系卡牌抽牌时恢复的生命值")]
        public int hpOnExhaustDraw = 1;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.BossReluctantChainActive = true;
            GameLogger.Log("[BossReluctantChain] 不舍锁链激活，强化不舍系卡牌效果");
        }

        public override void Execute(EffectContext context)
        {
            Execute(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }
    }
}
