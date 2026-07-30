using System.Collections.Generic;
using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "DrawReluctantFromDiscardEffect", menuName = "MutationChess/Potion Effects/Draw Reluctant From Discard")]
    public class DrawReluctantFromDiscardEffect : CardEffect
    {
        [Header("?")]
        [Tooltip("")]
        public int drawCount = 2;

        public override void Execute(CombatContext context)
        {
            DrawFromDiscard(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            DrawFromDiscard(context?.battleManager);
        }

        private void DrawFromDiscard(BattleManager battleManager)
        {
            var handManager = HandManager.Instance;
            if (handManager == null) return;


            List<Card> discardPile = handManager.GetDiscardPile();
            int drawn = 0;


            for (int i = discardPile.Count - 1; i >= 0 && drawn < drawCount; i--)
            {
                Card c = discardPile[i];
                if (c == null) continue;

                bool isReluctantCard = c.HasTag(CardTag.Reluctant) || c.faction == CardFaction.Reluctant;
                if (!isReluctantCard) continue;


                discardPile.RemoveAt(i);
                handManager.AddCardToHand(c);
                GameLogger.Log($"[DrawReluctantFromDiscard] �z {c.cardName}");
                drawn++;
            }

            handManager.UpdatePileCountUI();

            if (battleManager != null)
                battleManager.AddBattleLog($"�����ƶѳ�ȡ��{drawn}���ź޿��Ƶ�����");

            if (drawn == 0)
                GameLogger.Log("[DrawReluctantFromDiscard] ");
        }
    }
}


