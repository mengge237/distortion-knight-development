using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;
using MutationChess.Core;
using TMPro;

namespace MutationChess.UI
{
    public class RewardPanel : MonoBehaviour
    {
        [Header("Panel Root")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject rewardOverview;
        [SerializeField] private GameObject cardSelectionPanel;

        [Header("Overview")]
        [SerializeField] private TMP_Text goldAmountText;
        [SerializeField] private Button cardRewardButton;
        [SerializeField] private TMP_Text cardRewardLabel;

        [Header("Card Selection")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Button skipCardButton;

        [Header("Skip")]
        [SerializeField] private Button skipOverviewButton;

        [Header("Title")]
        [SerializeField] private TMP_Text titleText;

        [Header("Reward Objects")]
        [SerializeField] private GameObject goldRewardObject;
        [SerializeField] private GameObject relicRewardObject;
        [SerializeField] private GameObject potionRewardObject;
        [SerializeField] private GameObject cardRewardObject;

        [Header("Relic Display")]
        [SerializeField] private Image relicIconImage;
        [SerializeField] private TMP_Text relicNameText;

        [Header("Potion Display")]
        [SerializeField] private Image potionIconImage;
        [SerializeField] private TMP_Text potionNameText;

        [Header("Layout")]
        [SerializeField] private Vector2 cardContainerOffset = new Vector2(0, -50);
        [SerializeField] private bool useAutoLayout = true;

        private Card selectedCard = null;
        private int currentGoldReward = 0;
        private List<Card> currentCardRewards = new List<Card>();
        private Relic currentRelicReward = null;
        private Potion currentPotionReward = null;
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
                cardRewardLabel.text = "";

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

            // 统一按钮手感（按压回弹 + 悬停 + 点击音效）
            UiFeel.ApplyToAllButtons(gameObject);
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

        public void ShowRewards(int goldReward, List<Card> cardRewards, Relic relicReward,
                                System.Action<int, Card> onConfirmed,
                                System.Action onClosed = null)
        {
            ShowRewards(goldReward, cardRewards, relicReward, null, onConfirmed, onClosed);
        }

        public void ShowRewards(int goldReward, List<Card> cardRewards, Relic relicReward, Potion potionReward,
                                System.Action<int, Card> onConfirmed,
                                System.Action onClosed = null)
        {

            currentGoldReward = goldReward;
            currentCardRewards = cardRewards ?? new List<Card>();
            currentRelicReward = relicReward;
            currentPotionReward = potionReward;
            onRewardsConfirmed = onConfirmed;
            onPanelClosed = onClosed;

            selectedCard = null;
            goldClaimed = false;
            isPanelShowing = true;
            isCardSelectionActive = false;

            if (goldAmountText != null)
                goldAmountText.text = $"点击领取 {goldReward} G";

            if (cardRewardLabel != null)
                cardRewardLabel.text = "";

            HideAllRewardObjects();

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
                if (!useAutoLayout)
                {
                    RectTransform goldRect = goldRewardObject.GetComponent<RectTransform>();
                    if (goldRect != null)
                        goldRect.anchoredPosition = new Vector2(0, 250);
                }
            }

            if (cardRewardObject != null)
            {
                cardRewardObject.SetActive(true);
                if (!useAutoLayout)
                {
                    RectTransform cardRect = cardRewardObject.GetComponent<RectTransform>();
                    if (cardRect != null)
                        cardRect.anchoredPosition = new Vector2(0, 150);
                }
            }


            if (relicRewardObject != null)
            {
                if (currentRelicReward != null)
                {
                    relicRewardObject.SetActive(true);
                    if (relicIconImage != null)
                        relicIconImage.sprite = currentRelicReward.icon;
                    if (relicNameText != null)
                        relicNameText.text = $"{currentRelicReward.relicName} ({currentRelicReward.GetRarityName()})";

                    if (!useAutoLayout)
                    {
                        RectTransform relicRect = relicRewardObject.GetComponent<RectTransform>();
                        if (relicRect != null)
                            relicRect.anchoredPosition = new Vector2(0, 50);
                    }
                }
                else
                {
                    relicRewardObject.SetActive(false);
                }
            }


            if (potionRewardObject != null)
            {
                if (currentPotionReward != null)
                {
                    potionRewardObject.SetActive(true);
                    if (potionIconImage != null)
                    {
                        if (currentPotionReward.icon != null)
                        {
                            potionIconImage.sprite = currentPotionReward.icon;
                            potionIconImage.gameObject.SetActive(true);
                        }
                        else
                        {
                            potionIconImage.gameObject.SetActive(false);
                        }
                    }
                    if (potionNameText != null)
                        potionNameText.text = $"{currentPotionReward.potionName} ({currentPotionReward.GetRarityName()})";

                    if (!useAutoLayout)
                    {
                        RectTransform potionRect = potionRewardObject.GetComponent<RectTransform>();
                        if (potionRect != null)
                            potionRect.anchoredPosition = new Vector2(0, -50);
                    }
                }
                else
                {
                    potionRewardObject.SetActive(false);
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
            if (useAutoLayout)
            {
                // 
                if (relicRewardObject != null)
                    relicRewardObject.SetActive(currentRelicReward != null);
                if (potionRewardObject != null)
                    potionRewardObject.SetActive(currentPotionReward != null);
                return;
            }

            if (goldRewardObject != null && goldRewardObject.activeSelf)
            {
                RectTransform goldRect = goldRewardObject.GetComponent<RectTransform>();
                if (goldRect != null)
                    goldRect.anchoredPosition = new Vector2(0, 250);
            }

            if (cardRewardObject != null && cardRewardObject.activeSelf)
            {
                RectTransform cardRect = cardRewardObject.GetComponent<RectTransform>();
                if (cardRect != null)
                    cardRect.anchoredPosition = new Vector2(0, 150);
            }

            if (relicRewardObject != null && currentRelicReward != null)
            {
                relicRewardObject.SetActive(true);
                RectTransform relicRect = relicRewardObject.GetComponent<RectTransform>();
                if (relicRect != null)
                    relicRect.anchoredPosition = new Vector2(0, 50);
            }
            else if (relicRewardObject != null)
            {
                relicRewardObject.SetActive(false);
            }

            if (potionRewardObject != null && currentPotionReward != null)
            {
                potionRewardObject.SetActive(true);
                RectTransform potionRect = potionRewardObject.GetComponent<RectTransform>();
                if (potionRect != null)
                    potionRect.anchoredPosition = new Vector2(0, -50);
            }
            else if (potionRewardObject != null)
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
                GameLogger.LogError("RewardPanel: panelRoot is null");
                return;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            //
            Transform parent = transform.parent;
            while (parent != null)
            {
                if (!parent.gameObject.activeSelf)
                    parent.gameObject.SetActive(true);
                parent = parent.parent;
            }

            panelRoot.SetActive(true);

            if (rewardOverview != null)
                rewardOverview.SetActive(true);

            ArrangeRewards();

            if (cardContainer != null && cardContainer.gameObject != null)
            {
                RectTransform containerRect = cardContainer.GetComponent<RectTransform>();
                if (containerRect != null && !useAutoLayout)
                    containerRect.anchoredPosition = cardContainerOffset;
            }

            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(false);

            //
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

            // 面板弹入动画（放在 CanvasGroup 复位之后，保证淡入生效）
            UiFeel.AnimatePanelIn(panelRoot);
        }

        private void GenerateCards(List<Card> rewards)
        {
            if (cardContainer == null)
            {
                GameLogger.LogError("RewardPanel: cardContainer 未找到");
                return;
            }

            if (rewards == null || rewards.Count == 0)
            {
                GameLogger.LogWarning("RewardPanel: ");
                return;
            }

            if (cardPrefab == null)
            {
                GameLogger.LogError("RewardPanel: cardPrefab 未找到");
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
                    GameLogger.LogError("RewardPanel: CardUI ");
                }
            }
        }

        private void ClaimGold()
        {
            if (goldClaimed || currentGoldReward <= 0) return;
            goldClaimed = true;

            // 金币滑落音效 + 立即到账（不再随确认/跳过自动发放）
            AudioManager.Instance?.PlayCoinSlide();

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
            {
                dataManager.AddGold(currentGoldReward);
                dataManager.UpdateUI();
            }

            if (goldAmountText != null)
                goldAmountText.text = $"+{currentGoldReward}";

            // 金币图标飞向顶栏金币显示处
            FlyCoinToTopBar();

            if (goldRewardObject != null)
            {
                goldRewardObject.SetActive(false);
            }

            var statusBar = StatusBarManager.Instance;
            if (statusBar != null)
            {
                statusBar.UpdateUI();
            }

            ArrangeRewards();
        }

        /// <summary>金币图标从奖励堆飞向顶栏金币显示处（视觉反馈，与滑落音效同步）。</summary>
        private void FlyCoinToTopBar()
        {
            if (goldRewardObject == null) return;

            Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (rootCanvas == null) return;

            RectTransform goldRt = goldRewardObject.GetComponent<RectTransform>();
            if (goldRt == null) return;

            Sprite coinSprite = Resources.Load<Sprite>("InterfaceUI/获胜奖励界面金币条目图标");

            GameObject fly = new GameObject("CoinFly", typeof(RectTransform), typeof(Image));
            fly.transform.SetParent(rootCanvas.transform, false);
            RectTransform flyRt = fly.GetComponent<RectTransform>();
            flyRt.position = goldRt.position; // 复用金币堆屏幕位置
            flyRt.sizeDelta = new Vector2(72f, 72f);
            Image flyImg = fly.GetComponent<Image>();
            flyImg.sprite = coinSprite;
            flyImg.raycastTarget = false;

            // 顶栏金币（左上角）
            Vector3 targetWorld = new Vector3(Screen.width * 0.105f, Screen.height * 0.945f, 0f);

            Sequence seq = DOTween.Sequence().SetUpdate(true); // 奖励界面可能处于暂停状态
            seq.Append(flyRt.DOMove(targetWorld, 0.6f).SetEase(Ease.InQuad));
            seq.Join(flyRt.DOScale(0.55f, 0.6f).SetEase(Ease.InQuad));
            seq.Join(flyRt.DORotate(new Vector3(0f, 0f, 300f), 0.6f, RotateMode.FastBeyond360));
            seq.OnComplete(() => { if (fly != null) Destroy(fly); });
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
                cardRewardLabel.text = "";
        }

        public void ShowOverview()
        {
            if (rewardOverview != null)
                rewardOverview.SetActive(true);

            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(false);

            if (titleText != null)
                titleText.text = "";

            isCardSelectionActive = false;
            ArrangeRewards();
        }

        public void ShowCardSelection()
        {
            if (cardSelectionPanel != null)
                cardSelectionPanel.SetActive(true);

            if (titleText != null)
                titleText.text = "";

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
            // 金币不再自动领取：只有点击金币堆（ClaimGold 即时到账）才会获得，跳过后未领取的金币即放弃
            int finalGold = 0;
            Card finalCard = selectedCard;

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


