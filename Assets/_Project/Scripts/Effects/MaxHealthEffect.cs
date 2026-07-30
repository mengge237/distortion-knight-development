using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    ///
    /// </summary>
    [CreateAssetMenu(fileName = "MaxHealthEffect", menuName = "MutationChess/Relic Effects/Max Health")]
    public class MaxHealthEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int maxHealthGain = 1;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[MaxHealthEffect] playerData is null");
                return;
            }

            playerData.maxHealth += maxHealthGain;
            playerData.currentHealth += maxHealthGain;
            GameLogger.Log($"[MaxHealthEffect] Max HP +{maxHealthGain}: {playerData.currentHealth}/{playerData.maxHealth}");
        }
    }
}