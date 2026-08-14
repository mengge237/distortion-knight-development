using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BloodAltarBoostedEffect", menuName = "MutationChess/Relic Effects/Blood Altar Boosted")]
    public class BloodAltarBoostedEffect : CardEffect
    {
        [Tooltip("恢复的生命值数量")]
        public int healthCostReduction = 1;

        [Tooltip("额外获得的力量层数")]
        public int extraStrength = 1;

        public override void Execute(CombatContext context)
        {
            ApplyBoost(context?.targetPlayer ?? context?.battleManager?.GetPlayerData());
        }

        public override void Execute(EffectContext context)
        {
            ApplyBoost(context?.battleManager?.GetPlayerData());
        }

        private void ApplyBoost(PlayerData playerData)
        {
            if (playerData == null) return;

            playerData.Heal(healthCostReduction);
            playerData.AddBuff(new Buff
            {
                type = BuffType.Strength,
                amount = extraStrength,
                duration = -1
            });

            GameLogger.Log($"[BloodAltarBoosted] 恢复生命 +{healthCostReduction}，获得力量 +{extraStrength}");
        }
    }
}
