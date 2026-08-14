using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;
using TMPro;

namespace MutationChess.UI
{
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

        [Header("槽位预制体")]
        [SerializeField] private GameObject cardSlotPrefab;
        [SerializeField] private GameObject relicSlotPrefab;
        [SerializeField] private GameObject potionSlotPrefab;
        [SerializeField] private GameObject removalSlotPrefab;

        [Header("移除服务")]
        [SerializeField] private Transform removalContainer;

        private List<ShopItem> shopItems = new List<ShopItem>();
        private System.Action onShopClosed;
        private CanvasGroup canvasGroup;

        private ShopDataService shopDataService;
        private PlayerDataManager playerDataManager;
        private RelicManager relicManager;

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

            if (titleText != null)
                titleText.text = "";
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
                    return cardSlotPrefab != null ? cardSlotPrefab : relicSlotPrefab;
                case ShopItemType.Potion:
                    return potionSlotPrefab != null ? potionSlotPrefab : relicSlotPrefab;
                case ShopItemType.CardRemoval:
                    return removalSlotPrefab != null ? removalSlotPrefab : relicSlotPrefab;
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

            TMP_Text nameText = slotObj.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = card.cardName;

            TMP_Text descText = slotObj.transform.Find("Description")?.GetComponent<TMP_Text>();
            if (descText != null)
                descText.text = card.GetDescription();

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

            TMP_Text nameText = slotObj.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = $"{relic.relicName} ({relic.GetRarityName()})";

            TMP_Text descText = slotObj.transform.Find("Description")?.GetComponent<TMP_Text>();
            if (descText != null)
                descText.text = relic.description;

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

            TMP_Text nameText = slotObj.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = $"{potion.potionName} ({potion.GetRarityName()})";

            TMP_Text descText = slotObj.transform.Find("Description")?.GetComponent<TMP_Text>();
            if (descText != null)
                descText.text = potion.description;

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

            TMP_Text descText = slotObj.transform.Find("Description")?.GetComponent<TMP_Text>();
            if (descText != null)
                descText.text = "从牌组中永久移除一张卡牌";
        }

        private void SetupSlotBase(GameObject slotObj, ShopItem item, string slotName)
        {
            slotObj.name = slotName;

            TMP_Text priceText = slotObj.transform.Find("Price")?.GetComponent<TMP_Text>();
            if (priceText != null)
            {
                string priceStr = item.isOnSale
                    ? $"<color=yellow>{item.finalPrice} G</color> <color=#888>( {item.basePrice} G)</color>"
                    : $"{item.finalPrice} G";
                priceText.text = priceStr;
            }

            Button buyButton = slotObj.transform.Find("BuyButton")?.GetComponent<Button>();
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(() => TryPurchaseItem(item, slotObj));
            }
        }

        private void TryPurchaseItem(ShopItem item, GameObject slotObj)
        {
            if (item.isSold) return;

            if (playerDataManager == null)
            {
                GameLogger.LogError("[ShopPanel] PlayerDataManager 未找到");
                return;
            }

            if (!playerDataManager.RemoveGold(item.finalPrice))
            {
                GameLogger.Log($"[ShopPanel] 金币不足：需要 {item.finalPrice}，当前 {playerDataManager.GetGold()}");
                return;
            }

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

                case ShopItemType.CardRemoval:
                    OpenCardRemovalDialog();
                    if (shopDataService != null)
                        shopDataService.OnRemovalPurchased();
                    break;
            }

            if (shopDataService != null)
                shopDataService.OnItemPurchased(item);

            bool hasRestockTalisman = relicManager != null && relicManager.HasRelic(RelicIds.Shop_RestockTalisman);
            if (!hasRestockTalisman)
            {
                item.isSold = true;

                Button buyButton = slotObj.transform.Find("BuyButton")?.GetComponent<Button>();
                if (buyButton != null)
                    buyButton.interactable = false;

                TMP_Text priceText = slotObj.transform.Find("Price")?.GetComponent<TMP_Text>();
                if (priceText != null)
                    priceText.text = "<color=#888>已售出</color>";
            }
            else
            {
                GameLogger.Log("[ShopPanel] 补货符生效，商品未标记为已售出");
            }

            UpdateGoldDisplay();
            UpdateRemovalInfo();
        }

        private void OpenCardRemovalDialog()
        {
            var deck = playerDataManager?.GetRuntimeDeckRef();
            if (deck == null || deck.Count == 0) return;

            GameLogger.Log("[ShopPanel] 开启卡牌移除 - 暂未实现完整UI选择");

            if (deck.Count > 0)
            {
                int removeIndex = 0;
                Card cardToRemove = deck[removeIndex];
                playerDataManager.RemoveCardFromDeck(cardToRemove);
                GameLogger.Log($"[ShopPanel] 移除卡牌：{cardToRemove.cardName}");
            }
        }

        private void ClearAllContainers()
        {
            ClearContainer(cardContainer);
            ClearContainer(relicContainer);
            ClearContainer(potionContainer);
            ClearContainer(removalContainer);
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

        public void CloseShop()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            ClearAllContainers();
            shopItems.Clear();
            onShopClosed?.Invoke();
        }

        public bool IsShopOpen()
        {
            return panelRoot != null && panelRoot.activeSelf;
        }
    }
}

