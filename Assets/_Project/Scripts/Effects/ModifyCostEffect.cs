using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ModifyCost", menuName = "MutationChess/Effects/Modify Cost")]
    public class ModifyCostEffect : CardEffect
    {
        public int costModifier = -1;
        public bool applyToAllHand = false;

        public override void Execute(CombatContext context)
        {
            if (context.sourceCard == null) return;

            if (applyToAllHand)
            {
                var handManager = UI.HandManager.Instance;
                if (handManager != null)
                {
                    // 修改手牌中所有卡牌的费用（保留原实现）
                }
            }
            else
            {
                context.sourceCard.cost = Mathf.Max(0, context.sourceCard.cost + costModifier);
            }
        }
    }
}