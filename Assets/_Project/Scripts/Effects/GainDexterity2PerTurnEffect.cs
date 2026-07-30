using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    [CreateAssetMenu(fileName = "GainDexterity2PerTurnEffect", menuName = "MutationChess/Relic Effects/Gain Dexterity 2 Per Turn")]
    public class GainDexterity2PerTurnEffect : CardEffect
    {
        [Tooltip("每回合获得的敏捷值")]
        public int dexterityAmount = 2;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;


            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[GainDexterity2PerTurnEffect] playerData 为空");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = dexterityAmount, duration = -1 });
            GameLogger.Log($"[GainDexterity2PerTurnEffect] 获得敏捷 {dexterityAmount} 点");
        }
    }
}


