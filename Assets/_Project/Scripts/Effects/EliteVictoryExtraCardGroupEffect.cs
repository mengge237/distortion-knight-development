using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 
    ///
    /// ?? Execute ?? EffectManager Victory 
    /// </summary>
    [CreateAssetMenu(fileName = "EliteVictoryExtraCardGroupEffect", menuName = "MutationChess/Relic Effects/Elite Extra Cards")]
    public class EliteVictoryExtraCardGroupEffect : CardEffect
    {
        [Tooltip("")]
        public int extraGroup = 1;

        public override void Execute(CombatContext context)
        {
            GameLogger.Log($"[EliteVictoryExtraCardGroup] {extraGroup} BattleManager ");
        }

        public override void Execute(EffectContext context)
        {
            GameLogger.Log($"[EliteVictoryExtraCardGroup] (trigger={context?.trigger}){extraGroup} ");

            //
            //
            if (context?.battleManager != null)
                context.battleManager.AddBattleLog($"x{extraGroup}");
        }
    }
}
