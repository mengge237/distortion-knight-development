using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    /// <summary>Boss 遗物激活标记（供效果类做 Boss 加成判定）</summary>
    public enum BossFlagType
    {
        None = 0,
        FrostHeart = 1,
        ReluctantChain = 2,
        SlimeGland = 3,
    }

    /// <summary>
    /// 标签卡牌格挡加成效果：打出指定标签（或同阵营）的卡牌时额外获得格挡，Boss 遗物激活时翻倍。
    /// 由效果合并从 FrostBonusBlockEffect / ReluctantBlockBonusEffect 合并而来
    /// （仅过滤标签与 Boss 标记不同，逻辑完全一致）。
    /// </summary>
    [CreateAssetMenu(fileName = "TagBlockBonusEffect", menuName = "MutationChess/Relic Effects/Tag Block Bonus")]
    public class TagBlockBonusEffect : CardEffect
    {
        [Header("格挡配置")]
        [Tooltip("触发加成的卡牌标签（按标签或同阵营判断）")]
        public CardTag filterTag = CardTag.Frost;

        [Tooltip("打出对应标签卡牌时给予的格挡值")]
        public int blockAmount = 8;

        [Tooltip("对应 Boss 遗物激活时格挡翻倍")]
        public BossFlagType bossFlag = BossFlagType.None;

        public override void Execute(CombatContext context)
        {
            GrantBonusBlock(context);
        }

        public override void Execute(EffectContext context)
        {
            GrantBonusBlock(context?.combat);
        }

        private bool BossFlagActive
        {
            get
            {
                switch (bossFlag)
                {
                    case BossFlagType.FrostHeart: return ConversionModifier.BossFrostHeartActive;
                    case BossFlagType.ReluctantChain: return ConversionModifier.BossReluctantChainActive;
                    default: return false;
                }
            }
        }

        private void GrantBonusBlock(CombatContext context)
        {
            if (context == null || context.battleManager == null) return;

            Card playedCard = context.sourceCard;
            if (playedCard == null) return;

            if (!playedCard.HasTag(filterTag) && playedCard.faction != FactionForTag(filterTag)) return;

            int effectiveBlock = BossFlagActive ? blockAmount * 2 : blockAmount;

            context.battleManager.PlayerBlock(effectiveBlock);
            GameLogger.Log($"[TagBlockBonus] {playedCard.cardName} +{effectiveBlock} 格挡{(BossFlagActive ? " (Boss加倍)" : "")}");
        }

        private static CardFaction FactionForTag(CardTag tag)
        {
            switch (tag)
            {
                case CardTag.Slime: return CardFaction.Slime;
                case CardTag.Reluctant: return CardFaction.Reluctant;
                case CardTag.Blood: return CardFaction.Blood;
                case CardTag.Frost: return CardFaction.Frost;
                case CardTag.Corrupt: return CardFaction.Corrupt;
                case CardTag.Shadow: return CardFaction.Shadow;
                default: return CardFaction.None;
            }
        }
    }
}
