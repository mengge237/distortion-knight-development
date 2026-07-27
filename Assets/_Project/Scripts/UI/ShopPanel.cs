using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using MutationChess.Core;
using TMPro;

namespace MutationChess.UI
{
    public class ShopPanel : MonoBehaviour
    {
        [Header("=== 面板 ===")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button closeButton;

        [Header("=== 文本 ===")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text goldText;

        [Header("=== 遗物内容（运行时生成） + 模板 ===")]
        [SerializeField] private Transform relicContainer;
        [SerializeField] private GameObject relicSlotPrefab;
        [SerializeField] private float slotSpacing = 30f;

        private List<Relic> shopRelics = new List<Relic>();
        private System.Action onShopClosed;
        private CanvasGroup canvasGroup;

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
            Debug.Log("[ShopPanel] === OpenShop 开始 ===");
            onShopClosed = onClosed;

            // 确保 ShopPanel 已激活
            if (!gameObject.activeSelf)
            {
                Debug.Log("[ShopPanel] ShopPanel 未激活，正在激活...");
                gameObject.SetActive(true);
            }

            // 获取商店遗物
            var relicManager = RelicManager.Instance;
            if (relicManager != null)
            {
                Debug.Log("[ShopPanel] 调用 RelicManager 生成遗物...");
                shopRelics = relicManager.GenerateShopRelics(4);
                Debug.Log($"[ShopPanel] 已生成 {shopRelics.Count} 个遗物");
            }
            else
            {
                shopRelics = new List<Relic>();
                Debug.LogWarning("[ShopPanel] RelicManager.Instance 为 null，请在场景中放置 RelicManager 对象");
            }

            // 填充插槽
            PopulateSlots();

            // 更新金币显示
            UpdateGoldDisplay();

            // 显示面板 + 置顶
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                panelRoot.transform.SetAsLastSibling();
                Debug.Log($"[ShopPanel] panelRoot 激活状态: {panelRoot.name} | activeSelf={panelRoot.activeSelf}");
            }
            else
            {
                Debug.LogError("[ShopPanel] panelRoot 为 null！请在 Inspector 中拖入 Panel Root 对象");
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            if (titleText != null)
                titleText.text = "商店";
            else
                Debug.LogWarning("[ShopPanel] titleText 未设置");

            Debug.Log("[ShopPanel] === OpenShop 完成 ===");
        }

        private void PopulateSlots()
        {
            if (relicContainer == null)
            {
                Debug.LogWarning("[ShopPanel] relicContainer 未设置，跳过插槽生成");
                return;
            }

            if (relicSlotPrefab == null)
            {
                Debug.LogWarning("[ShopPanel] relicSlotPrefab 未设置，跳过插槽生成");
                return;
            }

            // 清空旧插槽
            foreach (Transform child in relicContainer)
            {
                Destroy(child.gameObject);
            }

            if (shopRelics.Count == 0)
            {
                Debug.Log("[ShopPanel] 没有遗物可显示（可能 Resources/Relics/ 中没有 .asset 文件）");
                return;
            }

            // 水平排列，使用 prefab 原始宽度 + 额外间距
            float slotWidth = relicSlotPrefab.GetComponent<RectTransform>()?.sizeDelta.x ?? 200f;
            float step = slotWidth + slotSpacing;
            float startX = -(shopRelics.Count - 1) * step / 2f;

            for (int i = 0; i < shopRelics.Count; i++)
            {
                Relic relic = shopRelics[i];
                if (relic == null) continue;

                GameObject slotObj = Instantiate(relicSlotPrefab, relicContainer);
                slotObj.name = $"Slot_{relic.relicName}";
                RectTransform slotRect = slotObj.GetComponent<RectTransform>();
                if (slotRect != null)
                {
                    slotRect.anchoredPosition = new Vector2(startX + i * step, 0);
                }

                // 设置背景色（稀有度对应）
                Image bgImage = slotObj.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = GetRarityBackgroundColor(relic.rarity);
                }

                // 设置图标
                Image iconImage = slotObj.transform.Find("Icon")?.GetComponent<Image>();
                if (iconImage != null)
                {
                    if (relic.icon != null)
                    {
                        iconImage.sprite = relic.icon;
                        iconImage.color = Color.white;
                    }
                    else
                    {
                        iconImage.sprite = null;
                        iconImage.color = GetRarityIconColor(relic.rarity);
                        Debug.Log($"[ShopPanel] 遗物 {relic.relicName} 没有图标，使用纯色占位");
                    }
                }

                // 设置遗物名称
                TMP_Text nameText = slotObj.transform.Find("Name")?.GetComponent<TMP_Text>();
                if (nameText != null)
                    nameText.text = $"<color=#FFF>{relic.relicName}</color> <color=#AAA>({relic.GetRarityName()})</color>";

                // 设置遗物描述
                TMP_Text descText = slotObj.transform.Find("Description")?.GetComponent<TMP_Text>();
                if (descText != null)
                    descText.text = relic.description;

                // 设置价格和购买按钮
                TMP_Text priceText = slotObj.transform.Find("Price")?.GetComponent<TMP_Text>();
                if (priceText != null)
                    priceText.text = $"售价: {relic.price} G";

                Button buyButton = slotObj.transform.Find("BuyButton")?.GetComponent<Button>();
                if (buyButton != null)
                {
                    int index = i;
                    buyButton.onClick.RemoveAllListeners();
                    buyButton.onClick.AddListener(() => BuyRelic(index));
                }
            }
        }

        private Color GetRarityBackgroundColor(RelicRarity rarity)
        {
            switch (rarity)
            {
                case RelicRarity.Common:    return new Color(0.25f, 0.25f, 0.3f, 0.9f);
                case RelicRarity.Rare:      return new Color(0.2f, 0.25f, 0.4f, 0.9f);
                case RelicRarity.Legendary: return new Color(0.4f, 0.25f, 0.1f, 0.9f);
                case RelicRarity.Mythic:    return new Color(0.35f, 0.1f, 0.15f, 0.9f);
                case RelicRarity.Special:   return new Color(0.3f, 0.1f, 0.35f, 0.9f);
                default:                    return new Color(0.2f, 0.2f, 0.2f, 0.9f);
            }
        }

        private Color GetRarityIconColor(RelicRarity rarity)
        {
            switch (rarity)
            {
                case RelicRarity.Common:    return new Color(0.6f, 0.6f, 0.65f);
                case RelicRarity.Rare:      return new Color(0.3f, 0.5f, 0.9f);
                case RelicRarity.Legendary: return new Color(1f, 0.65f, 0.2f);
                case RelicRarity.Mythic:    return new Color(0.9f, 0.2f, 0.3f);
                case RelicRarity.Special:   return new Color(0.7f, 0.3f, 0.8f);
                default:                    return new Color(0.5f, 0.5f, 0.5f);
            }
        }

        private void BuyRelic(int index)
        {
            if (index < 0 || index >= shopRelics.Count) return;

            Relic relic = shopRelics[index];
            if (relic == null) return;

            var dataManager = PlayerDataManager.Instance;
            if (dataManager == null)
            {
                Debug.LogError("[ShopPanel] PlayerDataManager 未找到");
                return;
            }

            if (!dataManager.RemoveGold(relic.price))
            {
                Debug.Log($"[ShopPanel] 尝试购买遗物 价格={relic.price} 当前金币={dataManager.GetGold()}");
                return;
            }

            var relicManager = RelicManager.Instance;
            if (relicManager != null)
                relicManager.AddRelic(relic);

            // 更新已购买插槽样式
            shopRelics[index] = null;
            Transform slot = relicContainer.GetChild(index);
            if (slot != null)
            {
                Button buyButton = slot.Find("BuyButton")?.GetComponent<Button>();
                if (buyButton != null)
                    buyButton.interactable = false;

                TMP_Text priceText = slot.Find("Price")?.GetComponent<TMP_Text>();
                if (priceText != null)
                    priceText.text = "<color=#888>已售出</color>";
            }

            UpdateGoldDisplay();
            Debug.Log($"[ShopPanel] 购买遗物成功: {relic.relicName}");
        }

        private void UpdateGoldDisplay()
        {
            var dataManager = PlayerDataManager.Instance;
            if (goldText != null && dataManager != null)
                goldText.text = $"金币: {dataManager.GetGold()} G";
        }

        public void CloseShop()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (relicContainer != null)
            {
                foreach (Transform child in relicContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            shopRelics.Clear();
            onShopClosed?.Invoke();
            Debug.Log("[ShopPanel] 关闭商店");
        }

        public bool IsShopOpen()
        {
            return panelRoot != null && panelRoot.activeSelf;
        }
    }
}
