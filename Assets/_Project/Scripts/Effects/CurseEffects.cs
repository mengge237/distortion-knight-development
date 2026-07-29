using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>


    /// </summary>
    public abstract class CurseEffect : CardEffect
    {
        public override void Execute(CombatContext context)
        {

        }
    }

    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "CurseDecayEffect", menuName = "MutationChess/Curse Effects/Decay")]
    public class CurseDecayEffect : CurseEffect
    {
        [Tooltip("")]
        public int hpLossPerTurn = 1;

        public override void Execute(CombatContext context)
        {

        }
    }

    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "CurseFogEffect", menuName = "MutationChess/Curse Effects/Fog")]
    public class CurseFogEffect : CurseEffect
    {
        [Tooltip("")]
        public int handSizeReduction = 1;

        public override void Execute(CombatContext context)
        {

        }
    }

    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "CurseChainsEffect", menuName = "MutationChess/Curse Effects/Chains")]
    public class CurseChainsEffect : CurseEffect
    {
        [Tooltip("")]
        public int drawReduction = 1;

        public override void Execute(CombatContext context)
        {

        }
    }

    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "CurseDevourEffect", menuName = "MutationChess/Curse Effects/Devour")]
    public class CurseDevourEffect : CurseEffect
    {
        [Tooltip("")]
        public int hpLossPerCard = 1;

        public override void Execute(CombatContext context)
        {

        }
    }

    /// <summary>

    /// </summary>
    [CreateAssetMenu(fileName = "CurseVoidEffect", menuName = "MutationChess/Curse Effects/Void")]
    public class CurseVoidEffect : CurseEffect
    {
        public override void Execute(CombatContext context)
        {

        }
    }
}


