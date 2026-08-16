using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 战斗内设置管理：ESC 开关设置面板。
    /// 面板统一为标签页式（显示/音量/游戏 + 滚轮内容区），由 SettingsPanelBuilder 构建：
    /// 旧版平铺面板（无 TabBar 标记）启动时自动销毁重建，旧序列化控件引用清空后由
    /// 新面板句柄回填同名字段——Update 的 FPS 统计、读档、恢复默认等逻辑零改动。
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("面板控件")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button resetButton;

        [Header("旧场景接线（面板重建后自动清空，仅读档兼容用）")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("音量设置（重建后指向新面板对应控件）")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeText;
        [SerializeField] private TMP_Text musicVolumeText;
        [SerializeField] private TMP_Text sfxVolumeText;

        [Header("UI样式（旧下拉，重建后由游戏页步进行接管）")]
        [SerializeField] private TMP_Dropdown uiStyleDropdown;

        [Header("FPS显示")]
        [SerializeField] private Toggle showFpsToggle;
        [SerializeField] private TMP_Text fpsText;

        [Header("Boss遗物音效")]
        [SerializeField] private Toggle bossRelicSfxToggle;

        private SettingsPanelHandle settingsHandle; // 标签页式设置面板句柄

        private Resolution[] resolutions;
        private float fpsTimer = 0f;
        private int frameCount = 0;
        private bool settingsOpen = false;
        private bool isInitialized = false;

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

            MigrateSettingsPanel(); // 旧平铺面板 → 标签页式重建/接线（先于控件初始化和读档）
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

        // ================= 面板迁移（标签页式） =================

        /// <summary>
        /// 设置面板迁移：新版标签页面板（含 TabBar 标记）直接反查句柄接线；
        /// 旧版平铺面板销毁重建为标签页式（显示/音量/游戏 + 滚轮内容区，外框锚点
        /// 直贴画布四边，任何屏幕比例都完整落在屏内）。重建后旧序列化控件引用全部
        /// 清空，由新面板句柄回填同名字段，Update/读档/重置逻辑继续可用。
        /// </summary>
        private void MigrateSettingsPanel()
        {
            if (settingsPanel != null && settingsPanel.transform.Find("TabBar") != null)
            {
                settingsHandle = SettingsPanelBuilder.GetHandle(settingsPanel);
                BindSettingsPanel();
                return;
            }

            if (settingsPanel == null) return;

            Transform oldParent = settingsPanel.transform.parent;
            settingsPanel.transform.SetParent(null);
            Destroy(settingsPanel);
            settingsPanel = null;
            // 旧面板已销毁，其控件引用全部失效——清空后由新面板句柄回填
            resolutionDropdown = null;
            uiStyleDropdown = null;
            fullscreenToggle = null;
            masterVolumeSlider = null;
            musicVolumeSlider = null;
            sfxVolumeSlider = null;
            masterVolumeText = null;
            musicVolumeText = null;
            sfxVolumeText = null;
            showFpsToggle = null;
            bossRelicSfxToggle = null;

            if (oldParent == null) return;
            settingsHandle = SettingsPanelBuilder.Build(oldParent, "SettingsPanel");
            settingsPanel = settingsHandle != null ? settingsHandle.Panel : null;
            if (settingsPanel == null) return;
            settingsPanel.transform.SetAsLastSibling();
            settingsPanel.SetActive(false);
            BindSettingsPanel();
        }

        /// <summary>标签页式设置面板接线：动作挂载 + 旧字段回填（战斗 HUD 旧 FPS 文本跟随开关）。</summary>
        private void BindSettingsPanel()
        {
            if (settingsHandle == null) return;
            var actions = SettingsPanelActions.CreateDefault();
            actions.OnBack = HideSettings;
            actions.OnClose = HideSettings;
            actions.OnReset = () =>
            {
                actions.DefaultReset();
                AudioManager.Instance?.PlayUIClick(0.3f);
            };
            SettingsPanelBuilder.Bind(settingsHandle, actions);

            // 旧字段回填（Update 的 FPS 统计/读档/重置逻辑依赖这些引用）
            fullscreenToggle = settingsHandle.FullscreenToggle;
            showFpsToggle = settingsHandle.ShowFpsToggle;
            bossRelicSfxToggle = settingsHandle.BossSfxToggle;
            masterVolumeSlider = settingsHandle.MasterSlider;
            musicVolumeSlider = settingsHandle.MusicSlider;
            sfxVolumeSlider = settingsHandle.SfxSlider;
            masterVolumeText = settingsHandle.MasterPercent;
            musicVolumeText = settingsHandle.MusicPercent;
            sfxVolumeText = settingsHandle.SfxPercent;
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
                uiStyleDropdown.AddOptions(new List<string>(SettingsPanelBuilder.UiStyleNames));
                uiStyleDropdown.value = SettingsPanelBuilder.GetUiStyleIndex();
                uiStyleDropdown.RefreshShownValue();
                uiStyleDropdown.onValueChanged.AddListener(OnUIStyleChanged);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            if (showFpsToggle != null)
                showFpsToggle.onValueChanged.AddListener(OnShowFpsChanged);
        }

        void OnResolutionChanged(int index)
        {
            if (resolutions == null || index < 0 || index >= resolutions.Length) return;
            Resolution res = resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
            PlayerPrefs.SetInt("ResolutionIndex", index);
            PlayerPrefs.Save();
            // 同步分辨率候选索引 + 刷新步进行标签
            DisplaySettings.SyncResOptionFromCurrent(res.width, res.height);
            settingsHandle?.RefreshAll();
        }

        void OnFullscreenChanged(bool isFullscreen)
        {
            // 经由 DisplaySettings 统一入口：同步 WindowMode 键与旧 Fullscreen 键，两处显示设置互不打架
            DisplaySettings.SetFullscreen(isFullscreen);
            settingsHandle?.RefreshAll();
        }

        void OnUIStyleChanged(int index)
        {
            PlayerPrefs.SetInt("UIStyleIndex", index);
            PlayerPrefs.Save();
        }

        void OnMasterVolumeChanged(float value)
        {
            AudioListener.volume = value;
            if (masterVolumeText != null)
                masterVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
            PlayerPrefs.SetFloat("MasterVolume", value);
            PlayerPrefs.Save();
        }

        void OnMusicVolumeChanged(float value)
        {
            if (musicVolumeText != null)
                musicVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
            PlayerPrefs.SetFloat("MusicVolume", value);
            PlayerPrefs.Save();
        }

        void OnSFXVolumeChanged(float value)
        {
            if (sfxVolumeText != null)
                sfxVolumeText.text = $"{Mathf.RoundToInt(value * 100)}%";
            AudioManager.SetSFXVolume(value); // 同步到音效管理器（实际生效）
            PlayerPrefs.SetFloat("SFXVolume", value);
            PlayerPrefs.Save();
        }

        void OnShowFpsChanged(bool show)
        {
            PlayerPrefs.SetInt("ShowFPS", show ? 1 : 0);
            PlayerPrefs.Save();
            if (fpsText != null)
                fpsText.gameObject.SetActive(show);
            MutationChess.UI.FpsDisplay.SetVisible(show); // 无场景角标时由 FpsDisplay 组件接管（首页等）
        }

        public void ToggleSettings()
        {
            if (settingsPanel == null) return;
            settingsOpen = !settingsOpen;
            settingsPanel.SetActive(settingsOpen);
            if (settingsOpen)
            {
                settingsHandle?.RefreshAll(); // 打开前回读当前值
                UiFeel.AnimatePanelIn(settingsPanel); // 打开时面板弹入
            }
            Time.timeScale = settingsOpen ? 0f : 1f;
        }

        public void ShowSettings()
        {
            if (settingsPanel == null) return;
            settingsPanel.SetActive(true);
            settingsOpen = true;
            settingsHandle?.RefreshAll();
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

        void SaveSettings()
        {
            PlayerPrefs.Save();
        }

        void LoadSettings()
        {
            if (resolutionDropdown != null && PlayerPrefs.HasKey("ResolutionIndex"))
            {
                int index = PlayerPrefs.GetInt("ResolutionIndex");
                if (resolutions != null && index >= 0 && index < resolutions.Length)
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
                if (index < SettingsPanelBuilder.UiStyleNames.Length)
                {
                    uiStyleDropdown.value = index;
                    uiStyleDropdown.RefreshShownValue();
                }
            }

            if (fullscreenToggle != null && PlayerPrefs.HasKey("Fullscreen"))
            {
                bool isOn = PlayerPrefs.GetInt("Fullscreen") == 1;
                fullscreenToggle.isOn = isOn;
                DisplaySettings.SetFullscreen(isOn); // 同步 WindowMode 键（旧实现只改 Screen 不改键）
            }

            if (masterVolumeSlider != null && PlayerPrefs.HasKey("MasterVolume"))
            {
                float vol = PlayerPrefs.GetFloat("MasterVolume");
                masterVolumeSlider.value = vol;
                AudioListener.volume = vol;
                if (masterVolumeText != null)
                    masterVolumeText.text = $"{Mathf.RoundToInt(vol * 100)}%";
            }

            if (musicVolumeSlider != null && PlayerPrefs.HasKey("MusicVolume"))
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume");
                if (musicVolumeText != null)
                    musicVolumeText.text = $"{Mathf.RoundToInt(musicVolumeSlider.value * 100)}%";
            }

            if (sfxVolumeSlider != null && PlayerPrefs.HasKey("SFXVolume"))
            {
                sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume");
                if (sfxVolumeText != null)
                    sfxVolumeText.text = $"{Mathf.RoundToInt(sfxVolumeSlider.value * 100)}%";
                AudioManager.SetSFXVolume(sfxVolumeSlider.value); // 同步到音效管理器
            }

            if (showFpsToggle != null && PlayerPrefs.HasKey("ShowFPS"))
            {
                bool show = PlayerPrefs.GetInt("ShowFPS") == 1;
                showFpsToggle.isOn = show;
                if (fpsText != null)
                    fpsText.gameObject.SetActive(show);
            }

            // 标签页式面板步进行/开关/滑条统一回读
            settingsHandle?.RefreshAll();
        }

        public void ResetSettings()
        {
            // 显示/音量设置恢复默认并立即应用——只删设置相关键，
            // 不动 ActiveSlot/图鉴等存档键（旧实现 PlayerPrefs.DeleteAll 会误删全部）
            DisplaySettings.ResetToDefaults();

            // UI 控件回显默认值（控件赋值会触发监听写回 PlayerPrefs，与默认值一致）
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = 1f;
                AudioListener.volume = 1f;
                if (masterVolumeText != null)
                    masterVolumeText.text = "100%";
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = 0.8f;
                if (musicVolumeText != null)
                    musicVolumeText.text = "80%";
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = 0.8f;
                if (sfxVolumeText != null)
                    sfxVolumeText.text = "80%";
            }

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = Screen.fullScreen;

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

            AudioManager.SetSFXVolume(0.8f);
            AudioManager.SetBossRelicPickSfxEnabled(true);
            settingsHandle?.RefreshAll();
        }

        public bool IsSettingsOpen() => settingsOpen;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
