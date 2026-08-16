using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;

namespace MutationChess.UI
{
    /// <summary>
    /// 全屏图鉴面板（以撒式"见过才解锁"）：卡牌图鉴/遗物图鉴/药水图鉴（按 codexId 排序，
    /// 未见过条目在正式模式完全隐藏、开发者模式（DevConfig.DevMode）显示全部并标"未发现"），
    /// 卡组（运行时牌组统计）、弃牌堆（本场战斗弃牌堆+消耗堆）。
    /// 战斗中点击"抽牌/弃牌"计数或按 F2 打开，点击条目弹出大卡/遗物预览，
    /// 卡牌可切换"升级前后对比"查看绿色数值增量（翻面动画）。
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

        /// <summary>确保单例存在并开始监听 F2/ESC（懒加载单例若不创建，Update 永远不会执行导致快捷键失效）。</summary>
        public static void EnsureExists()
        {
            _ = Instance;
        }

        [Header("场景接线（可选，缺失时运行时自动构建）")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;

        public enum ArchiveTab { Cards, Relics, Potions, Deck, Discard }

        private const int CanvasOrder = 700;

        private Canvas canvas;
        private ArchiveTab currentTab = ArchiveTab.Cards;
        private ScrollRect scrollRect;        // 列表滚动视图
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

        // 真实卡牌预制体（由编辑器脚本 CardPrefabResourcesSetup 同步到 Resources）
        private static GameObject cardTilePrefab;
        private static bool cardTilePrefabTried;
        private RectTransform currentCardGrid; // 最近一次 BeginCardGrid 创建的网格容器（卡牌瓦片挂载点）

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
                openButton.onClick.AddListener(() => Open(ArchiveTab.Cards));
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2) &&
                (SettingsManager.Instance == null || !SettingsManager.Instance.IsSettingsOpen()))
            {
                if (IsVisible) Close();
                else Open(ArchiveTab.Cards);
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
                case ArchiveTab.Cards: BuildCardCodexList(); break;
                case ArchiveTab.Relics: BuildRelicCodexList(); break;
                case ArchiveTab.Potions: BuildPotionCodexList(); break;
                case ArchiveTab.Deck: BuildDeckList(); break;
                case ArchiveTab.Discard: BuildDiscardList(); break;
            }
        }

        #region 三个标签页

        /// <summary>卡牌图鉴：仅显示已见过的卡牌（见过才解锁），按 codexId 排序并分组，附 No. 编号与拥有/强化标记。</summary>
        private void BuildCardCodexList()
        {
            var seen = CodexProgress.Instance;
            var assets = CodexIdRegistry.GetCardsByIdOrdered();
            int seenCount = assets.Count(a => seen.IsCardSeen(a.codexId));
            bool devMode = DevConfig.DevMode; // 开发者模式：显示全部条目（不隐藏未见过）

            CreateHeaderRow($"卡牌图鉴 · 已发现 {seenCount} / {assets.Count}{(devMode ? " · 开发者模式" : "")}", new Color(0.9f, 0.82f, 0.5f));
            if (!devMode && seenCount == 0)
            {
                CreateEmptyHint("尚未发现任何卡牌——获得或见过卡牌后将自动收录于此（调试命令 seeall 可解锁全部）");
                return;
            }

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
                bool isSeen = seen.IsCardSeen(asset.codexId);
                if (!devMode && !isSeen) continue; // 未见过：正式模式完全隐藏，开发者模式显示全部

                if (first || asset.faction != lastFaction)
                {
                    CreateHeaderRow(GetFactionDisplayName(asset.faction), GetFactionColor(asset.faction));
                    BeginCardGrid();
                    first = false;
                    lastFaction = asset.faction;
                }

                owned.TryGetValue(asset.cardName, out var o);
                string badge = CodexIds.Format(CodexCategory.Card, asset.codexId);
                if (o.count > 0) badge += $" ✓x{o.count}";
                if (o.upgraded) badge += " ★";
                else if (devMode && !isSeen) badge += " · 未发现";
                Color badgeColor = o.upgraded ? new Color(1f, 0.82f, 0.3f)
                    : o.count > 0 ? new Color(0.5f, 1f, 0.5f)
                    : isSeen ? new Color(0.8f, 0.8f, 0.8f)
                    : new Color(0.55f, 0.55f, 0.6f);
                CreateCardTile(CardData.CreateCardFromAsset(asset), asset, badge, badgeColor, devMode && !isSeen);
            }
        }

        /// <summary>遗物图鉴：仅显示已获得的遗物，按 codexId 排序，附 No. 编号，点击查看详情。</summary>
        private void BuildRelicCodexList()
        {
            var seen = CodexProgress.Instance;
            var assets = CodexIdRegistry.GetRelicsByIdOrdered();
            int seenCount = assets.Count(a => seen.IsRelicSeen(a.codexId));
            bool devMode = DevConfig.DevMode; // 开发者模式：显示全部条目（不隐藏未见过）

            CreateHeaderRow($"遗物图鉴 · 已发现 {seenCount} / {assets.Count}{(devMode ? " · 开发者模式" : "")}", new Color(0.9f, 0.82f, 0.5f));
            if (!devMode && seenCount == 0)
            {
                CreateEmptyHint("尚未发现任何遗物——获得遗物后将自动收录于此");
                return;
            }

            BeginRelicGrid();
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                if (!devMode && !seen.IsRelicSeen(asset.codexId)) continue; // 未见过：正式模式隐藏
                CreateRelicTile(asset);
            }
        }

        /// <summary>药水图鉴：仅显示已获得的药水，按 codexId 排序，附 No. 编号。</summary>
        private void BuildPotionCodexList()
        {
            var seen = CodexProgress.Instance;
            var assets = CodexIdRegistry.GetPotionsByIdOrdered();
            int seenCount = assets.Count(a => seen.IsPotionSeen(a.codexId));
            bool devMode = DevConfig.DevMode; // 开发者模式：显示全部条目（不隐藏未见过）

            CreateHeaderRow($"药水图鉴 · 已发现 {seenCount} / {assets.Count}{(devMode ? " · 开发者模式" : "")}", new Color(0.9f, 0.82f, 0.5f));
            if (assets.Count == 0)
            {
                CreateEmptyHint("暂无药水资产（药水系统待实装，获得后自动收录）");
                return;
            }
            if (!devMode && seenCount == 0)
            {
                CreateEmptyHint("尚未发现任何药水——获得药水后将自动收录于此");
                return;
            }

            BeginRelicGrid();
            foreach (var asset in assets)
            {
                if (asset == null) continue;
                if (!devMode && !seen.IsPotionSeen(asset.codexId)) continue; // 未见过：正式模式隐藏
                CreatePotionTile(asset);
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
            BeginCardGrid();

            var groups = GroupByName(deck);
            var names = new List<string>(groups.Keys);
            names.Sort();
            foreach (var name in names)
            {
                var g = groups[name];
                var asset = CardData.GetAssetByName(name);
                string badge = $"x{g.count}" + (g.upgraded ? " ★" : "");
                CreateCardTile(
                    asset != null ? CardData.CreateCardFromAsset(asset) : null,
                    asset, badge,
                    g.upgraded ? new Color(1f, 0.82f, 0.3f) : new Color(0.5f, 1f, 0.5f));
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

            BeginCardGrid();
            var groups = GroupByName(pile);
            var names = new List<string>(groups.Keys);
            names.Sort();
            foreach (var name in names)
            {
                var g = groups[name];
                var asset = CardData.GetAssetByName(name);
                string badge = $"x{g.count}" + (g.upgraded ? " ★" : "");
                CreateCardTile(
                    asset != null ? CardData.CreateCardFromAsset(asset) : null,
                    asset, badge,
                    g.upgraded ? new Color(1f, 0.82f, 0.3f) : new Color(0.5f, 1f, 0.5f));
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
            // VLG childControlHeight=false 不会改子物体高度：容器默认 100 高时，文字盒
            // 垂直居中会把标题字形压进下一个元素（叠到卡面上）——容器高度对齐布局槽位
            var goRt = go.GetComponent<RectTransform>();
            goRt.sizeDelta = new Vector2(goRt.sizeDelta.x, 42f);
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
            // 同标题行：容器高度对齐布局槽位，避免文字盒溢出叠到后续元素
            var goRt = go.GetComponent<RectTransform>();
            goRt.sizeDelta = new Vector2(goRt.sizeDelta.x, 60f);
            var tmp = CreateText(go.transform, "Label", 22, TextAlignmentOptions.MidlineLeft, new Color(0.65f, 0.62f, 0.55f));
            var rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(18f, 0f);
            rt.offsetMax = new Vector2(-18f, 0f);
            tmp.text = text;
        }

        /// <summary>在滚动列表内新建一个 5 列卡牌网格区（真实卡牌预制体铺排，自适应高度）。</summary>
        private void BeginCardGrid()
        {
            var gridGo = new GameObject("CardGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridGo.transform.SetParent(listContent, false);
            var grid = gridGo.GetComponent<GridLayoutGroup>();
            // GridLayoutGroup 只摆放子物体位置、不改 sizeDelta，且没有 childControl/
            // childForceExpand 开关（那是 Horizontal/VerticalLayoutGroup 的成员）——
            // cellSize 直接取卡牌预制体当前尺寸（预制体日后改动自动跟随，不硬编码
            // 150×200），行距留 48 供底部徽标骑跨（徽标高 24 完全落在行距内，
            // 不遮卡面描述、也不碰下一行卡面）
            RectTransform prefabRt = null;
            GameObject prefab = GetCardTilePrefab();
            if (prefab != null) prefabRt = prefab.GetComponent<RectTransform>();
            Vector2 cell = prefabRt != null ? prefabRt.rect.size : new Vector2(150f, 200f);
            grid.cellSize = cell;
            grid.spacing = new Vector2(26f, 48f);
            grid.padding = new RectOffset(14, 14, 8, 22);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.MiddleCenter;
            var fitter = gridGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            currentCardGrid = gridGo.GetComponent<RectTransform>();
        }

        /// <summary>在滚动列表内新建一个 6 列图标网格区（遗物/药水瓦片用）。</summary>
        private void BeginRelicGrid()
        {
            var gridGo = new GameObject("IconGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
            gridGo.transform.SetParent(listContent, false);
            var grid = gridGo.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(130f, 150f);
            grid.spacing = new Vector2(18f, 26f);
            grid.padding = new RectOffset(14, 14, 8, 18);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.childAlignment = TextAnchor.MiddleCenter;
            var fitter = gridGo.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            currentCardGrid = gridGo.GetComponent<RectTransform>();
        }

        /// <summary>
        /// 放置一张真实卡牌预制体（CardUI 驱动，展示模式：禁悬停/拖拽，保留稀有度配色，点击弹大卡预览）。
        /// Resources 下预制体缺失时（编辑器同步任务未执行）或卡名无法映射到资产时用程序化卡面兜底。
        /// </summary>
        private GameObject CreateCardTile(Card card, CardDataAsset asset, string badgeText, Color badgeColor, bool dim = false)
        {
            GameObject tile = null;

            // 卡名无法映射到资产（如存档残留旧卡名）时用程序化卡面兜底，避免预制体空卡面
            GameObject prefab = GetCardTilePrefab();
            if (card != null && prefab != null)
            {
                Transform tileParent = currentCardGrid != null ? (Transform)currentCardGrid : listContent;
                tile = Instantiate(prefab, tileParent);
                var ui = tile.GetComponent<CardUI>();
                if (ui != null)
                {
                    ui.Initialize(card);
                    // 展示模式：直接禁用 CardUI 组件。其实现了拖拽/悬停等事件接口，
                    // Unity 事件系统沿层级向上查找首个实现者即停止，不禁用会拦截
                    // 列表滚动（ScrollRect 收不到拖拽）。禁用后事件穿透到滚动区，
                    // 点击由下方补挂的 Button 转发弹大卡预览。
                    ui.enabled = false;

                    // 网格瓦片只留卡面本体：CardUI 在 Awake 建的柔边投影左右各外扩
                    // 12px（相邻两卡阴影 24px > 行距，叠成暗带像卡牌堆叠）+ 辉光外扩
                    // 6px，全部关掉——网格里不需要投影层次感
                    Transform fx = tile.transform.Find("CardShadow");
                    if (fx != null) fx.gameObject.SetActive(false);
                    fx = tile.transform.Find("CardGlow");
                    if (fx != null) fx.gameObject.SetActive(false);

                    // 长描述会溢出预制体文本框（TMP 溢出渲染不裁剪），压到徽标与
                    // 下一行卡面；瓦片只显示名称/费用/卡图，完整描述点击大卡预览看
                    Transform desc = tile.transform.Find("DescriptionText");
                    if (desc != null) desc.gameObject.SetActive(false);
                }

                var clickBtn = tile.GetComponent<Button>();
                if (clickBtn == null) clickBtn = tile.AddComponent<Button>();
                clickBtn.transition = Selectable.Transition.None;
                clickBtn.targetGraphic = tile.GetComponent<Image>();
                UiFeel.ApplyButton(clickBtn);
                var captured = asset;
                clickBtn.onClick.AddListener(() =>
                {
                    if (captured != null) ShowPreview(captured);
                });
            }
            else
            {
                tile = BuildProceduralTile(card, asset);
            }

            if (dim) AddDimOverlay(tile);
            AddCardBadge(tile, badgeText, badgeColor);
            return tile;
        }

        /// <summary>开发者模式"未发现"条目压暗一层（半透明黑罩，不参与点击，徽标在其上保持可读）。</summary>
        private void AddDimOverlay(GameObject tile)
        {
            var dimGo = new GameObject("DimOverlay", typeof(RectTransform), typeof(Image));
            dimGo.transform.SetParent(tile.transform, false);
            StretchFull(dimGo.GetComponent<RectTransform>());
            dimGo.GetComponent<Image>().color = new Color(0.04f, 0.04f, 0.07f, 0.45f);
            dimGo.GetComponent<Image>().raycastTarget = false;
        }

        /// <summary>懒加载 Resources 下的卡牌预制体（编辑器脚本 CardPrefabResourcesSetup 同步产物）。</summary>
        private static GameObject GetCardTilePrefab()
        {
            if (cardTilePrefab == null && !cardTilePrefabTried)
            {
                cardTilePrefabTried = true;
                cardTilePrefab = Resources.Load<GameObject>("Prefabs/CardPrefab");
                if (cardTilePrefab == null)
                    GameLogger.LogWarning("[牌库档案] Resources 下未找到卡牌预制体（CardPrefabResourcesSetup 未执行？），使用程序化卡面兜底");
            }
            return cardTilePrefab;
        }

        /// <summary>程序化卡面兜底：稀有度边框 + 卡图 + 费用 + 名称 + 描述，尺寸与卡牌预制体一致（150×200）。</summary>
        private GameObject BuildProceduralTile(Card card, CardDataAsset asset)
        {
            var go = new GameObject("Tile_" + (card != null ? card.cardName : "?"),
                typeof(RectTransform), typeof(Image), typeof(Button));
            Transform tileParent = currentCardGrid != null ? (Transform)currentCardGrid : listContent;
            go.transform.SetParent(tileParent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 200f);

            Color rarity = card != null ? CardVisualConfig.GetRarityColor(card.rarity) : new Color(0.6f, 0.6f, 0.6f);

            // 稀有度边框（全铺底色即描边）
            go.GetComponent<Image>().color = rarity;
            go.GetComponent<Image>().raycastTarget = true;

            // 卡面内衬（暗底，四周留出稀有度描边）
            var innerGo = new GameObject("Inner", typeof(RectTransform), typeof(Image));
            innerGo.transform.SetParent(go.transform, false);
            var innerRt = innerGo.GetComponent<RectTransform>();
            innerRt.anchorMin = new Vector2(0.03f, 0.03f);
            innerRt.anchorMax = new Vector2(0.97f, 0.97f);
            innerRt.offsetMin = Vector2.zero;
            innerRt.offsetMax = Vector2.zero;
            innerGo.GetComponent<Image>().color = rarity * 0.3f + new Color(0.05f, 0.05f, 0.08f);
            innerGo.GetComponent<Image>().raycastTarget = false;

            // 卡图
            if (card != null && card.cardArt != null)
            {
                var artGo = new GameObject("Art", typeof(RectTransform), typeof(Image));
                artGo.transform.SetParent(go.transform, false);
                var artRt = artGo.GetComponent<RectTransform>();
                artRt.anchorMin = new Vector2(0.08f, 0.30f);
                artRt.anchorMax = new Vector2(0.92f, 0.78f);
                artRt.offsetMin = Vector2.zero;
                artRt.offsetMax = Vector2.zero;
                var artImg = artGo.GetComponent<Image>();
                artImg.sprite = card.cardArt;
                artImg.preserveAspect = true;
                artImg.raycastTarget = false;
            }

            // 费用（左上）
            var costTmp = CreateText(go.transform, "Cost", 18, TextAlignmentOptions.Midline, new Color(1f, 0.9f, 0.5f));
            costTmp.fontStyle = FontStyles.Bold;
            var costRt = costTmp.rectTransform;
            costRt.anchorMin = costRt.anchorMax = new Vector2(0f, 1f);
            costRt.pivot = new Vector2(0.5f, 0.5f);
            costRt.anchoredPosition = new Vector2(18f, -14f);
            costRt.sizeDelta = new Vector2(32f, 26f);
            costTmp.text = card != null ? card.cost.ToString() : "-";

            // 名称（顶部居中）
            var nameTmp = CreateText(go.transform, "Name", 16, TextAlignmentOptions.Top, Color.Lerp(rarity, Color.white, 0.45f));
            nameTmp.fontStyle = FontStyles.Bold;
            var nameRt = nameTmp.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -8f);
            nameRt.sizeDelta = new Vector2(138f, 40f);
            nameTmp.text = card != null ? card.cardName : "";

            // 描述（底部）
            if (card != null)
            {
                var descTmp = CreateText(go.transform, "Desc", 12, TextAlignmentOptions.Top, new Color(0.75f, 0.75f, 0.78f));
                var descRt = descTmp.rectTransform;
                descRt.anchorMin = descRt.anchorMax = new Vector2(0.5f, 0f);
                descRt.pivot = new Vector2(0.5f, 0f);
                descRt.anchoredPosition = new Vector2(0f, 10f);
                descRt.sizeDelta = new Vector2(136f, 42f);
                descTmp.text = card.GetDescription();
            }

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            btn.transition = Selectable.Transition.None;
            UiFeel.ApplyButton(btn);
            var captured = asset;
            btn.onClick.AddListener(() =>
            {
                if (captured != null) ShowPreview(captured);
            });
            return go;
        }

        /// <summary>卡面底部徽标（拥有 xN / ★强化），半透明暗底药丸，不参与点击。</summary>
        private void AddCardBadge(GameObject tile, string badgeText, Color badgeColor)
        {
            if (tile == null || string.IsNullOrEmpty(badgeText)) return;

            var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(tile.transform, false);
            var rt = badgeGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -24f); // 徽标完全落在 44px 行距内（占 -36..-12），不遮卡面描述也不碰下一行卡
            rt.sizeDelta = new Vector2(132f, 24f);
            badgeGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            badgeGo.GetComponent<Image>().raycastTarget = false;

            var tmp = CreateText(badgeGo.transform, "Label", 15, TextAlignmentOptions.Center, badgeColor);
            StretchFull(tmp.rectTransform);
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableWordWrapping = false;  // 单行徽标：换行会溢出徽标框叠到下一行卡面上
            tmp.enableAutoSizing = true;     // 长徽标（如 "k7 ✓x3 ★ · 未发现"）自动缩字号挤进单行
            tmp.fontSizeMin = 10f;
            tmp.fontSizeMax = 15f;
            tmp.text = badgeText;
            tmp.raycastTarget = false;
        }

        /// <summary>遗物瓦片：图标 + 名称 + No. 编号，点击弹出遗物详情预览。</summary>
        private GameObject CreateRelicTile(RelicDataAsset asset)
        {
            var go = new GameObject("RelicTile_" + asset.relicName, typeof(RectTransform), typeof(Image), typeof(Button));
            Transform tileParent = currentCardGrid != null ? (Transform)currentCardGrid : listContent;
            go.transform.SetParent(tileParent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 150f);

            Color rarity = GetRelicRarityColor(asset.rarity);
            go.GetComponent<Image>().color = rarity * 0.35f + new Color(0.05f, 0.05f, 0.08f);
            go.GetComponent<Image>().raycastTarget = true;

            // 图标（顶部居中）
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -10f);
            iconRt.sizeDelta = new Vector2(84f, 84f);
            var iconImg = iconGo.GetComponent<Image>();
            if (!string.IsNullOrEmpty(asset.iconPath))
                iconImg.sprite = Resources.Load<Sprite>(asset.iconPath);
            iconImg.preserveAspect = true;
            iconImg.color = iconImg.sprite != null ? Color.white : rarity * 0.5f;
            iconImg.raycastTarget = false;

            // 名称（底部居中）
            var nameTmp = CreateText(go.transform, "Name", 17, TextAlignmentOptions.Center, Color.Lerp(rarity, Color.white, 0.4f));
            nameTmp.fontStyle = FontStyles.Bold;
            var nameRt = nameTmp.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.anchoredPosition = new Vector2(0f, 24f);
            nameRt.sizeDelta = new Vector2(124f, 44f);
            nameTmp.text = asset.relicName;
            nameTmp.enableWordWrapping = true;
            nameTmp.raycastTarget = false;

            // No. 编号（左上角）
            var idTmp = CreateText(go.transform, "Id", 14, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.4f));
            var idRt = idTmp.rectTransform;
            idRt.anchorMin = idRt.anchorMax = new Vector2(0f, 1f);
            idRt.pivot = new Vector2(0f, 1f);
            idRt.anchoredPosition = new Vector2(6f, -4f);
            idRt.sizeDelta = new Vector2(72f, 20f);
            idTmp.text = CodexIds.Format(CodexCategory.Relic, asset.codexId);
            idTmp.raycastTarget = false;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            btn.transition = Selectable.Transition.None;
            UiFeel.ApplyButton(btn);
            var captured = asset;
            btn.onClick.AddListener(() => ShowRelicPreview(captured));
            return go;
        }

        /// <summary>药水瓦片：图标 + 名称 + No. 编号，点击弹出药水详情预览。</summary>
        private GameObject CreatePotionTile(PotionDataAsset asset)
        {
            var go = new GameObject("PotionTile_" + asset.potionName, typeof(RectTransform), typeof(Image), typeof(Button));
            Transform tileParent = currentCardGrid != null ? (Transform)currentCardGrid : listContent;
            go.transform.SetParent(tileParent, false);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(130f, 150f);

            Color rarity = GetPotionRarityColor(asset.rarity);
            go.GetComponent<Image>().color = rarity * 0.35f + new Color(0.05f, 0.05f, 0.08f);
            go.GetComponent<Image>().raycastTarget = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -10f);
            iconRt.sizeDelta = new Vector2(84f, 84f);
            var iconImg = iconGo.GetComponent<Image>();
            if (!string.IsNullOrEmpty(asset.iconPath))
                iconImg.sprite = Resources.Load<Sprite>(asset.iconPath);
            iconImg.preserveAspect = true;
            iconImg.color = iconImg.sprite != null ? Color.white : rarity * 0.5f;
            iconImg.raycastTarget = false;

            var nameTmp = CreateText(go.transform, "Name", 17, TextAlignmentOptions.Center, Color.Lerp(rarity, Color.white, 0.4f));
            nameTmp.fontStyle = FontStyles.Bold;
            var nameRt = nameTmp.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 0f);
            nameRt.pivot = new Vector2(0.5f, 0f);
            nameRt.anchoredPosition = new Vector2(0f, 24f);
            nameRt.sizeDelta = new Vector2(124f, 44f);
            nameTmp.text = asset.potionName;
            nameTmp.enableWordWrapping = true;
            nameTmp.raycastTarget = false;

            var idTmp = CreateText(go.transform, "Id", 14, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.4f));
            var idRt = idTmp.rectTransform;
            idRt.anchorMin = idRt.anchorMax = new Vector2(0f, 1f);
            idRt.pivot = new Vector2(0f, 1f);
            idRt.anchoredPosition = new Vector2(6f, -4f);
            idRt.sizeDelta = new Vector2(72f, 20f);
            idTmp.text = CodexIds.Format(CodexCategory.Potion, asset.codexId);
            idTmp.raycastTarget = false;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            btn.transition = Selectable.Transition.None;
            UiFeel.ApplyButton(btn);
            var captured = asset;
            btn.onClick.AddListener(() => ShowRelicPreviewForPotion(captured));
            return go;
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

            ClearPreviewCardArea();

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

            // 费用（左上角，与名称分行避免重叠）
            var costTmp = CreateText(previewCardArea, "Cost", 24, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.9f, 0.5f));
            costTmp.fontStyle = FontStyles.Bold;
            var costRt = costTmp.rectTransform;
            costRt.anchorMin = costRt.anchorMax = new Vector2(0f, 1f);
            costRt.pivot = new Vector2(0f, 1f);
            costRt.anchoredPosition = new Vector2(14f, -8f);
            costRt.sizeDelta = new Vector2(100f, 36f);
            costTmp.text = $"费用 {shown.cost}";

            // 名称（顶部居中，位于费用行下方）
            var nameTmp = CreateText(previewCardArea, "Name", 26, TextAlignmentOptions.Top, rarity);
            var nameRt = nameTmp.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -48f);
            nameRt.sizeDelta = new Vector2(256f, 40f);
            nameTmp.fontStyle = FontStyles.Bold;
            nameTmp.text = shown.cardName;

            // 元信息（类型 · 阵营 · 稀有度）
            var metaTmp = CreateText(previewCardArea, "Meta", 21, TextAlignmentOptions.Bottom, new Color(0.78f, 0.78f, 0.82f));
            var metaRt = metaTmp.rectTransform;
            metaRt.anchorMin = metaRt.anchorMax = new Vector2(0.5f, 0f);
            metaRt.pivot = new Vector2(0.5f, 0f);
            metaRt.anchoredPosition = new Vector2(0f, 62f);
            metaRt.sizeDelta = new Vector2(280f, 34f);
            string metaText = $"{GetCardTypeName(shown.cardType)} · {shown.GetFactionName()} · {shown.GetRarityName()}";
            if (currentAsset != null && currentAsset.codexId > 0)
                metaText += $" · {CodexIds.Format(CodexCategory.Card, currentAsset.codexId)}";
            metaTmp.text = metaText;

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

        /// <summary>清空预览卡面容器（卡牌翻面重建 / 图标预览共用）。</summary>
        private void ClearPreviewCardArea()
        {
            for (int i = previewCardArea.childCount - 1; i >= 0; i--)
                Destroy(previewCardArea.GetChild(i).gameObject);
        }

        /// <summary>遗物详情预览：图标 + 名称 + R编号/稀有度 + 描述。</summary>
        private void ShowRelicPreview(RelicDataAsset asset)
        {
            if (asset == null) return;
            ShowIconPreview(CodexCategory.Relic, asset.relicName, asset.codexId, asset.description, asset.iconPath,
                GetRelicRarityColor(asset.rarity), GetRelicRarityName(asset.rarity), asset.relicId);
        }

        /// <summary>药水详情预览（复用图标预览布局）。</summary>
        private void ShowRelicPreviewForPotion(PotionDataAsset asset)
        {
            if (asset == null) return;
            ShowIconPreview(CodexCategory.Potion, asset.potionName, asset.codexId, asset.description, asset.iconPath,
                GetPotionRarityColor(asset.rarity), GetPotionRarityName(asset.rarity), asset.potionId);
        }

        /// <summary>通用图标预览（遗物/药水）：无升级对比，卡牌专属控件隐藏。</summary>
        private void ShowIconPreview(CodexCategory cat, string name, int codexId, string description, string iconPath,
                                     Color rarity, string rarityName, string assetId)
        {
            currentAsset = null; // 清除卡牌预览状态
            previewBaseCard = null;
            previewUpgradedCard = null;
            showingUpgraded = false;

            ClearPreviewCardArea();

            // 底色（稀有度色调）
            var bgGo = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(previewCardArea, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            bgGo.GetComponent<Image>().color = rarity * 0.35f + new Color(0.06f, 0.06f, 0.09f);
            bgGo.GetComponent<Image>().raycastTarget = false;

            // 图标（顶部居中）
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(previewCardArea, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -20f);
            iconRt.sizeDelta = new Vector2(170f, 170f);
            var iconImg = iconGo.GetComponent<Image>();
            if (!string.IsNullOrEmpty(iconPath))
                iconImg.sprite = Resources.Load<Sprite>(iconPath);
            iconImg.preserveAspect = true;
            iconImg.color = iconImg.sprite != null ? Color.white : rarity * 0.5f;
            iconImg.raycastTarget = false;

            // 名称
            var nameTmp = CreateText(previewCardArea, "Name", 27, TextAlignmentOptions.Center, rarity);
            nameTmp.fontStyle = FontStyles.Bold;
            var nameRt = nameTmp.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0.5f, 1f);
            nameRt.pivot = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -202f);
            nameRt.sizeDelta = new Vector2(272f, 42f);
            nameTmp.text = name;

            // 元信息（前缀编号 · 稀有度 · 资产ID）
            var metaTmp = CreateText(previewCardArea, "Meta", 18, TextAlignmentOptions.Center, new Color(0.78f, 0.78f, 0.82f));
            var metaRt = metaTmp.rectTransform;
            metaRt.anchorMin = metaRt.anchorMax = new Vector2(0.5f, 0f);
            metaRt.pivot = new Vector2(0.5f, 0f);
            metaRt.anchoredPosition = new Vector2(0f, 14f);
            metaRt.sizeDelta = new Vector2(280f, 64f);
            metaTmp.text = $"{CodexIds.Format(cat, codexId)} · {rarityName}\n{assetId}";

            previewDescText.text = description;
            previewDeltaText.text = "";
            previewDeltaText.color = new Color(0f, 0f, 0f, 0f);
            upgradeToggleBtn.gameObject.SetActive(false);

            previewPanel.SetActive(true);
            UiFeel.AnimatePanelIn(previewPanel);
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

            // 中央面板（全屏图鉴：铺满整屏，四周留 20px 边框）
            var centerGo = new GameObject("CenterPanel", typeof(RectTransform), typeof(Image));
            centerGo.transform.SetParent(panelRoot.transform, false);
            var centerRt = centerGo.GetComponent<RectTransform>();
            centerRt.anchorMin = Vector2.zero;
            centerRt.anchorMax = Vector2.one;
            centerRt.pivot = new Vector2(0.5f, 0.5f);
            centerRt.offsetMin = new Vector2(20f, 20f);
            centerRt.offsetMax = new Vector2(-20f, -20f);
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
            titleTmp.text = "图鉴";

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
            closeTmp.text = "× 关闭";
            var closeBtn = closeBtnGo.GetComponent<Button>();
            closeBtn.targetGraphic = closeBtnGo.GetComponent<Image>();
            closeBtn.onClick.AddListener(Close);

            // 标签页按钮（卡牌图鉴 / 遗物图鉴 / 药水图鉴 / 卡组 / 弃牌堆）
            string[] tabNames = { "卡牌图鉴", "遗物图鉴", "药水图鉴", "卡组", "弃牌堆" };
            tabButtons = new Button[tabNames.Length];
            tabLabels = new TMP_Text[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
            {
                var tabGo = new GameObject("Tab_" + tabNames[i], typeof(RectTransform), typeof(Image), typeof(Button));
                tabGo.transform.SetParent(centerGo.transform, false);
                var tabRt = tabGo.GetComponent<RectTransform>();
                tabRt.anchorMin = tabRt.anchorMax = new Vector2(0.5f, 1f);
                tabRt.pivot = new Vector2(0.5f, 1f);
                tabRt.anchoredPosition = new Vector2(-500f + i * 250f, -128f);
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

            // 列表滚动区（左，占满下方区域）
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(centerGo.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.02f, 0.05f);
            scrollRt.anchorMax = new Vector2(0.62f, 0.82f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            scrollRect = scrollGo.GetComponent<ScrollRect>();

            // 与编辑器 ScrollRect 向导同构：RectTransform + Image + RectMask2D。
            // Image 近全透明（0.002）仅作射线接收层——空白处拖拽/滚轮也能命中
            // 滚动区；RectMask2D 只裁剪 MaskableGraphic（Image/TMP 都是），
            // 滑出视口的卡牌/徽标在此被裁掉，不会溢出列表边框
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            StretchFull(viewportRt);
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.002f);

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

            // 右侧细滚动条（内容超高时出现，滚轮/拖拽均可滚动）
            var sbGo = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            sbGo.transform.SetParent(scrollGo.transform, false);
            var sbRt = sbGo.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f);
            sbRt.anchorMax = new Vector2(1f, 1f);
            sbRt.pivot = new Vector2(1f, 0.5f);
            sbRt.anchoredPosition = new Vector2(-6f, 0f);
            sbRt.sizeDelta = new Vector2(12f, -16f);
            sbGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

            var sbHandleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            sbHandleGo.transform.SetParent(sbGo.transform, false);
            sbHandleGo.GetComponent<Image>().color = new Color(0.85f, 0.72f, 0.35f, 0.8f);

            var scrollbar = sbGo.GetComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = sbHandleGo.GetComponent<Image>();
            scrollbar.handleRect = sbHandleGo.GetComponent<RectTransform>();
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // 大卡预览区（右）
            previewPanel = new GameObject("Preview", typeof(RectTransform), typeof(Image));
            previewPanel.transform.SetParent(centerGo.transform, false);
            var previewRt = previewPanel.GetComponent<RectTransform>();
            previewRt.anchorMin = new Vector2(0.64f, 0.05f);
            previewRt.anchorMax = new Vector2(0.98f, 0.82f);
            previewRt.offsetMin = Vector2.zero;
            previewRt.offsetMax = Vector2.zero;
            previewPanel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.14f, 0.96f);

            // 卡面容器（翻面动画对象，左侧：卡面 300×420，绕顶部中心翻面）
            var cardAreaGo = new GameObject("CardArea", typeof(RectTransform));
            cardAreaGo.transform.SetParent(previewPanel.transform, false);
            previewCardArea = cardAreaGo.GetComponent<RectTransform>();
            previewCardArea.anchorMin = previewCardArea.anchorMax = new Vector2(0f, 1f);
            previewCardArea.pivot = new Vector2(0.5f, 1f);
            previewCardArea.anchoredPosition = new Vector2(172f, -18f);
            previewCardArea.sizeDelta = new Vector2(300f, 420f);

            // 描述框（右侧上部：与卡面并排，足够容纳完整描述）
            var descBoxGo = new GameObject("DescBox", typeof(RectTransform), typeof(Image));
            descBoxGo.transform.SetParent(previewPanel.transform, false);
            var descBoxRt = descBoxGo.GetComponent<RectTransform>();
            descBoxRt.anchorMin = descBoxRt.anchorMax = new Vector2(1f, 1f);
            descBoxRt.pivot = new Vector2(1f, 1f);
            descBoxRt.anchoredPosition = new Vector2(-18f, -18f);
            descBoxRt.sizeDelta = new Vector2(300f, 400f);
            descBoxGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.28f);
            previewDescText = CreateText(descBoxGo.transform, "Desc", 21, TextAlignmentOptions.TopLeft, new Color(0.88f, 0.88f, 0.9f));
            var descRt = previewDescText.rectTransform;
            descRt.anchorMin = Vector2.zero;
            descRt.anchorMax = Vector2.one;
            descRt.offsetMin = new Vector2(12f, 10f);
            descRt.offsetMax = new Vector2(-12f, -10f);
            previewDescText.enableWordWrapping = true;

            // 升级增量（绿色高亮，描述框正下方，升级后视图显示）
            previewDeltaText = CreateText(previewPanel.transform, "Delta", 20, TextAlignmentOptions.Midline, new Color(0.45f, 1f, 0.55f));
            var deltaRt = previewDeltaText.rectTransform;
            deltaRt.anchorMin = deltaRt.anchorMax = new Vector2(1f, 1f);
            deltaRt.pivot = new Vector2(1f, 1f);
            deltaRt.anchoredPosition = new Vector2(-18f, -426f);
            deltaRt.sizeDelta = new Vector2(300f, 60f);
            previewDeltaText.enableWordWrapping = true;

            // 升级前后对比按钮（底部居中，避开右侧返回按钮）
            var upgradeGo = new GameObject("UpgradeToggle", typeof(RectTransform), typeof(Image), typeof(Button));
            upgradeGo.transform.SetParent(previewPanel.transform, false);
            var upgradeRt = upgradeGo.GetComponent<RectTransform>();
            upgradeRt.anchorMin = upgradeRt.anchorMax = new Vector2(0.5f, 0f);
            upgradeRt.pivot = new Vector2(0.5f, 0f);
            upgradeRt.anchoredPosition = new Vector2(0f, 16f);
            upgradeRt.sizeDelta = new Vector2(240f, 52f);
            upgradeGo.GetComponent<Image>().color = new Color(0.3f, 0.34f, 0.2f, 1f);
            upgradeToggleLabel = CreateText(upgradeGo.transform, "Label", 22, TextAlignmentOptions.Center, new Color(0.8f, 1f, 0.7f));
            StretchFull(upgradeToggleLabel.rectTransform);
            upgradeToggleLabel.text = "查看升级后 →";
            upgradeToggleBtn = upgradeGo.GetComponent<Button>();
            upgradeToggleBtn.targetGraphic = upgradeGo.GetComponent<Image>();
            upgradeToggleBtn.onClick.AddListener(ToggleUpgradePreview);

            // 返回列表按钮（右下角，与居中的对比按钮互不重叠）
            var backGo = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            backGo.transform.SetParent(previewPanel.transform, false);
            var backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = backRt.anchorMax = new Vector2(1f, 0f);
            backRt.pivot = new Vector2(1f, 0f);
            backRt.anchoredPosition = new Vector2(-14f, 14f);
            backRt.sizeDelta = new Vector2(130f, 44f);
            backGo.GetComponent<Image>().color = new Color(0.24f, 0.22f, 0.18f, 1f);
            var backTmp = CreateText(backGo.transform, "Label", 18, TextAlignmentOptions.Center, new Color(0.9f, 0.88f, 0.8f));
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
            hintTmp.text = "F2 打开/关闭 · 点击条目查看详情 · 未发现的内容不显示";

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

        private static Color GetRelicRarityColor(RelicRarity r)
        {
            switch (r)
            {
                case RelicRarity.Starting: return new Color(1f, 0.6f, 0.2f);
                case RelicRarity.Common: return new Color(0.9f, 0.9f, 0.9f);
                case RelicRarity.Rare: return new Color(0.4f, 0.7f, 1f);
                case RelicRarity.Legendary: return new Color(1f, 0.5f, 0.1f);
                case RelicRarity.Special: return new Color(1f, 0.25f, 0.25f);
                default: return Color.white;
            }
        }

        private static string GetRelicRarityName(RelicRarity r)
        {
            switch (r)
            {
                case RelicRarity.Starting: return "初始";
                case RelicRarity.Common: return "普通";
                case RelicRarity.Rare: return "稀有";
                case RelicRarity.Legendary: return "传说";
                case RelicRarity.Special: return "Boss";
                default: return r.ToString();
            }
        }

        private static Color GetPotionRarityColor(PotionRarity r)
        {
            switch (r)
            {
                case PotionRarity.Common: return new Color(0.9f, 0.9f, 0.9f);
                case PotionRarity.Uncommon: return new Color(0.4f, 0.7f, 1f);
                case PotionRarity.Rare: return new Color(1f, 0.5f, 0.1f);
                default: return Color.white;
            }
        }

        private static string GetPotionRarityName(PotionRarity r)
        {
            switch (r)
            {
                case PotionRarity.Common: return "普通";
                case PotionRarity.Uncommon: return "罕见";
                case PotionRarity.Rare: return "稀有";
                default: return r.ToString();
            }
        }

        private static Sprite LoadUISprite(string name)
        {
            return Resources.Load<Sprite>("InterfaceUI/" + name);
        }

        private static TMP_FontAsset LoadFont()
        {
            if (cachedFont == null)
                cachedFont = UiFonts.Load();
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
