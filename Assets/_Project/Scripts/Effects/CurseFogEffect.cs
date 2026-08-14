using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 诅咒_迷雾：降低手牌上限，使玩家每回合能持有更少的卡牌。
    /// 实际逻辑由 HandManager.GetEffectiveMaxHandSize() 读取 handSizeReduction 字段，Execute 留空。
    /// </summary>
    [CreateAssetMenu(fileName = "CurseFogEffect", menuName = "MutationChess/Curse Effects/Fog")]
    public class CurseFogEffect : CurseEffect
    {
        [Tooltip("手牌上限减少量")]
        [Min(1)]
        public int handSizeReduction = 1;

        public override void Execute(CombatContext context)
        {
            // 效果不通过 Execute 生效，依赖 HandManager / UI 的专用流程读取字段值
        }
    }
}
