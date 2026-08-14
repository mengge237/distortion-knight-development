using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BossBloodVeinEffect", menuName = "MutationChess/Relic Effects/Boss/Blood Vein")]
    public class BossBloodVeinEffect : CardEffect
    {
        [Tooltip("最大生命值变化量（负值为降低）")]
        public int maxHp = -5;

        [Tooltip("每点最大生命值转化的力量系数")]
        public float strengthPerMaxHp = 0.5f;

        public override void Execute(CombatContext context)
        {
            ApplyBloodVein(context?.targetPlayer ?? context?.battleManager?.GetPlayerData());
        }

        public override void Execute(EffectContext context)
        {
            ApplyBloodVein(context?.battleManager?.GetPlayerData());
        }

        private void ApplyBloodVein(PlayerData playerData)
        {
            if (playerData == null) return;

            playerData.maxHealth += maxHp;
            playerData.currentHealth = Mathf.Min(playerData.currentHealth, playerData.maxHealth);

            int bonusStrength = Mathf.FloorToInt(playerData.maxHealth * strengthPerMaxHp);
            if (bonusStrength > 0)
            {
                playerData.AddBuff(new Buff { type = BuffType.Strength, amount = bonusStrength, duration = -1 });
                GameLogger.Log($"[BossBloodVein] 最大生命值 {maxHp}，获得力量 {bonusStrength}");
            }

            ConversionModifier.BossBloodVeinActive = true;
        }
    }
}
