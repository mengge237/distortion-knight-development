using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DealDamage", menuName = "MutationChess/Effects/Deal Damage")]
    public class DealDamageEffect : CardEffect
    {
        public override string GetDescription(Card card)
        {
            if (card != null && card.damage > 0)
                return $"造成 {card.damage} 点伤害";
            return string.IsNullOrEmpty(effectDescription) ? "造成伤害" : effectDescription;
        }

        public override void Execute(CombatContext context)
        {
            if (context.battleManager == null)
            {
                GameLogger.LogError("DealDamageEffect: battleManager 为空");
                return;
            }

            if (context.targetEnemy == null)
            {
                GameLogger.LogError("DealDamageEffect: targetEnemy 为空");
                return;
            }

            if (context.sourceCard == null)
            {
                GameLogger.LogError("DealDamageEffect: sourceCard 为空");
                return;
            }

            int damage = context.sourceCard.damage;
            context.battleManager.PlayerAttack(damage);
        }
    }
}
