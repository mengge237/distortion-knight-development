using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "PotionDataAsset", menuName = "MutationChess/Potion Data Asset")]
    public class PotionDataAsset : ScriptableObject
    {
        [Header("药水基础信息")]
        public string potionId;
        public string potionName;
        public PotionRarity rarity;

        [Header("药水效果")]
        [Tooltip("效果ID列表，从 Resources/Effects 加载")]
        public List<string> effectIds = new List<string>();

        [Header("药水描述")]
        [TextArea(2, 4)]
        public string description;

        [Header("经济属性")]
        public int price = 50;

        [Header("资源路径")]
        public string iconPath;
    }
}

