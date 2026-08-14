using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyThorns", menuName = "MutationChess/Card Effects/Apply Thorns")]
    public class ApplyThornsEffect : CardEffect
    {
        [Tooltip("荆棘数值（magicNumber>0时使用卡牌值）")]
        public int thornsAmount = 3;

        public override string GetDescription(Card card)
        {
            int amount = (card != null && card.magicNumber > 0) ? card.magicNumber : thornsAmount;
            return $"获得 {amount} 点荆棘";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[ApplyThornsEffect] playerData 为空");
                return;
            }

            int amount = thornsAmount;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                amount = context.sourceCard.magicNumber;
            }

            playerData.AddBuff(new Buff { type = BuffType.Thorns, amount = amount, duration = -1 });
            context.battleManager?.AddLog($"获得 {amount} 点荆棘（受击时反弹伤害）");
            GameLogger.Log($"[ApplyThornsEffect] 荆棘 +{amount}");
        }
    }
}
