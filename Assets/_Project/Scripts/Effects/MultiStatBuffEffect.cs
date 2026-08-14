using UnityEngine;
using MutationChess.Battle;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 多属性增益效果：一次性获得力量/敏捷/格挡/能量（只应用非零项）。
    /// 由 EffectMergeMigration 工具从以下 3 个同构效果类合并而来：
    /// AllStatsBuffEffect / EternalFlameBattleStartEffect / BlockAndStrengthEffect
    /// （触发时机由遗物配置/卡牌决定，与效果类本身无关）
    /// </summary>
    [CreateAssetMenu(fileName = "MultiStatBuffEffect", menuName = "MutationChess/Effects/Multi Stat Buff")]
    public class MultiStatBuffEffect : CardEffect
    {
        [Header("多属性增益")]
        [Tooltip("获得的力量层数（0=不获得）")]
        public int strength = 0;

        [Tooltip("获得的敏捷层数（0=不获得）")]
        public int dexterity = 0;

        [Tooltip("获得的格挡值（0=不获得）")]
        public int block = 0;

        [Tooltip("回复的能量值（0=不回复）")]
        public int energy = 0;

        public override string GetDescription(Card card)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (strength > 0) parts.Add($"{strength} 力量");
            if (dexterity > 0) parts.Add($"{dexterity} 敏捷");
            if (block > 0) parts.Add($"{block} 格挡");
            if (energy > 0) parts.Add($"{energy} 能量");
            return parts.Count > 0 ? $"获得 {string.Join("、", parts)}" : (effectDescription ?? "");
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;
            var bm = context.battleManager;

            PlayerData playerData = context.targetPlayer ?? bm?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[MultiStatBuff] playerData 为空");
                return;
            }

            if (strength != 0)
                playerData.AddBuff(new Buff { type = BuffType.Strength, amount = strength, duration = -1 });
            if (dexterity != 0)
                playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = dexterity, duration = -1 });
            if (block > 0 && bm != null)
                bm.PlayerBlock(block);
            if (energy > 0)
            {
                var handManager = HandManager.Instance;
                if (handManager != null)
                    handManager.RestoreEnergy(energy);
            }

            GameLogger.Log($"[MultiStatBuff] 力量{strength:+0;-0} 敏捷{dexterity:+0;-0} 格挡{block:+0;-0} 能量{energy:+0;-0}");
        }
    }
}
