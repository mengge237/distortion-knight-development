using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MutationChess.UI;

namespace MutationChess.Core
{
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("面板控件")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetButton;

        [Header("显示设置")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("音量设置")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private TMP_Text sfxVolumeText;

        [Header("UI样式")]
        [SerializeField] private TMP_Dropdown uiStyleDropdown;

        [Header("FPS显示")]
        [SerializeField] private Toggle showFpsToggle;
        [SerializeField] private TMP_Text fpsText;

        [Header("Boss遗物音效")]
        [Tooltip("Boss遗物选取主题音效开关（缺失时运行时在设置面板内自动构建）")]
        [SerializeField] private Toggle bossRelicSfxToggle;

        // 显示设置附加行（窗口模式/目标帧率/长宽比——运行时自动构建，无场景接线）
        private TMP_Text windowModeValueTmp;
        private TMP_Text targetFpsValueTmp;
        private TMP_Text aspectValueTmp;

        private Resolution[] resolutions;
        private float fpsTimer = 0f;
        private int frameCount = 0;
        private bool settingsOpen = false;
        private bool isInitialized = false;

        private readonly string[] uiStyleOptions = { "默认", "简洁", "经典", "深色" };

        void Awake()
        {
            if (Instance != null && Instance != this)
            {

                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        void Start()
        {
            if (Instance != this) return;
            if (isInitialized) return;
            isInitialized = true;

            InitializeSettings();
            LoadSettings();

            if (settingsButton != null)
                settingsButton.onClick.AddListener(ToggleSettings);
            if (closeButton != null)
                closeButton.onClick.AddListener(HideSettings);
            if (saveButton != null)
                saveButton.onClick.AddListener(SaveSettings);
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetSettings);

            if (showFpsToggle != null)
            {
                bool showFps = PlayerPrefs.GetInt("ShowFPS", 0) == 1;
                showFpsToggle.isOn = showFps;
                if (fpsText != null)
                    fpsText.gameObject.SetActive(showFps);
            }

            // Boss遗物主题音效开关（场景缺失时运行时自动构建）
            EnsureBossRelicSfxToggle();

            // 显示设置附加行：窗口模式/目标帧率/长宽比（场景缺失时运行时自动构建）
            EnsureExtraSettingRows();

            // 启动恢复显示设置（目标帧率/窗口模式/长宽比）
            DisplaySettings.ApplyAll();

            // 设置面板统一手感（按压回弹/悬停/点击音效）
            if (settingsPanel != null)
                UiFeel.ApplyToAllButtons(settingsPanel);
        }

        void Update()
        {
            if (Instance != this) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // 牌库档案打开时，ESC 由档案面板接管（避免与设置面板同时开关）
                if (CardArchivePanel.IsAnyVisible)
                    return;
                ToggleSettings();
            }

            if (showFpsToggle != null && showFpsToggle.isOn)
            {
                frameCount++;
                fpsTimer += Time.unscaledDeltaTime;
                if (fpsTimer >= 0.5f)
                {
                    int fps = Mathf.RoundToInt(frameCount / fpsTimer);
                    if (fpsText != null)
                        fpsText.text = $"FPS: {fps}";
                    fpsTimer = 0f;
                    frameCount = 0;
                }
            }
        }

        void InitializeSettings()
        {
            if (resolutionDropdown != null)
            {
                resolutions = Screen.resolutions;
                resolutionDropdown.ClearOptions();

                List<string> options = new List<string>();
                int currentIndex = 0;
                for (int i = 0; i < resolutions.Length; i++)
                {
                    float refreshRate = (float)resolutions[i].refreshRateRatio.value;
                    options.Add($"{resolutions[i].width} x {resolutions[i].height} @ {Mathf.RoundToInt(refreshRate)}Hz");

                    if (resolutions[i].width == Screen.currentResolution.width &&
                        resolutions[i].height == Screen.currentResolution.height)
                        currentIndex = i;
                }

                resolutionDropdown.AddOptions(options);
                resolutionDropdown.value = currentIndex;
                resolutionDropdown.RefreshShownValue();
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            if (uiStyleDropdown != null)
            {
                uiStyleDropdown.ClearOptions();
                uiStyleDropdown.AddOptions(new List<string>(uiStyleOptions));
                uiStyleDropdown.value = 0;
                uiStyleDropdown.RefreshShownValue();
                uiStyleDropdown.onValueChanged.AddListener(OnUIStyleChanged);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
                UpdateVolumeText(masterVolumeSlider, masterVolumeText);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                UpdateVolumeText(musicVolumeSlider, musicVolumeText);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
                UpdateVolumeText(sfxVolumeSlider, sfxVolumeText);
            }

            if (showFpsToggle != null)
                showFpsToggle.onValueChanged.AddListener(OnShowFpsChanged);
        }

        void UpdateVolumeText(Slider slider, TMP_Text text)
        {
            if (slider == null || text == null) return;
            text.text = $"{Mathf.RoundToInt(slider.value * 100)}%";
        }

        public void ToggleSettings()
        {
            if (settingsPanel == null) return;
            settingsOpen = !settingsOpen;
            settingsPanel.SetActive(settingsOpen);
            if (settingsOpen)
                UiFeel.AnimatePanelIn(settingsPanel); // 打开时面板弹入
            Time.timeScale = settingsOpen ? 0f : 1f;
        }

        public void ShowSettings()
        {
            if (settingsPanel == null) return;
            settingsPanel.SetActive(true);
            settingsOpen = true;
            UiFeel.AnimatePanelIn(settingsPanel); // 打开时面板弹入
            Time.timeScale = 0f;
        }

        public void HideSettings()
        {
            if (settingsPanel == null) return;
            settingsPanel.SetActive(false);
            settingsOpen = false;
            Time.timeScale = 1f;
        }

        void OnResolutionChanged(int index)
        {
            if (index < 0 || index >= resolutions.Length) return;
            Resolution res = resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
            PlayerPrefs.SetInt("ResolutionIndex", index);
            PlayerPrefs.Save();
        }

        void OnFullscreenChanged(bool isFullscreen)
        {
            // 经由 DisplaySettings 统一入口：同步 WindowMode 键与旧 Fullscreen 键，两处显示设置互不打架
            DisplaySettings.SetFullscreen(isFullscreen);
        }

        void OnUIStyleChanged(int index)
        {
            PlayerPrefs.SetInt("UIStyleIndex", index);
            PlayerPrefs.Save();
        }

        void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            UpdateVolumeText(masterVolumeSlider, masterVolumeText);
            PlayerPrefs.SetFloat("MasterVolume", value);
            PlayerPrefs.Save();
        }

        void OnMusicVolumeChanged(float value)
        {
            UpdateVolumeText(musicVolumeSlider, musicVolumeText);
            PlayerPrefs.SetFloat("MusicVolume", value);
            PlayerPrefs.Save();
        }

        void OnSFXVolumeChanged(float value)
        {
            UpdateVolumeText(sfxVolumeSlider, sfxVolumeText);
            AudioManager.SetSFXVolume(value); // 同步到音效管理器（实际生效）
            PlayerPrefs.SetFloat("SFXVolume", value);
            PlayerPrefs.Save();
        }

        void OnBossRelicSfxChanged(bool enabled)
        {
            AudioManager.SetBossRelicPickSfxEnabled(enabled);
            // 反馈：切换后播放一次轻响（开）以确认手感
            if (enabled)
                AudioManager.Instance?.PlayUIClick(0.3f);
        }

        void OnShowFpsChanged(bool show)
        {
            PlayerPrefs.SetInt("ShowFPS", show ? 1 : 0);
            PlayerPrefs.Save();
            if (fpsText != null)
                fpsText.gameObject.SetActive(show);
            MutationChess.UI.FpsDisplay.SetVisible(show); // 无场景角标时由 FpsDisplay 组件接管（首页等）
        }

        /// <summary>
        /// Boss遗物主题音效开关：场景已接线则直接使用；否则运行时在设置面板内
        /// 以 SFX 滑条为锚点自动构建一个标准 Toggle 行（含中文字体标签）。
        /// </summary>
        private void EnsureBossRelicSfxToggle()
        {
            if (bossRelicSfxToggle != null)
            {
                bossRelicSfxToggle.isOn = AudioManager.IsBossRelicPickSfxEnabled();
                bossRelicSfxToggle.onValueChanged.RemoveListener(OnBossRelicSfxChanged);
                bossRelicSfxToggle.onValueChanged.AddListener(OnBossRelicSfxChanged);
                return;
            }
            if (settingsPanel == null || sfxVolumeSlider == null) return;

            RectTransform sliderRt = sfxVolumeSlider.GetComponent<RectTransform>();
            if (sliderRt == null) return;
            Transform anchorParent = sliderRt.parent;

            TMP_FontAsset font = UiFonts.Load();

            // 行容器：开关盒 + 中文标签
            GameObject rowGo = new GameObject("BossRelicSfxToggle", typeof(RectTransform));
            rowGo.transform.SetParent(anchorParent, false);
            RectTransform rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(sliderRt.sizeDelta.x, 34f);
            if (anchorParent.GetComponent<VerticalLayoutGroup>() == null &&
                anchorParent.GetComponent<HorizontalLayoutGroup>() == null &&
                anchorParent.GetComponent<GridLayoutGroup>() == null)
            {
                // 无自动布局：手动放在 SFX 滑条下方
                rowRt.anchorMin = rowRt.anchorMax = sliderRt.anchorMin;
                rowRt.pivot = sliderRt.pivot;
                rowRt.anchoredPosition = sliderRt.anchoredPosition + new Vector2(0f, -42f);
            }

            // 标签
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(rowGo.transform, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.68f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            TMP_Text label = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = new Color(0.9f, 0.88f, 0.8f);
            label.text = "Boss遗物主题音效（选取时播放）";

            // 标准开关盒（防御式构建：逐组件判空，缺失时补建，避免运行时 NRE）
            GameObject toggleGo = new GameObject("Switch", typeof(RectTransform), typeof(Image));
            toggleGo.transform.SetParent(rowGo.transform, false);
            RectTransform toggleRt = toggleGo.GetComponent<RectTransform>();
            if (toggleRt == null) toggleRt = toggleGo.AddComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(0.72f, 0.5f);
            toggleRt.anchorMax = new Vector2(0.72f, 0.5f);
            toggleRt.pivot = new Vector2(0f, 0.5f);
            toggleRt.sizeDelta = new Vector2(64f, 30f);
            Image bg = toggleGo.GetComponent<Image>();
            if (bg == null) bg = toggleGo.AddComponent<Image>();
            bg.color = new Color(0.24f, 0.22f, 0.18f, 1f);

            GameObject markGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            markGo.transform.SetParent(toggleGo.transform, false);
            RectTransform markRt = markGo.GetComponent<RectTransform>();
            if (markRt == null) markRt = markGo.AddComponent<RectTransform>();
            markRt.anchorMin = new Vector2(0.1f, 0.15f);
            markRt.anchorMax = new Vector2(0.9f, 0.85f);
            markRt.offsetMin = Vector2.zero;
            markRt.offsetMax = Vector2.zero;
            Image mark = markGo.GetComponent<Image>();
            if (mark == null) mark = markGo.AddComponent<Image>();
            mark.color = new Color(0.85f, 0.72f, 0.35f, 1f);

            // Toggle 单独补建（若 GameObject 构造未挂载则手动添加），并强制绑定 targetGraphic
            Toggle toggle = toggleGo.GetComponent<Toggle>();
            if (toggle == null) toggle = toggleGo.AddComponent<Toggle>();
            if (toggle == null)
            {
                GameLogger.LogError("[Settings] Boss遗物主题音效开关创建失败（Toggle 组件缺失），已跳过");
                if (rowGo != null) Destroy(rowGo);
                return;
            }
            // Toggle 本身即 Selectable（自带点击/色变反馈），不可再挂 Button——
            // Button 与 Toggle 同属 Selectable，同一 GameObject 挂第二个会抛
            // "A GameObject can only contain one 'Selectable' component"
            toggle.transition = Selectable.Transition.ColorTint;
            toggle.targetGraphic = bg;
            toggle.graphic = mark;
            toggle.isOn = AudioManager.IsBossRelicPickSfxEnabled();
            toggle.onValueChanged.AddListener(OnBossRelicSfxChanged);

            bossRelicSfxToggle = toggle;
            GameLogger.Log("[Settings] Boss遗物主题音效开关已运行时构建");
        }

        // ================= 显示设置附加行（窗口模式/目标帧率/长宽比） =================

        /// <summary>
        /// 构建三条步进选择行：窗口模式 / 目标帧率 / 长宽比（场景缺失时运行时自动构建）。
        /// 以 Boss 遗物音效开关行（或 SFX 滑条行）为锚点依次向下排布，
        /// 左标签 + ◀ 值 ▶ 步进按钮（TMP_Dropdown 模板在运行时构建成本高，步进器更稳）。
        /// </summary>
        private void EnsureExtraSettingRows()
        {
            if (settingsPanel == null) return;
            if (windowModeValueTmp != null) return; // 幂等

            // 锚点：Boss 遗物行 → 回退 SFX 滑条行
            RectTransform anchor = null;
            if (bossRelicSfxToggle != null)
                anchor = bossRelicSfxToggle.GetComponent<RectTransform>();
            if (anchor == null && sfxVolumeSlider != null)
                anchor = sfxVolumeSlider.GetComponent<RectTransform>();
            if (anchor == null) return;

            TMP_FontAsset font = UiFonts.Load();
            Transform parent = anchor.parent;
            float yOffset = -44f;

            windowModeValueTmp = BuildStepperRow(parent, font, anchor, yOffset, "Row_窗口模式", "窗口模式",
                DisplaySettings.WindowModeNames, DisplaySettings.GetWindowMode(), idx =>
                {
                    DisplaySettings.SetWindowMode(idx);
                    RefreshExtraSettingRowLabels();
                });
            yOffset -= 44f;

            string[] fpsLabels = new string[DisplaySettings.TargetFpsOptions.Length];
            for (int i = 0; i < fpsLabels.Length; i++)
                fpsLabels[i] = DisplaySettings.TargetFpsOptions[i] > 0 ? $"{DisplaySettings.TargetFpsOptions[i]} FPS" : "不限";
            int fpsIndex = 0;
            for (int i = 0; i < DisplaySettings.TargetFpsOptions.Length; i++)
                if (DisplaySettings.TargetFpsOptions[i] == DisplaySettings.GetTargetFPS()) fpsIndex = i;
            targetFpsValueTmp = BuildStepperRow(parent, font, anchor, yOffset, "Row_目标帧率", "目标帧率",
                fpsLabels, fpsIndex, idx =>
                {
                    DisplaySettings.SetTargetFPS(DisplaySettings.TargetFpsOptions[idx]);
                    RefreshExtraSettingRowLabels();
                });
            yOffset -= 44f;

            aspectValueTmp = BuildStepperRow(parent, font, anchor, yOffset, "Row_长宽比", "长宽比",
                DisplaySettings.AspectRatioNames, DisplaySettings.GetAspectRatioIndex(), idx =>
                {
                    DisplaySettings.SetAspectRatioIndex(idx);
                    RefreshExtraSettingRowLabels();
                });

            GameLogger.Log("[Settings] 显示设置附加行已运行时构建（窗口模式/目标帧率/长宽比）");
        }

        /// <summary>构建单个步进选择行：左标签 + ◀ 当前值 ▶（相对锚定，分辨率无关）。返回值文本。</summary>
        private TMP_Text BuildStepperRow(Transform parent, TMP_FontAsset font, RectTransform anchor, float yOffset, string rowName, string label, string[] options, int valueIndex, System.Action<int> onChanged)
        {
            GameObject rowGo = new GameObject(rowName, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            RectTransform rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(anchor.sizeDelta.x, 34f);
            if (parent.GetComponent<VerticalLayoutGroup>() == null &&
                parent.GetComponent<HorizontalLayoutGroup>() == null &&
                parent.GetComponent<GridLayoutGroup>() == null)
            {
                // 无自动布局：手动排在锚点行下方
                rowRt.anchorMin = rowRt.anchorMax = anchor.anchorMin;
                rowRt.pivot = anchor.pivot;
                rowRt.anchoredPosition = anchor.anchoredPosition + new Vector2(0f, yOffset);
            }

            // 左标签
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(rowGo.transform, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.42f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            TMP_Text labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) labelTmp.font = font;
            labelTmp.fontSize = 22f;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.color = new Color(0.9f, 0.88f, 0.8f);
            labelTmp.text = label;

            // 当前值
            GameObject valueGo = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            valueGo.transform.SetParent(rowGo.transform, false);
            RectTransform valueRt = valueGo.GetComponent<RectTransform>();
            valueRt.anchorMin = new Vector2(0.55f, 0f);
            valueRt.anchorMax = new Vector2(0.84f, 1f);
            valueRt.offsetMin = Vector2.zero;
            valueRt.offsetMax = Vector2.zero;
            TMP_Text valueTmp = valueGo.GetComponent<TextMeshProUGUI>();
            if (font != null) valueTmp.font = font;
            valueTmp.fontSize = 21f;
            valueTmp.alignment = TextAlignmentOptions.Center;
            valueTmp.color = new Color(0.85f, 0.78f, 0.55f);
            valueTmp.text = options[Mathf.Clamp(valueIndex, 0, options.Length - 1)];

            // ◀ / ▶ 步进按钮（共享捕获变量 current，反复点击持续增减而非永远从初始值起步）
            int current = valueIndex;
            CreateStepButton(rowGo.transform, font, "PrevButton", "◀", new Vector2(0.44f, 0.15f), new Vector2(0.54f, 0.85f), () =>
            {
                current = Mathf.Max(0, current - 1);
                onChanged?.Invoke(current);
                UpdateStepValue(valueTmp, options, current);
                AudioManager.Instance?.PlayUIClick(0.25f);
            });
            CreateStepButton(rowGo.transform, font, "NextButton", "▶", new Vector2(0.85f, 0.15f), new Vector2(0.95f, 0.85f), () =>
            {
                current = Mathf.Min(options.Length - 1, current + 1);
                onChanged?.Invoke(current);
                UpdateStepValue(valueTmp, options, current);
                AudioManager.Instance?.PlayUIClick(0.25f);
            });

            // 步进器闭包捕获 valueIndex 后刷新由回调负责；按钮只做 +1/-1
            return valueTmp;
        }

        /// <summary>创建 ◀/▶ 步进小按钮。</summary>
        private void CreateStepButton(Transform parent, TMP_FontAsset font, string goName, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
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

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            TMP_Text labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) labelTmp.font = font;
            labelTmp.fontSize = 18f;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = new Color(0.9f, 0.86f, 0.66f);
            labelTmp.text = label;

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onClick);
            UiFeel.ApplyButton(btn);
        }

        private static void UpdateStepValue(TMP_Text valueTmp, string[] options, int index)
        {
            if (valueTmp == null) return;
            valueTmp.text = options[Mathf.Clamp(index, 0, options.Length - 1)];
        }

        /// <summary>从当前 PlayerPrefs 刷新三条步进行的显示值（读档/重置后调用）。</summary>
        private void RefreshExtraSettingRowLabels()
        {
            if (windowModeValueTmp != null)
                windowModeValueTmp.text = DisplaySettings.GetWindowModeLabel();
            if (targetFpsValueTmp != null)
                targetFpsValueTmp.text = DisplaySettings.GetTargetFpsLabel();
            if (aspectValueTmp != null)
                aspectValueTmp.text = DisplaySettings.GetAspectRatioLabel();
        }

        void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        void LoadSettings()
        {
            if (resolutionDropdown != null && PlayerPrefs.HasKey("ResolutionIndex"))
            {
                int index = PlayerPrefs.GetInt("ResolutionIndex");
                if (index >= 0 && index < resolutions.Length)
                {
                    resolutionDropdown.value = index;
                    resolutionDropdown.RefreshShownValue();
                    Resolution res = resolutions[index];
                    Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
                }
            }

            if (uiStyleDropdown != null && PlayerPrefs.HasKey("UIStyleIndex"))
            {
                int index = PlayerPrefs.GetInt("UIStyleIndex");
                if (index < uiStyleOptions.Length)
                {
                    uiStyleDropdown.value = index;
                    uiStyleDropdown.RefreshShownValue();
                }
            }

            if (fullscreenToggle != null && PlayerPrefs.HasKey("Fullscreen"))
            {
                fullscreenToggle.isOn = PlayerPrefs.GetInt("Fullscreen") == 1;
                Screen.fullScreen = fullscreenToggle.isOn;
            }

            if (masterVolumeSlider != null && PlayerPrefs.HasKey("MasterVolume"))
            {
                float vol = PlayerPrefs.GetFloat("MasterVolume");
                masterVolumeSlider.value = vol;
                AudioListener.volume = vol;
                UpdateVolumeText(masterVolumeSlider, masterVolumeText);
            }

            if (musicVolumeSlider != null && PlayerPrefs.HasKey("MusicVolume"))
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume");
                UpdateVolumeText(musicVolumeSlider, musicVolumeText);
            }

            if (sfxVolumeSlider != null && PlayerPrefs.HasKey("SFXVolume"))
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume");
                UpdateVolumeText(sfxVolumeSlider, sfxVolumeText);
                AudioManager.SetSFXVolume(sfxVolumeSlider.value); // 同步到音效管理器
            }

            if (showFpsToggle != null && PlayerPrefs.HasKey("ShowFPS"))
            {
                bool show = PlayerPrefs.GetInt("ShowFPS") == 1;
                showFpsToggle.isOn = show;
                if (fpsText != null)
                    fpsText.gameObject.SetActive(show);
            }

            // 窗口模式/目标帧率/长宽比步进行读数（EnsureExtraSettingRows 之后行已存在）
            RefreshExtraSettingRowLabels();
        }

        public void ResetSettings()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = 1f;
                AudioListener.volume = 1f;
                UpdateVolumeText(masterVolumeSlider, masterVolumeText);
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = 0.8f;
                UpdateVolumeText(musicVolumeSlider, musicVolumeText);
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = 0.8f;
                UpdateVolumeText(sfxVolumeSlider, sfxVolumeText);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = true;
                Screen.fullScreen = true;
            }

            if (showFpsToggle != null)
            {
                showFpsToggle.isOn = false;
                if (fpsText != null)
                    fpsText.gameObject.SetActive(false);
            }

            if (uiStyleDropdown != null)
            {
                uiStyleDropdown.value = 0;
                uiStyleDropdown.RefreshShownValue();
            }

            if (bossRelicSfxToggle != null)
                bossRelicSfxToggle.isOn = true;

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            // 恢复默认值后重新落盘（DeleteAll 已清空旧值）
            AudioManager.SetSFXVolume(0.8f);
            AudioManager.SetBossRelicPickSfxEnabled(true);

            if (resolutions != null && resolutions.Length > 0)
            {
                var res = resolutions[resolutions.Length - 1];
                Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
            }

            // 显示设置恢复默认并即时应用（DeleteAll 后各键回默认：帧率不限/全屏/跟随分辨率）
            DisplaySettings.ApplyAll();
            if (fullscreenToggle != null)
                fullscreenToggle.isOn = Screen.fullScreen;
            RefreshExtraSettingRowLabels();

        }

        public bool IsSettingsOpen() => settingsOpen;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}