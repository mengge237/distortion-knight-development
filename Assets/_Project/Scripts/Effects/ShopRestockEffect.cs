using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 商店补货效果
    /// 此效果不通过 Execute 触发，而是 Passive 被动触发
    /// </summary>
    [CreateAssetMenu(fileName = "ShopRestockEffect", menuName = "MutationChess/Relic Effects/Shop Restock")]
    public class ShopRestockEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            GameLogger.Log("[ShopRestockEffect] 商店补货");
        }
    }
}

