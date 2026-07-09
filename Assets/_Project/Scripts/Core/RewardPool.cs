using UnityEngine;
using System.Collections.Generic;
using MutationChess.Core;

namespace MutationChess.UI
{

    [CreateAssetMenu(fileName = "RewardPool", menuName = "MutationChess/Reward Pool")]
    public class RewardPool : ScriptableObject
    {
        [Header("=== 奖励池名称 ===")]
        public string poolName = "奖励池";

        [Header("=== 奖励池类型 ===")]
        public RewardPoolType poolType = RewardPoolType.Common;

        [Header("=== 每次展示数量（可在代码中覆盖） ===")]
        public int cardsToShow = 3;

        [Header("=== 可用卡牌列表（只读，由代码自动填充） ===")]
        [SerializeField] private List<CardName> availableRewards = new List<CardName>();

        /// <summary>
        /// 获取奖励卡牌列表
        /// </summary>
        public List<Card> GetRewards()
        {
            List<Card> result = new List<Card>();

            if (availableRewards == null || availableRewards.Count == 0)
            {
                Debug.LogWarning($"奖励池 {poolName} 为空，使用默认卡牌");
                return GetDefaultRewards();
            }

            int count = Mathf.Min(cardsToShow, availableRewards.Count);
            List<CardName> shuffled = new List<CardName>(availableRewards);
            Shuffle(shuffled);

            for (int i = 0; i < count; i++)
            {
                Card card = CardData.CreateCard(shuffled[i]);
                if (card != null)
                {
                    result.Add(card);
                }
            }

            return result;
        }

        /// <summary>
        /// 获取默认奖励（当池为空时使用）
        /// </summary>
        public List<Card> GetDefaultRewards()
        {
            List<Card> result = new List<Card>();

            Card attack = CardData.CreateCard(CardName.攻击);
            Card defend = CardData.CreateCard(CardName.防御);
            Card bash = CardData.CreateCard(CardName.痛击);

            if (attack != null) result.Add(attack);
            if (defend != null) result.Add(defend);
            if (bash != null) result.Add(bash);

            return result;
        }

        /// <summary>
        /// 刷新可用卡牌列表
        /// </summary>
        public void RefreshAvailableRewards(List<CardName> cardList)
        {
            availableRewards.Clear();
            if (cardList != null)
            {
                availableRewards.AddRange(cardList);
            }
        }

        /// <summary>
        /// 获取可用卡牌列表的副本
        /// </summary>
        public List<CardName> GetAvailableRewardsCopy()
        {
            return new List<CardName>(availableRewards);
        }

        private void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

#if UNITY_EDITOR
        public void AddCardToPool(CardName cardName)
        {
            if (!availableRewards.Contains(cardName))
            {
                availableRewards.Add(cardName);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        public void RemoveCardFromPool(CardName cardName)
        {
            if (availableRewards.Contains(cardName))
            {
                availableRewards.Remove(cardName);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        public void ClearPool()
        {
            availableRewards.Clear();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}