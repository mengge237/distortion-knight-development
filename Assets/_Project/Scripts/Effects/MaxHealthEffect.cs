using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "MaxHealthEffect", menuName = "MutationChess/Relic Effects/Max Health")]
    public class MaxHealthEffect : CardEffect
    {
        [Header("")]
        [Tooltip("")]
        public int maxHealthGain = 1;

        public override void Execute(CombatContext context)
        {

        }
    }
}