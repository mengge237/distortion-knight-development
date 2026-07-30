using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    ///
    ///
    ///
    /// </summary>
    public enum CardTag
    {
        None = 0,
        Slime = 1,       //
        Reluctant = 2,   //
        Blood = 3,       //
        Frost = 4,       //
        Corrupt = 5,     //
        Shadow = 6,      //
        Curse = 7,       //
    }

    /// <summary>
    ///
    ///
    ///
    ///
    /// </summary>
    public abstract class InherentEffect : CardEffect
    {
        /// <summary>
        ///
        /// </summary>
        public abstract CardTag Tag { get; }

        /// <summary>
        ///
        /// </summary>
        public abstract void ApplyInherent(CombatContext context);

        public override void Execute(CombatContext context)
        {
            ApplyInherent(context);
        }

        /// <summary>
        ///
        /// </summary>
        public bool ShouldApply(Card card)
        {
            if (card == null) return false;
            return card.HasTag(Tag);
        }
    }

}
