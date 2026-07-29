using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>



    /// </summary>
    [CreateAssetMenu(fileName = "ShadowBurst", menuName = "MutationChess/Effects/Shadow Burst")]
    public class ShadowBurstEffect : CardEffect
    {
        [Header("")]
        [Tooltip(" =   ")]
        public int multiplier = 2;

        public override void Execute(CombatContext context)
        {
            if (context.targetPlayer == null)
            {
                GameLogger.LogError("ShadowBurstEffect: targetPlayer ");
                return;
            }

            if (context.targetEnemy == null)
            {
                GameLogger.LogError("ShadowBurstEffect: targetEnemy ");
                return;
            }


            int mult = multiplier;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                mult = context.sourceCard.magicNumber;
            }


            int totalStrength = context.targetPlayer.GetBuffAmount(BuffType.Strength);
            int damage = totalStrength * mult;

            GameLogger.Log($"[ShadowBurst]  {totalStrength}  {mult} = {damage} ");


            if (damage > 0)
            {
                context.targetEnemy.TakeDamage(damage);
            }


            int removed = context.targetPlayer.RemoveShadowStrengthBuffs();
            GameLogger.Log($"[ShadowBurst]  {removed} 㰵");
        }
    }
}


