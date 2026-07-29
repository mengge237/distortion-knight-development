using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "CleanseDebuffEffect", menuName = "MutationChess/Potion Effects/Cleanse Debuff")]
    public class CleanseDebuffEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public bool removeWeak = true;
        [Tooltip("")]
        public bool removeFrail = true;
        [Tooltip("")]
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

            GameLogger.Log($"[CleanseDebuff]  {removed} debuff");

            if (battleManager != null)
                battleManager.AddBattleLog($" {removed} ");

            dataManager.UpdateUI();
        }
    }
}


