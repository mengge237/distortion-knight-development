using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 储蓄罐：战斗胜利时按当前持有金币的百分比额外获得金币，
    /// 若百分比加成低于 minBonus 则保底发放 minBonus。
    /// 触发方式：在遗物加载时按 EffectTrigger.Victory 注册监听，
    /// 胜利回调中读取玩家金币并发放奖励。
    /// 来源：储蓄罐(Generic_PiggyBank)等配置获取。
    /// </summary>
    [CreateAssetMenu(fileName = "VictoryGoldPercentEffect", menuName = "MutationChess/Relic Effects/Victory Gold Percent")]
    public class VictoryGoldPercentEffect : CardEffect
    {
        [Header("金币奖励配置")]
        [Tooltip("胜利时获得当前持有金币的百分比(0~1，如 0.1 表示 +10%)")]
        [Range(0f, 1f)]
        public float goldPercent = 0.10f;

        [Tooltip("最低奖励保底（避免金币为0时完全不发）")]
        [Min(0)]
        public int minBonus = 5;

        public override void Execute(CombatContext context)
        {
            GrantGold(context);
        }

        public override void Execute(EffectContext context)
        {
            GrantGold(context?.combat);
        }

        private void GrantGold(CombatContext combat)
        {
            PlayerData player = combat?.targetPlayer;
            if (player == null)
            {
                var dm = PlayerDataManager.Instance;
                if (dm != null) player = dm.GetPlayerData();
            }
            if (player == null)
            {
                GameLogger.LogWarning("[VictoryGoldPercentEffect] PlayerData 为空，无法发放金币奖励");
                return;
            }

            int bonus = Mathf.Max(minBonus, Mathf.FloorToInt(player.gold * goldPercent));
            player.AddGold(bonus);
            GameLogger.Log(
                $"[VictoryGoldPercentEffect] 战斗胜利额外获得金币 +{bonus}" +
                $"（持有金币={player.gold}，百分比={(int)(goldPercent * 100)}%，保底={minBonus}）");
        }
    }
}
