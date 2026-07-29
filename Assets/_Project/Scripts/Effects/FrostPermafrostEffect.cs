using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    ///
    /// Boss
    /// </summary>
    [CreateAssetMenu(fileName = "FrostPermafrostEffect", menuName = "MutationChess/Relic Effects/Frost Permafrost")]
    public class FrostPermafrostEffect : CardEffect
    {
        [Tooltip("")]
        public int startBlock = 10;

        [Tooltip("Boss")]
        public int bossDexterity = 1;

        public override void Execute(CombatContext context)
        {
            ApplyPermafrost(context);
        }

        public override void Execute(EffectContext context)
        {
            ApplyPermafrost(context?.combat ?? new CombatContext(context?.battleManager, null, null, null));
        }

        private void ApplyPermafrost(CombatContext context)
        {
            if (context == null || context.battleManager == null) return;
            context.battleManager.PlayerBlock(startBlock);
            GameLogger.Log($"[FrostPermafrost] ??? {startBlock} ??");

            if (ConversionModifier.BossFrostHeartActive)
            {
                PlayerData playerData = context.targetPlayer ?? context.battleManager?.GetPlayerData();
                if (playerData != null)
                {
                    playerData.AddBuff(new Buff { type = BuffType.Dexterity, amount = bossDexterity, duration = -1 });
                    GameLogger.Log($"[FrostPermafrost] Boss{bossDexterity} ");
                }
            }
        }
    }
}
