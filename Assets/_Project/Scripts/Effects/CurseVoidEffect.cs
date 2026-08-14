using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 诅咒_虚空：能量上限减少 / 或每回合开始失去能量。
    /// 实际逻辑由 HandManager 的能量流程读取 energyLossPerTurn 字段，Execute 留空。
    /// </summary>
    [CreateAssetMenu(fileName = "CurseVoidEffect", menuName = "MutationChess/Curse Effects/Void")]
    public class CurseVoidEffect : CurseEffect
    {
        [Tooltip("每回合开始时失去的能量数")]
        [Min(0)]
        public int energyLossPerTurn = 1;

        public override void Execute(CombatContext context)
        {
            // 效果不通过 Execute 生效，依赖 HandManager 的专用流程读取字段值
        }
    }
}
