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
                Debug.LogError("DealDamageEffect: battleManager 为空！");
                return;
            }

            if (context.targetEnemy == null)
            {
                Debug.LogError("DealDamageEffect: targetEnemy 为空！");
                return;
            }

            if (context.sourceCard == null)
            {
                Debug.LogError("DealDamageEffect: sourceCard 为空！");
                return;
            }

            int damage = context.sourceCard.damage;
            context.battleManager.PlayerAttack(damage);
        }
    }
}