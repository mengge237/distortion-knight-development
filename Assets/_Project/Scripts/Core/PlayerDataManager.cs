using System.Collections.Generic;
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

        [Header("=== 数据 ===")]
        [SerializeField] private PlayerData playerData;

        [Header("=== TopBar 引用 ===")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text goldText;

        [Header("=== 牌组 ===")]
        [SerializeField] private DeckData initialDeck;
        private List<Card> runtimeDeck = new List<Card>();

        [Header("=== 事件回调 ===")]
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
                playerData = new PlayerData();

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

        // ==================== 牌组管理 ====================

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
                Debug.LogWarning("未设置初始牌组，将使用空牌组");
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

        // ==================== 卡牌管理 ====================

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

        // ==================== 生命值 ====================

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

        // ==================== 金币 ====================

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

        // ==================== UI更新 ====================

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

        // ==================== 查询 ====================

        public PlayerData GetPlayerData() => playerData;
        public int GetHealth() => playerData.currentHealth;
        public int GetMaxHealth() => playerData.maxHealth;
        public int GetGold() => playerData.gold;
        public bool IsDead() => playerData.IsDead();

        // ==================== 重置 ====================

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