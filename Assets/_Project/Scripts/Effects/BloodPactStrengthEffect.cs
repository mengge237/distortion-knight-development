using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BloodPactStrengthEffect", menuName = "MutationChess/Relic Effects/Blood Pact Str & HP")]
    public class BloodPactStrengthEffect : CardEffect
    {
        [Tooltip("+2+1??")]
        public int extraStr = 1;

        [Tooltip("")]
        public int loseMaxHp = 5;

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

            GameLogger.Log($"[BloodPact] +{extraStr}-{loseMaxHp}");
        }
    }
}
