using System.Collections.Generic;
using MutationChess.Core;
using UnityEngine;
using TMPro;

namespace MutationChess.Core
{
    [System.Serializable]
    public class PlayerSaveData
    {
        public int maxHealth;
        public int currentHealth;
        public int gold;
    }

    public class PlayerDataManager : MonoBehaviour, ISaveable
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

        /// <summary>调试无敌开关（由调试台控制）：开启后玩家生命不再因任何伤害而减少</summary>
        public static bool DebugInvincible = false;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            SaveService.Instance.Register(this);

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
                GameLogger.LogWarning("?");
            }
        }

        public List<Card> GetRuntimeDeckCopy()
        {
            //
            List<Card> freshCards = new List<Card>();
            foreach (var card in runtimeDeck)
            {
                if (card == null) continue;
                if (System.Enum.TryParse<CardName>(card.cardName, out var cn))
                {
                    Card fresh = CardData.CreateCard(cn);
                    if (fresh != null)
                        freshCards.Add(fresh);
                }
                else
                {
                    freshCards.Add(card);
                }
            }
            return freshCards;
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

        public int Heal(int amount)
        {
            int actual = playerData.Heal(amount);
            OnDataChanged?.Invoke(playerData);
            UpdateUI();
            return actual;
        }

        public void TakeDamage(int damage, bool bypassInvincible = false)
        {
            playerData.TakeDamage(damage, bypassInvincible);
            OnDataChanged?.Invoke(playerData);
            UpdateUI();
        }

        public void AddGold(int amount)
        {
            playerData.AddGold(amount);
            OnDataChanged?.Invoke(playerData);
            UpdateUI();
        }

        /// <summary>增加血上限（遗物共鸣等永久性奖励）。</summary>
        public void AddMaxHealth(int amount)
        {
            playerData.maxHealth += amount;
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

        // ================= 存档接口 =================

        public string SaveKey => "player";

        public string SerializeState()
        {
            return JsonUtility.ToJson(new PlayerSaveData
            {
                maxHealth = playerData.maxHealth,
                currentHealth = playerData.currentHealth,
                gold = playerData.gold
            });
        }

        public void DeserializeState(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                PlayerSaveData d = JsonUtility.FromJson<PlayerSaveData>(json);
                if (d == null) return;
                playerData.maxHealth = Mathf.Max(1, d.maxHealth);
                playerData.currentHealth = Mathf.Clamp(d.currentHealth, 0, playerData.maxHealth);
                playerData.gold = Mathf.Max(0, d.gold);
                OnDataChanged?.Invoke(playerData);
                UpdateUI();
                GameLogger.Log($"[存档] 恢复玩家状态：HP {playerData.currentHealth}/{playerData.maxHealth} · 金币 {playerData.gold}");
            }
            catch (System.Exception e)
            {
                GameLogger.LogError($"[存档] player 反序列化失败：{e.Message}");
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}

