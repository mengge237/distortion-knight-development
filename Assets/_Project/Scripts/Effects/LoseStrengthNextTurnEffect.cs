using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "LoseStrengthNextTurnEffect", menuName = "MutationChess/Potion Effects/Lose Strength Next Turn")]
    public class LoseStrengthNextTurnEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int strengthLoss = 3;

        [Tooltip("2=+")]
        public int duration = 2;

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

            GameLogger.Log($"[LoseStrength] ���ʧȥ{strengthLoss}������������{duration}�غ�");

            if (battleManager != null)
                battleManager.AddBattleLog($"���ʧȥ{strengthLoss}������������{duration}�غ�");
        }
    }
}


