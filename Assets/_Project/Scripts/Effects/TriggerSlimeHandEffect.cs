using System.Collections.Generic;
using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "TriggerSlimeHandEffect", menuName = "MutationChess/Potion Effects/Trigger Slime Hand")]
    public class TriggerSlimeHandEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            TriggerSlimeCards(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            TriggerSlimeCards(context?.battleManager);
        }

        private void TriggerSlimeCards(BattleManager battleManager)
        {
            var handManager = HandManager.Instance;
            if (handManager == null) return;


            List<Card> handCards = handManager.GetHandCards();
            int triggered = 0;

            foreach (var card in handCards)
            {
                if (card == null) continue;

                bool isSlimeCard = card.HasTag(CardTag.Slime) || card.faction == CardFaction.Slime;
                if (!isSlimeCard) continue;


                CombatContext cardCtx = new CombatContext(
                    battleManager,
                    battleManager != null ? battleManager.GetCurrentEnemy() : null,
                    null,
                    card
                );

                card.ExecuteEffects(cardCtx);
                triggered++;
                GameLogger.Log($"[TriggerSlimeHand] : {card.cardName}");
            }

            if (battleManager != null)
                battleManager.AddBattleLog($"������������{triggered}��ʷ��ķ���Ƶ�Ч��");
        }
    }
}


