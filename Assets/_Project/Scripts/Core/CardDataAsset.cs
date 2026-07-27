using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "CardDataAsset", menuName = "MutationChess/Card Data Asset")]
    public class CardDataAsset : ScriptableObject
    {
        [Header("基本信息")]
        public string cardName;
        public CardType cardType;
        public CardRarity rarity;
        public CardFaction faction;

        [Header("数值")]
        public int cost = 1;
        public int damage;
        public int block;
        public int magicNumber;

        [Header("描述")]
        [TextArea(2, 4)]
        public string description;

        [Header("效果")]
        public List<string> effectIds = new List<string>();

        [Header("卡牌图片路径")]
        public string cardArtPath;
    }
}
