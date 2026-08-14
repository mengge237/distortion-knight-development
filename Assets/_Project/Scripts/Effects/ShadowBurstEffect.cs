using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ShadowBurst", menuName = "MutationChess/Effects/Shadow Burst")]
    public class ShadowBurstEffect : CardEffect
    {
        [Header("暗影爆发配置")]
        [Tooltip("力量转化为伤害的倍率（magicNumber>0时使用卡牌值）")]
        public int multiplier = 2;

        public override string GetDescription(Card card)
        {
            int mult = (card != null && card.magicNumber > 0) ? card.magicNumber : multiplier;
            return $"总暗影力量×{mult} 造成伤害并移除";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            PlayerData player = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            Enemy enemy = context.targetEnemy ?? context.battleManager?.GetCurrentEnemy();

            if (player == null)
            {
                GameLogger.LogError("ShadowBurstEffect: targetPlayer 为空");
                return;
            }
            if (enemy == null)
            {
                GameLogger.LogError("ShadowBurstEffect: targetEnemy 为空");
                return;
            }

            int mult = multiplier;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                mult = context.sourceCard.magicNumber;
            }

            int totalStrength = player.GetBuffAmount(BuffType.Strength);
            int damage = totalStrength * mult;

            if (damage > 0)
            {
                enemy.TakeDamage(damage);
                context.battleManager?.AddLog($"暗影爆发：将 {totalStrength} 点力量对 {enemy.enemyName} 造成 {damage} 点伤害（x{mult}）");
            }
            else
            {
                context.battleManager?.AddLog($"暗影爆发：玩家当前无暗影力量可用");
            }

            int removed = player.RemoveShadowStrengthBuffs();
            if (removed > 0)
            {
                context.battleManager?.AddLog($"移除了 {removed} 层暗影力量效果");
            }

            GameLogger.Log($"[ShadowBurst] 力量{totalStrength} x 倍率{mult} = 伤害{damage}，移除暗影buff{removed}层");
        }
    }
}
