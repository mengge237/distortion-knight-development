using UnityEngine;
using System.Collections.Generic;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "BackgroundConfig", menuName = "MutationChess/Background Config")]
    public class BackgroundConfig : ScriptableObject
    {
        [Header("=== 背景映射列表 ===")]
        public List<BackgroundMapping> mappings = new List<BackgroundMapping>();

        /// <summary>
        /// 根据敌人类型获取对应的背景图片
        /// </summary>
        public Sprite GetBackground(EnemyType enemyType)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.enemyType == enemyType && mapping.background != null)
                {
                    return mapping.background;
                }
            }
            return mappings.Count > 0 ? mappings[0].background : null;
        }

        /// <summary>
        /// 根据敌人名称枚举获取对应的背景图片
        /// </summary>
        public Sprite GetBackground(EnemyNameOption enemyName)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.enemyName == enemyName && mapping.background != null)
                {
                    return mapping.background;
                }
            }
            return mappings.Count > 0 ? mappings[0].background : null;
        }

        /// <summary>
        /// 根据敌人名称字符串获取对应的背景图片（兼容旧代码）
        /// </summary>
        public Sprite GetBackgroundByName(string enemyName)
        {
            if (System.Enum.TryParse(enemyName, out EnemyNameOption parsedName))
            {
                return GetBackground(parsedName);
            }

            // 如果转换失败，遍历查找匹配的名称
            foreach (var mapping in mappings)
            {
                if (mapping.enemyName.ToString() == enemyName && mapping.background != null)
                {
                    return mapping.background;
                }
            }
            return mappings.Count > 0 ? mappings[0].background : null;
        }
    }

    [System.Serializable]
    public class BackgroundMapping
    {
        [Header("=== 敌人信息 ===")]
        public EnemyType enemyType;

        [Header("=== 敌人名称（下拉选择） ===")]
        public EnemyNameOption enemyName;

        [Header("=== 背景图片 ===")]
        public Sprite background;
    }

    /// <summary>
    /// 所有敌人名称的枚举（用于下拉选择）
    /// </summary>
    public enum EnemyNameOption
    {
        腐化士兵,
        畸变猎犬,
        瘟疫侍僧,
        深渊蛆虫,
        腐蚀骑士,
        地狱审判官,
        虚空巫师,
        腐化巨兽,
        深渊之主克苏鲁之影,
        地狱复仇骑士
    }
}