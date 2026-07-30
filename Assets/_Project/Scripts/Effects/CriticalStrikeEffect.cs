using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "CriticalStrikeEffect", menuName = "MutationChess/Relic Effects/Critical Strike")]
    public class CriticalStrikeEffect : CardEffect
    {
        [Header("��������")]
        [Tooltip("�����������ʣ���Χ0~1��")]
        [Range(0f, 1f)]
        public float criticalChance = 0.15f;

        [Tooltip("�����˺�����")]
        public float damageMultiplier = 2f;

        public override string GetDescription(Card card)
        {
            int chance = Mathf.RoundToInt(criticalChance * 100f);
            return $"{chance}% ���ʱ������˺���{damageMultiplier}";
        }

        public override void Execute(CombatContext context)
        {
            // ��Ϊ����Ч������Ҫͨ�� CalculateAttackDamage ֵ�޸�������
            // ����������ж��Ƿ�������
        }

        public override void Execute(EffectContext context)
        {
            if (context == null) return;
            if (context.trigger != EffectTrigger.CalculateAttackDamage) return;

            if (Random.value < criticalChance)
            {
                int critDamage = Mathf.RoundToInt(context.baseValue * damageMultiplier);
                context.finalValue = critDamage;
                context.combat?.battleManager?.AddLog($"�������� {context.baseValue} -> {critDamage}��x{damageMultiplier}����");
                GameLogger.Log($"[CriticalStrike] �������� {context.baseValue} -> {critDamage}");
            }
        }
    }
}