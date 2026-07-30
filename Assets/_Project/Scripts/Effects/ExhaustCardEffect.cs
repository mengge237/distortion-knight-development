using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ExhaustCard", menuName = "MutationChess/Effects/Exhaust Card")]
    public class ExhaustCardEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context.sourceCard == null) return;
            context.sourceCard.exhaust = true;
            context.battleManager?.AddLog($"卡牌【{context.sourceCard.cardName}】使用后将被消耗");
            GameLogger.Log($"[ExhaustCardEffect] 标记卡牌消耗：{context.sourceCard.cardName}");
        }
    }
}