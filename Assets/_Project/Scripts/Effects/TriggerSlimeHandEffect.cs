using System.Collections.Generic;
using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 触发粘液手牌效果：触发手牌中所有粘液系卡牌的效果
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

            // 全手牌触发期间抑制相邻联动链：每张粘液卡只执行自身效果一次，
            // 否则 A→B→C 相互联动会造成 N² 级重复触发
            bool prevSuppress = SlimeTriggerGuard.SuppressAdjacency;
            SlimeTriggerGuard.SuppressAdjacency = true;

            try
            {
                foreach (var card in handCards)
                {
                    if (card == null) continue;

                    bool isSlimeCard = card.HasTag(CardTag.Slime) || card.faction == CardFaction.Slime;
                    if (!isSlimeCard) continue;

                    if (!SlimeTriggerGuard.TryEnter(card)) continue; // 防重入

                    try
                    {
                        CombatContext cardCtx = new CombatContext(
                            battleManager,
                            battleManager != null ? battleManager.GetCurrentEnemy() : null,
                            null,
                            card
                        );

                        card.ExecuteEffects(cardCtx);
                        triggered++;
                        GameLogger.Log($"[TriggerSlimeHand] 触发卡牌：{card.cardName}");
                    }
                    finally
                    {
                        SlimeTriggerGuard.Exit(card);
                    }
                }
            }
            finally
            {
                SlimeTriggerGuard.SuppressAdjacency = prevSuppress;
            }

            if (battleManager != null)
                battleManager.AddBattleLog($"触发手牌中所有粘液系卡牌，共触发 {triggered} 张粘液系卡牌的效果");
        }
    }
}