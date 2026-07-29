using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>





    /// </summary>
    [CreateAssetMenu(fileName = "HealOnVictoryEffect", menuName = "MutationChess/Relic Effects/Heal On Victory")]
    public class HealOnVictoryEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
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
            GameLogger.Log($"[HealOnVictory]  {healAmount} ");
            dataManager.UpdateUI();
        }
    }
}


