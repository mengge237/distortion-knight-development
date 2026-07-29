using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "DrawingPadDraw2Effect", menuName = "MutationChess/Relic Effects/Drawing Pad Draw 2")]
    public class DrawingPadDraw2Effect : CardEffect
    {
        [Tooltip("")]
        public int draw = 2;

        public override void Execute(CombatContext context)
        {
            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.DrawCards(draw);
                GameLogger.Log($"[DrawingPad] {draw} ");
            }
        }
    }
}
