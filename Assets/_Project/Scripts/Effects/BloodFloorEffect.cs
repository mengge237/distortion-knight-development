using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 鲜血保底效果：当玩家生命值低于阈值时恢复至阈值
    /// </summary>
    [CreateAssetMenu(fileName = "BloodFloorEffect", menuName = "MutationChess/Relic Effects/Blood Floor")]
    public class BloodFloorEffect : CardEffect
    {
        [Header("鲜血保底配置")]
        [Tooltip("生命值保底阈值，低于此值时恢复至此值")]
        public int healthFloor = 1;

        public override string GetDescription(Card card)
        {
            return $"战斗中生命最低 {healthFloor} 点（不致死）";
        }

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
                GameLogger.Log($"[BloodFloor] 生命值低于 {healthFloor}，恢复 {restored} 点生命");
                dataManager.UpdateUI();
            }
        }
    }
}


