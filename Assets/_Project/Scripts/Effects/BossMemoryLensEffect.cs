using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BossMemoryLensEffect", menuName = "MutationChess/Relic Effects/Boss/Memory Lens")]
    public class BossMemoryLensEffect : CardEffect
    {
        [Tooltip("")]
        public int shadowDmg = 3;

        [Tooltip("")]
        public int startBlock = 15;

        [Tooltip("?N")]
        public int loseDexPerNTurns = 1;

        [Tooltip("4")]
        public int turnInterval = 4;

        public override void Execute(CombatContext context)
        {
            PlayerData playerData = context?.targetPlayer ?? context?.battleManager?.GetPlayerData();
            if (playerData == null) return;

            if (context?.battleManager != null)
                context.battleManager.PlayerBlock(startBlock);

            ConversionModifier.BossMemoryLensActive = true;
            ConversionModifier.TurnCounterForMemoryLens = 0;
            GameLogger.Log($"[BossMemoryLens] +{shadowDmg}+{startBlock}{turnInterval}-{loseDexPerNTurns}");
        }

        public override void Execute(EffectContext context)
        {
            Execute(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }
    }
}
