using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 所有卡牌效果的基类
    /// </summary>
    public abstract class CardEffect : ScriptableObject
    {
        [TextArea(2, 4)]
        public string effectDescription; // 效果描述，用于UI显示

        /// <summary>
        /// 执行效果
        /// </summary>
        /// <param name="context">战斗上下文</param>
        public abstract void Execute(CombatContext context);
    }
}