using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    ///
    /// </summary>
    [CreateAssetMenu(fileName = "GiftEffect", menuName = "MutationChess/Card Effects/Gift")]
    public class GiftEffect : CardEffect
    {
        public enum GiftTriggerTime
        {
            TurnStart,   //
            TurnEnd,     //
        }

        [Header("")]
        [Tooltip("")]
        public GiftTriggerTime triggerTime = GiftTriggerTime.TurnStart;

        [Tooltip("")]
        public bool discardAfterTrigger = true;

        public override void Execute(CombatContext context)
        {
            //
            GameLogger.Log("[GiftEffect] Execute HandManager ");
        }

        /// <summary>
        ///
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

                GameLogger.Log($"[GiftEffect] : {topCard.cardName} (: {time})");


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
                GameLogger.Log($"[GiftEffect] {triggeredCards.Count} ");
            }

            return triggeredCards;
        }

        /// <summary>
        ///
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
