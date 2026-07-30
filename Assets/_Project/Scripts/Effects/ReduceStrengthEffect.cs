using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ReduceStrength", menuName = "MutationChess/Card Effects/Reduce Strength")]
    public class ReduceStrengthEffect : CardEffect
    {
        [Tooltip("减少的力量数值（magicNumber>0时使用卡牌值）")]
        public int reduceAmount = 3;

        public override string GetDescription(Card card)
        {
            int amount = (card != null && card.magicNumber > 0) ? card.magicNumber : reduceAmount;
            return $"减少 {amount} 点力量";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            Enemy enemy = context.targetEnemy ?? context.battleManager?.GetCurrentEnemy();
            if (enemy == null)
            {
                GameLogger.LogWarning("[ReduceStrengthEffect] 没有目标敌人");
                return;
            }

            int amount = reduceAmount;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                amount = context.sourceCard.magicNumber;
            }

            enemy.AddBuff(new Buff { type = BuffType.Strength, amount = -amount, duration = -1 });
            context.battleManager?.AddLog($"{enemy.enemyName} 减少 {amount} 点力量");
            GameLogger.Log($"[ReduceStrengthEffect] {enemy.enemyName} 力量 -{amount}");
        }
    }
}
