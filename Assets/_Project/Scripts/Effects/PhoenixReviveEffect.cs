using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 凤凰复活效果：玩家濒死时复活并恢复一定比例的最大生命值。
    /// </summary>
    [CreateAssetMenu(fileName = "PhoenixReviveEffect", menuName = "MutationChess/Relic Effects/Phoenix Revive")]
    public class PhoenixReviveEffect : CardEffect
    {
        [Tooltip("濒死时恢复的最大生命值比例（范围0~1）")]
        [Range(0f, 1f)]
        public float reviveHealthPercent = 0.5f;


        [System.NonSerialized]
        private bool usedThisBattle = false;

        public override string GetDescription(Card card)
        {
            int percent = Mathf.RoundToInt(reviveHealthPercent * 100f);
            return $"濒死时复活并恢复 {percent}% 最大生命值（每场战斗1次）";
        }

        public override void Execute(CombatContext context)
        {

            usedThisBattle = false;
            GameLogger.Log("[PhoenixReviveEffect] 凤凰复活效果已就绪");
        }

        public override void ResetForBattle()
        {
            usedThisBattle = false;
        }

        public override void Execute(EffectContext context)
        {
            if (context == null) return;


            if (context.trigger != EffectTrigger.CalculatePlayerDamage) return;


            PlayerData playerData = context.combat?.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[PhoenixReviveEffect] playerData 为 null");
                return;
            }


            if (usedThisBattle) return;


            if (playerData.currentHealth - context.baseValue <= 0)
            {

                // 复活：本次伤害完全抵消，并恢复一定比例最大生命
                context.finalValue = 0;


                int reviveHealth = Mathf.RoundToInt(playerData.maxHealth * reviveHealthPercent);
                playerData.currentHealth = reviveHealth;
                usedThisBattle = true;

                GameLogger.Log($"[PhoenixReviveEffect] 凤凰之火护卫玩家从濒死状态复活，恢复{reviveHealth}点生命值（最大生命值的{reviveHealthPercent * 100f}%）");

                if (context.battleManager != null)
                {
                    context.battleManager.AddBattleLog($"凤凰之火护卫玩家从濒死状态复活，恢复生命值（最大生命值的{reviveHealthPercent * 100f}%）");
                }
            }
        }
    }
}
