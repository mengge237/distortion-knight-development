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
    /// 开始游戏 → 弹出难度选择面板 → 确认后进入主场景；继续游戏 → 标记待读档槽位 1 并进入主场景；
    /// 牌库档案与设置子面板直接叠加在首页之上（画布层级低于难度面板 900 / 档案 700，保证互不遮挡）。
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        public static HomeScreen Instance { get; private set; }

        private Canvas canvas;
        private GameObject settingsSubPanel;
        private TMP_Text continueHintTmp; // 继续游戏按钮副标签（场景绑定与运行时自建共用）
        private TMP_Text homeWindowModeTmp;
        private TMP_Text homeTargetFpsTmp;
        private TMP_Text homeAspectTmp;
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
            CreateHomeButton("设置", "音量 · 全屏 · 音效开关", -820f, OpenSettings);

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
            BindSettingsPanel();
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

        private void OpenSettings()
        {
            if (settingsSubPanel == null)
            {
                BuildSettingsSubPanel(); // 运行时自建路径延迟构建
                if (settingsSubPanel == null) return;
            }
            RefreshHomeExtraRowLabels(); // 战斗内改过的显示设置回首页后同步
            settingsSubPanel.SetActive(true);
            UiFeel.AnimatePanelIn(settingsSubPanel);
        }

        private void BindSettingsPanel()
        {
            Transform p = settingsSubPanel.transform;

            BindSliderRow(p, "Row_主音量", PlayerPrefs.GetFloat("MasterVolume", 1f), v =>
            {
                AudioListener.volume = v;
                PlayerPrefs.SetFloat("MasterVolume", v);
                PlayerPrefs.Save();
            });

            BindSliderRow(p, "Row_音乐音量", PlayerPrefs.GetFloat("MusicVolume", 0.8f), v =>
            {
                PlayerPrefs.SetFloat("MusicVolume", v);
                PlayerPrefs.Save();
            });

            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            AudioManager.SetSFXVolume(sfxVol); // 首页也应用已保存的音效音量
            BindSliderRow(p, "Row_音效音量", sfxVol, v =>
            {
                AudioManager.SetSFXVolume(v);
                PlayerPrefs.SetFloat("SFXVolume", v);
                PlayerPrefs.Save();
            });

            BindToggleRow(p, "Row_Boss遗物主题音效", AudioManager.IsBossRelicPickSfxEnabled(), v =>
            {
                AudioManager.SetBossRelicPickSfxEnabled(v);
                PlayerPrefs.SetInt("BossRelicPickSfx", v ? 1 : 0);
                PlayerPrefs.Save();
                if (v) AudioManager.Instance?.PlayUIClick(0.3f);
            });

            BindToggleRow(p, "Row_全屏显示", Screen.fullScreen, v =>
            {
                Screen.fullScreen = v;
                PlayerPrefs.SetInt("Fullscreen", v ? 1 : 0);
                PlayerPrefs.Save();
            });

            // 场景面板补建显示设置行（窗口模式/目标帧率/长宽比/显示FPS），幂等
            EnsureHomeExtraSettingRows(p);

            var back = p.Find("BackButton")?.GetComponent<Button>();
            if (back != null)
            {
                back.onClick.AddListener(CloseSettingsSubPanel);
                UiFeel.ApplyButton(back);
            }
            UiFeel.ApplyToAllButtons(p.gameObject);
        }

        /// <summary>滑条行绑定：先赋值再监听（避免初始化触发 onValueChanged 写回默认值）。</summary>
        private static void BindSliderRow(Transform panel, string rowName, float value, UnityAction<float> onChanged)
        {
            var row = panel.Find(rowName);
            if (row == null) return;
            var slider = row.Find("Slider")?.GetComponent<Slider>();
            if (slider == null) return;
            var percent = row.Find("Percent")?.GetComponent<TMP_Text>();
            slider.value = value;
            if (percent != null) UpdatePercent(percent, value);
            slider.onValueChanged.AddListener(v =>
            {
                if (percent != null) UpdatePercent(percent, v);
                onChanged?.Invoke(v);
            });
        }

        /// <summary>开关行绑定：先赋值再监听。</summary>
        private static void BindToggleRow(Transform panel, string rowName, bool value, UnityAction<bool> onChanged)
        {
            var row = panel.Find(rowName);
            if (row == null) return;
            var toggle = row.Find("Switch")?.GetComponent<Toggle>();
            if (toggle == null) return;
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(onChanged);
        }

        // ================= 显示设置行（场景/运行时面板共用） =================

        /// <summary>场景设置面板补建显示设置行（显示FPS/窗口模式/目标帧率/长宽比），幂等。</summary>
        private void EnsureHomeExtraSettingRows(Transform p)
        {
            if (p == null || p.Find("Row_窗口模式") != null) return;

            // 场景面板加高容纳新增行（BackButton 底部锚定随面板下移）
            RectTransform panelRt = settingsSubPanel != null ? settingsSubPanel.GetComponent<RectTransform>() : null;
            if (panelRt != null)
                panelRt.sizeDelta = new Vector2(panelRt.sizeDelta.x, 960f);

            // 显示 FPS 角标
            CreateToggleRow(p, "显示FPS角标", -510f, PlayerPrefs.GetInt("ShowFPS", 0) == 1, v =>
            {
                PlayerPrefs.SetInt("ShowFPS", v ? 1 : 0);
                PlayerPrefs.Save();
                FpsDisplay.SetVisible(v);
            });

            // 窗口模式
            homeWindowModeTmp = CreateStepperRow(p, "窗口模式", -590f,
                DisplaySettings.WindowModeNames, DisplaySettings.GetWindowMode(), idx =>
                {
                    DisplaySettings.SetWindowMode(idx);
                    RefreshHomeExtraRowLabels();
                });

            // 目标帧率
            string[] fpsLabels = new string[DisplaySettings.TargetFpsOptions.Length];
            for (int i = 0; i < fpsLabels.Length; i++)
                fpsLabels[i] = DisplaySettings.TargetFpsOptions[i] > 0 ? $"{DisplaySettings.TargetFpsOptions[i]} FPS" : "不限";
            int fpsIndex = 0;
            for (int i = 0; i < DisplaySettings.TargetFpsOptions.Length; i++)
                if (DisplaySettings.TargetFpsOptions[i] == DisplaySettings.GetTargetFPS()) fpsIndex = i;
            homeTargetFpsTmp = CreateStepperRow(p, "目标帧率", -670f, fpsLabels, fpsIndex, idx =>
            {
                DisplaySettings.SetTargetFPS(DisplaySettings.TargetFpsOptions[idx]);
                RefreshHomeExtraRowLabels();
            });

            // 长宽比
            homeAspectTmp = CreateStepperRow(p, "长宽比", -750f,
                DisplaySettings.AspectRatioNames, DisplaySettings.GetAspectRatioIndex(), idx =>
                {
                    DisplaySettings.SetAspectRatioIndex(idx);
                    RefreshHomeExtraRowLabels();
                });

            RefreshHomeExtraRowLabels();
        }

        /// <summary>步进行：左标签 + ◀ 当前值 ▶（返回当前值文本供跨面板刷新）。</summary>
        private TMP_Text CreateStepperRow(Transform parent, string label, float y, string[] options, int valueIndex, System.Action<int> onChanged)
        {
            var rowGo = new GameObject("Row_" + label, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 1f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, y);
            rowRt.sizeDelta = new Vector2(740f, 56f);

            var labelTmp = CreateText(rowGo.transform, "Label", 22, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.88f, 0.8f));
            var labelRt = labelTmp.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.5f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            labelTmp.text = label;

            var valueTmp = CreateText(rowGo.transform, "Value", 21, TextAlignmentOptions.Center, new Color(0.85f, 0.78f, 0.55f));
            var valueRt = valueTmp.rectTransform;
            valueRt.anchorMin = new Vector2(0.63f, 0f);
            valueRt.anchorMax = new Vector2(0.89f, 1f);
            valueRt.offsetMin = Vector2.zero;
            valueRt.offsetMax = Vector2.zero;
            valueTmp.text = options[Mathf.Clamp(valueIndex, 0, options.Length - 1)];

            // 共享捕获变量：◀/▶ 连点必须从"当前值"步进，捕获形参会在每次点击时从初始值重算
            int current = valueIndex;
            CreateStepButton(rowGo.transform, "PrevButton", "◀", new Vector2(0.51f, 0.2f), new Vector2(0.59f, 0.8f), () =>
            {
                current = Mathf.Max(0, current - 1);
                onChanged?.Invoke(current);
                valueTmp.text = options[current];
                AudioManager.Instance?.PlayUIClick(0.25f);
            });
            CreateStepButton(rowGo.transform, "NextButton", "▶", new Vector2(0.9f, 0.2f), new Vector2(0.98f, 0.8f), () =>
            {
                current = Mathf.Min(options.Length - 1, current + 1);
                onChanged?.Invoke(current);
                valueTmp.text = options[current];
                AudioManager.Instance?.PlayUIClick(0.25f);
            });

            return valueTmp;
        }

        private void CreateStepButton(Transform parent, string goName, string label, Vector2 anchorMin, Vector2 anchorMax, UnityAction onClick)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.24f, 0.22f, 0.18f, 1f);

            var labelTmp = CreateText(go.transform, "Label", 18, TextAlignmentOptions.Center, new Color(0.9f, 0.86f, 0.66f));
            StretchFull(labelTmp.rectTransform);
            labelTmp.text = label;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onClick);
            UiFeel.ApplyButton(btn);
        }

        /// <summary>显示设置行当前值文本刷新（战斗内改动后回首页同步）。</summary>
        private void RefreshHomeExtraRowLabels()
        {
            if (homeWindowModeTmp != null) homeWindowModeTmp.text = DisplaySettings.GetWindowModeLabel();
            if (homeTargetFpsTmp != null) homeTargetFpsTmp.text = DisplaySettings.GetTargetFpsLabel();
            if (homeAspectTmp != null) homeAspectTmp.text = DisplaySettings.GetAspectRatioLabel();
        }

        // ================= 设置子面板 =================

        private void BuildSettingsSubPanel()
        {
            settingsSubPanel = new GameObject("HomeSettings", typeof(RectTransform), typeof(Image));
            settingsSubPanel.transform.SetParent(transform, false);
            var panelRt = settingsSubPanel.GetComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(820f, 960f);
            settingsSubPanel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.11f, 0.99f);

            // 标题
            var title = CreateText(settingsSubPanel.transform, "Title", 36, TextAlignmentOptions.Center, new Color(0.92f, 0.8f, 0.42f));
            title.fontStyle = FontStyles.Bold;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -28f);
            titleRt.sizeDelta = new Vector2(600f, 48f);
            title.text = "设 置";

            // 主音量
            float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            CreateSliderRow(settingsSubPanel.transform, "主音量", -110f, masterVol, v =>
            {
                AudioListener.volume = v;
                PlayerPrefs.SetFloat("MasterVolume", v);
                PlayerPrefs.Save();
            });

            // 音乐音量
            float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
            CreateSliderRow(settingsSubPanel.transform, "音乐音量", -190f, musicVol, v =>
            {
                PlayerPrefs.SetFloat("MusicVolume", v);
                PlayerPrefs.Save();
            });

            // 音效音量
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            AudioManager.SetSFXVolume(sfxVol); // 首页也应用已保存的音效音量
            CreateSliderRow(settingsSubPanel.transform, "音效音量", -270f, sfxVol, v =>
            {
                AudioManager.SetSFXVolume(v);
                PlayerPrefs.SetFloat("SFXVolume", v);
                PlayerPrefs.Save();
            });

            // Boss遗物主题音效
            CreateToggleRow(settingsSubPanel.transform, "Boss遗物主题音效（选取时播放）", -350f,
                AudioManager.IsBossRelicPickSfxEnabled(), v =>
                {
                    AudioManager.SetBossRelicPickSfxEnabled(v);
                    PlayerPrefs.SetInt("BossRelicPickSfx", v ? 1 : 0);
                    PlayerPrefs.Save();
                    if (v) AudioManager.Instance?.PlayUIClick(0.3f);
                });

            // 全屏
            CreateToggleRow(settingsSubPanel.transform, "全屏显示", -430f, Screen.fullScreen, v =>
            {
                Screen.fullScreen = v;
                PlayerPrefs.SetInt("Fullscreen", v ? 1 : 0);
                PlayerPrefs.Save();
            });

            // 显示 FPS 角标
            CreateToggleRow(settingsSubPanel.transform, "显示FPS角标", -510f, PlayerPrefs.GetInt("ShowFPS", 0) == 1, v =>
            {
                PlayerPrefs.SetInt("ShowFPS", v ? 1 : 0);
                PlayerPrefs.Save();
                FpsDisplay.SetVisible(v);
            });

            // 窗口模式 / 目标帧率 / 长宽比 步进行
            homeWindowModeTmp = CreateStepperRow(settingsSubPanel.transform, "窗口模式", -590f,
                DisplaySettings.WindowModeNames, DisplaySettings.GetWindowMode(), idx =>
                {
                    DisplaySettings.SetWindowMode(idx);
                    RefreshHomeExtraRowLabels();
                });

            string[] fpsLabels = new string[DisplaySettings.TargetFpsOptions.Length];
            for (int i = 0; i < fpsLabels.Length; i++)
                fpsLabels[i] = DisplaySettings.TargetFpsOptions[i] > 0 ? $"{DisplaySettings.TargetFpsOptions[i]} FPS" : "不限";
            int fpsIndex = 0;
            for (int i = 0; i < DisplaySettings.TargetFpsOptions.Length; i++)
                if (DisplaySettings.TargetFpsOptions[i] == DisplaySettings.GetTargetFPS()) fpsIndex = i;
            homeTargetFpsTmp = CreateStepperRow(settingsSubPanel.transform, "目标帧率", -670f, fpsLabels, fpsIndex, idx =>
            {
                DisplaySettings.SetTargetFPS(DisplaySettings.TargetFpsOptions[idx]);
                RefreshHomeExtraRowLabels();
            });

            homeAspectTmp = CreateStepperRow(settingsSubPanel.transform, "长宽比", -750f,
                DisplaySettings.AspectRatioNames, DisplaySettings.GetAspectRatioIndex(), idx =>
                {
                    DisplaySettings.SetAspectRatioIndex(idx);
                    RefreshHomeExtraRowLabels();
                });

            // 返回按钮
            var backGo = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            backGo.transform.SetParent(settingsSubPanel.transform, false);
            var backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot = new Vector2(0.5f, 0f);
            backRt.anchoredPosition = new Vector2(0f, 30f);
            backRt.sizeDelta = new Vector2(320f, 58f);
            var backImg = backGo.GetComponent<Image>();
            backImg.color = new Color(0.28f, 0.25f, 0.19f, 1f);
            var backTmp = CreateText(backGo.transform, "Label", 26, TextAlignmentOptions.Center, new Color(0.93f, 0.86f, 0.66f));
            StretchFull(backTmp.rectTransform);
            backTmp.text = "返 回";
            var backBtn = backGo.GetComponent<Button>();
            backBtn.targetGraphic = backImg;
            backBtn.transition = Selectable.Transition.None;
            backBtn.onClick.AddListener(CloseSettingsSubPanel);
            UiFeel.ApplyButton(backBtn);

            UiFeel.ApplyToAllButtons(settingsSubPanel);
            settingsSubPanel.SetActive(false);
        }

        private void CloseSettingsSubPanel()
        {
            if (settingsSubPanel != null)
                settingsSubPanel.SetActive(false);
        }

        /// <summary>滑条行：左侧标签 + 滑条 + 右侧百分比（返回 Slider 供初始化后读取）。</summary>
        private Slider CreateSliderRow(Transform parent, string label, float y, float value, UnityAction<float> onChanged)
        {
            var rowGo = new GameObject("Row_" + label, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 1f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, y);
            rowRt.sizeDelta = new Vector2(740f, 56f);

            var labelTmp = CreateText(rowGo.transform, "Label", 22, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.88f, 0.8f));
            var labelRt = labelTmp.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.3f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            labelTmp.text = label;

            var percentTmp = CreateText(rowGo.transform, "Percent", 20, TextAlignmentOptions.MidlineRight, new Color(0.85f, 0.78f, 0.55f));
            var percentRt = percentTmp.rectTransform;
            percentRt.anchorMin = new Vector2(0.9f, 0f);
            percentRt.anchorMax = new Vector2(1f, 1f);
            percentRt.offsetMin = Vector2.zero;
            percentRt.offsetMax = Vector2.zero;
            UpdatePercent(percentTmp, value);

            // 滑条（运行时构建：背景 + 填充 + 手柄）
            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(rowGo.transform, false);
            var sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.32f, 0.5f);
            sliderRt.anchorMax = new Vector2(0.88f, 0.5f);
            sliderRt.pivot = new Vector2(0.5f, 0.5f);
            sliderRt.sizeDelta = new Vector2(0f, 16f);
            var slider = sliderGo.AddComponent<Slider>();

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(sliderGo.transform, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            bgGo.GetComponent<Image>().color = new Color(0.2f, 0.19f, 0.17f, 1f);

            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var fillAreaRt = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0f);
            fillAreaRt.anchorMax = new Vector2(1f, 1f);
            fillAreaRt.offsetMin = new Vector2(6f, 4f);
            fillAreaRt.offsetMax = new Vector2(-6f, -4f);
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            StretchFull(fillGo.GetComponent<RectTransform>());
            fillGo.GetComponent<Image>().color = new Color(0.62f, 0.5f, 0.24f, 1f);

            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var handleAreaRt = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = new Vector2(0f, 0f);
            handleAreaRt.anchorMax = new Vector2(1f, 1f);
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);
            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(24f, 24f);
            handleGo.GetComponent<Image>().color = new Color(0.92f, 0.8f, 0.42f, 1f);

            slider.fillRect = fillGo.GetComponent<RectTransform>();
            slider.handleRect = handleRt;
            slider.targetGraphic = handleGo.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(v =>
            {
                UpdatePercent(percentTmp, v);
                onChanged?.Invoke(v);
            });

            return slider;
        }

        private static void UpdatePercent(TMP_Text text, float value)
        {
            if (text != null)
                text.text = $"{Mathf.RoundToInt(value * 100)}%";
        }

        /// <summary>开关行：左侧标签 + 右侧标准开关盒。</summary>
        private void CreateToggleRow(Transform parent, string label, float y, bool value, UnityAction<bool> onChanged)
        {
            var rowGo = new GameObject("Row_" + label, typeof(RectTransform));
            rowGo.transform.SetParent(parent, false);
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 1f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(0f, y);
            rowRt.sizeDelta = new Vector2(740f, 56f);

            var labelTmp = CreateText(rowGo.transform, "Label", 22, TextAlignmentOptions.MidlineLeft, new Color(0.9f, 0.88f, 0.8f));
            var labelRt = labelTmp.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(0.72f, 1f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            labelTmp.text = label;

            // 开关盒（防御式构建：逐组件判空补建）
            var toggleGo = new GameObject("Switch", typeof(RectTransform), typeof(Image));
            toggleGo.transform.SetParent(rowGo.transform, false);
            var toggleRt = toggleGo.GetComponent<RectTransform>();
            if (toggleRt == null) toggleRt = toggleGo.AddComponent<RectTransform>();
            toggleRt.anchorMin = toggleRt.anchorMax = new Vector2(0.76f, 0.5f);
            toggleRt.pivot = new Vector2(0f, 0.5f);
            toggleRt.sizeDelta = new Vector2(64f, 30f);
            var bg = toggleGo.GetComponent<Image>();
            if (bg == null) bg = toggleGo.AddComponent<Image>();
            bg.color = new Color(0.24f, 0.22f, 0.18f, 1f);

            var markGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            markGo.transform.SetParent(toggleGo.transform, false);
            var markRt = markGo.GetComponent<RectTransform>();
            if (markRt == null) markRt = markGo.AddComponent<RectTransform>();
            markRt.anchorMin = new Vector2(0.1f, 0.15f);
            markRt.anchorMax = new Vector2(0.9f, 0.85f);
            markRt.offsetMin = Vector2.zero;
            markRt.offsetMax = Vector2.zero;
            var mark = markGo.GetComponent<Image>();
            if (mark == null) mark = markGo.AddComponent<Image>();
            mark.color = new Color(0.85f, 0.72f, 0.35f, 1f);

            // Toggle 本身即 Selectable（自带点击/色变反馈），不可再挂 Button——
            // Button 与 Toggle 同属 Selectable，同一 GameObject 挂第二个会抛
            // "A GameObject can only contain one 'Selectable' component"
            var toggle = toggleGo.GetComponent<Toggle>();
            if (toggle == null) toggle = toggleGo.AddComponent<Toggle>();
            toggle.transition = Selectable.Transition.ColorTint;
            toggle.targetGraphic = bg;
            toggle.graphic = mark;
            toggle.isOn = value;
            toggle.onValueChanged.AddListener(onChanged);
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
