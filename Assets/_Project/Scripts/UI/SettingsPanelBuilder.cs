using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;

namespace MutationChess.UI
{
    /// <summary>
    /// 标签页式设置面板构建器（首页与战斗共用，运行时构建与场景生成共用）：
    /// 显示 / 音量 / 游戏 三个标签页 + 固定内容区滚轮（ScrollRect，内容超高时滚动
    /// 而非溢出），面板外框锚点直贴画布四边——任何屏幕比例下外框都完整落在屏内，
    /// 不再依赖固定像素尺寸被屏幕比例裁切。同一套结构既可由 HomeSceneSetup 在
    /// 编辑器里生成进场景（可手动编辑），也可在场景缺失时运行时自动构建——两处
    /// 共用 Build/GetHandle/Bind，零双份维护。
    /// </summary>
    public class SettingsPanelHandle
    {
        public GameObject Panel;
        public ScrollRect Scroll;
        public Button[] TabButtons;
        public TMP_Text[] TabLabels;
        public Transform[] TabContents;
        public TMP_Text WindowModeValue, TargetFpsValue, AspectValue, ResOptionValue, QualityValue, UiStyleValue;
        public Toggle FullscreenToggle, ShowFpsToggle, BossSfxToggle;
        public Slider MasterSlider, MusicSlider, SfxSlider;
        public TMP_Text MasterPercent, MusicPercent, SfxPercent;
        public Button BackButton, ResetButton, CloseButton;

        /// <summary>全部控件按当前 PlayerPrefs/Screen 回读刷新（打开面板/读档/恢复默认后调用）。</summary>
        public void RefreshAll()
        {
            if (WindowModeValue != null) WindowModeValue.text = DisplaySettings.GetWindowModeLabel();
            if (TargetFpsValue != null) TargetFpsValue.text = DisplaySettings.GetTargetFpsLabel();
            if (AspectValue != null) AspectValue.text = DisplaySettings.GetAspectRatioLabel();
            if (ResOptionValue != null) ResOptionValue.text = DisplaySettings.GetResOptionLabel();
            if (QualityValue != null) QualityValue.text = DisplaySettings.GetQualityLabel();
            if (UiStyleValue != null) UiStyleValue.text = SettingsPanelBuilder.UiStyleNames[SettingsPanelBuilder.GetUiStyleIndex()];
            if (FullscreenToggle != null) FullscreenToggle.isOn = Screen.fullScreen;
            if (ShowFpsToggle != null) ShowFpsToggle.isOn = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
            if (BossSfxToggle != null) BossSfxToggle.isOn = AudioManager.IsBossRelicPickSfxEnabled();
            if (MasterSlider != null) { MasterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 1f); UpdatePercent(MasterPercent, MasterSlider.value); }
            if (MusicSlider != null) { MusicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.8f); UpdatePercent(MusicPercent, MusicSlider.value); }
            if (SfxSlider != null) { SfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.8f); UpdatePercent(SfxPercent, SfxSlider.value); }
        }

        private static void UpdatePercent(TMP_Text text, float value)
        {
            if (text != null)
                text.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }

    /// <summary>
    /// 设置项动作集合：写入 PlayerPrefs 并即时应用。场景生成模式不接线（运行时
    /// Bind 再挂载），HomeScreen/SettingsManager 各自创建实例并覆写 OnBack/OnClose。
    /// </summary>
    public class SettingsPanelActions
    {
        public static SettingsPanelActions CreateDefault() => new SettingsPanelActions();

        public Action<int> OnWindowMode, OnTargetFps, OnAspectRatio, OnResOption, OnQuality, OnUiStyle;
        public Action<float> OnMasterVolume, OnMusicVolume, OnSfxVolume;
        public Action<bool> OnShowFps, OnBossSfx, OnFullscreen;
        public Action OnReset, OnBack, OnClose;

        public SettingsPanelActions()
        {
            OnWindowMode = idx => DisplaySettings.SetWindowMode(idx);
            OnTargetFps = idx => DisplaySettings.SetTargetFPS(DisplaySettings.TargetFpsOptions[idx]);
            OnAspectRatio = idx => DisplaySettings.SetAspectRatioIndex(idx);
            OnResOption = idx => DisplaySettings.SetResOptionIndex(idx);
            OnQuality = idx => DisplaySettings.SetQualityIndex(idx);
            OnUiStyle = idx => { PlayerPrefs.SetInt("UIStyleIndex", idx); PlayerPrefs.Save(); };
            OnMasterVolume = v => { AudioListener.volume = v; PlayerPrefs.SetFloat("MasterVolume", v); PlayerPrefs.Save(); };
            OnMusicVolume = v => { PlayerPrefs.SetFloat("MusicVolume", v); PlayerPrefs.Save(); };
            OnSfxVolume = v => { AudioManager.SetSFXVolume(v); };
            OnShowFps = v => { PlayerPrefs.SetInt("ShowFPS", v ? 1 : 0); PlayerPrefs.Save(); FpsDisplay.SetVisible(v); };
            OnBossSfx = v =>
            {
                AudioManager.SetBossRelicPickSfxEnabled(v);
                if (v) AudioManager.Instance?.PlayUIClick(0.3f);
            };
            OnFullscreen = v => DisplaySettings.SetFullscreen(v);
            OnReset = DefaultReset;
        }

        /// <summary>恢复默认：显示+音量全部回默认并立即应用（只清设置键，不动存档键）。</summary>
        public void DefaultReset()
        {
            DisplaySettings.ResetToDefaults();
            AudioListener.volume = 1f;
            AudioManager.SetSFXVolume(0.8f);
            AudioManager.SetBossRelicPickSfxEnabled(true);
        }
    }

    public static class SettingsPanelBuilder
    {
        public static readonly string[] UiStyleNames = { "默认", "简洁", "经典", "深色" };

        private const int TabCount = 3;
        private static readonly string[] TabNames = { "显示", "音量", "游戏" };

        private static TMP_FontAsset cachedFont;

        public static int GetUiStyleIndex() => Mathf.Clamp(PlayerPrefs.GetInt("UIStyleIndex", 0), 0, UiStyleNames.Length - 1);

        /// <summary>
        /// 构建标签页式设置面板（只搭结构不接线，接线由 Bind 完成——场景版运行时
        /// 接线、运行时版构建后立即接线，同一条链路）。父物体须为全屏画布根。
        /// </summary>
        public static SettingsPanelHandle Build(Transform parent, string panelName)
        {
            var h = new SettingsPanelHandle();

            // ===== 面板外框：锚点直贴画布四边（任何屏幕比例都在屏内） =====
            GameObject panelGo = new GameObject(panelName, typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.08f, 0.055f);
            panelRt.anchorMax = new Vector2(0.92f, 0.945f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            Image panelImg = panelGo.GetComponent<Image>();
            Sprite innerBg = Resources.Load<Sprite>("InterfaceUI/获胜奖励面板底层内嵌背景");
            if (innerBg != null)
            {
                panelImg.sprite = innerBg;
                panelImg.color = Color.white;
            }
            else panelImg.color = new Color(0.08f, 0.08f, 0.11f, 0.99f);
            h.Panel = panelGo;

            // ===== 标题 =====
            TMP_Text title = CreateText(panelGo.transform, "Title", 40, TextAlignmentOptions.Center, new Color(0.92f, 0.8f, 0.42f));
            title.fontStyle = FontStyles.Bold;
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -14f);
            titleRt.sizeDelta = new Vector2(560f, 56f);
            title.text = "设 置";

            // ===== 关闭按钮（右上角） =====
            GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panelGo.transform, false);
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-18f, -16f);
            closeRt.sizeDelta = new Vector2(140f, 48f);
            closeGo.GetComponent<Image>().color = new Color(0.32f, 0.16f, 0.12f, 0.95f);
            TMP_Text closeLabel = CreateText(closeGo.transform, "Label", 22, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.8f));
            StretchFull(closeLabel.rectTransform);
            closeLabel.text = "✕ 关闭";
            h.CloseButton = SetupButton(closeGo);

            // ===== 标签页栏 =====
            GameObject tabBarGo = new GameObject("TabBar", typeof(RectTransform));
            tabBarGo.transform.SetParent(panelGo.transform, false);
            RectTransform tabBarRt = tabBarGo.GetComponent<RectTransform>();
            tabBarRt.anchorMin = tabBarRt.anchorMax = new Vector2(0.5f, 1f);
            tabBarRt.pivot = new Vector2(0.5f, 1f);
            tabBarRt.anchoredPosition = new Vector2(0f, -88f);
            tabBarRt.sizeDelta = new Vector2(780f, 58f);

            h.TabButtons = new Button[TabCount];
            h.TabLabels = new TMP_Text[TabCount];
            for (int i = 0; i < TabCount; i++)
            {
                GameObject tabGo = new GameObject("Tab_" + TabNames[i], typeof(RectTransform), typeof(Image), typeof(Button));
                tabGo.transform.SetParent(tabBarGo.transform, false);
                RectTransform tabRt = tabGo.GetComponent<RectTransform>();
                tabRt.anchorMin = tabRt.anchorMax = new Vector2(0.5f, 0.5f);
                tabRt.anchoredPosition = new Vector2(-260f + i * 260f, 0f);
                tabRt.sizeDelta = new Vector2(230f, 54f);
                Image tabImg = tabGo.GetComponent<Image>();
                tabImg.color = new Color(0.2f, 0.18f, 0.15f, 1f);
                TMP_Text tabLabel = CreateText(tabGo.transform, "Label", 26, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.8f));
                StretchFull(tabLabel.rectTransform);
                tabLabel.fontStyle = FontStyles.Bold;
                tabLabel.text = TabNames[i];
                Button tabBtn = tabGo.GetComponent<Button>();
                tabBtn.targetGraphic = tabImg;
                tabBtn.image = tabImg; // 运行时构建的 Button 不会自动关联 image 序列化字段
                tabBtn.transition = Selectable.Transition.None;
                h.TabButtons[i] = tabBtn;
                h.TabLabels[i] = tabLabel;
            }

            // ===== 内容滚动区（固定视口 + 滚轮，内容超高时滚动） =====
            GameObject scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(panelGo.transform, false);
            RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.05f, 0.145f);
            scrollRt.anchorMax = new Vector2(0.95f, 0.885f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            h.Scroll = scroll;

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            StretchFull(viewportGo.GetComponent<RectTransform>());

            GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero;
            VerticalLayoutGroup vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter csf = contentGo.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportGo.GetComponent<RectTransform>();
            scroll.content = contentRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.12f;
            scroll.scrollSensitivity = 40f;

            // ===== 三个标签页内容组 =====
            h.TabContents = new Transform[TabCount];
            h.TabContents[0] = CreateTabContent(contentGo.transform, "Content显示");
            h.TabContents[1] = CreateTabContent(contentGo.transform, "Content音量");
            h.TabContents[2] = CreateTabContent(contentGo.transform, "Content游戏");

            // —— 显示页：全屏/窗口模式/目标帧率/长宽比/分辨率/画质/FPS角标 ——
            Transform display = h.TabContents[0];
            BuildToggleRow(display, "全屏显示", Screen.fullScreen, out h.FullscreenToggle);
            h.WindowModeValue = BuildStepperRow(display, "窗口模式", DisplaySettings.WindowModeNames, DisplaySettings.GetWindowMode());
            string[] fpsLabels = new string[DisplaySettings.TargetFpsOptions.Length];
            for (int i = 0; i < fpsLabels.Length; i++)
                fpsLabels[i] = DisplaySettings.TargetFpsOptions[i] > 0 ? $"{DisplaySettings.TargetFpsOptions[i]} FPS" : "不限";
            int fpsIndex = Mathf.Max(0, Array.IndexOf(DisplaySettings.TargetFpsOptions, DisplaySettings.GetTargetFPS()));
            h.TargetFpsValue = BuildStepperRow(display, "目标帧率", fpsLabels, fpsIndex);
            h.AspectValue = BuildStepperRow(display, "长宽比", DisplaySettings.AspectRatioNames, DisplaySettings.GetAspectRatioIndex());
            h.ResOptionValue = BuildStepperRow(display, "分辨率", DisplaySettings.ResolutionNames, DisplaySettings.GetResOptionIndex());
            h.QualityValue = BuildStepperRow(display, "画质", DisplaySettings.QualityNames, DisplaySettings.GetQualityIndex());
            BuildToggleRow(display, "显示FPS角标", PlayerPrefs.GetInt("ShowFPS", 0) == 1, out h.ShowFpsToggle);

            // —— 音量页：主/音乐/音效 + Boss遗物主题音效 ——
            Transform audioTab = h.TabContents[1];
            BuildSliderRow(audioTab, "主音量", PlayerPrefs.GetFloat("MasterVolume", 1f), out h.MasterSlider, out h.MasterPercent);
            BuildSliderRow(audioTab, "音乐音量", PlayerPrefs.GetFloat("MusicVolume", 0.8f), out h.MusicSlider, out h.MusicPercent);
            BuildSliderRow(audioTab, "音效音量", PlayerPrefs.GetFloat("SFXVolume", 0.8f), out h.SfxSlider, out h.SfxPercent);
            BuildToggleRow(audioTab, "Boss遗物主题音效", AudioManager.IsBossRelicPickSfxEnabled(), out h.BossSfxToggle);

            // —— 游戏页：UI样式 + 说明 ——
            Transform gameTab = h.TabContents[2];
            h.UiStyleValue = BuildStepperRow(gameTab, "UI样式", UiStyleNames, GetUiStyleIndex());
            BuildInfoRow(gameTab, "Info",
                "· 设置立即生效并自动保存\n· 分辨率按系统支持列表切换\n· 恢复默认只重置显示与音量，不影响游戏存档");

            // ===== 底部固定栏：返回 + 恢复默认 + 滚轮提示 =====
            GameObject bottomGo = new GameObject("BottomBar", typeof(RectTransform));
            bottomGo.transform.SetParent(panelGo.transform, false);
            RectTransform bottomRt = bottomGo.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0.05f, 0f);
            bottomRt.anchorMax = new Vector2(0.95f, 0.125f);
            bottomRt.offsetMin = Vector2.zero;
            bottomRt.offsetMax = Vector2.zero;

            h.BackButton = BuildBottomButton(bottomGo.transform, "BackButton", "返 回", new Vector2(0f, 0.5f), new Vector2(110f, 0f), new Color(0.28f, 0.25f, 0.19f, 1f));
            h.ResetButton = BuildBottomButton(bottomGo.transform, "ResetButton", "恢复默认", new Vector2(1f, 0.5f), new Vector2(-110f, 0f), new Color(0.28f, 0.25f, 0.19f, 1f));

            TMP_Text bottomHint = CreateText(bottomGo.transform, "Hint", 18, TextAlignmentOptions.Center, new Color(0.6f, 0.58f, 0.52f));
            RectTransform hintRt = bottomHint.rectTransform;
            hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 0.5f);
            hintRt.pivot = new Vector2(0.5f, 0.5f);
            hintRt.anchoredPosition = Vector2.zero;
            hintRt.sizeDelta = new Vector2(560f, 30f);
            bottomHint.text = "内容过多时滚轮滚动查看";

            // 初始状态：显示页激活
            SwitchTab(h, 0);
            return h;
        }

        /// <summary>从已构建面板（场景实体）反查句柄（运行时接线用）。</summary>
        public static SettingsPanelHandle GetHandle(GameObject panel)
        {
            if (panel == null) return null;
            Transform t = panel.transform;
            var h = new SettingsPanelHandle();
            h.Panel = panel;
            h.Scroll = t.Find("Scroll")?.GetComponent<ScrollRect>();
            h.TabButtons = new Button[TabCount];
            h.TabLabels = new TMP_Text[TabCount];
            h.TabContents = new Transform[TabCount];
            for (int i = 0; i < TabCount; i++)
            {
                Transform tab = t.Find("TabBar/Tab_" + TabNames[i]);
                if (tab != null)
                {
                    h.TabButtons[i] = tab.GetComponent<Button>();
                    h.TabLabels[i] = tab.Find("Label")?.GetComponent<TMP_Text>();
                }
                h.TabContents[i] = t.Find("Scroll/Viewport/Content/Content" + TabNames[i]);
            }
            string contentRoot = "Scroll/Viewport/Content/";
            h.WindowModeValue = FindValue(t, contentRoot + "Content显示/Row_窗口模式");
            h.TargetFpsValue = FindValue(t, contentRoot + "Content显示/Row_目标帧率");
            h.AspectValue = FindValue(t, contentRoot + "Content显示/Row_长宽比");
            h.ResOptionValue = FindValue(t, contentRoot + "Content显示/Row_分辨率");
            h.QualityValue = FindValue(t, contentRoot + "Content显示/Row_画质");
            h.UiStyleValue = FindValue(t, contentRoot + "Content游戏/Row_UI样式");
            h.FullscreenToggle = FindToggle(t, contentRoot + "Content显示/Row_全屏显示");
            h.ShowFpsToggle = FindToggle(t, contentRoot + "Content显示/Row_显示FPS角标");
            h.BossSfxToggle = FindToggle(t, contentRoot + "Content音量/Row_Boss遗物主题音效");
            h.MasterSlider = FindSlider(t, contentRoot + "Content音量/Row_主音量");
            h.MusicSlider = FindSlider(t, contentRoot + "Content音量/Row_音乐音量");
            h.SfxSlider = FindSlider(t, contentRoot + "Content音量/Row_音效音量");
            if (h.MasterSlider != null) h.MasterPercent = h.MasterSlider.transform.parent.Find("Percent")?.GetComponent<TMP_Text>();
            if (h.MusicSlider != null) h.MusicPercent = h.MusicSlider.transform.parent.Find("Percent")?.GetComponent<TMP_Text>();
            if (h.SfxSlider != null) h.SfxPercent = h.SfxSlider.transform.parent.Find("Percent")?.GetComponent<TMP_Text>();
            h.BackButton = t.Find("BottomBar/BackButton")?.GetComponent<Button>();
            h.ResetButton = t.Find("BottomBar/ResetButton")?.GetComponent<Button>();
            h.CloseButton = t.Find("CloseButton")?.GetComponent<Button>();
            return h;
        }

        /// <summary>面板接线（场景版与运行时版共用）：清旧监听 → 回读当前值 → 挂动作。</summary>
        public static void Bind(SettingsPanelHandle h, SettingsPanelActions actions)
        {
            if (h == null || actions == null) return;

            // 标签页切换
            for (int i = 0; i < h.TabButtons.Length; i++)
            {
                if (h.TabButtons[i] == null) continue;
                h.TabButtons[i].onClick.RemoveAllListeners();
                int idx = i;
                h.TabButtons[i].onClick.AddListener(() => SwitchTab(h, idx));
            }

            // 步进行（共享捕获变量 current：◀/▶ 连点从当前值步进）
            WireStepper(h, h.WindowModeValue, DisplaySettings.WindowModeNames, actions.OnWindowMode);
            WireStepper(h, h.TargetFpsValue, BuildFpsLabels(), actions.OnTargetFps);
            WireStepper(h, h.AspectValue, DisplaySettings.AspectRatioNames, actions.OnAspectRatio);
            WireStepper(h, h.ResOptionValue, DisplaySettings.ResolutionNames, actions.OnResOption);
            WireStepper(h, h.QualityValue, DisplaySettings.QualityNames, actions.OnQuality);
            WireStepper(h, h.UiStyleValue, UiStyleNames, actions.OnUiStyle);

            // 开关（先回读再挂监听，避免赋值触发写回）
            WireToggle(h.FullscreenToggle, Screen.fullScreen, v => { actions.OnFullscreen?.Invoke(v); h.RefreshAll(); });
            WireToggle(h.ShowFpsToggle, PlayerPrefs.GetInt("ShowFPS", 0) == 1, v => { actions.OnShowFps?.Invoke(v); h.RefreshAll(); });
            WireToggle(h.BossSfxToggle, AudioManager.IsBossRelicPickSfxEnabled(), v => { actions.OnBossSfx?.Invoke(v); h.RefreshAll(); });

            // 滑条
            WireSlider(h.MasterSlider, h.MasterPercent, PlayerPrefs.GetFloat("MasterVolume", 1f), actions.OnMasterVolume);
            WireSlider(h.MusicSlider, h.MusicPercent, PlayerPrefs.GetFloat("MusicVolume", 0.8f), actions.OnMusicVolume);
            WireSlider(h.SfxSlider, h.SfxPercent, PlayerPrefs.GetFloat("SFXVolume", 0.8f), actions.OnSfxVolume);

            // 底部按钮
            if (h.CloseButton != null)
            {
                h.CloseButton.onClick.RemoveAllListeners();
                h.CloseButton.onClick.AddListener(() => actions.OnClose?.Invoke());
            }
            if (h.BackButton != null)
            {
                h.BackButton.onClick.RemoveAllListeners();
                h.BackButton.onClick.AddListener(() => actions.OnBack?.Invoke());
            }
            if (h.ResetButton != null)
            {
                h.ResetButton.onClick.RemoveAllListeners();
                h.ResetButton.onClick.AddListener(() =>
                {
                    actions.OnReset?.Invoke();
                    h.RefreshAll();
                });
            }

            UiFeel.ApplyToAllButtons(h.Panel);
        }

        // ================= 结构构建 =================

        private static Transform CreateTabContent(Transform content, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(content, false);
            VerticalLayoutGroup vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            ContentSizeFitter csf = go.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return go.transform;
        }

        /// <summary>行容器：LayoutElement 定高 58，容器高度对齐槽位（VLG 不驱动子物体高度）。</summary>
        private static GameObject CreateRow(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.GetComponent<LayoutElement>().preferredHeight = 58f;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 58f);
            return go;
        }

        /// <summary>步进行：左标签 + ◀ 当前值 ▶（只搭结构，监听由 Bind 挂载）。</summary>
        private static TMP_Text BuildStepperRow(Transform parent, string label, string[] options, int valueIndex)
        {
            GameObject rowGo = CreateRow(parent, "Row_" + label);

            TMP_Text labelTmp = CreateText(rowGo.transform, "Label", 23, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.88f, 0.8f));
            RectTransform labelRt = labelTmp.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.44f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            labelTmp.text = label;

            TMP_Text valueTmp = CreateText(rowGo.transform, "Value", 22, TextAlignmentOptions.Center, new Color(0.85f, 0.78f, 0.55f));
            RectTransform valueRt = valueTmp.rectTransform;
            valueRt.anchorMin = new Vector2(0.56f, 0f);
            valueRt.anchorMax = new Vector2(0.84f, 1f);
            valueRt.offsetMin = Vector2.zero;
            valueRt.offsetMax = Vector2.zero;
            valueTmp.text = options[Mathf.Clamp(valueIndex, 0, options.Length - 1)];

            BuildStepButton(rowGo.transform, "PrevButton", "◀", new Vector2(0.45f, 0.16f), new Vector2(0.55f, 0.84f));
            BuildStepButton(rowGo.transform, "NextButton", "▶", new Vector2(0.85f, 0.16f), new Vector2(0.95f, 0.84f));
            return valueTmp;
        }

        private static void BuildStepButton(Transform parent, string goName, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = go.GetComponent<Image>();
            img.color = new Color(0.24f, 0.22f, 0.18f, 1f);
            TMP_Text labelTmp = CreateText(go.transform, "Label", 18, TextAlignmentOptions.Center, new Color(0.9f, 0.86f, 0.66f));
            StretchFull(labelTmp.rectTransform);
            labelTmp.text = label;
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
        }

        /// <summary>滑条行：左标签 + 滑条（背景/填充/手柄）+ 右侧百分比。</summary>
        private static void BuildSliderRow(Transform parent, string label, float value, out Slider slider, out TMP_Text percent)
        {
            GameObject rowGo = CreateRow(parent, "Row_" + label);

            TMP_Text labelTmp = CreateText(rowGo.transform, "Label", 23, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.88f, 0.8f));
            RectTransform labelRt = labelTmp.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.32f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            labelTmp.text = label;

            GameObject sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(rowGo.transform, false);
            RectTransform sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.36f, 0.5f);
            sliderRt.anchorMax = new Vector2(0.78f, 0.5f);
            sliderRt.pivot = new Vector2(0.5f, 0.5f);
            sliderRt.sizeDelta = new Vector2(0f, 16f);
            Slider s = sliderGo.AddComponent<Slider>();

            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(sliderGo.transform, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            bgGo.GetComponent<Image>().color = new Color(0.2f, 0.19f, 0.17f, 1f);

            GameObject fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            RectTransform fillAreaRt = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(6f, 4f);
            fillAreaRt.offsetMax = new Vector2(-6f, -4f);
            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            StretchFull(fillGo.GetComponent<RectTransform>());
            fillGo.GetComponent<Image>().color = new Color(0.62f, 0.5f, 0.24f, 1f);

            GameObject handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            RectTransform handleAreaRt = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);
            GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            RectTransform handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(24f, 24f);
            handleGo.GetComponent<Image>().color = new Color(0.92f, 0.8f, 0.42f, 1f);

            s.fillRect = fillGo.GetComponent<RectTransform>();
            s.handleRect = handleRt;
            s.targetGraphic = handleGo.GetComponent<Image>();
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.value = value;

            percent = CreateText(rowGo.transform, "Percent", 21, TextAlignmentOptions.Center, new Color(0.85f, 0.78f, 0.55f));
            RectTransform percentRt = percent.rectTransform;
            percentRt.anchorMin = new Vector2(0.8f, 0f);
            percentRt.anchorMax = new Vector2(0.97f, 1f);
            percentRt.offsetMin = Vector2.zero;
            percentRt.offsetMax = Vector2.zero;
            percent.text = $"{Mathf.RoundToInt(value * 100)}%";

            slider = s;
        }

        /// <summary>开关行：左标签 + 右侧标准开关盒（Toggle 本身即 Selectable，不可再挂 Button）。</summary>
        private static void BuildToggleRow(Transform parent, string label, bool value, out Toggle toggle)
        {
            GameObject rowGo = CreateRow(parent, "Row_" + label);

            TMP_Text labelTmp = CreateText(rowGo.transform, "Label", 23, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.88f, 0.8f));
            RectTransform labelRt = labelTmp.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.72f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            labelTmp.text = label;

            GameObject toggleGo = new GameObject("Switch", typeof(RectTransform), typeof(Image));
            toggleGo.transform.SetParent(rowGo.transform, false);
            RectTransform toggleRt = toggleGo.GetComponent<RectTransform>();
            toggleRt.anchorMin = toggleRt.anchorMax = new Vector2(0.78f, 0.5f);
            toggleRt.pivot = new Vector2(0f, 0.5f);
            toggleRt.sizeDelta = new Vector2(64f, 30f);
            Image bg = toggleGo.GetComponent<Image>();
            bg.color = new Color(0.24f, 0.22f, 0.18f, 1f);

            GameObject markGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            markGo.transform.SetParent(toggleGo.transform, false);
            RectTransform markRt = markGo.GetComponent<RectTransform>();
            markRt.anchorMin = new Vector2(0.1f, 0.15f);
            markRt.anchorMax = new Vector2(0.9f, 0.85f);
            markRt.offsetMin = Vector2.zero;
            markRt.offsetMax = Vector2.zero;
            Image mark = markGo.GetComponent<Image>();
            mark.color = new Color(0.85f, 0.72f, 0.35f, 1f);

            Toggle t = toggleGo.AddComponent<Toggle>();
            t.transition = Selectable.Transition.ColorTint;
            t.targetGraphic = bg;
            t.graphic = mark;
            t.isOn = value;
            toggle = t;
        }

        private static void BuildInfoRow(Transform parent, string name, string text)
        {
            GameObject rowGo = CreateRow(parent, name);
            rowGo.GetComponent<LayoutElement>().preferredHeight = 96f;
            rowGo.GetComponent<RectTransform>().sizeDelta = new Vector2(rowGo.GetComponent<RectTransform>().sizeDelta.x, 96f);
            TMP_Text tmp = CreateText(rowGo.transform, "Label", 20, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.6f, 0.55f));
            RectTransform rt = tmp.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 0f);
            rt.offsetMax = new Vector2(-8f, 0f);
            tmp.text = text;
        }

        private static Button BuildBottomButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPosition, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(220f, 62f);
            Image img = go.GetComponent<Image>();
            img.color = color;
            TMP_Text labelTmp = CreateText(go.transform, "Label", 26, TextAlignmentOptions.Center, new Color(0.93f, 0.86f, 0.66f));
            StretchFull(labelTmp.rectTransform);
            labelTmp.text = label;
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            return btn;
        }

        private static Button SetupButton(GameObject go)
        {
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            btn.transition = Selectable.Transition.None;
            return btn;
        }

        // ================= 接线 =================

        private static void WireStepper(SettingsPanelHandle h, TMP_Text valueTmp, string[] options, Action<int> action)
        {
            if (valueTmp == null) return;
            Transform row = valueTmp.transform.parent;
            Button prev = row.Find("PrevButton")?.GetComponent<Button>();
            Button next = row.Find("NextButton")?.GetComponent<Button>();
            if (prev == null && next == null) return;

            // 从当前显示文本反查下标作为步进起点（场景版无构建期闭包）
            int current = Array.IndexOf(options, valueTmp.text);
            if (current < 0) current = 0;

            if (prev != null)
            {
                prev.onClick.RemoveAllListeners();
                prev.onClick.AddListener(() =>
                {
                    current = Mathf.Max(0, current - 1);
                    valueTmp.text = options[current];
                    action?.Invoke(current);
                    h.RefreshAll();
                    if (Application.isPlaying) AudioManager.Instance?.PlayUIClick(0.25f);
                });
            }
            if (next != null)
            {
                next.onClick.RemoveAllListeners();
                next.onClick.AddListener(() =>
                {
                    current = Mathf.Min(options.Length - 1, current + 1);
                    valueTmp.text = options[current];
                    action?.Invoke(current);
                    h.RefreshAll();
                    if (Application.isPlaying) AudioManager.Instance?.PlayUIClick(0.25f);
                });
            }
        }

        private static void WireToggle(Toggle toggle, bool value, Action<bool> onChanged)
        {
            if (toggle == null) return;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.isOn = value; // 先回读再挂监听，避免赋值触发写回
            toggle.onValueChanged.AddListener(v => onChanged?.Invoke(v));
        }

        private static void WireSlider(Slider slider, TMP_Text percent, float value, Action<float> onChanged)
        {
            if (slider == null) return;
            slider.onValueChanged.RemoveAllListeners();
            slider.value = value;
            if (percent != null)
                percent.text = $"{Mathf.RoundToInt(value * 100)}%";
            slider.onValueChanged.AddListener(v =>
            {
                if (percent != null)
                    percent.text = $"{Mathf.RoundToInt(v * 100)}%";
                onChanged?.Invoke(v);
            });
        }

        /// <summary>切换标签页：激活对应内容组 + 标签配色 + 滚轮回顶。</summary>
        private static void SwitchTab(SettingsPanelHandle h, int index)
        {
            if (h.TabContents == null) return;
            for (int i = 0; i < h.TabContents.Length; i++)
            {
                bool on = i == index;
                if (h.TabContents[i] != null)
                    h.TabContents[i].gameObject.SetActive(on);
                if (h.TabLabels != null && h.TabLabels[i] != null)
                    h.TabLabels[i].color = on ? new Color(0.15f, 0.12f, 0.06f, 1f) : new Color(0.85f, 0.85f, 0.8f, 1f);
                if (h.TabButtons != null && h.TabButtons[i] != null && h.TabButtons[i].image != null)
                    h.TabButtons[i].image.color = on ? new Color(0.85f, 0.72f, 0.35f, 1f) : new Color(0.2f, 0.18f, 0.15f, 1f);
            }
            if (h.Scroll != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(h.Scroll.content);
                h.Scroll.verticalNormalizedPosition = 1f;
            }
        }

        // ================= 工具 =================

        private static TMP_Text FindValue(Transform root, string rowPath)
        {
            Transform row = root.Find(rowPath);
            return row != null ? row.Find("Value")?.GetComponent<TMP_Text>() : null;
        }

        private static Toggle FindToggle(Transform root, string rowPath)
        {
            Transform row = root.Find(rowPath);
            return row != null ? row.Find("Switch")?.GetComponent<Toggle>() : null;
        }

        private static Slider FindSlider(Transform root, string rowPath)
        {
            Transform row = root.Find(rowPath);
            return row != null ? row.Find("Slider")?.GetComponent<Slider>() : null;
        }

        private static string[] BuildFpsLabels()
        {
            string[] fpsLabels = new string[DisplaySettings.TargetFpsOptions.Length];
            for (int i = 0; i < fpsLabels.Length; i++)
                fpsLabels[i] = DisplaySettings.TargetFpsOptions[i] > 0 ? $"{DisplaySettings.TargetFpsOptions[i]} FPS" : "不限";
            return fpsLabels;
        }

        private static TMP_Text CreateText(Transform parent, string goName, int fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = LoadFont();
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
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
    }
}
