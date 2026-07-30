using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyTemporaryStrength", menuName = "MutationChess/Effects/Apply Temporary Strength")]
    public class ApplyTemporaryStrengthEffect : CardEffect
    {
        [Header("临时力量配置")]
        [Tooltip("获得的力量数值")]
        public int strengthAmount = 2;

        [Tooltip("持续回合数（-1表示战斗永久生效）。另外magicNumber>0时使用卡牌值作为持续回合")]
        public int duration = -1;

        public override string GetDescription(Card card)
        {
            int dur = (card != null && card.magicNumber > 0) ? card.magicNumber : duration;
            string durText = dur < 0 ? "永久" : $"{dur} 回合";
            if (strengthAmount >= 0)
                return $"临时获得 {strengthAmount} 点力量（{durText}）";
            else
                return $"临时失去 {-strengthAmount} 点力量（{durText}）";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            int amount = strengthAmount;
            int dur = duration;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                dur = context.sourceCard.magicNumber;
            }

            if (context.targetPlayer != null)
            {
                var buff = new Buff { type = BuffType.Strength, amount = amount, duration = dur };
                context.targetPlayer.AddBuff(buff);
                string durText = dur < 0 ? "永久" : $"{dur}回合";
                context.battleManager?.AddLog($"获得 {amount} 点临时力量{durText}。");
            }
            else if (context.targetEnemy != null)
            {
                context.targetEnemy.AddBuff(new Buff { type = BuffType.Strength, amount = amount, duration = dur });
                string durText = dur < 0 ? "永久" : $"{dur}回合";
                context.battleManager?.AddLog($"{context.targetEnemy.enemyName} 获得 {amount} 点临时力量{durText}。");
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
