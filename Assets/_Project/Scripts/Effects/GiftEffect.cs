using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 文档说明
    /// </summary>
    [CreateAssetMenu(fileName = "GiftEffect", menuName = "MutationChess/Card Effects/Gift")]
    public class GiftEffect : CardEffect
    {
        public enum GiftTriggerTime
        {
            TurnStart,   //
            TurnEnd,     //
        }

        [Header("触发配置")]
        [Tooltip("礼物触发的时机")]
        public GiftTriggerTime triggerTime = GiftTriggerTime.TurnStart;

        [Tooltip("触发后是否弃置卡牌")]
        public bool discardAfterTrigger = true;

        public override string GetDescription(Card card)
        {
            return "回合开始触发礼物效果后置弃";
        }

        public override void Execute(CombatContext context)
        {
            // 实际逻辑由 CheckAndTriggerGifts 静态方法处理
            GameLogger.Log("[GiftEffect] Execute 需要 HandManager");
        }

        /// <summary>
        /// 检查并触发抽牌堆中所有符合条件的礼物卡
        /// </summary>
        public static List<Card> CheckAndTriggerGifts(GiftTriggerTime time)
        {
            List<Card> triggeredCards = new List<Card>();

            HandManager handManager = HandManager.Instance;
            if (handManager == null) return triggeredCards;

            List<Card> drawPile = handManager.GetDrawPile();
            if (drawPile.Count == 0) return triggeredCards;

            BattleManager battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null) return triggeredCards;

            PlayerData playerData = null;
            var dataMgr = PlayerDataManager.Instance;
            if (dataMgr != null) playerData = dataMgr.GetPlayerData();


            int index = 0;
            while (index < drawPile.Count)
            {
                Card topCard = drawPile[index];
                if (topCard == null)
                {
                    index++;
                    continue;
                }

                GiftEffect giftEffect = FindGiftEffect(topCard, time);
                if (giftEffect == null)
                {
                    index++;
                    continue;
                }


                drawPile.RemoveAt(index);
                triggeredCards.Add(topCard);

                GameLogger.Log($"[GiftEffect] 触发礼物: {topCard.cardName}（时机: {time}）");


                CombatContext context = new CombatContext(
                    battleManager,
                    battleManager.GetCurrentEnemy(),
                    playerData,
                    topCard
                );

                topCard.ExecuteEffects(context);


                if (giftEffect.discardAfterTrigger)
                {
                    handManager.AddToDiscardPile(topCard);
                }
                else
                {
                    handManager.AddToExhaustPile(topCard);
                }


                index = 0;
            }

            if (triggeredCards.Count > 0)
            {
                handManager.UpdatePileCountUI();
                GameLogger.Log($"[GiftEffect] 触发了 {triggeredCards.Count} 张礼物卡");
            }

            return triggeredCards;
        }

        /// <summary>
        /// 从卡牌效果列表中查找指定时机的礼物效果
        /// </summary>
        private static GiftEffect FindGiftEffect(Card card, GiftTriggerTime time)
        {
            if (card == null || card.effects == null) return null;

            foreach (var effect in card.effects)
            {
                if (effect is GiftEffect gift && gift.triggerTime == time)
                {
                    return gift;
                }
            }
            return null;
        }
    }
}
