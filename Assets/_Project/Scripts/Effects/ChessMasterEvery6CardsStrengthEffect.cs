using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ChessMasterEvery6CardsStrengthEffect", menuName = "MutationChess/Relic Effects/Chess Master 6 Cards")]
    public class ChessMasterEvery6CardsStrengthEffect : CardEffect
    {
        [Tooltip("触发力量加成的出牌数量阈值")]
        public int threshold = 6;

        [Tooltip("触发时获得的力量层数")]
        public int strengthGain = 1;

        public override void Execute(CombatContext context)
        {
            // 通过出牌事件触发
            // CombatContext 为战斗上下文
            PlayerData playerData = context?.battleManager?.GetPlayerData();
            if (playerData == null) return;

            if (ConversionModifier.CardsPlayedThisBattle % threshold != 0) return;

            playerData.AddBuff(new Buff
            {
                type = BuffType.Strength,
                amount = strengthGain,
                duration = -1
            });

            GameLogger.Log($"[ChessMaster] 出牌 {ConversionModifier.CardsPlayedThisBattle} 张，获得力量 +{strengthGain}");
        }

        public override void Execute(EffectContext context)
        {
            if (context == null || context.trigger != EffectTrigger.AfterCardsPlayed) return;

            ConversionModifier.CardsPlayedThisBattle++;
            if (ConversionModifier.CardsPlayedThisBattle % threshold != 0) return;

            PlayerData playerData = context.battleManager?.GetPlayerData();
            if (playerData == null) return;

            playerData.AddBuff(new Buff
            {
                type = BuffType.Strength,
                amount = strengthGain,
                duration = -1
            });

            GameLogger.Log($"[ChessMaster] 出牌 {ConversionModifier.CardsPlayedThisBattle} 张，获得力量 +{strengthGain}");
        }
    }
}
