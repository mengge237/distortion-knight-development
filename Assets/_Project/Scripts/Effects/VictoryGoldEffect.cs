using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 胜利金币效果：战斗胜利时按配置发放金币（固定值 / 基础估算×倍率 / 持有金币百分比+保底，可组合叠加）。
    /// 由效果合并从 Gain12GoldOnVictoryEffect / GoldBonusEffect / VictoryGoldPercentEffect 合并而来
    /// （三者逻辑同构：胜利时发金币，仅计算公式不同，统一为一个类按非零字段组合发放）。
    /// </summary>
    [CreateAssetMenu(fileName = "VictoryGoldEffect", menuName = "MutationChess/Relic Effects/Victory Gold")]
    public class VictoryGoldEffect : CardEffect
    {
        [Header("固定金币")]
        [Tooltip("固定发放的金币数（0=不发放）")]
        public int fixedGold = 0;

        [Header("倍率金币")]
        [Tooltip("金币加成倍率（如 0.2 表示 +20%，0=不启用）")]
        [Range(0f, 2f)]
        public float goldBonusMultiplier = 0f;

        [Tooltip("用于估算额外金币的基础金币值")]
        public int baseGoldEstimate = 25;

        [Header("百分比金币")]
        [Tooltip("按当前持有金币的百分比额外发放（0=不启用）")]
        [Range(0f, 1f)]
        public float goldPercent = 0f;

        [Tooltip("百分比加成低于该值时按保底发放")]
        [Min(0)]
        public int minBonus = 0;

        public override void Execute(CombatContext context)
        {
            GrantGold(context?.battleManager?.GetPlayerData());
        }

        public override void Execute(EffectContext context)
        {
            GrantGold(context?.battleManager?.GetPlayerData());
        }

        private void GrantGold(PlayerData playerData)
        {
            if (playerData == null)
            {
                var dataManager = PlayerDataManager.Instance;
                if (dataManager != null) playerData = dataManager.GetPlayerData();
            }
            if (playerData == null)
            {
                GameLogger.LogWarning("[VictoryGoldEffect] PlayerData 为空，无法发放金币奖励");
                return;
            }

            int bonus = fixedGold;
            if (goldBonusMultiplier > 0f)
                bonus += Mathf.FloorToInt(baseGoldEstimate * goldBonusMultiplier);
            if (goldPercent > 0f)
                bonus += Mathf.Max(minBonus, Mathf.FloorToInt(playerData.gold * goldPercent));

            if (bonus <= 0) return;

            playerData.AddGold(bonus);
            GameLogger.Log(
                $"[VictoryGoldEffect] 战斗胜利额外获得金币 +{bonus}" +
                $"（固定={fixedGold}，倍率={goldBonusMultiplier}×{baseGoldEstimate}，" +
                $"百分比={(int)(goldPercent * 100)}%保底{minBonus}）");

            var dm = PlayerDataManager.Instance;
            if (dm != null) dm.UpdateUI();
        }
    }
}
