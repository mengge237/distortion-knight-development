using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "HealPlayer", menuName = "MutationChess/Effects/Heal Player")]
    public class HealPlayerEffect : CardEffect
    {
        [Header("恢复配置")]
        [Tooltip("默认恢复生命值（magicNumber>0时使用卡牌值）")]
        public int healAmount = 5;

        public override string GetDescription(Card card)
        {
            int amount = (card != null && card.magicNumber > 0) ? card.magicNumber : healAmount;
            return $"恢复 {amount} 点生命";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            int amount = (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                ? context.sourceCard.magicNumber : healAmount;

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
            {
                int actualHeal = dataManager.Heal(amount);
                context.battleManager?.AddLog($"恢复 {actualHeal} 点生命");
            }
        }
    }
}
