using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "EternalFlameBattleStartEffect", menuName = "MutationChess/Relic Effects/Eternal Flame")]
    public class EternalFlameBattleStartEffect : CardEffect
    {
        [Tooltip("战斗开始时获得的力量值")]
        public int str = 2;

        [Tooltip("战斗开始时获得的敏捷值")]
        public int dex = 2;

        [Tooltip("战斗开始时获得的格挡值")]
        public int block = 10;

        [Tooltip("战斗开始时回复的能量值")]
        public int energy = 2;

        public override void Execute(CombatContext context)
        {
            var bm = context?.battleManager;
            var playerData = context?.targetPlayer ?? bm?.GetPlayerData();
            if (playerData == null) return;

            playerData.AddBuff(new Buff { type = BuffType.Strength, amount = str, duration = -1 });
            playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = dex, duration = -1 });

            if (bm != null)
            {
                bm.PlayerBlock(block);
            }

            var handManager = UI.HandManager.Instance;
            if (handManager != null)
            {
                handManager.RestoreEnergy(energy);
            }

            GameLogger.Log($"[EternalFlame] 力量 +{str} 敏捷 +{dex} 格挡 +{block} 能量 +{energy}");
        }

        public override void Execute(EffectContext context)
        {
            Execute(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }
    }
}
