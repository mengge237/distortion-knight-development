using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyDexterity", menuName = "MutationChess/Effects/Apply Dexterity")]
    public class ApplyDexterityEffect : CardEffect
    {
        [Header("敏捷配置")]
        [Tooltip("默认敏捷数值（magicNumber>0时使用卡牌值）")]
        public int defaultAmount = 3;

        public override string GetDescription(Card card)
        {
            int amount = (card != null && card.magicNumber > 0) ? card.magicNumber : defaultAmount;
            if (amount >= 0)
                return $"获得 {amount} 点敏捷";
            else
                return $"失去 {-amount} 点敏捷";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            int amount = (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                ? context.sourceCard.magicNumber : defaultAmount;

            var buff = new Buff { type = BuffType.Dexterity, amount = amount, duration = 999 };

            if (context.targetPlayer != null)
            {
                context.targetPlayer.AddBuff(buff);
                context.battleManager?.AddLog($"获得 {amount} 点敏捷（永久生效）");
            }
            else if (context.targetEnemy != null)
            {
                context.targetEnemy.AddBuff(buff);
                context.battleManager?.AddLog($"{context.targetEnemy.enemyName} 获得 {amount} 点敏捷");
            }
        }
    }
}
