using MutationChess.Core;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Battle
{
    public class RewardPoolConfig
    {
        public List<CardDataAsset> allCards { get; private set; } = new List<CardDataAsset>();

        /// <summary>
        ///
        /// </summary>
        private static readonly HashSet<string> BasicCardNames = new HashSet<string>
        {
            "", "", "", "", "", "", ""
        };

        public void LoadAllCards()
        {
            allCards.Clear();
            CardDataAsset[] assets = Resources.LoadAll<CardDataAsset>("Cards");
            foreach (var asset in assets)
            {
                allCards.Add(asset);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public List<CardDataAsset> GetRewardableCardsByRarity(CardRarity rarity)
        {
            return allCards.FindAll(a =>
                !BasicCardNames.Contains(a.cardName) &&
                a.rarity == rarity &&
                a.rarity != CardRarity.Cursed
            );
        }

        public List<CardDataAsset> GetColoredCardsByRarity(CardRarity rarity)
        {
            return allCards.FindAll(a =>
                !a.isColorless &&
                a.rarity == rarity
            );
        }

        public List<CardDataAsset> GetColorlessCardsByRarity(CardRarity rarity)
        {
            return allCards.FindAll(a =>
                a.isColorless &&
                a.rarity == rarity
            );
        }

        /// <summary>
        ///
        /// </summary>
        public static bool IsBasicCard(string cardName)
        {
            return BasicCardNames.Contains(cardName);
        }
    }

    public static class RewardPoolManager
    {
        private static RewardPoolConfig config;

        public static RewardPoolConfig Config
        {
            get
            {
                if (config == null)
                {
                    config = new RewardPoolConfig();
                    config.LoadAllCards();
                }
                return config;
            }
        }
    }
}
