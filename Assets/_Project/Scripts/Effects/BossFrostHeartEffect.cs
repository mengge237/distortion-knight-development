using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BossFrostHeartEffect", menuName = "MutationChess/Relic Effects/Boss/Frost Heart")]
    public class BossFrostHeartEffect : CardEffect
    {
        [Tooltip("")]
        public int dexterity = 1;

        [Tooltip("")]
        public int frostBonusBlock = 1;

        public override void Execute(CombatContext context)
        {
            ApplyFrostHeart(context?.targetPlayer ?? context?.battleManager?.GetPlayerData());
        }

        public override void Execute(EffectContext context)
        {
            ApplyFrostHeart(context?.battleManager?.GetPlayerData());
        }

        private void ApplyFrostHeart(PlayerData playerData)
        {
            if (playerData == null) return;

            playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = dexterity, duration = -1 });
            ConversionModifier.BossFrostHeartActive = true;
            GameLogger.Log($"[BossFrostHeart] {dexterity}+{frostBonusBlock}??");
        }
    }
}
