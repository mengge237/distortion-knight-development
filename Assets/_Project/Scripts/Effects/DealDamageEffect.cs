using UnityEngine;
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
                Debug.LogError("DealDamageEffect: battleManager Îª¿Õ£¡");
                return;
            }

            if (context.targetEnemy == null)
            {
                Debug.LogError("DealDamageEffect: targetEnemy Îª¿Õ£¡");
                return;
            }

            if (context.sourceCard == null)
            {
                Debug.LogError("DealDamageEffect: sourceCard Îª¿Õ£¡");
                return;
            }

            int damage = context.sourceCard.damage;
            context.battleManager.PlayerAttack(damage);
        }
    }
}