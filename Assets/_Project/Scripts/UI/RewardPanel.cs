using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MutationChess.Core;
using TMPro;

namespace MutationChess.UI
{
    public class RewardPanel : MonoBehaviour
    {
        [Header("=== 面板引用 ===")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject rewardOverview;
        [SerializeField] private GameObject cardSelectionPanel;

        [Header("=== 奖励显示（概览层） ===")]
        [SerializeField] private TMP_Text goldAmountText;
        [SerializeField] private Button cardRewardButton;
        [SerializeField] private TMP_Text cardRewardLabel;

        [Header("=== 卡牌选择层 ===")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Button skipCardButton;

        [Header("=== 概览层跳过 ===")]
        [SerializeField] private Button skipOverviewButton;

        [Header("=== 标题 ===")]
        [SerializeField] private TMP_Text titleText;

        [Header("=== 奖励对象（用于显示/隐藏） ===")]
        [SerializeField] private GameObject goldRewardObject;
        [SerializeField] private GameObject relicRewardObject;
        [SerializeField] private GameObject potionRewardObject;
        [SerializeField] private GameObject cardRewardObject;

        [Header("=== 卡牌容器设置 ===")]
        [SerializeField] private Vector2 cardContainerOffset = new Vector2(0, -50);

        private Card selectedCard = null;
        private int currentGoldReward = 0;
        private List<Card> currentCardRewards = new List<Card>();
        private System.Action<int, Card> onRewardsConfirmed;
        private System.Action onPanelClosed;
        private bool goldClaimed = false;
        private bool isInitialized = false;
        private bool isPanelShowing = false;
        private bool isCardSelectionActive = false;

        void Awake()
        {
            if (cardRewardButton != null)
            {
                cardRewardButton.onClick.RemoveAllListeners();
                cardRewardButton.onClick.AddListener(ShowCardSelection);
            }

            if (cardRewardLabel != null)
                cardRewardLabel.text = "卡牌奖励";

            if (goldAmountText != null)
            {
                Button btn = goldAmountText.GetComponentInParent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ClaimGold);
                }
            }

            if (skipCardButton != null)
            {
                skipCardButton.onClick.RemoveAllListeners();
                skipCardButton.onClick.AddListener(OnSkipCard);
            }

            if (skipOverviewButton != null)
            {
                skipOverviewButton.onClick.RemoveAllListeners();
                skipOverviewButton.onClick.AddListener(OnSkipAll);
            }

            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (rewardOverview != null)
                rewardOverview.SetActive(false);
            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(false);

            if (relicRewardObject != null)
                relicRewardObject.SetActive(false);
            if (potionRewardObject != null)
                potionRewardObject.SetActive(false);
        }

        void Start()
        {
            if (isInitialized || isPanelShowing) return;
            isInitialized = true;

            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (rewardOverview != null)
                rewardOverview.SetActive(false);
            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && isCardSelectionActive)
            {
                OnSkipCard();
            }
        }

        public void ShowRewards(int goldReward, List<Card> cardRewards,
                                System.Action<int, Card> onConfirmed,
                                System.Action onClosed = null)
        {

            currentGoldReward = goldReward;
            currentCardRewards = cardRewards ?? new List<Card>();
            onRewardsConfirmed = onConfirmed;
            onPanelClosed = onClosed;

            selectedCard = null;
            goldClaimed = false;
            isPanelShowing = true;
            isCardSelectionActive = false;

            if (goldAmountText != null)
                goldAmountText.text = goldReward.ToString();

            if (cardRewardLabel != null)
                cardRewardLabel.text = "卡牌奖励";

            HideAllRewardObjects();

            if (relicRewardObject != null)
            {
                relicRewardObject.SetActive(false);
                foreach (Transform child in relicRewardObject.transform)
                {
                    if (child != null && child.gameObject != null)
                        child.gameObject.SetActive(false);
                }
            }

            if (potionRewardObject != null)
            {
                potionRewardObject.SetActive(false);
                foreach (Transform child in potionRewardObject.transform)
                {
                    if (child != null && child.gameObject != null)
                        child.gameObject.SetActive(false);
                }
            }

            if (goldRewardObject != null)
            {
                goldRewardObject.SetActive(true);
                RectTransform goldRect = goldRewardObject.GetComponent<RectTransform>();
                if (goldRect != null)
                {
                    goldRect.anchoredPosition = new Vector2(0, 250);
                }
            }

            if (cardRewardObject != null)
            {
                cardRewardObject.SetActive(true);
                RectTransform cardRect = cardRewardObject.GetComponent<RectTransform>();
                if (cardRect != null)
                {
                    cardRect.anchoredPosition = new Vector2(0, 150);
                }
            }

            if (cardSelectionPanel != null)
            {
                cardSelectionPanel.SetActive(false);
            }

            ClearCardContainer();
            GenerateCards(currentCardRewards);
            ActivatePanel();

        }

        private void HideAllRewardObjects()
        {
            if (goldRewardObject != null)
                goldRewardObject.SetActive(false);
            if (relicRewardObject != null)
                relicRewardObject.SetActive(false);
            if (potionRewardObject != null)
                potionRewardObject.SetActive(false);
            if (cardRewardObject != null)
                cardRewardObject.SetActive(false);
        }

        private void ArrangeRewards()
        {
            if (goldRewardObject != null && goldRewardObject.activeSelf)
            {
                RectTransform goldRect = goldRewardObject.GetComponent<RectTransform>();
                if (goldRect != null)
                {
                    goldRect.anchoredPosition = new Vector2(0, 250);
                }
            }

            if (cardRewardObject != null && cardRewardObject.activeSelf)
            {
                RectTransform cardRect = cardRewardObject.GetComponent<RectTransform>();
                if (cardRect != null)
                {
                    cardRect.anchoredPosition = new Vector2(0, 150);
                }
            }

            if (relicRewardObject != null && relicRewardObject.activeSelf)
            {
                relicRewardObject.SetActive(false);
            }
            if (potionRewardObject != null && potionRewardObject.activeSelf)
            {
                potionRewardObject.SetActive(false);
            }
        }

        private void ClearCardContainer()
        {
            if (cardContainer == null) return;

            List<GameObject> children = new List<GameObject>();
            foreach (Transform child in cardContainer)
            {
                if (child != null && child.gameObject != null)
                {
                    children.Add(child.gameObject);
                }
            }

            foreach (GameObject child in children)
            {
                if (child != null)
                {
                    Destroy(child);
                }
            }
        }

        private void ActivatePanel()
        {
            if (panelRoot == null)
            {
                Debug.LogError("RewardPanel: panelRoot 为空！");
                return;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            Transform parent = transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                    parent.gameObject.SetActive(true);
                parent = parent.parent;
            }

            SetActiveRecursive(panelRoot, true);

            if (rewardOverview != null)
                SetActiveRecursive(rewardOverview, true);

            ArrangeRewards();

            if (cardContainer != null && cardContainer.gameObject != null)
            {
                RectTransform containerRect = cardContainer.GetComponent<RectTransform>();
                if (containerRect != null)
                    containerRect.anchoredPosition = cardContainerOffset;
            }

            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(false);

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
                canvas.enabled = true;
            }

            CanvasGroup canvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

        }

        private void SetActiveRecursive(GameObject obj, bool active)
        {
            if (obj == null) return;

            if (obj == cardSelectionPanel)
                return;

            if (obj == relicRewardObject || obj == potionRewardObject)
                return;

            if (obj.activeSelf != active)
                obj.SetActive(active);

            List<Transform> children = new List<Transform>();
            foreach (Transform child in obj.transform)
            {
                if (child != null && child.gameObject != null)
                    children.Add(child);
            }

            foreach (Transform child in children)
            {
                if (child != null && child.gameObject != null)
                    SetActiveRecursive(child.gameObject, active);
            }
        }

        private void GenerateCards(List<Card> rewards)
        {
            if (cardContainer == null)
            {
                Debug.LogError("RewardPanel: cardContainer 为空！");
                return;
            }

            if (rewards == null || rewards.Count == 0)
            {
                Debug.LogWarning("RewardPanel: 没有卡牌奖励");
                return;
            }

            if (cardPrefab == null)
            {
                Debug.LogError("RewardPanel: cardPrefab 为空！");
                return;
            }

            float cardWidth = 150f;
            float spacing = 20f;
            float totalWidth = (rewards.Count - 1) * (cardWidth + spacing);
            float startX = -totalWidth / 2f;

            for (int i = 0; i < rewards.Count; i++)
            {
                Card card = rewards[i];
                if (card == null) continue;

                GameObject cardObj = Instantiate(cardPrefab, cardContainer);
                CardUI cardUI = cardObj.GetComponent<CardUI>();

                if (cardUI != null)
                {
                    cardUI.Initialize(card);
                    Button btn = cardObj.GetComponent<Button>();
                    if (btn == null) btn = cardObj.AddComponent<Button>();
                    btn.onClick.AddListener(() => SelectCard(card));

                    RectTransform rect = cardObj.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchoredPosition = new Vector2(startX + i * (cardWidth + spacing), 0);
                    }
                }
                else
                {
                    Debug.LogError("RewardPanel: CardUI 组件为空！");
                }
            }
        }

        private void ClaimGold()
        {
            if (goldClaimed) return;
            goldClaimed = true;

            if (goldAmountText != null)
                goldAmountText.text = $"{currentGoldReward} ?";

            if (goldRewardObject != null)
            {
                // 播放消失动画或直接隐藏
                goldRewardObject.SetActive(false);
            }

            var statusBar = StatusBarManager.Instance;
            if (statusBar != null)
            {
                statusBar.UpdateUI();
            }

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
            {
                dataManager.UpdateUI();
            }

            ArrangeRewards();
        }

        private void SelectCard(Card card)
        {
            selectedCard = card;
            isCardSelectionActive = false;

            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(false);

            if (cardRewardObject != null)
            {
                cardRewardObject.SetActive(false);
            }

            var statusBar = StatusBarManager.Instance;
            if (statusBar != null)
            {
                statusBar.UpdateUI();
            }

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
            {
                dataManager.UpdateUI();
            }

            ArrangeRewards();

            if (cardRewardLabel != null)
                cardRewardLabel.text = "卡牌已选 ?";
        }

        public void ShowOverview()
        {
            if (rewardOverview != null)
                SetActiveRecursive(rewardOverview, true);

            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(false);

            if (titleText != null)
                titleText.text = "战斗奖励";

            isCardSelectionActive = false;
            ArrangeRewards();
        }

        public void ShowCardSelection()
        {

            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(true);

            if (titleText != null)
                titleText.text = "选择一张卡牌";

            isCardSelectionActive = true;
        }

        private void OnSkipCard()
        {
            selectedCard = null;
            isCardSelectionActive = false;

            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(false);

            if (cardRewardObject != null)
            {
                cardRewardObject.SetActive(false);
            }

            ArrangeRewards();
            ShowOverview();
        }

        private void OnSkipAll()
        {
            int finalGold = currentGoldReward;
            Card finalCard = selectedCard;

            if (!goldClaimed)
            {
                var dataManager = PlayerDataManager.Instance;
                if (dataManager != null && finalGold > 0)
                {
                    dataManager.AddGold(finalGold);
                }
            }

            isPanelShowing = false;
            isCardSelectionActive = false;
            onRewardsConfirmed?.Invoke(finalGold, finalCard);
            ClosePanel();
        }

        public void ClosePanel()
        {
            isPanelShowing = false;
            isCardSelectionActive = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(false);
            onPanelClosed?.Invoke();
        }

        public void ResetPanel()
        {
            isInitialized = false;
            isPanelShowing = false;
            isCardSelectionActive = false;
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(false);
        }
    }
}
