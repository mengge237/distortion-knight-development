using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 宝藏效果：从无色卡池中随机抽取 X 张卡牌加入手牌。
    /// 无色卡池由所有 isColorless=true 的卡牌组成。
    /// </summary>
    [CreateAssetMenu(fileName = "TreasureEffect", menuName = "MutationChess/Card Effects/Treasure")]
    public class TreasureEffect : CardEffect
    {
        [Header("宝藏参数")]
        [Tooltip("获取的无色牌数量")]
        [Min(1)]
        public int count = 1;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            HandManager handManager = HandManager.Instance;
            if (handManager == null)
            {
                GameLogger.LogWarning("[TreasureEffect] HandManager 未找到");
                return;
            }

            // 从所有卡牌中筛选无色卡，构建无色卡池
            List<CardName> colorlessPool = new List<CardName>();
            var allNames = CardData.GetAllCardNames();
            foreach (var name in allNames)
            {
                var template = CardData.GetTemplate(name);
                if (template != null && template.isColorless)
                {
                    colorlessPool.Add(name);
                }
            }

            if (colorlessPool.Count == 0)
            {
                GameLogger.LogWarning("[TreasureEffect] 无色卡池为空");
                return;
            }

            GameLogger.Log($"[TreasureEffect] 无色卡池数量: {colorlessPool.Count}, 本次抽取: {count}");

            // 随机抽取 count 张无色牌加入手牌（允许重复抽取）
            for (int i = 0; i < count; i++)
            {
                CardName pickedName = colorlessPool[Random.Range(0, colorlessPool.Count)];
                Card newCard = CardData.CreateCard(pickedName);
                if (newCard != null)
                {
                    handManager.AddCardToHand(newCard);
                    GameLogger.Log($"[TreasureEffect] 获得无色牌: {newCard.cardName}");
                }
                else
                {
                    GameLogger.LogError($"[TreasureEffect] 无法创建无色牌: {pickedName}");
                }
            }

            handManager.UpdatePileCountUI();
        }
    }
}
