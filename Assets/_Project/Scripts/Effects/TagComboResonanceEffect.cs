using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 标签共振效果
    /// </summary>
    [CreateAssetMenu(fileName = "TagComboResonanceEffect", menuName = "MutationChess/Effects/Tag Combo Resonance")]
    public class TagComboResonanceEffect : CardEffect
    {
        [Header("共振配置")]
        [Tooltip("是否永久生效")]
        public bool isPermanent = false;

        [Tooltip("临时生效的回合数")]
        public int temporaryDuration = 1;

        public override void Execute(CombatContext context)
        {
            ActivateResonance(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            ActivateResonance(context?.battleManager);
        }

        private void ActivateResonance(BattleManager battleManager)
        {
            ConversionModifier.TagEffectDoubleTrigger = true;
            GameLogger.Log("[TagComboResonance] 下一回合标签共振：标签效果触发两次");

            if (battleManager != null)
                battleManager.AddBattleLog("下一回合标签共振：标签效果触发两次");
        }
    }
}
