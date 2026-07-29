using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 不舍格挡遗物效果：打出不舍标签卡牌时，获得格挡。
    /// 触发时机：CardPlayed（context.combat.sourceCard 为打出的卡牌）。
    /// 仅当 sourceCard 拥有 Reluctant 标签时触发，通过 BattleManager.PlayerBlock 获得格挡。
    /// </summary>
    [CreateAssetMenu(fileName = "ReluctantBlockBonusEffect", menuName = "MutationChess/Relic Effects/Reluctant Block Bonus")]
    public class ReluctantBlockBonusEffect : CardEffect
    {
        [Header("不舍格挡")]
        [Tooltip("打出不舍卡时获得的格挡值")]
        public int blockAmount = 1;

        public override void Execute(CombatContext context)
        {
            GrantReluctantBlock(context);
        }

        public override void Execute(EffectContext context)
        {
            GrantReluctantBlock(context?.combat);
        }

        private void GrantReluctantBlock(CombatContext context)
        {
            if (context == null || context.battleManager == null) return;

            Card playedCard = context.sourceCard;
            if (playedCard == null || !playedCard.HasTag(CardTag.Reluctant)) return;

            context.battleManager.PlayerBlock(blockAmount);
            GameLogger.Log($"[ReluctantBlock] 不舍卡 {playedCard.cardName} 获得 {blockAmount} 格挡");
        }
    }
}
