using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 下回合失去力量效果
    /// </summary>
    [CreateAssetMenu(fileName = "LoseStrengthNextTurnEffect", menuName = "MutationChess/Potion Effects/Lose Strength Next Turn")]
    public class LoseStrengthNextTurnEffect : CardEffect
    {
        [Header("力量损失配置")]
        [Tooltip("损失的力量值")]
        public int strengthLoss = 3;

        [Tooltip("2=+")]
        public int duration = 2;

        public override string GetDescription(Card card)
        {
            return $"{duration} 回合失去 {strengthLoss} 力量";
        }

        public override void Execute(CombatContext context)
        {
            ApplyLoss(context?.battleManager);
        }

        public override void Execute(EffectContext context)
        {
            ApplyLoss(context?.battleManager);
        }

        private void ApplyLoss(BattleManager battleManager)
        {
            var dataManager = PlayerDataManager.Instance;
            if (dataManager == null) return;

            PlayerData playerData = dataManager.GetPlayerData();
            if (playerData == null) return;

            playerData.AddBuff(new Buff
            {
                type = BuffType.Strength,
                amount = -strengthLoss,
                duration = duration
            });

            GameLogger.Log($"[LoseStrength] 下回合失去{strengthLoss}点力量，持续{duration}回合");

            if (battleManager != null)
                battleManager.AddBattleLog($"下回合失去{strengthLoss}点力量，持续{duration}回合");
        }
    }
}
