using System.Collections.Generic;
using UnityEngine;
using MutationChess.Core;

namespace MutationChess.Battle
{
    [CreateAssetMenu(fileName = "BossRewardConfig", menuName = "MutationChess/Boss Reward Config")]
    public class BossRewardConfig : ScriptableObject
    {
        [Header("金币奖励范围")]
        public int minGold = 95;
        public int maxGold = 105;

        [Header("阵营解锁遗物池")]
        [Tooltip("击败Boss后可选择的阵营解锁遗物")]
        public List<RelicDataAsset> factionUnlockRelics = new List<RelicDataAsset>();

        [Header("Boss额外遗物池")]
        public List<RelicDataAsset> bonusRelics = new List<RelicDataAsset>();

        [Header("阵营卡牌奖励")]
        [Tooltip("Boss击败后奖励的阵营卡牌")]
        public List<CardDataAsset> factionCardRewards = new List<CardDataAsset>();
    }
}

