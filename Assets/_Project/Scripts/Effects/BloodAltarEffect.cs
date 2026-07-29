using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 血色祭坛遗物效果：战斗开始时失去一定血量，获得永久力量。
    /// 触发时机：BattleStart（此时 EffectContext.combat 为空，需通过 battleManager 获取 PlayerData）。
    /// 简化实现：直接对 PlayerData.TakeDamage 扣血，AddBuff 添加永久力量（duration=-1）。
    /// </summary>
    [CreateAssetMenu(fileName = "BloodAltarEffect", menuName = "MutationChess/Relic Effects/Blood Altar")]
    public class BloodAltarEffect : CardEffect
    {
        [Header("血祭参数")]
        [Tooltip("战斗开始时失去的血量")]
        public int healthCost = 5;

        [Tooltip("获得的永久力量层数")]
        public int strengthGain = 2;

        public override void Execute(CombatContext context)
        {
            ApplyBloodAltar(context?.targetPlayer ?? context?.battleManager?.GetPlayerData());
        }

        public override void Execute(EffectContext context)
        {
            ApplyBloodAltar(context?.battleManager?.GetPlayerData());
        }

        private void ApplyBloodAltar(PlayerData playerData)
        {
            if (playerData == null)
            {
                GameLogger.LogError("[BloodAltar] playerData 为空！");
                return;
            }

            playerData.TakeDamage(healthCost);
            playerData.AddBuff(new Buff
            {
                type = BuffType.Strength,
                amount = strengthGain,
                duration = -1
            });

            GameLogger.Log($"[BloodAltar] 失去 {healthCost} 血，获得 {strengthGain} 层永久力量");
        }
    }
}
