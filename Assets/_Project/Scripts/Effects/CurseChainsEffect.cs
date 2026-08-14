using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 诅咒_锁链：每回合抽牌数减少。
    /// 实际值由 HandManager.GetEffectiveCardsPerTurn() 读取 drawReduction 字段，
    /// Execute 留空，不走通用管线。
    /// </summary>
    [CreateAssetMenu(fileName = "CurseChainsEffect", menuName = "MutationChess/Curse Effects/Chains")]
    public class CurseChainsEffect : CurseEffect
    {
        [Tooltip("每回合抽牌数减少量")]
        [Min(0)]
        public int drawReduction = 1;

        public override void Execute(CombatContext context)
        {
            // 效果不通过 Execute 生效，HandManager 的专用流程读取字段值
        }
    }
}
