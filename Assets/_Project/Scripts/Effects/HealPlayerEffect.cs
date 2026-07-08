using UnityEngine;
using MutationChess.Core;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "HealPlayer", menuName = "MutationChess/Effects/Heal Player")]
    public class HealPlayerEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context.sourceCard == null) return;
            int healAmount = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : 5;

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
            {
                dataManager.Heal(healAmount);
            }
        }
    }
}