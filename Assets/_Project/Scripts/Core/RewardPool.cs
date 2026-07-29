using UnityEngine;
using System.Collections.Generic;
using MutationChess.Core;

namespace MutationChess.UI
{

    [CreateAssetMenu(fileName = "RewardPool", menuName = "MutationChess/Reward Pool")]
    public class RewardPool : ScriptableObject
    {
        [Header("奖励池")]
        public string poolName = "";

        [Header("奖励池类型")]
        public RewardPoolType poolType = RewardPoolType.Common;

        [Header("展示卡牌数")]
        public int cardsToShow = 3;

        [Header("可用奖励")]
        [SerializeField] private List<CardName> availableRewards = new List<CardName>();
    }
}
