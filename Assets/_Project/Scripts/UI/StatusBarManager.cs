using UnityEngine;
using TMPro;
using MutationChess.Core;

namespace MutationChess.UI
{
    public class StatusBarManager : MonoBehaviour
    {
        public static StatusBarManager Instance { get; private set; }

        [Header("UI Reference")]
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text mapNameText;

        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.3f;

        private PlayerData playerData;
        private float updateTimer = 0f;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
                playerData = dataManager.GetPlayerData();

            UpdateUI();
        }

        void Update()
        {
            if (playerData == null)
            {
                var dataManager = PlayerDataManager.Instance;
                if (dataManager != null)
                    playerData = dataManager.GetPlayerData();
                return;
            }

            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateUI();
            }
        }

        public void UpdateUI()
        {
            if (playerData == null)
            {
                var dataManager = PlayerDataManager.Instance;
                if (dataManager != null)
                    playerData = dataManager.GetPlayerData();
            }

            if (playerData == null) return;

            if (healthText != null)
                healthText.text = $"{playerData.currentHealth}/{playerData.maxHealth}";

            if (goldText != null)
                goldText.text = $"{playerData.gold}";
        }

        public void SetMapName(string mapName)
        {
            if (mapNameText != null)
                mapNameText.text = mapName;
        }

        public void UpdatePlayerData(PlayerData data)
        {
            playerData = data;
            UpdateUI();
        }
    }
}