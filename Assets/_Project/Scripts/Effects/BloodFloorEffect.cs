using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "BloodFloorEffect", menuName = "MutationChess/Relic Effects/Blood Floor")]
    public class BloodFloorEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int healthFloor = 1;

        public override void Execute(CombatContext context)
        {
            CheckBloodFloor(context);
        }

        public override void Execute(EffectContext context)
        {
            CheckBloodFloor(context?.combat);
        }

        private void CheckBloodFloor(CombatContext context)
        {
            var dataManager = PlayerDataManager.Instance;
            if (dataManager == null) return;

            PlayerData playerData = dataManager.GetPlayerData();
            if (playerData == null) return;

            if (playerData.currentHealth < healthFloor)
            {
                int restored = healthFloor - playerData.currentHealth;
                playerData.currentHealth = healthFloor;
                GameLogger.Log($"[BloodFloor]  {healthFloor}+{restored}");
                dataManager.UpdateUI();
            }
        }
    }
}


