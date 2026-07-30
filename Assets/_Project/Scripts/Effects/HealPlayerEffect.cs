using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "HealPlayer", menuName = "MutationChess/Effects/Heal Player")]
    public class HealPlayerEffect : CardEffect
    {
        [Header("�ָ�����")]
        [Tooltip("Ĭ�ϻָ�����������magicNumber>0ʱʹ�ÿ���ֵ")]
        public int healAmount = 5;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            int amount = (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                ? context.sourceCard.magicNumber : healAmount;

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
            {
                int actualHeal = dataManager.Heal(amount);
                context.battleManager?.AddLog($"��һָ� {actualHeal} ������");
            }
        }
    }
}
