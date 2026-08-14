using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 金币加成遗物效果：战斗胜利时按倍率获得额外金币。
    /// 触发时机：Victory（此时 EffectContext.combat 为空，通过 battleManager 获取 PlayerData）。
    /// 简化实现：项目暂无金币掉落值修改触发器，此处按基础金币估算值乘以倍率，
    /// 在战斗胜利时直接给玩家发放额外金币。
    /// </summary>
    [CreateAssetMenu(fileName = "GoldBonusEffect", menuName = "MutationChess/Relic Effects/Gold Bonus")]
    public class GoldBonusEffect : CardEffect
    {
        [Header("金币加成")]
        [Tooltip("金币加成倍率（如 0.2 表示 +20%）")]
        [Range(0f, 2f)]
        public float goldBonusMultiplier = 0.2f;

        [Tooltip("用于估算额外金币的基础金币值（简化实现，非真实掉落数值）")]
        public int baseGoldEstimate = 25;

        public override void Execute(CombatContext context)
        {
            GrantBonusGold(context?.battleManager?.GetPlayerData());
        }

        public override void Execute(EffectContext context)
        {
            GrantBonusGold(context?.battleManager?.GetPlayerData());
        }

        private void GrantBonusGold(PlayerData playerData)
        {
            if (playerData == null)
            {
                GameLogger.LogError("[GoldBonus] playerData 为空！");
                return;
            }

            int bonus = Mathf.FloorToInt(baseGoldEstimate * goldBonusMultiplier);
            if (bonus <= 0) return;

            playerData.AddGold(bonus);
            GameLogger.Log($"[GoldBonus] 金币加成 +{bonus}（基础 {baseGoldEstimate}  {goldBonusMultiplier}）");

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null) dataManager.UpdateUI();
        }
    }
}

