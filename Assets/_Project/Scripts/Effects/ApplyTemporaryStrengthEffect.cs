using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyTemporaryStrength", menuName = "MutationChess/Effects/Apply Temporary Strength")]
    public class ApplyTemporaryStrengthEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {
            if (context.targetPlayer != null && context.sourceCard != null)
            {
                int amount = 2; // 固定获得 2 层力量
                int duration = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : 3;
                // 需要对玩家添加 Buff 系统（后续扩展）
            }
            else if (context.targetEnemy != null)
            {
                int amount = 2;
                int duration = context.sourceCard.magicNumber > 0 ? context.sourceCard.magicNumber : 3;
                context.targetEnemy.AddBuff(new Buff { type = BuffType.Strength, amount = amount, duration = duration });
            }
        }
    }
}