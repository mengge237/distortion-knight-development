using System.Collections.Generic;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 卡牌标签枚举：标识卡牌所属系列
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
    /// 固有效果抽象基类：卡牌开局时自动触发的效果
    /// </summary>
    public abstract class InherentEffect : CardEffect
    {
        /// <summary>
        /// 获取该固有效果对应的卡牌标签
        /// </summary>
        public abstract CardTag Tag { get; }

        /// <summary>
        /// 应用固有效果
        /// </summary>
        public abstract void ApplyInherent(CombatContext context);

        public override void Execute(CombatContext context)
        {
            ApplyInherent(context);
        }

        /// <summary>
        /// 判断该卡牌是否应该应用此固有效果
        /// </summary>
        public bool ShouldApply(Card card)
        {
            if (card == null) return false;
            return card.HasTag(Tag);
        }
    }

}
