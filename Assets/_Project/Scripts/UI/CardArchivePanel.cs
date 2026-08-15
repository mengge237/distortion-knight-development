using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;

namespace MutationChess.UI
{
    /// <summary>
    /// 牌库档案面板：图鉴（按阵营分组的全部卡牌+拥有/强化标记）、卡组（运行时牌组统计）、
    /// 弃牌堆（本场战斗弃牌堆+消耗堆）。战斗中点击"抽牌/弃牌"计数或按 F2 打开，
    /// 点击卡牌弹出大卡预览，可切换"升级前后对比"查看绿色数值增量（翻面动画）。
    /// 场景可接线（panelRoot/openButton），缺失时运行时自动构建，视觉复用获胜奖励图集。
    /// </summary>
    public class CardArchivePanel : MonoBehaviour
    {
        private static CardArchivePanel _instance;
        public static CardArchivePanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CardArchivePanel>();
                    if (_instance == null)
                    {
                        var go = new GameObject("CardArchivePanel");
                        _instance = go.AddComponent<CardArchivePanel>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>面板当前是否可见（供 SettingsManager 判断 ESC 归属，不触发自动创建）。</summary>
        public static bool IsAnyVisible { get; private set; }

        [Header("场景接线（可选，缺失时运行时自动构建）")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;

        public enum ArchiveTab { Codex, Deck, Discard }

        private const int CanvasOrder = 700;

        private Canvas canvas;
        private ArchiveTab currentTab = ArchiveTab.Codex;
        private RectTransform listContent;    // 滚动列表内容根
        private GameObject previewPanel;      // 右侧大卡预览
        private Button upgradeToggleBtn;
        private TMP_Text upgradeToggleLabel;
        private Button[] tabButtons;
        private TMP_Text[] tabLabels;

        private CardDataAsset currentAsset;
        private Card previewBaseCard;
        private Card previewUpgradedCard;
        private bool showingUpgraded = false;
        private RectTransform previewCardArea; // 翻面动画对象
        private TMP_Text previewDescText;
        private TMP_Text previewDeltaText;

        private float timeScaleBeforeOpen = 1f;
        private static TMP_FontAsset cachedFont;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        void Start()
        {
            if (openButton != null)
                openButton.onClick.AddListener(() => Open(ArchiveTab.Codex));
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2) &&
                (SettingsManager.Instance == null || !SettingsManager.Instance.IsSettingsOpen()))
            {
                if (IsVisible) Close();
                else Open(ArchiveTab.Codex);
            }
            else if (Input.GetKeyDown(KeyCode.Escape) && IsVisible)
            {
                Close();
            }
        }

        /// <summary>打开面板并切换到指定标签页（暂停游戏，关闭时恢复）。</summary>
        public void Open(ArchiveTab tab)
        {
            if (panelRoot == null) BuildPanel();
            if (panelRoot == null) return;

            panelRoot.SetActive(true);
            if (canvas != null)
            {
                canvas.enabled = false;
                canvas.enabled = true;
            }

            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f;

            SwitchTab(tab);
            UiFeel.AnimatePanelIn(panelRoot);
            IsAnyVisible = true;
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (canvas != null) canvas.enabled = false;
            Time.timeScale = timeScaleBeforeOpen;
            IsAnyVisible = false;
        }

        /// <summary>静态入口：战斗中点击"抽牌/弃牌"计数时调用。</summary>
        public static void OpenTab(ArchiveTab tab) => Instance.Open(tab);

        private void SwitchTab(ArchiveTab tab)
        {
            currentTab = tab;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                bool selected = (int)tab == i;
                tabButtons[i].image.color = selected
                    ? new Color(0.85f, 0.72f, 0.35f, 1f)
                    : new Color(0.2f, 0.18f, 0.15f, 1f);
                tabLabels[i].color = selected
                    ? new Color(0.15f, 0.12f, 0.06f, 1f)
                    : new Color(0.85f, 0.85f, 0.8f, 1f);
            }
            HidePreview();
            RefreshList();
        }

        private void RefreshList()
        {
            for (int i = listContent.childCount - 1; i >= 0; i--)
                Destroy(listContent.GetChild(i).gameObject);

            switch (currentTab)
            {
                case ArchiveTab.Codex: BuildCodexList(); break;
                case ArchiveTab.Deck: BuildDeckList(); break;
                case ArchiveTab.Discard: BuildDiscardList(); break;
            }
        }

        #region 三个标签页

        /// <summary>图鉴：全部卡牌资产，按阵营分组，附带拥有/强化标记。</summary>
        private void BuildCodexList()
        {
            var assets = CardData.GetAllCardAssets();
            var dm = PlayerDataManager.Instance;
            var deck = dm != null ? dm.GetRuntimeDeckCopy() : null;
            var owned = new Dictionary<string, (int count, bool upgraded)>();
            if (deck != null)
            {
                foreach (var c in deck)
                {
                    if (c == null) continue;
                    owned.TryGetValue(c.cardName, out var v);
                    owned[c.cardName] = (v.count + 1, v.upgraded || c.isUpgraded);
                }
            }

            CardFaction lastFaction = CardFaction.None;
            bool first = true;
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                if (first || asset.faction != lastFaction)
                {
                    CreateHeaderRow(GetFactionDisplayName(asset.faction), GetFactionColor(asset.faction));
                    first = false;
                    lastFaction = asset.faction;
                }

                owned.TryGetValue(asset.cardName, out var o);
                string ownedMark = o.count > 0 ? $"  <color=#7FFF7F>✓拥有 x{o.count}</color>" : "";
                if (o.upgraded) ownedMark += " <color=#FFD24D>★强化</color>";

                string main = $"<color=#{ColorUtility.ToHtmlStringRGB(CardVisualConfig.GetRarityColor(asset.rarity))}>{asset.cardName}</color>"
                    + $"  <color=#CCCCCC>{GetCardTypeName(asset.cardType)} · 费用 {asset.cost}</color>{ownedMark}";
                CreateCardRow(asset, main);
            }
        }

        /// <summary>卡组：运行时牌组按卡名聚合（数量+强化标记）。</summary>
        private void BuildDeckList()
        {
            var dm = PlayerDataManager.Instance;
            var deck = dm != null ? dm.GetRuntimeDeckCopy() : null;
            if (deck == null || deck.Count == 0)
            {
                CreateEmptyHint("当前牌组为空（进入战斗后自动构建牌组）");
                return;
            }

            CreateHeaderRow("当前牌组", new Color(0.9f, 0.85f, 0.6f));

            var groups = GroupByName(deck);
            var names = new List<string>(groups.Keys);
            names.Sort();
            foreach (var name in names)
            {
                var g = groups[name];
                var asset = CardData.GetAssetByName(name);
                string star = g.upgraded ? " <color=#FFD24D>★</color>" : "";
                string main = asset != null
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(CardVisualConfig.GetRarityColor(asset.rarity))}>{name}</color>"
                        + $"  <color=#7FFF7F>x{g.count}</color>{star}"
                        + $"  <color=#CCCCCC>{GetCardTypeName(asset.cardType)} · 费用 {asset.cost}</color>"
                    : $"<color=#EEEEEE>{name}</color>  <color=#7FFF7F>x{g.count}</color>{star}";
                CreateCardRow(asset, main);
            }
        }

        /// <summary>弃牌堆：本场战斗弃牌堆+消耗堆（战斗外提示不可用）。</summary>
        private void BuildDiscardList()
        {
            var hm = HandManager.Instance;
            if (hm == null)
            {
                CreateEmptyHint("战斗外无法查看弃牌堆");
                return;
            }

            CreateHeaderRow("弃牌堆", new Color(0.9f, 0.8f, 0.65f));
            BuildPileGrouped(hm.GetDiscardPile());
            CreateHeaderRow("消耗堆", new Color(0.75f, 0.6f, 0.5f));
            BuildPileGrouped(hm.GetExhaustPile());
        }

        private void BuildPileGrouped(List<Card> pile)
        {
            if (pile == null || pile.Count == 0)
            {
                CreateEmptyHint("（空）");
                return;
            }

            var groups = GroupByName(pile);
            var names = new List<string>(groups.Keys);
            names.Sort();
            foreach (var name in names)
            {
                var g = groups[name];
                var asset = CardData.GetAssetByName(name);
                string star = g.upgraded ? " <color=#FFD24D>★</color>" : "";
                string main = asset != null
                    ? $"<color=#{ColorUtility.ToHtmlStringRGB(CardVisualConfig.GetRarityColor(asset.rarity))}>{name}</color>"
                        + $"  <color=#7FFF7F>x{g.count}</color>{star}  <color=#CCCCCC>{GetCardTypeName(asset.cardType)}</color>"
                    : $"<color=#EEEEEE>{name}</color>  <color=#7FFF7F>x{g.count}</color>{star}";
                CreateCardRow(asset, main);
            }
        }

        private static Dictionary<string, (int count, bool upgraded)> GroupByName(List<Card> pile)
        {
            var groups = new Dictionary<string, (int count, bool upgraded)>();
            foreach (var c in pile)
            {
                if (c == null) continue;
                groups.TryGetValue(c.cardName, out var v);
                groups[c.cardName] = (v.count + 1, v.upgraded || c.isUpgraded);
            }
            return groups;
        }

        #endregion

        #region 列表行构建

        private void CreateHeaderRow(string title, Color color)
        {
            var go = new GameObject("Header_" + title, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(listContent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 42f;
            var tmp = CreateText(go.transform, "Label", 25, TextAlignmentOptions.MidlineLeft, color);
            var rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(14f, 0f);
            rt.offsetMax = new Vector2(-14f, 0f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.text = $"◆ {title}";
        }

        private void CreateEmptyHint(string text)
        {
            var go = new GameObject("Hint", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(listContent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 60f;
            var tmp = CreateText(go.transform, "Label", 22, TextAlignmentOptions.MidlineLeft, new Color(0.65f, 0.62f, 0.55f));
            var rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(18f, 0f);
            rt.offsetMax = new Vector2(-18f, 0f);
            tmp.text = text;
        }

        private void CreateCardRow(CardDataAsset asset, string richText)
        {
            var rowGo = new GameObject("Row_" + (asset != null ? asset.cardName : "?"),
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            rowGo.transform.SetParent(listContent, false);
            rowGo.GetComponent<LayoutElement>().preferredHeight = 46f;
            var img = rowGo.GetComponent<Image>();
            img.color = new Color(0.13f, 0.12f, 0.1f, 0.85f);

            var tmp = CreateText(rowGo.transform, "Label", 24, TextAlignmentOptions.MidlineLeft, Color.white);
            var rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(18f, 0f);
            rt.offsetMax = new Vector2(-18f, 0f);
            tmp.text = richText;

            var btn = rowGo.GetComponent<Button>();
            btn.targetGraphic = img;
            UiFeel.ApplyButton(btn);
            var captured = asset;
            btn.onClick.AddListener(() => ShowPreview(captured));
        }

        #endregion

        #region 大卡预览与升级前后对比

        private void ShowPreview(CardDataAsset asset)
        {
            if (asset == null) return;
            currentAsset = asset;
            previewBaseCard = CardData.CreateCardFromAsset(asset);
            previewUpgradedCard = CardData.CreateCardFromAsset(asset);
            if (previewUpgradedCard != null) previewUpgradedCard.Upgrade();

            showingUpgraded = false;
            previewPanel.SetActive(true);
            UiFeel.AnimatePanelIn(previewPanel);
            RebuildPreviewVisual();
        }

        private void HidePreview()
        {
            if (previewPanel != null)
                previewPanel.SetActive(false);
            currentAsset = null;
        }

        /// <summary>切换升级前后视图（卡面翻面动画，暂停时用 unscaled 时间）。</summary>
        private void ToggleUpgradePreview()
        {
            if (currentAsset == null || previewBaseCard == null) return;
            if (previewBaseCard.cardType == CardType.Curse) return; // 诅咒卡无法升级

            showingUpgraded = !showingUpgraded;
            AudioManager.Instance?.PlayUIClick(0.35f);

            DOTween.Kill(previewCardArea, true);
            DOTween.Sequence()
                .Append(previewCardArea.DOScaleX(0.01f, 0.12f).SetEase(Ease.InQuad).SetUpdate(true))
                .AppendCallback(RebuildPreviewVisual)
                .Join(previewCardArea.DOScaleX(1f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true));
        }

        private void RebuildPreviewVisual()
        {
            Card shown = showingUpgraded ? previewUpgradedCard : previewBaseCard;
            if (shown == null) return;

            for (int i = previewCardArea.childCount - 1; i >= 0; i--)
                Destroy(previewCardArea.GetChild(i).gameObject);

            Color rarity = CardVisualConfig.GetRarityColor(shown.rarity);

            // 卡面底色（稀有度色调）
            var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(previewCardArea, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            StretchFull(bgRt);
            bgGo.GetComponent<Image>().color = rarity * 0.4f + new Color(0.06f, 0.06f, 0.09f);

            // 卡图（有则显示）
            if (shown.cardArt != null)
            {
                var artGo = new GameObject("Art", typeof(RectTransform), typeof(Image));
                artGo.transform.SetParent(previewCardArea, false);
                var artRt = artGo.GetComponent<RectTransform>();
                artRt.anchorMin = new Vector2(0.1f, 0.24f);
                artRt.anchorMax = new Vector2(0.9f, 0.84f);
                artRt.offsetMin = Vector2.zero;
                artRt.offsetMax = Vector2.zero;
                var artImg = artGo.GetComponent<Image>();
                artImg.sprite = shown.cardArt;
                artImg.preserveAspect = true;
                artImg.color = Color.white;
            }

            // 费用
            var costTmp = CreateText(previewCardArea, "Cost", 26, TextAlignmentOptions.TopLeft, new Color(1f, 0.9f, 0.5f));
            var costRt = costTmp.rectTransform;
            costRt.anchorMin = costRt.anchorMax = new Vector2(0f, 1f);
            costRt.pivot = new Vector2(0f, 1f);
            costRt.anchoredPosition = new Vector2(14f, -10f);
            costRt.sizeDelta = new Vector2(150f, 40f);
            costTmp.text = $"费用 {shown.cost}";

            // 名称
            var nameTmp = CreateText(previewCardArea, "Name", 32, TextAlignmentOptions.Top, rarity);
            var nameRt = nameTmp.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -14f);
            nameRt.sizeDelta = new Vector2(270f, 46f);
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.text = shown.cardName;

            // 元信息（类型 · 阵营 · 稀有度）
            var metaTmp = CreateText(previewCardArea, "Meta", 21, TextAlignmentOptions.Bottom, new Color(0.78f, 0.78f, 0.82f));
            var metaRt = metaTmp.rectTransform;
            metaRt.anchorMin = metaRt.anchorMax = new Vector2(0.5f, 0f);
            metaRt.pivot = new Vector2(0.5f, 0f);
            metaRt.anchoredPosition = new Vector2(0f, 62f);
            metaRt.sizeDelta = new Vector2(280f, 34f);
            metaTmp.text = $"{GetCardTypeName(shown.cardType)} · {shown.GetFactionName()} · {shown.GetRarityName()}";

            // 属性行
            string stats = shown.cardType == CardType.Attack ? $"伤害 {shown.damage}"
                : shown.cardType == CardType.Defense ? $"格挡 {shown.block}"
                : shown.cardType == CardType.Curse ? "诅咒"
                : shown.magicNumber > 0 ? $"效果值 {shown.magicNumber}"
                : "";
            var statsTmp = CreateText(previewCardArea, "Stats", 25, TextAlignmentOptions.Bottom, new Color(0.95f, 0.95f, 1f));
            var statsRt = statsTmp.rectTransform;
            statsRt.anchorMin = statsRt.anchorMax = new Vector2(0.5f, 0f);
            statsRt.pivot = new Vector2(0.5f, 0f);
            statsRt.anchoredPosition = new Vector2(0f, 22f);
            statsRt.sizeDelta = new Vector2(280f, 38f);
            statsTmp.text = stats;

            // 描述与升级增量
            previewDescText.text = shown.description;
            string delta = showingUpgraded ? BuildUpgradeDelta() : "";
            previewDeltaText.text = delta;
            previewDeltaText.color = showingUpgraded
                ? new Color(0.45f, 1f, 0.55f, 1f)
                : new Color(0f, 0f, 0f, 0f);

            // 升级对比按钮（诅咒卡隐藏）
            bool canUpgrade = previewBaseCard != null && previewBaseCard.cardType != CardType.Curse;
            upgradeToggleBtn.gameObject.SetActive(canUpgrade);
            upgradeToggleLabel.text = showingUpgraded ? "← 查看升级前" : "查看升级后 →";
        }

        /// <summary>升级前后数值增量（绿色行，供对比视图展示）。</summary>
        private string BuildUpgradeDelta()
        {
            var b = previewBaseCard;
            var u = previewUpgradedCard;
            if (b == null || u == null) return "";

            var parts = new List<string>();
            if (b.cost != u.cost)
                parts.Add($"费用 {b.cost} → {u.cost}（{(u.cost < b.cost ? "-" : "+")}{Mathf.Abs(u.cost - b.cost)}）");
            if (b.damage != u.damage)
                parts.Add($"伤害 {b.damage} → {u.damage}（+{u.damage - b.damage}）");
            if (b.block != u.block)
                parts.Add($"格挡 {b.block} → {u.block}（+{u.block - b.block}）");
            if (b.magicNumber != u.magicNumber)
                parts.Add($"效果值 {b.magicNumber} → {u.magicNumber}（{(u.magicNumber > b.magicNumber ? "+" : "")}{u.magicNumber - b.magicNumber}）");

            if (parts.Count == 0) return "该卡牌升级无数值变化";
            return "升级变化：" + string.Join(" · ", parts);
        }

        #endregion

        #region 运行时构建

        private void BuildPanel()
        {
            if (panelRoot != null)
            {
                if (tabButtons != null) return;
                Destroy(panelRoot); // 场景接线不完整：清空重建
                panelRoot = null;
            }

            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasOrder;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            var rootGo = new GameObject("PanelRoot", typeof(RectTransform));
            rootGo.transform.SetParent(transform, false);
            StretchFull(rootGo.GetComponent<RectTransform>());
            panelRoot = rootGo;
            panelRoot.SetActive(false);

            // 全屏遮罩
            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image));
            maskGo.transform.SetParent(panelRoot.transform, false);
            StretchFull(maskGo.GetComponent<RectTransform>());
            maskGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            // 中央面板
            var centerGo = new GameObject("CenterPanel", typeof(RectTransform), typeof(Image));
            centerGo.transform.SetParent(panelRoot.transform, false);
            var centerRt = centerGo.GetComponent<RectTransform>();
            centerRt.anchorMin = centerRt.anchorMax = new Vector2(0.5f, 0.5f);
            centerRt.pivot = new Vector2(0.5f, 0.5f);
            centerRt.sizeDelta = new Vector2(1600f, 860f);
            var centerImg = centerGo.GetComponent<Image>();
            Sprite innerBg = LoadUISprite("获胜奖励面板底层内嵌背景");
            if (innerBg != null)
            {
                centerImg.sprite = innerBg;
                centerImg.color = Color.white;
            }
            else centerImg.color = new Color(0.07f, 0.07f, 0.11f, 0.98f);

            // 标题栏
            var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleBar.transform.SetParent(centerGo.transform, false);
            var titleBarRt = titleBar.GetComponent<RectTransform>();
            titleBarRt.anchorMin = titleBarRt.anchorMax = new Vector2(0.5f, 1f);
            titleBarRt.pivot = new Vector2(0.5f, 1f);
            titleBarRt.anchoredPosition = new Vector2(0f, -24f);
            titleBarRt.sizeDelta = new Vector2(900f, 86f);
            var titleBarImg = titleBar.GetComponent<Image>();
            Sprite titleBg = LoadUISprite("获胜奖励标题背景图");
            if (titleBg != null)
            {
                titleBarImg.sprite = titleBg;
                titleBarImg.color = Color.white;
            }
            else titleBarImg.color = new Color(0.12f, 0.08f, 0.05f, 0.9f);

            var titleTmp = CreateText(titleBar.transform, "Title", 42, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.35f));
            StretchFull(titleTmp.rectTransform);
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.text = "牌库档案";

            // 关闭按钮
            var closeBtnGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnGo.transform.SetParent(centerGo.transform, false);
            var closeRt = closeBtnGo.GetComponent<RectTransform>();
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-22f, -22f);
            closeRt.sizeDelta = new Vector2(160f, 48f);
            closeBtnGo.GetComponent<Image>().color = new Color(0.32f, 0.16f, 0.12f, 0.95f);
            var closeTmp = CreateText(closeBtnGo.transform, "Label", 24, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.8f));
            StretchFull(closeTmp.rectTransform);
            closeTmp.text = "✕ 关闭";
            var closeBtn = closeBtnGo.GetComponent<Button>();
            closeBtn.targetGraphic = closeBtnGo.GetComponent<Image>();
            closeBtn.onClick.AddListener(Close);

            // 标签页按钮（图鉴 / 卡组 / 弃牌堆）
            string[] tabNames = { "图鉴", "卡组", "弃牌堆" };
            tabButtons = new Button[tabNames.Length];
            tabLabels = new TMP_Text[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
            {
                var tabGo = new GameObject("Tab_" + tabNames[i], typeof(RectTransform), typeof(Image), typeof(Button));
                tabGo.transform.SetParent(centerGo.transform, false);
                var tabRt = tabGo.GetComponent<RectTransform>();
                tabRt.anchorMin = tabRt.anchorMax = new Vector2(0.5f, 1f);
                tabRt.pivot = new Vector2(0.5f, 1f);
                tabRt.anchoredPosition = new Vector2(-260f + i * 260f, -128f);
                tabRt.sizeDelta = new Vector2(230f, 54f);
                var tabImg = tabGo.GetComponent<Image>();
                tabImg.color = new Color(0.2f, 0.18f, 0.15f, 1f);
                var tabLabel = CreateText(tabGo.transform, "Label", 26, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.8f));
                StretchFull(tabLabel.rectTransform);
                tabLabel.fontStyle = FontStyles.Bold;
                tabLabel.text = tabNames[i];
                var tabBtn = tabGo.GetComponent<Button>();
                tabBtn.targetGraphic = tabImg;
                tabBtn.image = tabImg; // 运行时构建的 Button 不会自动关联 image 序列化字段
                int tabIndex = i;
                tabBtn.onClick.AddListener(() => SwitchTab((ArchiveTab)tabIndex));
                tabButtons[i] = tabBtn;
                tabLabels[i] = tabLabel;
            }

            // 列表滚动区（左）
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(centerGo.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.02f, 0.08f);
            scrollRt.anchorMax = new Vector2(0.60f, 0.78f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            scrollRect = scrollGo.GetComponent<ScrollRect>();

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            StretchFull(viewportRt);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            listContent = contentGo.GetComponent<RectTransform>();
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.sizeDelta = new Vector2(0f, 0f);
            var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewportRt;
            scrollRect.content = listContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.12f;
            scrollRect.scrollSensitivity = 40f;

            // 大卡预览区（右）
            previewPanel = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            previewPanel.transform.SetParent(centerGo.transform, false);
            var previewRt = previewPanel.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0.62f, 0.08f);
            previewRt.anchorMax = new Vector2(0.98f, 0.78f);
            previewRt.offsetMin = Vector2.zero;
            previewRt.offsetMax = Vector2.zero;
            previewPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.96f);

            // 卡面容器（翻面动画对象）
            var cardAreaGo = new GameObject("CardArea", typeof(RectTransform));
            cardAreaGo.transform.SetParent(previewPanel.transform, false);
            previewCardArea = cardAreaGo.GetComponent<RectTransform>();
            previewCardArea.anchorMin = previewCardArea.anchorMax = new Vector2(0.5f, 1f);
            previewCardArea.pivot = new Vector2(0.5f, 1f);
            previewCardArea.anchoredPosition = new Vector2(0f, -18f);
            previewCardArea.sizeDelta = new Vector2(300f, 390f);

            // 描述框
            var descBoxGo = new GameObject("DescBox", typeof(RectTransform), typeof(Image));
            descBoxGo.transform.SetParent(previewPanel.transform, false);
            var descBoxRt = descBoxGo.GetComponent<RectTransform>();
            descBoxRt.anchorMin = Vector2.zero;
            descBoxRt.anchorMax = Vector2.one;
            descBoxRt.offsetMin = new Vector2(16f, 110f);
            descBoxRt.offsetMax = new Vector2(-16f, -425f);
            descBoxGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.28f);
            previewDescText = CreateText(descBoxGo.transform, "Desc", 22, TextAlignmentOptions.TopLeft, new Color(0.88f, 0.88f, 0.9f));
            var descRt = previewDescText.rectTransform;
            descRt.anchorMin = Vector2.zero;
            descRt.anchorMax = Vector2.one;
            descRt.offsetMin = new Vector2(12f, 10f);
            descRt.offsetMax = new Vector2(-12f, -10f);
            previewDescText.enableWordWrapping = true;

            // 升级增量（绿色高亮，升级后视图显示）
            previewDeltaText = CreateText(previewPanel.transform, "Delta", 21, TextAlignmentOptions.Midline, new Color(0.45f, 1f, 0.55f));
            var deltaRt = previewDeltaText.rectTransform;
            deltaRt.anchorMin = Vector2.zero;
            deltaRt.anchorMax = Vector2.one;
            deltaRt.offsetMin = new Vector2(16f, 66f);
            deltaRt.offsetMax = new Vector2(-16f, -502f);

            // 升级前后对比按钮
            var upgradeGo = new GameObject("UpgradeToggle", typeof(RectTransform), typeof(Image), typeof(Button));
            upgradeGo.transform.SetParent(previewPanel.transform, false);
            var upgradeRt = upgradeGo.GetComponent<RectTransform>();
            upgradeRt.anchorMin = upgradeRt.anchorMax = new Vector2(0.5f, 0f);
            upgradeRt.pivot = new Vector2(0.5f, 0f);
            upgradeRt.anchoredPosition = new Vector2(24f, 16f);
            upgradeRt.sizeDelta = new Vector2(300f, 52f);
            upgradeGo.GetComponent<Image>().color = new Color(0.3f, 0.34f, 0.2f, 1f);
            upgradeToggleLabel = CreateText(upgradeGo.transform, "Label", 24, TextAlignmentOptions.Center, new Color(0.8f, 1f, 0.7f));
            StretchFull(upgradeToggleLabel.rectTransform);
            upgradeToggleLabel.text = "查看升级后 →";
            upgradeToggleBtn = upgradeGo.GetComponent<Button>();
            upgradeToggleBtn.targetGraphic = upgradeGo.GetComponent<Image>();
            upgradeToggleBtn.onClick.AddListener(ToggleUpgradePreview);

            // 返回列表按钮
            var backGo = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            backGo.transform.SetParent(previewPanel.transform, false);
            var backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = backRt.anchorMax = new Vector2(0f, 0f);
            backRt.pivot = new Vector2(0f, 0f);
            backRt.anchoredPosition = new Vector2(14f, 14f);
            backRt.sizeDelta = new Vector2(130f, 44f);
            backGo.GetComponent<Image>().color = new Color(0.24f, 0.22f, 0.18f, 1f);
            var backTmp = CreateText(backGo.transform, "Label", 22, TextAlignmentOptions.Center, new Color(0.9f, 0.88f, 0.8f));
            StretchFull(backTmp.rectTransform);
            backTmp.text = "← 返回列表";
            var backBtn = backGo.GetComponent<Button>();
            backBtn.targetGraphic = backGo.GetComponent<Image>();
            backBtn.onClick.AddListener(HidePreview);

            previewPanel.SetActive(false);

            // 底部提示
            var hintTmp = CreateText(centerGo.transform, "Hint", 18, TextAlignmentOptions.BottomLeft, new Color(0.6f, 0.58f, 0.52f, 0.9f));
            var hintRt = hintTmp.rectTransform;
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(0.6f, 0f);
            hintRt.pivot = new Vector2(0f, 0f);
            hintRt.anchoredPosition = new Vector2(24f, 6f);
            hintRt.sizeDelta = new Vector2(900f, 34f);
            hintTmp.text = "F2 打开/关闭 · 点击卡牌查看详情与升级前后对比";

            UiFeel.ApplyToAllButtons(panelRoot);
        }

        #endregion

        #region 工具

        private static TMP_Text CreateText(Transform parent, string goName, int fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            return tmp;
        }

        private static string GetCardTypeName(CardType t)
        {
            switch (t)
            {
                case CardType.Attack: return "攻击";
                case CardType.Defense: return "防御";
                case CardType.Skill: return "技能";
                case CardType.Power: return "能力";
                case CardType.Curse: return "诅咒";
                default: return t.ToString();
            }
        }

        private static string GetFactionDisplayName(CardFaction f)
        {
            switch (f)
            {
                case CardFaction.None: return "无阵营";
                case CardFaction.Slime: return "粘液";
                case CardFaction.Reluctant: return "不舍";
                case CardFaction.Blood: return "鲜血";
                case CardFaction.Frost: return "寒霜";
                case CardFaction.Shadow: return "暗影";
                case CardFaction.Corrupt: return "腐化";
                default: return "未知";
            }
        }

        private static Color GetFactionColor(CardFaction f)
        {
            switch (f)
            {
                case CardFaction.Slime: return Hex("#00FF88");
                case CardFaction.Reluctant: return Hex("#CC66FF");
                case CardFaction.Blood: return Hex("#FF4444");
                case CardFaction.Frost: return Hex("#66CCFF");
                case CardFaction.Shadow: return Hex("#999999");
                case CardFaction.Corrupt: return Hex("#9933CC");
                default: return Hex("#BBAA88");
            }
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        private static Sprite LoadUISprite(string name)
        {
            return Resources.Load<Sprite>("InterfaceUI/" + name);
        }

        private static TMP_FontAsset LoadFont()
        {
            if (cachedFont == null)
                cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/SIMSUN SDF");
            return cachedFont;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        #endregion

        void OnDestroy()
        {
            // 场景卸载时若面板仍打开，恢复时间缩放避免卡死
            if (IsAnyVisible)
                Time.timeScale = timeScaleBeforeOpen;
            IsAnyVisible = false;
            if (_instance == this)
                _instance = null;
        }
    }
}
