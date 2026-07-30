using UnityEngine;
using MutationChess.Core;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 消耗回复能量效果
    /// Boss 加成：能量获取 +2（翻倍）
    /// </summary>
    [CreateAssetMenu(fileName = "ExhaustEnergyEffect", menuName = "MutationChess/Relic Effects/Exhaust Energy")]
    public class ExhaustEnergyEffect : CardEffect
    {
        [Header("能量配置")]
        [Tooltip("每次触发回复的能量值")]
        public int energyGain = 1;

        [Tooltip("每回合最多触发的次数")]
        public int triggersPerTurn = 1;

        [System.NonSerialized]
        private int triggersThisTurn = 0;

        public override string GetDescription(Card card)
        {
            return $"每回合消耗牌时获 {energyGain} 能量（最多 {triggersPerTurn} 次）";
        }

        public override void Execute(CombatContext context)
        {
            TryGrantEnergy(context);
        }

        public override void Execute(EffectContext context)
        {
            if (context != null && context.trigger == EffectTrigger.PlayerTurnStart)
            {
                ResetTurnCount();
                return;
            }
            TryGrantEnergy(context?.combat);
        }

        private void TryGrantEnergy(CombatContext context)
        {
            if (context == null) return;
            if (triggersThisTurn >= triggersPerTurn) return;

            triggersThisTurn++;

            int effectiveGain = energyGain;
            if (ConversionModifier.BossCorruptLiverActive)
                effectiveGain = energyGain * 2;

            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.RestoreEnergy(effectiveGain);
                GameLogger.Log($"[ExhaustEnergy] 回复能量 +{effectiveGain} 点 ({triggersThisTurn}/{triggersPerTurn}){(ConversionModifier.BossCorruptLiverActive ? " (Boss加倍)" : "")}");
            }
        }

        public void ResetTurnCount()
        {
            triggersThisTurn = 0;
        }
    }
}
