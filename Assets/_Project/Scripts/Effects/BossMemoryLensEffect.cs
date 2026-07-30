using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BossMemoryLensEffect", menuName = "MutationChess/Relic Effects/Boss/Memory Lens")]
    public class BossMemoryLensEffect : CardEffect
    {
        [Tooltip("暗影伤害值")]
        public int shadowDmg = 3;

        [Tooltip("战斗开始时获得的格挡值")]
        public int startBlock = 15;

        [Tooltip("每N回合损失的敏捷值")]
        public int loseDexPerNTurns = 1;

        [Tooltip("触发损失的回合间隔")]
        public int turnInterval = 4;

        public override void Execute(CombatContext context)
        {
            PlayerData playerData = context?.targetPlayer ?? context?.battleManager?.GetPlayerData();
            if (playerData == null) return;

            if (context?.battleManager != null)
                context.battleManager.PlayerBlock(startBlock);

            ConversionModifier.BossMemoryLensActive = true;
            ConversionModifier.TurnCounterForMemoryLens = 0;
            GameLogger.Log($"[BossMemoryLens] 暗影伤害+{shadowDmg} 格挡+{startBlock} 每{turnInterval}回合-{loseDexPerNTurns}敏捷");
        }

        public override void Execute(EffectContext context)
        {
            Execute(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }
    }
}
