using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>
    /// ��˸���Ч������ұ���ʱ����ָ�һ������������ֵ
    /// </summary>
    [CreateAssetMenu(fileName = "PhoenixReviveEffect", menuName = "MutationChess/Relic Effects/Phoenix Revive")]
    public class PhoenixReviveEffect : CardEffect
    {
        [Tooltip("����ʱ�ָ����������ֵ������0~1��")]
        [Range(0f, 1f)]
        public float reviveHealthPercent = 0.5f;


        [System.NonSerialized]
        private bool usedThisBattle = false;

        public override string GetDescription(Card card)
        {
            int percent = Mathf.RoundToInt(reviveHealthPercent * 100f);
            return $"����ʱ����ָ� {percent}% �������ֵ��ÿ��ս��1�Σ�";
        }

        public override void Execute(CombatContext context)
        {

            usedThisBattle = false;
            GameLogger.Log("[PhoenixReviveEffect] ��˸���Ч��������");
        }

        public override void Execute(EffectContext context)
        {
            if (context == null) return;


            if (context.trigger != EffectTrigger.CalculatePlayerDamage) return;


            PlayerData playerData = context.combat?.targetPlayer ?? context.battleManager?.GetPlayerData();
            if (playerData == null)
            {
                GameLogger.LogWarning("[PhoenixReviveEffect] playerData Ϊ null");
                return;
            }


            if (usedThisBattle) return;


            if (playerData.currentHealth - context.baseValue <= 0)
            {

                context.finalValue = playerData.currentHealth - 1;


                int reviveHealth = Mathf.RoundToInt(playerData.maxHealth * reviveHealthPercent);
                playerData.currentHealth = reviveHealth;
                usedThisBattle = true;

                GameLogger.Log($"[PhoenixReviveEffect] ���֮�𱣻���Ҵӱ���״̬����ָ�{reviveHealth}������ֵ���������ֵ��{reviveHealthPercent * 100f}%��");

                if (context.battleManager != null)
                {
                    context.battleManager.AddBattleLog($"���֮�𱣻���Ҵӱ���״̬����ָ�����ֵ���������ֵ��{reviveHealthPercent * 100f}%��");
                }
            }
        }
    }
}