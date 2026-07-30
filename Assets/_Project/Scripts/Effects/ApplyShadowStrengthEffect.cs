using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyShadowStrength", menuName = "MutationChess/Effects/Apply Shadow Strength")]
    public class ApplyShadowStrengthEffect : CardEffect
    {
        [Header("暗影力量配置")]
        [Tooltip("暗影力量数值（magicNumber>0时使用卡牌值）")]
        public int strengthAmount = 2;

        public override string GetDescription(Card card)
        {
            int amount = (card != null && card.magicNumber > 0) ? card.magicNumber : strengthAmount;
            return $"暗影获得 {amount} 点力量";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

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
                context.battleManager?.AddLog($"获得 {amount} 点暗影力量（可被暗影爆发触发）");
                GameLogger.Log($"[ApplyShadowStrength] 获得暗影力量 +{amount}");
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
                context.battleManager?.AddLog($"{context.targetEnemy.enemyName} 获得 {amount} 点暗影力量");
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
