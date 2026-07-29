using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "AttackHeal1Effect", menuName = "MutationChess/Relic Effects/Attack Heal 1")]
    public class AttackHeal1Effect : CardEffect
    {
        [Tooltip("")]
        public int healAmount = 1;

        public override void Execute(CombatContext context)
        {
            ApplyAttackHeal(context);
        }

        public override void Execute(EffectContext context)
        {
            ApplyAttackHeal(context?.combat);
        }

        private void ApplyAttackHeal(CombatContext context)
        {
            if (context == null) return;

            Card playedCard = context.sourceCard;
            if (playedCard == null || playedCard.cardType != CardType.Attack) return;

            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null) return;

            var dataManager = PlayerDataManager.Instance;
            if (dataManager == null) return;

            dataManager.Heal(healAmount);
            GameLogger.Log($"[AttackHeal1] {playedCard.cardName} {healAmount} HP");
        }
    }
}
