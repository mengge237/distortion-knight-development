using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MutationChess.Core;

namespace MutationChess.UI
{
    /// <summary>
    /// 首页屏幕：游戏标题 + 开始游戏（难度选择）/ 继续游戏（读档）/ 牌库档案 / 设置 四大入口。
    /// 优先绑定 HomeSceneSetup 生成的场景实体画布（HomeCanvas，编辑器内可见可调）；
    /// 场景缺接线（旧场景）时回退运行时自建全部 UI。
    /// 开始游戏 → 弹出难度选择面板 → 确认后进入主场景；继续游戏 → 标记待读档槽位并进入主场景；
    /// 设置子面板为标签页式（显示/音量/游戏 + 滚轮内容区），由 SettingsPanelBuilder 统一构建——
    /// 场景版与运行时版同一结构，场景缺失时运行时补建，编辑器里可手动编辑场景实体。
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        public static HomeScreen Instance { get; private set; }

        private Canvas canvas;
        private GameObject settingsSubPanel;
        private TMP_Text continueHintTmp; // 继续游戏按钮副标签（场景绑定与运行时自建共用）
        private SettingsPanelHandle homeSettingsHandle; // 标签页式设置面板句柄（场景/运行时共用）
        private static TMP_FontAsset cachedFont;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            // 显示设置启动恢复（目标帧率/窗口模式/长宽比）+ 首页 FPS 角标
            DisplaySettings.ApplyAll();
            FpsDisplay.EnsureExists();

            // 优先绑定场景内实体画布（HomeSceneSetup 生成）；旧场景缺接线时回退运行时自建
            if (!TryBindSceneUI())
                BuildHomeUI();

            // 设置子面板运行时收起（场景内保持激活是为了编辑器可见）
            if (settingsSubPanel != null)
                settingsSubPanel.SetActive(false);

            // 牌库档案快捷键就绪（首页也可按 F2 打开图鉴）
            CardArchivePanel.EnsureExists();
        }

        void Update()
        {
            // ESC：设置子面板打开时优先关闭它（档案面板的 ESC 由其自身处理）
            if (Input.GetKeyDown(KeyCode.Escape) && settingsSubPanel != null && settingsSubPanel.activeSelf)
            {
                CloseSettingsSubPanel();
            }
        }

        // ================= 首页构建 =================

        private void BuildHomeUI()
        {
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // 低于难度面板(900)/牌库档案(700)，弹出时不被遮挡
            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // 全屏暗底（深渊夜色）
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(transform, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            bgGo.GetComponent<Image>().color = new Color(0.045f, 0.045f, 0.07f, 1f);
            bgGo.GetComponent<Image>().raycastTarget = false;

            // 顶部金线装饰
            var lineGo = new GameObject("TopLine", typeof(RectTransform), typeof(Image));
            lineGo.transform.SetParent(transform, false);
            var lineRt = lineGo.GetComponent<RectTransform>();
            lineRt.anchorMin = lineRt.anchorMax = new Vector2(0.5f, 1f);
            lineRt.pivot = new Vector2(0.5f, 1f);
            lineRt.anchoredPosition = new Vector2(0f, -236f);
            lineRt.sizeDelta = new Vector2(780f, 3f);
            lineGo.GetComponent<Image>().color = new Color(0.55f, 0.45f, 0.22f, 0.8f);
            lineGo.GetComponent<Image>().raycastTarget = false;

            // 标题
            var title = CreateText(transform, "Title", 84, TextAlignmentOptions.Center, new Color(0.92f, 0.8f, 0.42f));
            title.fontStyle = FontStyles.Bold;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -104f);
            titleRt.sizeDelta = new Vector2(1000f, 120f);
            title.text = "异 变 棋 局";

            // 副标题
            var subtitle = CreateText(transform, "Subtitle", 24, TextAlignmentOptions.Center, new Color(0.6f, 0.58f, 0.52f));
            var subRt = subtitle.rectTransform;
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -258f);
            subRt.sizeDelta = new Vector2(1000f, 40f);
            subtitle.text = "以牌局对抗深渊 · 在诅咒中抉择";

            // 开始游戏
            CreateHomeButton("开始游戏", "选择难度，踏入深渊", -400f, StartNewGame);

            // 继续游戏（有存档才可进入，副标签实时显示存档摘要）
            continueHintTmp = CreateHomeButton("继续游戏", "", -540f, ContinueGame);
            RefreshContinueHint();

            // 图鉴（全屏，见过才解锁）
            CreateHomeButton("图鉴", "卡牌 · 遗物 · 药水", -680f, OpenArchive);

            // 设置
            CreateHomeButton("设置", "显示 · 音量 · 游戏", -820f, OpenSettings);

            // 底部提示
            var footer = CreateText(transform, "Footer", 18, TextAlignmentOptions.Center, new Color(0.45f, 0.43f, 0.4f));
            var footerRt = footer.rectTransform;
            footerRt.anchorMin = footerRt.anchorMax = new Vector2(0.5f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.anchoredPosition = new Vector2(0f, 34f);
            footerRt.sizeDelta = new Vector2(1200f, 34f);
            footer.text = "分支 8.16.3 · F2 图鉴 · ESC 关闭面板";
        }

        private TMP_Text CreateHomeButton(string label, string hint, float y, UnityAction onClick)
        {
            var btnGo = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 1f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(0f, y);
            btnRt.sizeDelta = new Vector2(460f, 108f);
            var bg = btnGo.GetComponent<Image>();
            bg.color = new Color(0.13f, 0.12f, 0.15f, 0.97f);

            // 左侧金条
            var accentGo = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentGo.transform.SetParent(btnGo.transform, false);
            var accentRt = accentGo.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 0.1f);
            accentRt.anchorMax = new Vector2(0f, 0.9f);
            accentRt.pivot = new Vector2(0f, 0.5f);
            accentRt.anchoredPosition = Vector2.zero;
            accentRt.sizeDelta = new Vector2(9f, 0f);
            accentGo.GetComponent<Image>().color = new Color(0.62f, 0.5f, 0.24f);
            accentGo.GetComponent<Image>().raycastTarget = false;

            // 主标签
            var labelTmp = CreateText(btnGo.transform, "Label", 32, TextAlignmentOptions.Center, new Color(0.93f, 0.9f, 0.82f));
            labelTmp.fontStyle = FontStyles.Bold;
            var labelRt = labelTmp.rectTransform;
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = new Vector2(0f, -16f);
            labelRt.sizeDelta = new Vector2(420f, 46f);
            labelTmp.text = label;

            // 副标签
            var hintTmp = CreateText(btnGo.transform, "Hint", 16, TextAlignmentOptions.Center, new Color(0.55f, 0.53f, 0.5f));
            var hintRt = hintTmp.rectTransform;
            hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 14f);
            hintRt.sizeDelta = new Vector2(430f, 26f);
            hintTmp.text = hint;

            var btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onClick);
            UiFeel.ApplyButton(btn);

            return hintTmp;
        }

        private void RefreshContinueHint()
        {
            if (continueHintTmp == null) return;
            int active = SaveService.GetActiveSlot();
            var meta = SaveService.Instance.GetMeta(active);
            if (meta == null)
            {
                continueHintTmp.text = "（暂无存档）";
                continueHintTmp.color = new Color(0.45f, 0.43f, 0.4f);
                return;
            }
            continueHintTmp.text = $"继续槽位 {active}：{meta.difficulty} · 第 {meta.floor} 层 · HP {meta.hp}/{meta.maxHp}";
            continueHintTmp.color = new Color(0.62f, 0.68f, 0.5f);
        }

        // ================= 场景绑定（HomeSceneSetup 生成的实体画布） =================

        /// <summary>绑定场景内实体画布控件；找不到画布返回 false（调用方回退运行时自建）。</summary>
        private bool TryBindSceneUI()
        {
            var canvasGo = GameObject.Find("HomeCanvas");
            if (canvasGo == null) return false;
            canvas = canvasGo.GetComponent<Canvas>();
            if (canvas == null) return false;
            Transform root = canvasGo.transform;

            if (!BindHomeButton(root, "BtnPanel/Btn_开始游戏", StartNewGame)) return false;
            if (!BindHomeButton(root, "BtnPanel/Btn_继续游戏", ContinueGame)) return false;
            if (!BindHomeButton(root, "BtnPanel/Btn_牌库档案", OpenArchive)) return false;
            if (!BindHomeButton(root, "BtnPanel/Btn_设置", OpenSettings)) return false;

            continueHintTmp = root.Find("BtnPanel/Btn_继续游戏/Hint")?.GetComponent<TMP_Text>();
            RefreshContinueHint();

            settingsSubPanel = root.Find("HomeSettings")?.gameObject;
            if (settingsSubPanel == null) return false;
            // 旧版平铺面板（无 TabBar 标签页标记）→ 销毁并按新版标签页结构重建，两版保持一致
            if (settingsSubPanel.transform.Find("TabBar") == null)
            {
                Destroy(settingsSubPanel);
                BuildHomeSettingsPanel();
                return settingsSubPanel != null;
            }
            homeSettingsHandle = SettingsPanelBuilder.GetHandle(settingsSubPanel);
            BindHomeSettingsPanel();
            return true;
        }

        private static bool BindHomeButton(Transform root, string path, UnityAction onClick)
        {
            var btn = root.Find(path)?.GetComponent<Button>();
            if (btn == null) return false;
            btn.onClick.AddListener(onClick);
            UiFeel.ApplyButton(btn);
            return true;
        }

        private void StartNewGame()
        {
            // 以撒式存档位选择（新游戏模式：空位直接开、有档需确认覆盖）→ 难度选择（含冒险须知）→ 加载缓冲屏
            SaveSlotPanel.Show(false, slot =>
            {
                SaveService.SetActiveSlot(slot);
                var dm = DifficultyManager.Instance;
                dm.ResetChosen();
                dm.ShowSelectionPanel(() => LoadingScreen.ShowAndLoad("MainScene"));
            }, null);
        }

        private void ContinueGame()
        {
            // 以撒式存档位选择（继续模式：仅已有存档的槽位可点）→ 加载缓冲屏 → 主场景读档
            SaveSlotPanel.Show(true, slot =>
            {
                SaveService.SetActiveSlot(slot);
                SaveService.SetPendingLoad(slot);
                LoadingScreen.ShowAndLoad("MainScene");
            }, null);
        }

        private void OpenArchive()
        {
            CardArchivePanel.Instance.Open(CardArchivePanel.ArchiveTab.Cards);
        }

        // ================= 设置子面板（标签页式，场景/运行时共用 SettingsPanelBuilder） =================

        private void OpenSettings()
        {
            if (settingsSubPanel == null)
            {
                BuildHomeSettingsPanel(); // 运行时自建路径延迟构建
                if (settingsSubPanel == null) return;
            }
            homeSettingsHandle?.RefreshAll(); // 战斗内改过的显示设置回首页后同步
            settingsSubPanel.SetActive(true);
            UiFeel.AnimatePanelIn(settingsSubPanel);
        }

        private void CloseSettingsSubPanel()
        {
            if (settingsSubPanel != null)
                settingsSubPanel.SetActive(false);
        }

        /// <summary>运行时补建标签页式设置子面板（无 HomeScene 场景接线时调用）。</summary>
        private void BuildHomeSettingsPanel()
        {
            homeSettingsHandle = SettingsPanelBuilder.Build(transform, "HomeSettings");
            settingsSubPanel = homeSettingsHandle != null ? homeSettingsHandle.Panel : null;
            if (settingsSubPanel == null) return;
            settingsSubPanel.transform.SetAsLastSibling();
            BindHomeSettingsPanel();
            settingsSubPanel.SetActive(false);
        }

        /// <summary>设置面板接线：场景版与运行时版共用同一套动作（只差构建来源）。</summary>
        private void BindHomeSettingsPanel()
        {
            if (homeSettingsHandle == null) return;
            var actions = SettingsPanelActions.CreateDefault();
            actions.OnBack = CloseSettingsSubPanel;
            actions.OnClose = CloseSettingsSubPanel;
            actions.OnReset = () =>
            {
                actions.DefaultReset();
                AudioManager.Instance?.PlayUIClick(0.3f);
            };
            SettingsPanelBuilder.Bind(homeSettingsHandle, actions);
        }

        // ================= 工具 =================

        private static TMP_Text CreateText(Transform parent, string goName, int fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            return tmp;
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

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
