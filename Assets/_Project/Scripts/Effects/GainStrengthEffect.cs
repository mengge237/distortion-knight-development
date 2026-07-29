using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "GainStrengthEffect", menuName = "MutationChess/Relic Effects/Gain Strength")]
    public class GainStrengthEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int strengthAmount = 2;

        public override void Execute(CombatContext context)
        {

        }
    }
}