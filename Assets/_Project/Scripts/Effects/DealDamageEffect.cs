using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DealDamage", menuName = "MutationChess/Effects/Deal Damage")]
    public class DealDamageEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context.battleManager == null)
            {
                GameLogger.LogError("DealDamageEffect: battleManager is null");
                return;
            }

            if (context.targetEnemy == null)
            {
                GameLogger.LogError("DealDamageEffect: targetEnemy is null");
                return;
            }

            if (context.sourceCard == null)
            {
                GameLogger.LogError("DealDamageEffect: sourceCard is null");
                return;
            }

            int damage = context.sourceCard.damage;
            context.battleManager.PlayerAttack(damage);
        }
    }
}