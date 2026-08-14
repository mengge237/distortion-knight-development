using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>





    /// </summary>
    [CreateAssetMenu(fileName = "SlimeEnergyEffect", menuName = "MutationChess/Relic Effects/Slime Energy")]
    public class SlimeEnergyEffect : CardEffect
    {
        [Header("能量配置")]
        [Tooltip("打出史莱姆卡牌时获得的能量")]
        public int energyGain = 1;

        [Tooltip("Boss")]
        public int bossExtraEnergy = 1;

        public override void Execute(CombatContext context)
        {
            TryGrantEnergy(context);
        }

        public override void Execute(EffectContext context)
        {
            TryGrantEnergy(context?.combat);
        }

        private void TryGrantEnergy(CombatContext context)
        {
            if (context == null || context.sourceCard == null) return;


            bool isSlimeCard = context.sourceCard.HasTag(CardTag.Slime)
                || context.sourceCard.faction == CardFaction.Slime;
            if (!isSlimeCard) return;


            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                int totalEnergy = energyGain + (ConversionModifier.BossSlimeGlandActive ? bossExtraEnergy : 0);
                handManager.RestoreEnergy(totalEnergy);
                GameLogger.Log($"[SlimeEnergy] 史莱姆卡 {context.sourceCard.cardName} 能量+{totalEnergy}" +
                    (ConversionModifier.BossSlimeGlandActive ? " (Boss加成)" : ""));
            }
        }
    }
}


