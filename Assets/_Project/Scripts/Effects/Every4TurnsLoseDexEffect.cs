using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "Every4TurnsLoseDexEffect", menuName = "MutationChess/Relic Effects/Every 4 Turns Lose Dex")]
    public class Every4TurnsLoseDexEffect : CardEffect
    {
        [Tooltip("每次损失的敏捷值")]
        public int loseDex = 1;

        [Tooltip("触发损失的回合间隔")]
        public int turnInterval = 4;

        public override void Execute(CombatContext context) { }

        public override void Execute(EffectContext context)
        {
            if (context == null || context.trigger != EffectTrigger.TurnEnd) return;

            ConversionModifier.TurnCounterForMemoryLens++;
            if (ConversionModifier.TurnCounterForMemoryLens % turnInterval != 0) return;

            PlayerData playerData = context.battleManager?.GetPlayerData();
            if (playerData == null) return;

            playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = -loseDex, duration = -1 });
            GameLogger.Log($"[Every4TurnsLoseDex] 第 {ConversionModifier.TurnCounterForMemoryLens} 回合，损失敏捷 {loseDex}");
        }
    }
}
