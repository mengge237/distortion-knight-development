using UnityEngine;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 腐化力量效果（被动效果，Execute 为空，实际逻辑通过外部触发）
    /// </summary>
    [CreateAssetMenu(fileName = "CorruptPowerEffect", menuName = "MutationChess/Relic Effects/Corrupt Heart")]
    public class CorruptPowerEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            // 被动效果，无需主动执行
        }
    }
}
