using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 下回合失去敏捷效果
    /// </summary>
    [CreateAssetMenu(fileName = "LoseDexterityNextTurnEffect", menuName = "MutationChess/Potion Effects/Lose Dexterity Next Turn")]
    public class LoseDexterityNextTurnEffect : CardEffect
    {
        [Header("敏捷损失配置")]
        [Tooltip("损失的敏捷值")]
        public int dexterityLoss = 3;

        [Tooltip("2=+")]
        public int duration = 2;

        public override string GetDescription(Card card)
        {
            return $"{duration} 回合失去 {dexterityLoss} 敏捷";
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
                type = BuffType.Dexterity,
                amount = -dexterityLoss,
                duration = duration
            });

            GameLogger.Log($"[LoseDexterity] 下回合失去{dexterityLoss}点敏捷，持续{duration}回合");

            if (battleManager != null)
                battleManager.AddBattleLog($"下回合失去{dexterityLoss}点敏捷，持续{duration}回合");
        }
    }
}
