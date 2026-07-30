using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyBlock", menuName = "MutationChess/Effects/Apply Block")]
    public class ApplyBlockEffect : CardEffect
    {
        public override string GetDescription(Card card)
        {
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

            if (context.sourceCard == null)
            {
                GameLogger.LogError("ApplyBlockEffect: sourceCard 为空");
                return;
            }

            int block = context.sourceCard.block;
            context.battleManager.PlayerBlock(block);
        }
    }
}
