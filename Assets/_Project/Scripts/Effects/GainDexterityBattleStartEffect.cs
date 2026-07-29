using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "GainDexterityBattleStartEffect", menuName = "MutationChess/Relic Effects/Gain Dexterity Battle Start")]
    public class GainDexterityBattleStartEffect : CardEffect
    {
        [Tooltip("")]
        public int dexterityAmount = 1;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;


            PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[GainDexterityBattleStartEffect] playerData ");
                return;
            }


            playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = dexterityAmount, duration = -1 });
            GameLogger.Log($"[GainDexterityBattleStartEffect]  {dexterityAmount} ");
        }
    }
}


