using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 血祭效果：战斗开始时以生命为代价（或回复生命）换取永久力量。
    /// 由效果合并从 BloodAltarEffect（失去血量获得力量）/ BloodAltarBoostedEffect（回复生命获得力量）合并而来
    /// （仅生命变化方向与数值不同，逻辑完全一致）。
    /// </summary>
    [CreateAssetMenu(fileName = "BloodTributeEffect", menuName = "MutationChess/Relic Effects/Blood Tribute")]
    public class BloodTributeEffect : CardEffect
    {
        [Header("血祭参数")]
        [Tooltip("生命变化量（负=失去，正=回复）")]
        public int hpChange = -5;

        [Tooltip("获得的永久力量层数")]
        public int strengthGain = 2;

        public override void Execute(CombatContext context)
        {
            ApplyTribute(context?.targetPlayer ?? context?.battleManager?.GetPlayerData());
        }

        public override void Execute(EffectContext context)
        {
            ApplyTribute(context?.battleManager?.GetPlayerData());
        }

        private void ApplyTribute(PlayerData playerData)
        {
            if (playerData == null)
            {
                GameLogger.LogError("[BloodTribute] playerData 为空！");
                return;
            }

            if (hpChange < 0)
                playerData.TakeDamage(-hpChange);
            else if (hpChange > 0)
                playerData.Heal(hpChange);

            playerData.AddBuff(new Buff
            {
                type = BuffType.Strength,
                amount = strengthGain,
                duration = -1
            });

            GameLogger.Log(hpChange < 0
                ? $"[BloodTribute] 失去 {-hpChange} 血，获得 {strengthGain} 层永久力量"
                : $"[BloodTribute] 恢复生命 +{hpChange}，获得力量 +{strengthGain}");
        }
    }
}
