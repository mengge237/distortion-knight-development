using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "CardDataAsset", menuName = "MutationChess/Card Data Asset")]
    public class CardDataAsset : ScriptableObject
    {
        [Header("")]
        public string cardName;
        public CardType cardType;
        public CardRarity rarity;
        public CardFaction faction;

        [Header("")]
        public int cost = 1;
        public int damage;
        public int block;
        public int magicNumber;
        public bool exhaust = false;

        [Header("")]
        [Tooltip("")]
        public List<CardTag> tags = new List<CardTag>();

        [Header("")]
        [Tooltip("33=10")]
        public int bloodPerEnergy = 0;

        [Header("")]
        [Tooltip("55=10")]
        public int blockPerEnergy = 0;

        [Header("")]
        [TextArea(2, 4)]
        public string description;

        [Header("")]
        public List<string> effectIds = new List<string>();

        [Header("")]
        [Tooltip("ID Resources/InherentEffects ")]
        public List<string> inherentEffectIds = new List<string>();

        [Header("")]
        public bool isColorless = false;
        public bool isFactionLocked = true;

        [Header("")]
        public string cardArtPath;
    }
}

