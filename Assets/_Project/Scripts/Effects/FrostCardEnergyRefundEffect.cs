using UnityEngine;
using MutationChess.UI;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 寒霜之心：玩家每次打出带有 Frost 标签的卡牌后，返还指定数量的能量。
    /// 触发方式：在遗物加载时按 EffectTrigger.CardPlayed 注册事件监听，
    /// 在监听回调里检查 combat.sourceCard 是否拥有 Frost 标签，满足则返还能量。
    /// 来源：Boss_寒霜之心(Boss_FrostHeart)等配置获取。
    /// </summary>
    [CreateAssetMenu(fileName = "FrostCardEnergyRefundEffect", menuName = "MutationChess/Relic Effects/Frost Card Energy Refund")]
    public class FrostCardEnergyRefundEffect : CardEffect
    {
        [Header("能量返还")]
        [Tooltip("每张打出的寒霜卡返还的能量数")]
        [Min(1)]
        public int refundAmount = 1;

        public override void Execute(CombatContext context)
        {
            TryRefundEnergy(context);
        }

        public override void Execute(EffectContext context)
        {
            TryRefundEnergy(context?.combat);
        }

        private void TryRefundEnergy(CombatContext combat)
        {
            if (combat == null) return;
            Card src = combat.sourceCard;
            if (src == null || !src.HasTag(CardTag.Frost)) return;

            var hm = HandManager.Instance;
            if (hm == null)
            {
                GameLogger.LogWarning("[FrostCardEnergyRefund] HandManager 为空，无法返还能量");
                return;
            }

            hm.RestoreEnergy(refundAmount);
            GameLogger.Log(
                $"[FrostCardEnergyRefund] 寒霜卡 {src.cardName} 打出后返还 {refundAmount} 能量");
        }
    }
}
