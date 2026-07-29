using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyTemporaryStrength", menuName = "MutationChess/Effects/Apply Temporary Strength")]
    public class ApplyTemporaryStrengthEffect : CardEffect
    {
        [Header("力量加成")]
        [Tooltip("获得的力量数值")]
        public int strengthAmount = 2;
        [Tooltip("持续回合数，-1为永久")]
        public int duration = -1;

        public override void Execute(CombatContext context)
        {
            if (context.targetPlayer != null && context.sourceCard != null)
            {
                int amount = strengthAmount;
                int dur = duration;
                if (context.sourceCard.magicNumber > 0) dur = context.sourceCard.magicNumber;
                var buff = new Buff { type = BuffType.Strength, amount = amount, duration = dur };
                context.targetPlayer.AddBuff(buff);
            }
            else if (context.targetEnemy != null)
            {
                int amount = strengthAmount;
                int dur = duration;
                if (context.sourceCard != null && context.sourceCard.magicNumber > 0) dur = context.sourceCard.magicNumber;
                context.targetEnemy.AddBuff(new Buff { type = BuffType.Strength, amount = amount, duration = dur });
            }
        }

        public void ApplyToPlayer(PlayerData player, int amount, int duration)
        {
            if (player == null) return;
            var buff = new Buff
            {
                type = BuffType.Strength,
                amount = amount,
                duration = duration
            };
            player.AddBuff(buff);
        }
    }
}