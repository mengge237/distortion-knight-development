using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "Gain12GoldOnVictoryEffect", menuName = "MutationChess/Relic Effects/Gain 12 Gold Victory")]
    public class Gain12GoldOnVictoryEffect : CardEffect
    {
        [Tooltip("")]
        public int gold = 12;

        public override void Execute(CombatContext context)
        {
            GrantGold();
        }

        public override void Execute(EffectContext context)
        {
            GrantGold();
        }

        private void GrantGold()
        {
            var dataManager = PlayerDataManager.Instance;
            if (dataManager == null) return;

            dataManager.AddGold(gold);
            dataManager.UpdateUI();
            GameLogger.Log($"[Gain12GoldOnVictory] {gold} ");
        }
    }
}
