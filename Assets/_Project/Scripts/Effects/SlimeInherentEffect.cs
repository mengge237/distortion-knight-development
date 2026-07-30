using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 粘液固有效果：打出粘液系卡牌时触发相邻粘液卡牌的效果
    /// </summary>
    [CreateAssetMenu(fileName = "SlimeInherentEffect", menuName = "MutationChess/Inherent/Slime")]
    public class SlimeInherentEffect : InherentEffect
    {
        public override CardTag Tag => CardTag.Slime;

        public override void ApplyInherent(CombatContext context)
        {
            if (context?.sourceCard == null) return;

            var handManager = UI.HandManager.Instance;
            if (handManager == null) return;

            var handCards = handManager.GetHandCards();
            int playedIndex = handCards.IndexOf(context.sourceCard);
            if (playedIndex < 0) return;

            int range = SlimeExpandEffect.SlimeTriggerRange > 0
                ? SlimeExpandEffect.SlimeTriggerRange : 1;

            for (int offset = -range; offset <= range; offset++)
            {
                if (offset == 0) continue;
                int idx = playedIndex + offset;
                if (idx >= 0 && idx < handCards.Count)
                {
                    Card adj = handCards[idx];
                    if (adj != null && adj.HasTag(CardTag.Slime))
                    {
                        GameLogger.Log($"[史莱姆联动] 触发相邻: {adj.cardName} ({offset})");
                        CombatContext adjCtx = new CombatContext(
                            context.battleManager,
                            context.targetEnemy,
                            context.targetPlayer,
                            adj
                        );
                        adj.ExecuteEffects(adjCtx);
                    }
                }
            }
        }
    }
}