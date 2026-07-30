using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 胜利回复效果：战斗胜利时恢复一定生命值
    /// </summary>
    [CreateAssetMenu(fileName = "HealOnVictoryEffect", menuName = "MutationChess/Relic Effects/Heal On Victory")]
    public class HealOnVictoryEffect : CardEffect
    {
        [Header("胜利回复配置")]
        [Tooltip("战斗胜利时恢复的生命值数量")]
        public int healAmount = 6;

        public override void Execute(CombatContext context)
        {
            HealOnVictory();
        }

        public override void Execute(EffectContext context)
        {
            HealOnVictory();
        }

        private void HealOnVictory()
        {
            var dataManager = PlayerDataManager.Instance;
            if (dataManager == null) return;

            dataManager.Heal(healAmount);
            GameLogger.Log($"[HealOnVictory] 恢复生命 {healAmount} 点");
            dataManager.UpdateUI();
        }
    }
}


