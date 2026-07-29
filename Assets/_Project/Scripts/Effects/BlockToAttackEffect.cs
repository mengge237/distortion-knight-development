using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "BlockToAttack", menuName = "MutationChess/Effects/Block To Attack")]
    public class BlockToAttackEffect : CardEffect
    {
        [Header("")]
        [Tooltip(" =   ")]
        public int multiplier = 2;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            if (context.battleManager == null)
            {
                GameLogger.LogError("BlockToAttackEffect: battleManager ");
                return;
            }

            if (context.targetEnemy == null)
            {
                GameLogger.LogError("BlockToAttackEffect: targetEnemy ");
                return;
            }


            int mult = multiplier;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                mult = context.sourceCard.magicNumber;
            }


            int currentBlock = context.battleManager.GetPlayerBlock();
            int damage = currentBlock * mult;

            GameLogger.Log($"[BlockToAttack]  {currentBlock}  {mult} = {damage} ");


            if (currentBlock > 0)
            {
                context.battleManager.ConsumePlayerBlock(currentBlock);
            }


            if (damage > 0)
            {
                context.targetEnemy.TakeDamage(damage);
            }
        }
    }
}


