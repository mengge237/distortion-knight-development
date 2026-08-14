using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 恢复生命效果。
    /// 由 EffectMergeMigration 工具合并了 AttackHeal1Effect / HealOnVictoryEffect。
    /// </summary>
    [CreateAssetMenu(fileName = "HealPlayer", menuName = "MutationChess/Effects/Heal Player")]
    public class HealPlayerEffect : CardEffect
    {
        [Header("恢复配置")]
        [Tooltip("默认恢复生命值")]
        public int healAmount = 5;

        [Tooltip("仅当打出的卡牌为攻击牌时触发")]
        public bool onlyAttackCards = false;

        [Tooltip("卡牌 magicNumber>0 时覆盖 healAmount（默认关闭，避免与卡牌自身数值冲突）")]
        public bool useMagicNumber = false;

        public override string GetDescription(Card card)
        {
            int amount = (useMagicNumber && card != null && card.magicNumber > 0) ? card.magicNumber : healAmount;
            string prefix = onlyAttackCards ? "打出攻击牌时恢复" : "恢复";
            return $"{prefix} {amount} 点生命";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            if (onlyAttackCards && (context.sourceCard == null || context.sourceCard.cardType != CardType.Attack))
                return;

            int amount = (useMagicNumber && context.sourceCard != null && context.sourceCard.magicNumber > 0)
                ? context.sourceCard.magicNumber : healAmount;

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
            {
                int actualHeal = dataManager.Heal(amount);
                dataManager.UpdateUI();
                context.battleManager?.AddLog($"恢复 {actualHeal} 点生命");
                GameLogger.Log($"[HealPlayer] 恢复 {actualHeal} 点生命");
            }
        }
    }
}
