using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    [CreateAssetMenu(fileName = "CardDataAsset", menuName = "MutationChess/Card Data Asset")]
    public class CardDataAsset : ScriptableObject
    {
        [Header("卡牌基础信息")]
        public string cardName;
        public CardType cardType;
        public CardRarity rarity;
        public CardFaction faction;

        [Header("强度分级")]
        [Tooltip("机制级强力卡牌（牌库操纵/能量引擎/转换机制等）：奖励掉落概率大幅降低")]
        public PowerTier powerTier = PowerTier.Normal;

        [Header("卡牌数值")]
        public int cost = 1;
        public int damage;
        public int block;
        public int magicNumber;
        public bool exhaust = false;

        [Header("卡牌标签")]
        [Tooltip("卡牌标签列表，用于标签联动与效果触发")]
        public List<CardTag> tags = new List<CardTag>();

        [Header("鲜血转换")]
        [Tooltip("鲜血换能量比率（3=3滴血换1点能量，0=使用默认值）")]
        public int bloodPerEnergy = 0;

        [Header("格挡转换")]
        [Tooltip("格挡换能量比率（5=5点格挡换1点能量，0=使用默认值）")]
        public int blockPerEnergy = 0;

        [Header("卡牌描述")]
        [TextArea(2, 4)]
        public string description;

        [Header("卡牌效果")]
        public List<string> effectIds = new List<string>();

        [Header("固有效果")]
        [Tooltip("固有效果ID列表，从 Resources/InherentEffects 加载")]
        public List<string> inherentEffectIds = new List<string>();

        [Header("卡牌属性")]
        public bool isColorless = false;
        public bool isFactionLocked = true;

        [Header("资源路径")]
        public string cardArtPath;

        [Header("图鉴")]
        [Tooltip("图鉴稳定编号（1-999），由 Tools/分配图鉴ID 自动分配，勿手动修改")]
        public int codexId;
    }
}

