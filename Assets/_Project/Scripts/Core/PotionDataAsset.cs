using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "PotionDataAsset", menuName = "MutationChess/Potion Data Asset")]
    public class PotionDataAsset : ScriptableObject
    {
        [Header("")]
        public string potionId;
        public string potionName;
        public PotionRarity rarity;

        [Header("")]
        [Tooltip("Effect")]
        public List<string> effectIds = new List<string>();

        [Header("")]
        [TextArea(2, 4)]
        public string description;

        [Header("")]
        public int price = 50;

        [Header("")]
        public string iconPath;
    }
}

