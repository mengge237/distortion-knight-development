using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "ShadowCardBonusDamageEffect", menuName = "MutationChess/Relic Effects/Shadow Card Bonus Dmg")]
    public class ShadowCardBonusDamageEffect : CardEffect
    {
        [Tooltip("暗影系卡牌额外造成的伤害")]
        public int bonus = 3;

        [Tooltip("Boss遗物激活时额外增加的伤害（默认 0）")]
        public int bossBonus = 0;

        public override void Execute(CombatContext context) { }

        public override void Execute(EffectContext context)
        {
            if (context == null || context.trigger != EffectTrigger.CalculateAttackDamage) return;

            Card sourceCard = context.tag as Card;
            if (sourceCard == null) return;
            if (!sourceCard.HasTag(CardTag.Shadow) && sourceCard.faction != CardFaction.Shadow) return;

            int totalBonus = ConversionModifier.BossMemoryLensActive ? bonus + bossBonus : bonus;
            context.finalValue = context.baseValue + totalBonus;
            GameLogger.Log($"[ShadowCardBonusDmg] {sourceCard.cardName} +{totalBonus}: {context.baseValue} -> {context.finalValue}");
        }
    }
}
