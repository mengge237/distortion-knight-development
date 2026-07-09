using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyDexterity", menuName = "MutationChess/Effects/Apply Dexterity")]
    public class ApplyDexterityEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context.targetPlayer != null && context.sourceCard != null)
            {
                int amount = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : 3;
                // 需要对玩家添加 Buff 系统（后续扩展）
            }
            else if (context.targetEnemy != null)
            {
                int amount = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : 3;
                context.targetEnemy.AddBuff(new Buff { type = BuffType.Dexterity, amount = amount, duration = 999 });
            }
        }
    }
}