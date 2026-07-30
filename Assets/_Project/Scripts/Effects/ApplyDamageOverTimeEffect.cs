using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyDamageOverTime", menuName = "MutationChess/Effects/Apply Damage Over Time")]
    public class ApplyDamageOverTimeEffect : CardEffect
    {
        [Header("持续伤害配置")]
        [Tooltip("默认中毒层数（当卡牌 magicNumber > 0 时使用 magicNumber）")]
        public int defaultPoison = 3;

        public override string GetDescription(Card card)
        {
            int poisonCount = (card != null && card.magicNumber > 0) ? card.magicNumber : defaultPoison;
            return $"施加 {poisonCount} 层中毒";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;
            if (context.targetEnemy == null || context.sourceCard == null) return;
            int poisonCount = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : defaultPoison;
            context.targetEnemy.AddBuff(new Buff
            {
                type = BuffType.Poison,
                amount = poisonCount,
                duration = 999
            });
        }
    }
}
