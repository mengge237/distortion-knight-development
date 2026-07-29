using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// 幻影闪避遗物效果：回合开始时按概率闪避本回合所有伤害。
    /// 触发时机：PlayerTurnStart（此时 EffectContext.combat 为空，需通过 battleManager 获取 PlayerData）。
    /// 简化实现：判定成功时给玩家添加一个高额 Shield 标记 buff（duration=1）代表本回合闪避。
    /// 注意：项目当前伤害流程未直接识别该标记，需后续在伤害结算处检查 Shield 标记或改用全局标记。
    /// </summary>
    [CreateAssetMenu(fileName = "PhantomDodgeEffect", menuName = "MutationChess/Relic Effects/Phantom Dodge")]
    public class PhantomDodgeEffect : CardEffect
    {
        [Header("幻影闪避")]
        [Tooltip("回合开始时闪避所有伤害的概率")]
        [Range(0f, 1f)]
        public float dodgeChance = 0.25f;

        [Tooltip("闪避成功时添加的标记 Shield 数值（仅作闪避标记）")]
        public int dodgeMarkAmount = 9999;

        public override void Execute(CombatContext context)
        {
            TryDodge(context?.targetPlayer ?? context?.battleManager?.GetPlayerData());
        }

        public override void Execute(EffectContext context)
        {
            TryDodge(context?.battleManager?.GetPlayerData());
        }

        private void TryDodge(PlayerData playerData)
        {
            if (playerData == null)
            {
                GameLogger.LogError("[PhantomDodge] playerData 为空！");
                return;
            }

            if (Random.value < dodgeChance)
            {
                playerData.AddBuff(new Buff
                {
                    type = BuffType.Shield,
                    amount = dodgeMarkAmount,
                    duration = 1
                });
                GameLogger.Log($"[PhantomDodge] 闪避触发！本回合标记闪避（Shield={dodgeMarkAmount}）");
            }
        }
    }
}
