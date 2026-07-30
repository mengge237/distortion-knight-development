using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BlockToAttack", menuName = "MutationChess/Effects/Block To Attack")]
    public class BlockToAttackEffect : CardEffect
    {
        [Header("转化配置")]
        [Tooltip("转化倍数（magicNumber>0时使用卡牌值）")]
        public int multiplier = 2;

        public override string GetDescription(Card card)
        {
            int mult = (card != null && card.magicNumber > 0) ? card.magicNumber : multiplier;
            int blockVal = card != null ? card.block : 0;
            if (blockVal > 0)
                return $"将 {blockVal} 点格挡×{mult} 转换为伤害";
            else
                return $"将当前格挡×{mult} 转换为伤害";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            if (context.battleManager == null)
            {
                GameLogger.LogError("BlockToAttackEffect: battleManager 为空");
                return;
            }

            if (context.targetEnemy == null)
            {
                GameLogger.LogError("BlockToAttackEffect: targetEnemy 为空");
                return;
            }

            int mult = multiplier;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                mult = context.sourceCard.magicNumber;
            }

            int currentBlock = context.battleManager.GetPlayerBlock();
            int damage = currentBlock * mult;

            if (currentBlock > 0)
            {
                context.battleManager.ConsumePlayerBlock(currentBlock);
            }

            if (damage > 0)
            {
                context.targetEnemy.TakeDamage(damage);
                context.battleManager?.AddLog($"将 {currentBlock} 点格挡，对 {context.targetEnemy.enemyName} 造成 {damage} 点伤害（x{mult}）");
            }
            else
            {
                context.battleManager?.AddLog($"玩家当前无格挡可转化为伤害");
            }

            GameLogger.Log($"[BlockToAttack] 格挡{currentBlock} x 倍率{mult} = 伤害{damage}");
        }
    }
}
