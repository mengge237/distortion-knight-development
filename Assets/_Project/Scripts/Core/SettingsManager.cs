using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
        }

        void Update()
        {
            if (Instance != this) return;

            if (Input.GetKeyDown(KeyCode.Escape))
                ToggleSettings();

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
            Time.timeScale = settingsOpen ? 0f : 1f;
        }

        public void ShowSettings()
        {
            if (settingsPanel == null) return;
            settingsPanel.SetActive(true);
            settingsOpen = true;
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
            Screen.fullScreen = isFullscreen;
            PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
            PlayerPrefs.Save();
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
            PlayerPrefs.SetFloat("SFXVolume", value);
            PlayerPrefs.Save();
        }

        void OnShowFpsChanged(bool show)
        {
            PlayerPrefs.SetInt("ShowFPS", show ? 1 : 0);
            PlayerPrefs.Save();
            if (fpsText != null)
                fpsText.gameObject.SetActive(show);
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
            }

            if (showFpsToggle != null && PlayerPrefs.HasKey("ShowFPS"))
            {
                bool show = PlayerPrefs.GetInt("ShowFPS") == 1;
                showFpsToggle.isOn = show;
                if (fpsText != null)
                    fpsText.gameObject.SetActive(show);
            }
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

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            if (resolutions != null && resolutions.Length > 0)
            {
                var res = resolutions[resolutions.Length - 1];
                Screen.SetResolution(res.width, res.height, Screen.fullScreenMode, res.refreshRateRatio);
            }

        }

        public bool IsSettingsOpen() => settingsOpen;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}