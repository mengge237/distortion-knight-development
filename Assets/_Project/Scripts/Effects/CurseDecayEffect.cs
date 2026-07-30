using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 诅咒_衰减：每回合结束时扣除固定HP。
    /// 实际逻辑由 HandManager.TriggerCurseDecayEffects() 等诅咒专用方法触发，
    /// Execute 留空，由 HandManager 专用流程读取 hpLossPerTurn 字段。
    /// </summary>
    [CreateAssetMenu(fileName = "CurseDecayEffect", menuName = "MutationChess/Curse Effects/Decay")]
    public class CurseDecayEffect : CurseEffect
    {
        [Tooltip("每回合结束时损失的HP数量")]
        [Min(1)]
        public int hpLossPerTurn = 1;

        public override void Execute(CombatContext context)
        {
            // 效果不通过 Execute 生效，依赖 HandManager 的专用流程读取字段值
        }
    }
}
