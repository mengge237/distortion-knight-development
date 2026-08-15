using UnityEngine;
using MutationChess.Battle;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 标签卡牌能量效果：打出指定标签（或同阵营）的卡牌时获得能量，Boss 遗物激活时额外加成。
    /// 由效果合并从 FrostCardEnergyRefundEffect / SlimeEnergyEffect 合并而来
    /// （仅过滤标签、Boss 加成来源不同，逻辑完全一致；原冰霜返还仅判标签，
    /// 此处与粘液能量统一为标签或同阵营均触发）。
    /// </summary>
    [CreateAssetMenu(fileName = "TagEnergyEffect", menuName = "MutationChess/Relic Effects/Tag Energy")]
    public class TagEnergyEffect : CardEffect
    {
        [Header("能量配置")]
        [Tooltip("触发加成的卡牌标签（按标签或同阵营判断）")]
        public CardTag filterTag = CardTag.Frost;

        [Tooltip("打出对应标签卡牌时获得的能量")]
        public int energyGain = 1;

        [Tooltip("Boss 遗物激活时额外增加的能量（叠加）")]
        public int bossExtraEnergy = 0;

        [Tooltip("额外能量的 Boss 遗物来源")]
        public BossFlagType bossFlag = BossFlagType.None;

        public override void Execute(CombatContext context)
        {
            TryGrantEnergy(context);
        }

        public override void Execute(EffectContext context)
        {
            TryGrantEnergy(context?.combat);
        }

        private bool BossFlagActive
        {
            get
            {
                switch (bossFlag)
                {
                    case BossFlagType.FrostHeart: return ConversionModifier.BossFrostHeartActive;
                    case BossFlagType.SlimeGland: return ConversionModifier.BossSlimeGlandActive;
                    default: return false;
                }
            }
        }

        private void TryGrantEnergy(CombatContext combat)
        {
            if (combat == null) return;
            Card src = combat.sourceCard;
            if (src == null) return;
            if (!src.HasTag(filterTag) && src.faction != FactionForTag(filterTag)) return;

            var hm = HandManager.Instance;
            if (hm == null)
            {
                GameLogger.LogWarning("[TagEnergy] HandManager 为空，无法发放能量");
                return;
            }

            int totalEnergy = energyGain + (BossFlagActive ? bossExtraEnergy : 0);
            hm.RestoreEnergy(totalEnergy);
            GameLogger.Log($"[TagEnergy] {src.cardName} 能量+{totalEnergy}" +
                (BossFlagActive ? " (Boss加成)" : ""));
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
