using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;
using TMPro;

namespace MutationChess.UI
{
    /// <summary>
    /// 商店面板——模仿杀戮尖塔（StS）商店布局：
    /// 遗物一排居中在顶部、药水在右上角、卡牌一排居中、移除服务在左下角、离开按钮在右下角。
    /// 场景接线（MainScene）与代码双重驱动：场景提供 CardRow/PotionRow/RemovalRow 容器、
    /// 移除费用文本与三种槽位预制体（Prefabs/Shop/）；代码负责布局统一、内容填充与交互。
    /// 场景接线缺失时全部回退到运行时自动构建，无场景接线也能完整工作。
    /// 先模仿 StS，后续再迭代自有风格。
    /// </summary>
    public class ShopPanel : MonoBehaviour
    {
        [Header("面板基础")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Header("文本显示")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text removalInfoText;

        [Header("容器")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private Transform relicContainer;
        [SerializeField] private Transform potionContainer;
        [SerializeField] private Transform removalContainer;

        [Header("槽位预制体")]
        [SerializeField] private GameObject cardSlotPrefab;
        [SerializeField] private GameObject relicSlotPrefab;
        [SerializeField] private GameObject potionSlotPrefab;
        [SerializeField] private GameObject removalSlotPrefab;

        private List<ShopItem> shopItems = new List<ShopItem>();
        private Dictionary<ShopItem, GameObject> itemToSlot = new Dictionary<ShopItem, GameObject>();
        private System.Action onShopClosed;
        private CanvasGroup canvasGroup;

        private ShopDataService shopDataService;
        private PlayerDataManager playerDataManager;
        private RelicManager relicManager;

        // 运行时构建的槽位预制体（场景未接线时的兜底）
        private GameObject builtCardSlotPrefab;
        private GameObject builtPotionSlotPrefab;
        private GameObject builtRemovalSlotPrefab;

        // 卡牌移除选择弹窗
        private GameObject removalDialog;

        // 反馈
        private Text toastText;
        private Coroutine toastRoutine;
        private Coroutine goldFlashRoutine;

        private static Font _chineseFont;
        private static Font ChineseFont
        {
            get
            {
                if (_chineseFont == null)
                    _chineseFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "SimSun" }, 16);
                return _chineseFont;
            }
        }

        // 场景文本统一使用的 TMP 中文字体（霞鹜文楷 SDF，由 UiFonts 集中加载）
        private static TMP_FontAsset _tmpChineseFont;
        private static TMP_FontAsset TmpChineseFont
        {
            get
            {
                if (_tmpChineseFont == null)
                    _tmpChineseFont = UiFonts.Load();
                return _tmpChineseFont;
            }
        }

        void Awake()
        {
            if (panelRoot == null)
                panelRoot = gameObject;

            if (closeButton == null)
                closeButton = GetComponentInChildren<Button>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            panelRoot.SetActive(false);

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(CloseShop);
            }

            // 全部按钮统一手感
            UiFeel.ApplyToAllButtons(panelRoot);

            ApplyStSLayout();
        }

        public void OpenShop(System.Action onClosed = null)
        {
            onShopClosed = onClosed;

            shopDataService = ShopDataService.Instance;
            playerDataManager = PlayerDataManager.Instance;
            relicManager = RelicManager.Instance;

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            GenerateShopContents();
            PopulateAllSlots();
            UpdateGoldDisplay();
            UpdateRemovalInfo();
            RefreshAffordability();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // 面板弹入动画
            UiFeel.AnimatePanelIn(panelRoot);

            if (titleText != null)
                titleText.text = "商店";
        }

        private void GenerateShopContents()
        {
            shopItems.Clear();

            if (shopDataService != null)
            {
                shopItems = shopDataService.GenerateShopContents();
            }
            else
            {
                if (relicManager != null)
                {
                    var relics = relicManager.GenerateShopRelics(3);
                    foreach (var relic in relics)
                    {
                        if (relic == null) continue;
                        shopItems.Add(new ShopItem
                        {
                            type = ShopItemType.Relic,
                            item = relic,
                            basePrice = relic.price,
                            finalPrice = relic.price
                        });
                    }
                }
            }
        }

        private void PopulateAllSlots()
        {
            ClearAllContainers();

            foreach (var item in shopItems)
            {
                switch (item.type)
                {
                    case ShopItemType.ColoredCard:
                    case ShopItemType.ColorlessCard:
                        PopulateCardSlot(item);
                        break;
                    case ShopItemType.Relic:
                        PopulateRelicSlot(item);
                        break;
                    case ShopItemType.Potion:
                        PopulatePotionSlot(item);
                        break;
                    case ShopItemType.CardRemoval:
                        PopulateRemovalSlot(item);
                        break;
                }
            }
        }

        private GameObject GetSlotPrefab(ShopItemType type)
        {
            switch (type)
            {
                case ShopItemType.ColoredCard:
                case ShopItemType.ColorlessCard:
                    if (cardSlotPrefab != null) return cardSlotPrefab;
                    if (builtCardSlotPrefab == null) builtCardSlotPrefab = BuildCardSlotPrefab();
                    return builtCardSlotPrefab;
                case ShopItemType.Potion:
                    if (potionSlotPrefab != null) return potionSlotPrefab;
                    if (builtPotionSlotPrefab == null) builtPotionSlotPrefab = BuildPotionSlotPrefab();
                    return builtPotionSlotPrefab;
                case ShopItemType.CardRemoval:
                    if (removalSlotPrefab != null) return removalSlotPrefab;
                    if (builtRemovalSlotPrefab == null) builtRemovalSlotPrefab = BuildRemovalSlotPrefab();
                    return builtRemovalSlotPrefab;
                default:
                    return relicSlotPrefab;
            }
        }

        private Transform GetContainer(ShopItemType type)
        {
            switch (type)
            {
                case ShopItemType.ColoredCard:
                case ShopItemType.ColorlessCard:
                    return cardContainer != null ? cardContainer : relicContainer;
                case ShopItemType.Potion:
                    return potionContainer != null ? potionContainer : relicContainer;
                case ShopItemType.CardRemoval:
                    return removalContainer != null ? removalContainer : relicContainer;
                default:
                    return relicContainer;
            }
        }

        private void PopulateCardSlot(ShopItem item)
        {
            Transform container = GetContainer(item.type);
            GameObject prefab = GetSlotPrefab(item.type);
            if (container == null || prefab == null) return;

            Card card = item.item as Card;
            if (card == null) return;

            GameObject slotObj = Instantiate(prefab, container);
            SetupSlotBase(slotObj, item, card.cardName);

            SetSlotText(slotObj.transform, "Name", card.cardName);
            SetSlotText(slotObj.transform, "Description", card.GetDescription());

            Image iconImage = slotObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                if (card.cardArt != null)
                {
                    iconImage.sprite = card.cardArt;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }
        }

        private void PopulateRelicSlot(ShopItem item)
        {
            Transform container = GetContainer(item.type);
            GameObject prefab = GetSlotPrefab(item.type);
            if (container == null || prefab == null) return;

            Relic relic = item.item as Relic;
            if (relic == null) return;

            GameObject slotObj = Instantiate(prefab, container);
            SetupSlotBase(slotObj, item, relic.relicName);

            SetSlotText(slotObj.transform, "Name", $"{relic.relicName} ({relic.GetRarityName()})");
            SetSlotText(slotObj.transform, "Description", relic.description);

            Image iconImage = slotObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                Sprite icon = relic.icon;

                if (icon == null && !string.IsNullOrEmpty(relic.relicName))
                {
                    icon = Resources.Load<Sprite>($"{ResourcePaths.RelicsArt}/{relic.relicName}");
                }

                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.color = Color.white;
                    iconImage.preserveAspect = true;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                    GameLogger.LogWarning($"[ShopPanel] 遗物图标未找到：{relic.relicName}");
                }
            }
        }

        private void PopulatePotionSlot(ShopItem item)
        {
            Transform container = GetContainer(item.type);
            GameObject prefab = GetSlotPrefab(item.type);
            if (container == null || prefab == null) return;

            Potion potion = item.item as Potion;
            if (potion == null) return;

            GameObject slotObj = Instantiate(prefab, container);
            SetupSlotBase(slotObj, item, potion.potionName);

            SetSlotText(slotObj.transform, "Name", $"{potion.potionName} ({potion.GetRarityName()})");
            SetSlotText(slotObj.transform, "Description", potion.description);

            Image iconImage = slotObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                if (potion.icon != null)
                {
                    iconImage.sprite = potion.icon;
                    iconImage.enabled = true;
                }
                else
                {
                    iconImage.enabled = false;
                }
            }
        }

        private void PopulateRemovalSlot(ShopItem item)
        {
            Transform container = GetContainer(item.type);
            GameObject prefab = GetSlotPrefab(item.type);
            if (container == null || prefab == null) return;

            GameObject slotObj = Instantiate(prefab, container);
            SetupSlotBase(slotObj, item, "移除卡牌");

            SetSlotText(slotObj.transform, "Description", "从牌组中永久移除一张卡牌");
        }

        private void SetupSlotBase(GameObject slotObj, ShopItem item, string slotName)
        {
            slotObj.name = slotName;
            itemToSlot[item] = slotObj;

            string priceStr = item.isOnSale ? $"特价 {item.finalPrice} G" : $"{item.finalPrice} G";

            SetSlotText(slotObj.transform, "Price", priceStr);

            Button buyButton = slotObj.transform.Find("BuyButton")?.GetComponent<Button>();
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => TryPurchaseItem(item, slotObj));
            }

            // 槽位按钮手感（场景预制体与运行时构建共用此路径）
            UiFeel.ApplyToAllButtons(slotObj);
        }

        private void TryPurchaseItem(ShopItem item, GameObject slotObj)
        {
            if (item.isSold) return;

            if (playerDataManager == null)
            {
                GameLogger.LogError("[ShopPanel] PlayerDataManager 未找到");
                return;
            }

            // 移除服务：先弹出卡牌选择，确认后再扣款
            if (item.type == ShopItemType.CardRemoval)
            {
                OpenCardRemovalDialog(item);
                return;
            }

            if (!playerDataManager.RemoveGold(item.finalPrice))
            {
                GameLogger.Log($"[ShopPanel] 金币不足：需要 {item.finalPrice}，当前 {playerDataManager.GetGold()}");
                ShowFeedback($"金币不足！需要 {item.finalPrice} G", true);
                FlashGoldText(new Color(1f, 0.35f, 0.3f));
                return;
            }

            string boughtName = item.Name;

            switch (item.type)
            {
                case ShopItemType.ColoredCard:
                case ShopItemType.ColorlessCard:
                    var card = item.item as Card;
                    if (card != null && playerDataManager != null)
                    {
                        playerDataManager.AddCardToDeck(card);
                        GameLogger.Log($"[ShopPanel] 购买卡牌：{card.cardName}");
                    }
                    break;

                case ShopItemType.Relic:
                    var relic = item.item as Relic;
                    if (relic != null && relicManager != null)
                    {
                        relicManager.AddRelic(relic);
                        GameLogger.Log($"[ShopPanel] 购买遗物：{relic.relicName}");
                    }
                    break;

                case ShopItemType.Potion:
                    var potion = item.item as Potion;
                    if (potion != null && playerDataManager != null)
                    {
                        playerDataManager.AddPotion(potion);
                        GameLogger.Log($"[ShopPanel] 购买药水：{potion.potionName}");
                    }
                    break;
            }

            if (shopDataService != null)
                shopDataService.OnItemPurchased(item);

            bool hasRestockTalisman = relicManager != null && relicManager.HasRelic(RelicIds.Shop_RestockTalisman);
            if (!hasRestockTalisman)
            {
                MarkSold(item);
            }
            else
            {
                GameLogger.Log("[ShopPanel] 补货符生效，商品未标记为已售出");
            }

            ShowFeedback($"已购买：{boughtName}", false);
            FlashGoldText(new Color(0.55f, 1f, 0.6f));
            UpdateGoldDisplay();
            UpdateRemovalInfo();
            RefreshAffordability();
        }

        /// <summary>
        /// 卡牌移除选择弹窗：运行时构建（遮罩 + 面板 + 牌组网格 + 取消）。
        /// 选中卡牌后扣款并移除，不再"移除第一张"。
        /// </summary>
        private void OpenCardRemovalDialog(ShopItem removalItem)
        {
            var deck = playerDataManager?.GetRuntimeDeckRef();
            if (deck == null || deck.Count == 0)
            {
                ShowFeedback("牌组为空，无法移除", true);
                return;
            }

            CloseRemovalDialog();

            var parent = panelRoot != null ? (RectTransform)panelRoot.transform : (RectTransform)transform;

            // 全屏遮罩
            var dimGo = new GameObject("CardRemovalDialog", typeof(RectTransform), typeof(Image));
            var dimRt = (RectTransform)dimGo.transform;
            dimRt.SetParent(parent, false);
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            dimGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            // 面板
            var panelRt = CreateChild("Panel", dimGo.transform, new Vector2(920, 620), Vector2.zero);
            panelRt.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.08f, 0.06f, 0.98f);

            // 标题
            var titleRt = CreateChild("Title", panelRt, new Vector2(560, 44), new Vector2(0, 274));
            var titleT = titleRt.gameObject.AddComponent<Text>();
            InitText(titleT, 22, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.6f), true);
            titleT.text = $"选择要移除的卡牌（费用 {removalItem.finalPrice} G）";

            // 取消按钮
            var cancelRt = CreateChild("Cancel", panelRt, new Vector2(110, 40), new Vector2(392, 274));
            cancelRt.gameObject.AddComponent<Image>().color = new Color(0.45f, 0.3f, 0.25f, 1f);
            var cancelBtn = cancelRt.gameObject.AddComponent<Button>();
            cancelBtn.onClick.AddListener(CloseRemovalDialog);
            var cancelLabel = CreateChild("Label", cancelRt, new Vector2(110, 40), Vector2.zero);
            cancelLabel.anchorMin = Vector2.zero;
            cancelLabel.anchorMax = Vector2.one;
            cancelLabel.sizeDelta = Vector2.zero;
            var cancelT = cancelLabel.gameObject.AddComponent<Text>();
            InitText(cancelT, 18, TextAnchor.MiddleCenter, Color.white, true);
            cancelT.text = "取消";

            // 滚动区域
            var viewRt = CreateChild("Viewport", panelRt, new Vector2(860, 520), new Vector2(0, -16));
            var viewImg = viewRt.gameObject.AddComponent<Image>();
            viewImg.color = new Color(0f, 0f, 0f, 0.35f);
            var mask = viewRt.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var scrollRect = viewRt.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.viewport = viewRt;

            var contentRt = CreateChild("Content", viewRt, new Vector2(840, 100), Vector2.zero);
            contentRt.anchorMin = new Vector2(0.5f, 1f);
            contentRt.anchorMax = new Vector2(0.5f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            var grid = contentRt.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(190, 210);
            grid.spacing = new Vector2(14, 14);
            grid.padding = new RectOffset(12, 12, 12, 12);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.UpperCenter;
            scrollRect.content = contentRt;

            int rows = Mathf.CeilToInt(deck.Count / 4f);
            contentRt.sizeDelta = new Vector2(840, rows * 224 + 24);

            foreach (var card in deck)
            {
                var cellRt = CreateChild("Card", contentRt, new Vector2(190, 210), Vector2.zero);
                cellRt.gameObject.AddComponent<Image>().color = new Color(0.24f, 0.2f, 0.16f, 1f);
                var cellBtn = cellRt.gameObject.AddComponent<Button>();
                var captured = card;
                cellBtn.onClick.AddListener(() => ConfirmRemoval(removalItem, captured));

                var nameRt = CreateChild("Name", cellRt, new Vector2(170, 190), new Vector2(0, 4));
                var nameT = nameRt.gameObject.AddComponent<Text>();
                InitText(nameT, 16, TextAnchor.MiddleCenter, new Color(0.95f, 0.9f, 0.8f), true);
                nameT.text = captured.cardName;
            }

            removalDialog = dimGo;
        }

        private void ConfirmRemoval(ShopItem item, Card card)
        {
            if (item.isSold || card == null) return;
            if (playerDataManager == null) return;

            if (!playerDataManager.RemoveGold(item.finalPrice))
            {
                ShowFeedback($"金币不足！需要 {item.finalPrice} G", true);
                return;
            }

            playerDataManager.RemoveCardFromDeck(card);
            if (shopDataService != null)
                shopDataService.OnItemPurchased(item);

            MarkSold(item);
            CloseRemovalDialog();

            ShowFeedback($"已移除「{card.cardName}」", false);
            FlashGoldText(new Color(0.55f, 1f, 0.6f));
            UpdateGoldDisplay();
            UpdateRemovalInfo();
            RefreshAffordability();
        }

        private void CloseRemovalDialog()
        {
            if (removalDialog != null)
            {
                Destroy(removalDialog);
                removalDialog = null;
            }
        }

        private void MarkSold(ShopItem item)
        {
            item.isSold = true;

            if (!itemToSlot.TryGetValue(item, out var slot) || slot == null) return;

            Button buyButton = slot.transform.Find("BuyButton")?.GetComponent<Button>();
            if (buyButton != null)
                buyButton.interactable = false;

            SetSlotText(slot.transform, "Price", "已售出");
            SetSlotTextColor(slot.transform, "Price", new Color(0.5f, 0.5f, 0.5f));
        }

        /// <summary>根据当前金币刷新所有商品的买得起/买不起配色</summary>
        private void RefreshAffordability()
        {
            int gold = playerDataManager != null ? playerDataManager.GetGold() : 0;

            foreach (var kv in itemToSlot)
            {
                if (kv.Key == null || kv.Value == null || kv.Key.isSold) continue;

                bool afford = kv.Key.finalPrice <= gold;
                Color target = !afford
                    ? new Color(1f, 0.5f, 0.45f)
                    : (kv.Key.isOnSale ? new Color(1f, 0.83f, 0.35f) : Color.white);

                SetSlotTextColor(kv.Value.transform, "Price", target);
            }
        }

        private void ClearAllContainers()
        {
            ClearContainer(cardContainer);
            ClearContainer(relicContainer);
            ClearContainer(potionContainer);
            ClearContainer(removalContainer);
            itemToSlot.Clear();
        }

        private void ClearContainer(Transform container)
        {
            if (container == null) return;
            foreach (Transform child in container)
                Destroy(child.gameObject);
        }

        private void UpdateGoldDisplay()
        {
            if (goldText != null && playerDataManager != null)
                goldText.text = $"金币: {playerDataManager.GetGold()} G";
        }

        private void UpdateRemovalInfo()
        {
            if (removalInfoText != null && shopDataService != null)
            {
                int nextCost = shopDataService.GetCurrentRemovalCost();
                removalInfoText.text = $"移除费用: {nextCost} G";
            }
        }

        /// <summary>底部居中提示条（金币不足/购买成功反馈）</summary>
        private void ShowFeedback(string msg, bool error)
        {
            if (toastText == null)
            {
                var parent = panelRoot != null ? (RectTransform)panelRoot.transform : (RectTransform)transform;
                var rt = CreateChild("Toast", parent, new Vector2(560, 40), Vector2.zero);
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0, 90);
                toastText = rt.gameObject.AddComponent<Text>();
                InitText(toastText, 20, TextAnchor.MiddleCenter, Color.white, true);
            }

            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastText.text = msg;
            toastText.color = error ? new Color(1f, 0.5f, 0.45f) : new Color(0.6f, 1f, 0.65f);
            toastRoutine = StartCoroutine(ToastFade());
        }

        private IEnumerator ToastFade()
        {
            var cg = toastText.GetComponent<CanvasGroup>();
            if (cg == null) cg = toastText.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            yield return new WaitForSeconds(1.6f);

            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
                yield return null;
            }
            cg.alpha = 0f;
        }

        private void FlashGoldText(Color flash)
        {
            if (goldText == null) return;
            if (goldFlashRoutine != null) StopCoroutine(goldFlashRoutine);
            goldFlashRoutine = StartCoroutine(FlashRoutine(flash));
        }

        private IEnumerator FlashRoutine(Color flash)
        {
            Color original = goldText.color;
            for (int i = 0; i < 2; i++)
            {
                goldText.color = flash;
                yield return new WaitForSeconds(0.12f);
                goldText.color = original;
                yield return new WaitForSeconds(0.12f);
            }
        }

        public void CloseShop()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            CloseRemovalDialog();
            ClearAllContainers();
            shopItems.Clear();
            onShopClosed?.Invoke();
        }

        public bool IsShopOpen()
        {
            return panelRoot != null && panelRoot.activeSelf;
        }

        #region StS 风格布局

        /// <summary>
        /// 将四个容器按杀戮尖塔商店布局摆放：
        /// 遗物顶部居中、药水右上、卡牌居中、移除服务左下、离开按钮右下。
        /// 容器为 null 时在运行时创建；已有布局组件统一替换为横排。
        /// </summary>
        private void ApplyStSLayout()
        {
            var parent = panelRoot != null ? (RectTransform)panelRoot.transform : (RectTransform)transform;

            if (cardContainer == null)
            {
                cardContainer = CreateChild("CardRow", parent, new Vector2(1500, 370), Vector2.zero);
                AddContainerBackground(cardContainer);
            }
            if (potionContainer == null)
            {
                potionContainer = CreateChild("PotionRow", parent, new Vector2(400, 170), Vector2.zero);
                AddContainerBackground(potionContainer);
            }
            if (removalContainer == null)
            {
                removalContainer = CreateChild("RemovalRow", parent, new Vector2(440, 160), Vector2.zero);
                AddContainerBackground(removalContainer);
            }

            // 移除费用提示：场景未接线时在金币文本左侧运行时创建（TMP 字体与场景一致）
            if (removalInfoText == null)
            {
                var rt = CreateChild("RemovalInfo", parent, new Vector2(320, 40), Vector2.zero);
                rt.anchoredPosition = new Vector2(-270, 230);
                var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.font = TmpChineseFont;
                tmp.fontSize = 20;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
                tmp.color = new Color(0.85f, 0.8f, 0.7f);
                tmp.raycastTarget = false;
                removalInfoText = tmp;
            }

            // 遗物：顶部居中一排（StS 遗物区在商店顶部）
            PlaceHorizontalRow(relicContainer, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1100, 180));
            // 药水：右上角一排
            PlaceHorizontalRow(potionContainer, new Vector2(1f, 1f), new Vector2(-230, -160), new Vector2(400, 170));
            // 卡牌：中央一排
            PlaceHorizontalRow(cardContainer, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1500, 370));
            // 移除服务：左下角
            PlaceHorizontalRow(removalContainer, new Vector2(0f, 0f), new Vector2(240, 140), new Vector2(440, 160));

            // 离开按钮：右下角
            if (closeButton != null)
            {
                var rt = closeButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(1f, 0f);
                    rt.anchorMax = new Vector2(1f, 0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-160, 140);
                    rt.sizeDelta = new Vector2(240, 90);
                }
                SetSlotText(closeButton.transform, "Text (TMP)", "离开");
            }
        }

        private static void PlaceHorizontalRow(Transform container, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            if (container == null) return;
            var rt = container.GetComponent<RectTransform>();
            if (rt == null) return;

            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            // 清掉其它类型的布局组件，统一为横排
            foreach (var lg in container.GetComponents<LayoutGroup>())
            {
                if (!(lg is HorizontalLayoutGroup))
                    Destroy(lg);
            }

            var hlg = container.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14;
            hlg.padding = new RectOffset(10, 10, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
        }

        private static RectTransform CreateChild(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        private static Text AddText(Transform parent, string name, Vector2 size, Vector2 pos, int fontSize, Color color, bool bold = false)
        {
            var rt = CreateChild(name, parent, size, pos);
            var t = rt.gameObject.AddComponent<Text>();
            InitText(t, fontSize, TextAnchor.MiddleCenter, color, bold);
            return t;
        }

        private static void InitText(Text t, int size, TextAnchor anchor, Color color, bool bold = false)
        {
            t.font = ChineseFont;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.raycastTarget = false;
        }

        /// <summary>
        /// 给槽位子文本赋值（按子物体名查找）：同时兼容场景接线用的 TMP_Text
        /// 与运行时构建用的旧版 UnityEngine.UI.Text。
        /// </summary>
        private static void SetSlotText(Transform slotRoot, string childName, string value)
        {
            Transform child = slotRoot.Find(childName);
            if (child == null) return;

            TMP_Text tmp = child.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = value;
                return;
            }

            Text legacy = child.GetComponent<Text>();
            if (legacy != null)
                legacy.text = value;
        }

        /// <summary>给槽位子文本设置颜色（TMP 与旧版 Text 通用）。</summary>
        private static void SetSlotTextColor(Transform slotRoot, string childName, Color color)
        {
            Transform child = slotRoot.Find(childName);
            if (child == null) return;

            TMP_Text tmp = child.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.color = color;
                return;
            }

            Text legacy = child.GetComponent<Text>();
            if (legacy != null)
                legacy.color = color;
        }

        /// <summary>给容器补半透明深色背景（已有背景组件则跳过），统一四排观感。</summary>
        private static void AddContainerBackground(Transform container)
        {
            if (container == null) return;
            Image img = container.GetComponent<Image>();
            if (img == null)
            {
                img = container.gameObject.AddComponent<Image>();
                img.raycastTarget = false;
            }
            img.color = new Color(0.05f, 0.04f, 0.03f, 0.55f);
        }

        /// <summary>运行时构建卡牌槽位：插画 + 名称 + 描述 + 价格 + 购买按钮</summary>
        private GameObject BuildCardSlotPrefab()
        {
            var root = new GameObject("CardSlot", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(160, 350);
            root.GetComponent<Image>().color = new Color(0.14f, 0.12f, 0.09f, 0.95f);

            var icon = CreateChild("Icon", root.transform, new Vector2(140, 175), new Vector2(0, 66));
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            AddText(root.transform, "Name", new Vector2(140, 30), new Vector2(0, -38), 16, new Color(0.96f, 0.91f, 0.8f), true);
            AddText(root.transform, "Description", new Vector2(140, 68), new Vector2(0, -87), 12, new Color(0.8f, 0.76f, 0.68f));
            AddText(root.transform, "Price", new Vector2(140, 24), new Vector2(0, -123), 16, new Color(1f, 0.83f, 0.35f));

            var buy = CreateChild("BuyButton", root.transform, new Vector2(130, 34), new Vector2(0, -150));
            buy.gameObject.AddComponent<Image>().color = new Color(0.55f, 0.3f, 0.22f, 1f);
            buy.gameObject.AddComponent<Button>();
            var label = CreateChild("Label", buy, new Vector2(130, 34), Vector2.zero);
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.sizeDelta = Vector2.zero;
            var lt = label.gameObject.AddComponent<Text>();
            InitText(lt, 15, TextAnchor.MiddleCenter, Color.white, true);
            lt.text = "购买";

            return root;
        }

        /// <summary>运行时构建药水槽位：图标 + 名称 + 价格 + 购买按钮</summary>
        private GameObject BuildPotionSlotPrefab()
        {
            var root = new GameObject("PotionSlot", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(110, 160);
            root.GetComponent<Image>().color = new Color(0.12f, 0.15f, 0.12f, 0.95f);

            var icon = CreateChild("Icon", root.transform, new Vector2(84, 84), new Vector2(0, 26));
            var iconImg = icon.gameObject.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            AddText(root.transform, "Name", new Vector2(100, 24), new Vector2(0, -30), 12, new Color(0.85f, 0.95f, 0.85f), true);
            AddText(root.transform, "Price", new Vector2(100, 20), new Vector2(0, -44), 14, new Color(1f, 0.83f, 0.35f));

            var buy = CreateChild("BuyButton", root.transform, new Vector2(100, 32), new Vector2(0, -62));
            buy.gameObject.AddComponent<Image>().color = new Color(0.3f, 0.5f, 0.3f, 1f);
            buy.gameObject.AddComponent<Button>();
            var label = CreateChild("Label", buy, new Vector2(100, 32), Vector2.zero);
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.sizeDelta = Vector2.zero;
            var lt = label.gameObject.AddComponent<Text>();
            InitText(lt, 14, TextAnchor.MiddleCenter, Color.white, true);
            lt.text = "购买";

            return root;
        }

        /// <summary>运行时构建移除服务槽位：说明 + 价格 + 选择按钮</summary>
        private GameObject BuildRemovalSlotPrefab()
        {
            var root = new GameObject("RemovalSlot", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(220, 140);
            root.GetComponent<Image>().color = new Color(0.15f, 0.13f, 0.18f, 0.95f);

            AddText(root.transform, "Description", new Vector2(200, 40), new Vector2(0, 20), 14, new Color(0.85f, 0.82f, 0.9f));
            AddText(root.transform, "Price", new Vector2(200, 24), new Vector2(0, -16), 16, new Color(1f, 0.83f, 0.35f));

            var buy = CreateChild("BuyButton", root.transform, new Vector2(140, 36), new Vector2(0, -52));
            buy.gameObject.AddComponent<Image>().color = new Color(0.45f, 0.3f, 0.25f, 1f);
            buy.gameObject.AddComponent<Button>();
            var label = CreateChild("Label", buy, new Vector2(140, 36), Vector2.zero);
            label.anchorMin = Vector2.zero;
            label.anchorMax = Vector2.one;
            label.sizeDelta = Vector2.zero;
            var lt = label.gameObject.AddComponent<Text>();
            InitText(lt, 15, TextAnchor.MiddleCenter, Color.white, true);
            lt.text = "选择卡牌";

            return root;
        }

        #endregion
    }
}
