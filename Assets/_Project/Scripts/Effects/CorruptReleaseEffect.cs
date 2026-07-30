using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 腐化释放效果：下一回合所有卡牌不会被消耗
    /// </summary>
    [CreateAssetMenu(fileName = "CorruptReleaseEffect", menuName = "MutationChess/Potion Effects/Corrupt Release")]
    public class CorruptReleaseEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            ConversionModifier.AllCardsNoExhaustThisTurn = true;
            GameLogger.Log("[CorruptRelease] 下一回合所有腐化系卡牌释放，本回合中所有卡牌不会被消耗");

            if (context?.battleManager != null)
                context.battleManager.AddBattleLog("下一回合所有腐化系卡牌释放，本回合中所有卡牌不会被消耗");
        }

        public override void Execute(EffectContext context)
        {
            ConversionModifier.AllCardsNoExhaustThisTurn = true;
            GameLogger.Log("[CorruptRelease] 下一回合所有腐化系卡牌释放，本回合中所有卡牌不会被消耗");
        }
    }
}