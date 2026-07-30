using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 精英胜利额外卡牌组效果
    /// 此效果不通过 Execute 触发，而是由 EffectManager 在 Victory 时触发
    /// </summary>
    [CreateAssetMenu(fileName = "EliteVictoryExtraCardGroupEffect", menuName = "MutationChess/Relic Effects/Elite Extra Cards")]
    public class EliteVictoryExtraCardGroupEffect : CardEffect
    {
        [Tooltip("精英胜利后额外增加的卡牌组数")]
        public int extraGroup = 1;

        public override void Execute(CombatContext context)
        {
            GameLogger.Log($"[EliteVictoryExtraCardGroup] 额外 {extraGroup} 组，BattleManager 为空");
        }

        public override void Execute(EffectContext context)
        {
            GameLogger.Log($"[EliteVictoryExtraCardGroup] (trigger={context?.trigger}) 额外 {extraGroup} 组卡牌");

            // 实际逻辑由 EffectManager 在胜利时处理
            // 此处仅记录战斗日志
            if (context?.battleManager != null)
                context.battleManager.AddBattleLog($"精英胜利后额外增加 {extraGroup} 组卡牌选项");
        }
    }
}
