using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BlockToAttack", menuName = "MutationChess/Effects/Block To Attack")]
    public class BlockToAttackEffect : CardEffect
    {
        [Header("��ת������")]
        [Tooltip("��ת������(magicNumber>0ʱʹ�ÿ���ֵ)")]
        public int multiplier = 2;

        public override void Execute(CombatContext context)
        {
            if (context == null) return;

            if (context.battleManager == null)
            {
                GameLogger.LogError("BlockToAttackEffect: battleManager Ϊ��");
                return;
            }

            if (context.targetEnemy == null)
            {
                GameLogger.LogError("BlockToAttackEffect: targetEnemy Ϊ��");
                return;
            }

            int mult = multiplier;
            if (context.sourceCard != null && context.sourceCard.magicNumber > 0)
            {
                mult = context.sourceCard.magicNumber;
            }

            int currentBlock = context.battleManager.GetPlayerBlock();
            int damage = currentBlock * mult;

            if (currentBlock > 0)
            {
                context.battleManager.ConsumePlayerBlock(currentBlock);
            }

            if (damage > 0)
            {
                context.targetEnemy.TakeDamage(damage);
                context.battleManager?.AddLog($"������� {currentBlock} �񵲣��� {context.targetEnemy.enemyName} ��� {damage} ���˺���x{mult}��");
            }
            else
            {
                context.battleManager?.AddLog($"��ҵ�ǰ�޸񵲿�����ת��Ϊ����");
            }

            GameLogger.Log($"[BlockToAttack] ��{currentBlock} x ����{mult} = �˺�{damage}");
        }
    }
}
