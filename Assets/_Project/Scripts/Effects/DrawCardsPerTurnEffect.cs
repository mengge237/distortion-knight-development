using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 每回合开始时抽牌效果，每回合额外抽一张牌。
    /// 类似粘液之心的效果。
    /// </summary>
    [CreateAssetMenu(fileName = "DrawCardsPerTurnEffect", menuName = "MutationChess/Relic Effects/Draw Cards Per Turn")]
    public class DrawCardsPerTurnEffect : CardEffect
    {
        [Header("抽牌效果")]
        [Tooltip("每回合额外抽牌数量")]
        public int cardsToDraw = 1;

        [Header("负面效果")]
        [Tooltip("手牌上限减少")]
        public int handSizeReduction = 1;

        public override void Execute(CombatContext context)
        {
            // 本效果由效果系统处理，不需要直接执行
            // 由 RelicManager 在 PlayerTurnStart 事件中调用
        }

        public void ExecuteDrawCards(BattleManager battleManager)
        {
            var handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[DrawCardsPerTurnEffect] HandManager 未找到");
                return;
            }

            handManager.DrawCards(cardsToDraw);

            if (battleManager != null)
            {
                battleManager.AddBattleLog($"粘液之心: 抽出 {cardsToDraw} 张牌");
            }
        }
    }
}
