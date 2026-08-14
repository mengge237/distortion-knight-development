using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyBlock", menuName = "MutationChess/Effects/Apply Block")]
    public class ApplyBlockEffect : CardEffect
    {
        [Header("固定格挡值")]
        [Tooltip(">0 时直接使用该值，忽略卡牌的 block 属性（供遗物等在无 sourceCard 的场景使用）")]
        public int blockAmount = 0;

        public override string GetDescription(Card card)
        {
            if (blockAmount > 0)
                return $"获得 {blockAmount} 点格挡";
            if (card != null && card.block > 0)
                return $"获得 {card.block} 点格挡";
            return string.IsNullOrEmpty(effectDescription) ? "获得格挡" : effectDescription;
        }

        public override void Execute(CombatContext context)
        {
            if (context.battleManager == null)
            {
                GameLogger.LogError("ApplyBlockEffect: battleManager 为空");
                return;
            }

            // 优先使用固定值；其次读取卡牌 block；两者皆无则跳过
            int block = blockAmount;
            if (block <= 0 && context.sourceCard != null)
                block = context.sourceCard.block;
            if (block <= 0)
            {
                GameLogger.LogWarning("ApplyBlockEffect: 无可用格挡值（blockAmount=0 且 sourceCard 为 null 或无格挡）");
                return;
            }

            context.battleManager.PlayerBlock(block);
        }
    }
}
