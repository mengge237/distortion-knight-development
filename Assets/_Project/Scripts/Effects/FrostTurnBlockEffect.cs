using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 冰霜回合格挡效果
    /// 通过 BattleManager.PlayerBlock 计算并赋予格挡
    /// </summary>
    [CreateAssetMenu(fileName = "FrostTurnBlockEffect", menuName = "MutationChess/Relic Effects/Frost Turn Block")]
    public class FrostTurnBlockEffect : CardEffect
    {
        [Header("格挡配置")]
        [Tooltip("普通敌人给予的格挡值")]
        public int blockAmount = 15;

        [Tooltip("Boss 战时给予的格挡值")]
        public int bossBlockAmount = 25;

        public override void Execute(CombatContext context)
        {
            GrantBlock(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            GrantBlock(context?.battleManager);
        }

        private void GrantBlock(BattleManager battleManager)
        {
            if (battleManager == null)
            {
                GameLogger.LogError("[FrostTurnBlock] battleManager 为空");
                return;
            }

            int finalBlock = ConversionModifier.BossFrostHeartActive ? bossBlockAmount : blockAmount;
            battleManager.PlayerBlock(finalBlock);
            GameLogger.Log($"[FrostTurnBlock] 赋予格挡 {finalBlock} 点" +
                (ConversionModifier.BossFrostHeartActive ? " (Boss)" : ""));
        }
    }
}
