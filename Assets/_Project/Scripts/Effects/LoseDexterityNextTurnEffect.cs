using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "LoseDexterityNextTurnEffect", menuName = "MutationChess/Potion Effects/Lose Dexterity Next Turn")]
    public class LoseDexterityNextTurnEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int dexterityLoss = 3;

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
                type = BuffType.Dexterity,
                amount = -dexterityLoss,
                duration = duration
            });

            GameLogger.Log($"[LoseDexterity] ���ʧȥ{dexterityLoss}����ݣ�����{duration}�غ�");

            if (battleManager != null)
                battleManager.AddBattleLog($"���ʧȥ{dexterityLoss}����ݣ�����{duration}�غ�");
        }
    }
}


