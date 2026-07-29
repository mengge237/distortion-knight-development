using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 霜寒回合格挡遗物效果：每回合开始时获得固定格挡。
    /// 触发时机：PlayerTurnStart（此时 EffectContext.combat 为空，需通过 battleManager 调用）。
    /// 通过 BattleManager.PlayerBlock 获得格挡（会经过 CalculateBlock 修正流程）。
    /// </summary>
    [CreateAssetMenu(fileName = "FrostTurnBlockEffect", menuName = "MutationChess/Relic Effects/Frost Turn Block")]
    public class FrostTurnBlockEffect : CardEffect
    {
        [Header("回合格挡")]
        [Tooltip("每回合开始获得的格挡值")]
        public int blockAmount = 5;

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
                GameLogger.LogError("[FrostTurnBlock] battleManager 为空！");
                return;
            }

            battleManager.PlayerBlock(blockAmount);
            GameLogger.Log($"[FrostTurnBlock] 回合开始获得 {blockAmount} 格挡");
        }
    }
}
