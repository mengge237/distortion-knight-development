using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 诅咒_噬命：每打出一张卡时损失固定HP。
    /// 实际逻辑由 HandManager.TriggerCurseDevourEffects() 读取 hpLossPerCard 字段，
    /// Execute 留空。
    /// </summary>
    [CreateAssetMenu(fileName = "CurseDevourEffect", menuName = "MutationChess/Curse Effects/Devour")]
    public class CurseDevourEffect : CurseEffect
    {
        [Tooltip("每打出一张卡时损失的HP数量")]
        [Min(0)]
        public int hpLossPerCard = 1;

        public override void Execute(CombatContext context)
        {
            // 效果不通过 Execute 生效，依赖 HandManager 的专用流程读取字段值
        }
    }
}
