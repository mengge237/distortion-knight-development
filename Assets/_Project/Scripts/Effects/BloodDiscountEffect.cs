using UnityEngine;
using MutationChess.Core;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>




    /// </summary>
    [CreateAssetMenu(fileName = "BloodDiscountEffect", menuName = "MutationChess/Potion Effects/Blood Discount")]
    public class BloodDiscountEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int overrideRate = 1;

        public override void Execute(CombatContext context)
        {
            ConversionModifier.TemporaryBloodRateOverride = overrideRate;
            GameLogger.Log($"[BloodDiscount] ��һ��Ѫ֮ת���ӳɣ�����{overrideRate + 1}:1��");

            if (context?.battleManager != null)
                context.battleManager.AddBattleLog($"��һ��Ѫ֮ת���ӳɣ�����{overrideRate + 1}:1��");
        }

        public override void Execute(EffectContext context)
        {
            ConversionModifier.TemporaryBloodRateOverride = overrideRate;
            GameLogger.Log($"[BloodDiscount] ��һ��Ѫ֮ת���ӳɣ�����{overrideRate + 1}:1��");
        }
    }
}


