using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    ///
    ///
    /// ?? Execute ? Passive 
    /// </summary>
    [CreateAssetMenu(fileName = "ShopRestockEffect", menuName = "MutationChess/Relic Effects/Shop Restock")]
    public class ShopRestockEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            GameLogger.Log("[ShopRestockEffect] ");
        }
    }
}

