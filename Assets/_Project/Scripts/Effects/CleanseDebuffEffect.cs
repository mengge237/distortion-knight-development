using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 清除减益效果
    /// </summary>
    [CreateAssetMenu(fileName = "CleanseDebuffEffect", menuName = "MutationChess/Potion Effects/Cleanse Debuff")]
    public class CleanseDebuffEffect : CardEffect
    {
        [Header("减益清除配置")]
        [Tooltip("是否清除虚弱")]
        public bool removeWeak = true;
        [Tooltip("是否清除脆弱")]
        public bool removeFrail = true;
        [Tooltip("是否清除易伤")]
        public bool removeVulnerability = true;

        public override void Execute(CombatContext context)
        {
            CleanseDebuffs(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            CleanseDebuffs(context?.battleManager);
        }

        private void CleanseDebuffs(BattleManager battleManager)
        {
            var dataManager = PlayerDataManager.Instance;
            if (dataManager == null) return;

            PlayerData playerData = dataManager.GetPlayerData();
            if (playerData == null) return;

            int removed = 0;

            if (removeWeak)
                removed += playerData.RemoveBuffsByType(BuffType.Weak);
            if (removeFrail)
                removed += playerData.RemoveBuffsByType(BuffType.Frail);
            if (removeVulnerability)
                removed += playerData.RemoveBuffsByType(BuffType.Vulnerability);

            GameLogger.Log($"[CleanseDebuff] 清除了{removed}个减益效果");

            if (battleManager != null)
                battleManager.AddBattleLog($"清除了{removed}个减益效果");

            dataManager.UpdateUI();
        }
    }
}
