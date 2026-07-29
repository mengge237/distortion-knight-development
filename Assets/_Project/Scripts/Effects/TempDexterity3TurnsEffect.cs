using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "TempDexterity3TurnsEffect", menuName = "MutationChess/Relic Effects/Temp Dexterity 3 Turns")]
    public class TempDexterity3TurnsEffect : CardEffect
    {
        [Tooltip("")]
        public int dex = 1;

        [Tooltip("")]
        public int turns = 3;

        public override void Execute(CombatContext context)
        {
            ApplyTempDex(context?.targetPlayer ?? context?.battleManager?.GetPlayerData());
        }

        public override void Execute(EffectContext context)
        {
            ApplyTempDex(context?.battleManager?.GetPlayerData());
        }

        private void ApplyTempDex(PlayerData playerData)
        {
            if (playerData == null) return;

            playerData.AddBuff(new Buff
            {
                type = BuffType.Dexterity,
                amount = dex,
                duration = turns
            });

            GameLogger.Log($"[TempDex3Turns] {dex} {turns} ");
        }
    }
}
