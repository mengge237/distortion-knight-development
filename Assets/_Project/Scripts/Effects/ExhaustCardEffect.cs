using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ExhaustCard", menuName = "MutationChess/Effects/Exhaust Card")]
    public class ExhaustCardEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context.sourceCard == null) return;
            // 标记为已消耗，在HandManager中特殊处理
            // 实际逻辑在HandManager中实现移除
        }
    }
}