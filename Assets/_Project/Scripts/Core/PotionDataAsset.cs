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

        [Header("图鉴")]
        [Tooltip("图鉴编号（药水类别内从1递增，命令形式 p1+，如 p3=本药水），由 Tools/分配图鉴ID 自动分配，勿手动修改")]
        public int codexId;
    }
}

