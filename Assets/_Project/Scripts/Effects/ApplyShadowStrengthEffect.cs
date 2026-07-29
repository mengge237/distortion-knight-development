using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>



    /// </summary>
    [CreateAssetMenu(fileName = "ApplyShadowStrength", menuName = "MutationChess/Effects/Apply Shadow Strength")]
    public class ApplyShadowStrengthEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int strengthAmount = 2;

        public override void Execute(CombatContext context)
        {
            if (context.targetPlayer != null)
            {
                int amount = strengthAmount;

                if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                {
                    amount = context.sourceCard.magicNumber;
                }


                var buff = new Buff
                {
                    type = BuffType.Strength,
                    amount = amount,
                    duration = -1,
                    isShadow = true
                };
                context.targetPlayer.AddBuff(buff);
                GameLogger.Log($"[ApplyShadowStrength]  {amount} 㰵");
            }
            else if (context.targetEnemy != null)
            {

                int amount = strengthAmount;
                if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                {
                    amount = context.sourceCard.magicNumber;
                }
                context.targetEnemy.AddBuff(new Buff
                {
                    type = BuffType.Strength,
                    amount = amount,
                    duration = -1,
                    isShadow = true
                });
            }
        }

        public void ApplyToPlayer(PlayerData player, int amount)
        {
            if (player == null) return;
            var buff = new Buff
            {
                type = BuffType.Strength,
                amount = amount,
                duration = -1,
                isShadow = true
            };
            player.AddBuff(buff);
        }
    }
}


