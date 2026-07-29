using System.Collections.Generic;
using MutationChess.Core;
using UnityEngine;
using TMPro;

namespace MutationChess.Core
{
    public class PlayerDataManager : MonoBehaviour
    {
        private static PlayerDataManager _instance;
        public static PlayerDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<PlayerDataManager>();
                }
                return _instance;
            }
        }

        [Header("===  ===")]
        [SerializeField] private PlayerData playerData;

        [Header("=== TopBar  ===")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text goldText;

        [Header("===  ===")]
        [SerializeField] private DeckData initialDeck;
        private List<Card> runtimeDeck = new List<Card>();

        [Header("===  ===")]
        public System.Action<PlayerData> OnDataChanged;

        public PlayerData PlayerData => playerData;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (playerData == null)
            {
                playerData = new PlayerData();
                playerData.InitFromConfig();
            }

            if (initialDeck != null)
            {
                ResetDeck();
            }
        }

        void Start()
        {
            if (_instance != this) return;
            UpdateUI();
        }

        public void InitializeDeck(DeckData deckTemplate)
        {
            initialDeck = deckTemplate;
            ResetDeck();
        }

        public void ResetDeck()
        {
            if (initialDeck != null)
            {
                runtimeDeck = initialDeck.GetDeckCopy();
            }
            else
            {
                runtimeDeck = new List<Card>();
                GameLogger.LogWarning("飬");
            }
        }

        public List<Card> GetRuntimeDeckCopy()
        {
            return new List<Card>(runtimeDeck);
        }

        public List<Card> GetRuntimeDeckRef()
        {
            return runtimeDeck;
        }

        public void AddCardToDeck(Card card)
        {
            if (card == null) return;
            runtimeDeck.Add(card);
            OnDataChanged?.Invoke(playerData);
        }

        public void AddCardsToDeck(List<Card> cards)
        {
            if (cards == null || cards.Count == 0) return;
            runtimeDeck.AddRange(cards);
            OnDataChanged?.Invoke(playerData);
        }

        public bool RemoveCardFromDeck(Card card)
        {
            if (card == null) return false;
            bool removed = runtimeDeck.Remove(card);
            if (removed)
            {
                OnDataChanged?.Invoke(playerData);
            }
            return removed;
        }

        public void Heal(int amount)
        {
            playerData.Heal(amount);
            OnDataChanged?.Invoke(playerData);
            UpdateUI();
        }

        public void TakeDamage(int damage)
        {
            playerData.TakeDamage(damage);
            OnDataChanged?.Invoke(playerData);
            UpdateUI();
        }

        public void AddGold(int amount)
        {
            playerData.AddGold(amount);
            OnDataChanged?.Invoke(playerData);
            UpdateUI();
        }

        public bool RemoveGold(int amount)
        {
            bool success = playerData.RemoveGold(amount);
            if (success)
            {
                OnDataChanged?.Invoke(playerData);
                UpdateUI();
            }
            return success;
        }

        public bool AddPotion(Potion potion)
        {
            if (potion == null) return false;
            bool success = playerData.AddPotion(potion);
            if (success)
            {
                OnDataChanged?.Invoke(playerData);
            }
            return success;
        }

        public List<Potion> GetPotions()
        {
            return playerData.GetPotions();
        }

        public void UpdateUI()
        {
            if (_instance != this) return;

            if (healthText != null)
            {
                healthText.text = $"HP:{playerData.currentHealth}/{playerData.maxHealth}";
            }

            if (goldText != null)
            {
                goldText.text = $"Gold:{playerData.gold}";
            }
        }

        public PlayerData GetPlayerData() => playerData;
        public int GetHealth() => playerData.currentHealth;
        public int GetMaxHealth() => playerData.maxHealth;
        public int GetGold() => playerData.gold;
        public bool IsDead() => playerData.IsDead();

        public void ResetData()
        {
            playerData = new PlayerData();
            ResetDeck();
            OnDataChanged?.Invoke(playerData);
            UpdateUI();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}

