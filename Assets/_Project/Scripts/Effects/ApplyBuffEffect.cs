using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ApplyBuff", menuName = "MutationChess/Effects/Apply Buff")]
    public class ApplyBuffEffect : CardEffect
    {
        [Header("Buff")]
        public BuffType buffType = BuffType.Strength;

        [Header("数值配置")]
        [Tooltip("(magicNumber>0)")]
        public int defaultAmount = 2;

        [Tooltip("(-1)")]
        public int defaultDuration = 3;

        public override string GetDescription(Card card)
        {
            int amount = (card != null && card.magicNumber > 0) ? card.magicNumber : defaultAmount;
            string durText = defaultDuration < 0 ? "永久" : $"{defaultDuration} 回合";
            return $"获得 {amount} 层增益（{durText}）";
        }

        public override void Execute(CombatContext context)
        {
            if (context == null) return;
            if (context.targetEnemy == null && context.targetPlayer == null) return;

            int amount = (context.sourceCard != null && context.sourceCard.magicNumber > 0)
                ? context.sourceCard.magicNumber : defaultAmount;

            var buff = new Buff { type = buffType, amount = amount, duration = defaultDuration };

            if (context.targetEnemy != null)
                context.targetEnemy.AddBuff(buff);
            else if (context.targetPlayer != null)
                context.targetPlayer.AddBuff(buff);
        }
    }
}
