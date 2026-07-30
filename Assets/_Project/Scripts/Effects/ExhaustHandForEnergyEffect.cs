using System.Collections.Generic;
using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "ExhaustHandForEnergyEffect", menuName = "MutationChess/Potion Effects/Exhaust Hand For Energy")]
    public class ExhaustHandForEnergyEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int energyPerCard = 1;

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
                GameLogger.Log("[ExhaustHand] ����Ϊ�գ��޷����Ļ������");
                if (battleManager != null)
                    battleManager.AddBattleLog("����Ϊ�գ��޷����Ļ������");
                return;
            }

            int exhaustedCount = handCards.Count;
            int totalEnergy = exhaustedCount * energyPerCard;


            int actualExhausted = handManager.ExhaustHand();


            if (totalEnergy > 0)
            {
                handManager.RestoreEnergy(totalEnergy);
            }

            GameLogger.Log($"[ExhaustHand] ������{actualExhausted}�����ƣ����{totalEnergy}������");

            if (battleManager != null)
                battleManager.AddBattleLog($"������{actualExhausted}�����ƣ����{totalEnergy}������");
        }
    }
}


