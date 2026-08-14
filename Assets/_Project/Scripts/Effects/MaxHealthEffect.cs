using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 最大生命值提升效果：增加玩家最大生命值并同步恢复等量生命
    /// </summary>
    [CreateAssetMenu(fileName = "MaxHealthEffect", menuName = "MutationChess/Relic Effects/Max Health")]
    public class MaxHealthEffect : CardEffect
    {
        [Header("最大生命值配置")]
        [Tooltip("提升的最大生命值数量")]
        public int maxHealthGain = 1;

        public override string GetDescription(Card card)
        {
            return $"最大生命值 +{maxHealthGain} 并回复等量生命";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[MaxHealthEffect] playerData 为 null");
                return;
            }

            playerData.maxHealth += maxHealthGain;
            playerData.currentHealth += maxHealthGain;
            GameLogger.Log($"[MaxHealthEffect] 最大生命值 +{maxHealthGain}：当前 {playerData.currentHealth}/{playerData.maxHealth}");
        }
    }
}