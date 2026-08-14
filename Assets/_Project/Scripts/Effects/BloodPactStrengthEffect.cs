using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BloodPactStrengthEffect", menuName = "MutationChess/Relic Effects/Blood Pact Str & HP")]
    public class BloodPactStrengthEffect : CardEffect
    {
        [Tooltip("额外增加的力量值")]
        public int extraStr = 1;

        [Tooltip("减少的最大生命值")]
        public int loseMaxHp = 5;

        public override string GetDescription(Card card)
        {
            return $"获得 {extraStr} 力量，最大生命 -{loseMaxHp}";
        }

        public override void Execute(CombatContext context)
        {
            ApplyBloodPact(context?.targetPlayer ?? context?.battleManager?.GetPlayerData());
        }

        public override void Execute(EffectContext context)
        {
            ApplyBloodPact(context?.battleManager?.GetPlayerData());
        }

        private void ApplyBloodPact(PlayerData playerData)
        {
            if (playerData == null) return;

            playerData.AddBuff(new Buff { type = BuffType.Strength, amount = extraStr, duration = -1 });
            playerData.maxHealth = Mathf.Max(1, playerData.maxHealth - loseMaxHp);
            playerData.currentHealth = Mathf.Min(playerData.currentHealth, playerData.maxHealth);

            GameLogger.Log($"[BloodPact] 获得力量 +{extraStr}，降低最大生命值 -{loseMaxHp}");
        }
    }
}
