using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ShadowBurst", menuName = "MutationChess/Effects/Shadow Burst")]
    public class ShadowBurstEffect : CardEffect
    {
        [Header("��Ӱ��������")]
        [Tooltip("����ת��Ϊ�˺��ı���(magicNumber>0ʱʹ�ÿ���ֵ)")]
        public int multiplier = 2;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            PlayerData player = context.targetPlayer ?? context.battleManager?.GetPlayerData();
            Enemy enemy = context.targetEnemy ?? context.battleManager?.GetCurrentEnemy();

            if (player == null)
            {
                GameLogger.LogError("ShadowBurstEffect: targetPlayer Ϊ��");
                return;
            }
            if (enemy == null)
            {
                GameLogger.LogError("ShadowBurstEffect: targetEnemy Ϊ��");
                return;
            }

            int mult = multiplier;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                mult = context.sourceCard.magicNumber;
            }

            int totalStrength = player.GetBuffAmount(BuffType.Strength);
            int damage = totalStrength * mult;

            if (damage > 0)
            {
                enemy.TakeDamage(damage);
                context.battleManager?.AddLog($"��Ӱ������������� {totalStrength} ���������� {enemy.enemyName} ��� {damage} ���˺���x{mult}��");
            }
            else
            {
                context.battleManager?.AddLog($"��Ӱ��������������ҵ�ǰ������������");
            }

            int removed = player.RemoveShadowStrengthBuffs();
            if (removed > 0)
            {
                context.battleManager?.AddLog($"�Ƴ���������� {removed} ����Ӱ����Ч��");
            }

            GameLogger.Log($"[ShadowBurst] ����{totalStrength} x ����{mult} = �˺�{damage}���Ƴ���Ӱbuff{removed}��");
        }
    }
}
