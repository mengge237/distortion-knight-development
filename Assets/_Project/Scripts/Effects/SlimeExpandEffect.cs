using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 粘液扩散效果：扩大粘液系卡牌的触发范围
    /// </summary>
    [CreateAssetMenu(fileName = "SlimeExpandEffect", menuName = "MutationChess/Relic Effects/Slime Expand")]
    public class SlimeExpandEffect : CardEffect
    {
        [Header("粘液扩散配置")]
        [Tooltip("粘液触发范围（卡牌数量）")]
        public int expandRange = 2;


        public static int SlimeTriggerRange = 1;

        public override void Execute(CombatContext context)
        {
            ApplyExpand();
        }

        public override void Execute(EffectContext context)
        {
            ApplyExpand();
        }

        private void ApplyExpand()
        {
            SlimeTriggerRange = expandRange;
            GameLogger.Log($"[SlimeExpand] 粘液触发范围扩大至 {expandRange}");
        }
    }
}


