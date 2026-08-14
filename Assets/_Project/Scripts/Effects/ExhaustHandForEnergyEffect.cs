using System.Collections.Generic;
using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 消耗手牌回能量效果
    /// </summary>
    [CreateAssetMenu(fileName = "ExhaustHandForEnergyEffect", menuName = "MutationChess/Potion Effects/Exhaust Hand For Energy")]
    public class ExhaustHandForEnergyEffect : CardEffect
    {
        [Header("能量配置")]
        [Tooltip("每消耗一张卡牌获得的能量")]
        public int energyPerCard = 1;

        public override string GetDescription(Card card)
        {
            return $"消耗全部手牌，每张获得 {energyPerCard} 能量";
        }

        public override void Execute(CombatContext context)
        {
            ExhaustHand(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            ExhaustHand(context?.battleManager);
        }

        private void ExhaustHand(BattleManager battleManager)
        {
            var handManager = HandManager.Instance;
            if (handManager == null) return;

            List<Card> handCards = handManager.GetHandCards();
            if (handCards.Count == 0)
            {
                GameLogger.Log("[ExhaustHand] 手牌为空，无法消耗回能量");
                if (battleManager != null)
                    battleManager.AddBattleLog("手牌为空，无法消耗回能量");
                return;
            }

            int exhaustedCount = handCards.Count;
            int totalEnergy = exhaustedCount * energyPerCard;

            int actualExhausted = handManager.ExhaustHand();

            if (totalEnergy > 0)
            {
                handManager.RestoreEnergy(totalEnergy);
            }

            GameLogger.Log($"[ExhaustHand] 消耗了{actualExhausted}张卡牌，回复{totalEnergy}点能量");

            if (battleManager != null)
                battleManager.AddBattleLog($"消耗了{actualExhausted}张卡牌，回复{totalEnergy}点能量");
        }
    }
}
