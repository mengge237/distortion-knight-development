using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BossSlimeGlandEffect", menuName = "MutationChess/Relic Effects/Boss/Slime Gland")]
    public class BossSlimeGlandEffect : CardEffect
    {
        [Tooltip("每回合获得的粘液层数")]
        public int slimePerTurn = 3;

        [Tooltip("施加给敌人的减益层数")]
        public int debuffStacks = 1;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.BossSlimeGlandActive = true;
            GameLogger.Log("[BossSlimeGland] 粘液腺体激活，强化粘液系卡牌效果");
        }

        public override void Execute(EffectContext context)
        {
            Execute(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }
    }
}
