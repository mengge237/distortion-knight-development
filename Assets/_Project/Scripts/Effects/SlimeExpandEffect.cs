using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>





    /// </summary>
    [CreateAssetMenu(fileName = "SlimeExpandEffect", menuName = "MutationChess/Relic Effects/Slime Expand")]
    public class SlimeExpandEffect : CardEffect
    {
        [Header("")]
        [Tooltip("N")]
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
            GameLogger.Log($"[SlimeExpand]  {expandRange}");
        }
    }
}


