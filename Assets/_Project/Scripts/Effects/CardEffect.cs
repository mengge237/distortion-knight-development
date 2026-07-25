using UnityEngine;

namespace MutationChess.Core
{
    public abstract class CardEffect : ScriptableObject
    {
        [TextArea(2, 4)]
        public string effectDescription;

        public abstract void Execute(CombatContext context);
    }
}