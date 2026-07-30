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
            context.battleManager?.AddLog($"���ơ�{context.sourceCard.cardName}��ʹ�ú󽫱����ģ����������ƶѣ�");
            GameLogger.Log($"[ExhaustCardEffect] �������: {context.sourceCard.cardName}");
        }
    }
}
