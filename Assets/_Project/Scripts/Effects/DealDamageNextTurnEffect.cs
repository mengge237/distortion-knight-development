using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DealDamageNextTurn", menuName = "MutationChess/Effects/Deal Damage Next Turn")]
    public class DealDamageNextTurnEffect : CardEffect
    {
        [Header("下回合伤害配置")]
        [Tooltip("默认伤害值（当卡牌 magicNumber > 0 时使用 magicNumber）")]
        public int defaultDamage = 12;

        public override string GetDescription(Card card)
        {
            int damage = (card != null && card.magicNumber > 0) ? card.magicNumber : defaultDamage;
            return $"下回合造成 {damage} 点伤害";
        }

        public override void Execute(CombatContext context)
        {
            if (context.targetEnemy == null || context.sourceCard == null) return;
            int damage = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : defaultDamage;
            context.targetEnemy.AddDelayedDamage(damage);
        }
    }
}
