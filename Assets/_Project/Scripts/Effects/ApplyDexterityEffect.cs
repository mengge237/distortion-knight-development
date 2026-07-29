using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyDexterity", menuName = "MutationChess/Effects/Apply Dexterity")]
    public class ApplyDexterityEffect : CardEffect
    {
        [Header("")]
        [Tooltip("(magicNumber>0magicNumber)")]
        public int defaultAmount = 3;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            int amount = (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                ? context.sourceCard.magicNumber : defaultAmount;

            if (context.targetPlayer != null)
            {
                var buff = new Buff { type = BuffType.Dexterity, amount = amount, duration = 999 };
                context.targetPlayer.AddBuff(buff);
                GameLogger.Log($"[ApplyDexterityEffect] {amount} ");
            }
            else if (context.targetEnemy != null && context.sourceCard != null)
            {
                context.targetEnemy.AddBuff(new Buff { type = BuffType.Dexterity, amount = amount, duration = 999 });
            }
        }
    }
}