using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ChessMasterEvery6CardsStrengthEffect", menuName = "MutationChess/Relic Effects/Chess Master 6 Cards")]
    public class ChessMasterEvery6CardsStrengthEffect : CardEffect
    {
        [Tooltip("")]
        public int threshold = 6;

        [Tooltip("")]
        public int strengthGain = 1;

        public override void Execute(CombatContext context)
        {
            //
            // CombatContext n
            PlayerData playerData = context?.battleManager?.GetPlayerData();
            if (playerData == null) return;

            if (ConversionModifier.CardsPlayedThisBattle % threshold != 0) return;

            playerData.AddBuff(new Buff
            {
                type = BuffType.Strength,
                amount = strengthGain,
                duration = -1
            });

            GameLogger.Log($"[ChessMaster] ??{ConversionModifier.CardsPlayedThisBattle}{strengthGain}");
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

            GameLogger.Log($"[ChessMaster] ??{ConversionModifier.CardsPlayedThisBattle}{strengthGain}");
        }
    }
}
