using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 困难度系统（运行时自动创建，无需场景接线）——六档难度：
    ///   简单   无诅咒
    ///   普通   每层 15% 概率降临诅咒 · 至多 1 个
    ///   困难   每层 30% 概率降临诅咒 · 至多 1 个
    ///   炼狱   起始 1 个诅咒 · 每层 45% 概率 · 至多 2 个
    ///   噩梦   起始 2 个诅咒 · 每层 60% 概率 · 至多 2 个
    ///   深渊   起始 3 个诅咒 · 每层 75% 概率 · 至多 3 个
    /// 每层诅咒由 GameManager 在楼层推进时调用 RollFloorCurses() 按概率抽签，
    /// 持有黑烛免疫一切诅咒，持有净秽香炉则概率减半。
    /// 本局尚未选择难度时，EnsureSelected() 弹出运行时自建的选择面板（暂停游戏）。
    /// </summary>
    public class DifficultyManager : MonoBehaviour, ISaveable
    {
        public enum Difficulty
        {
            Simple,     // 简单
            Normal,     // 普通
            Hard,       // 困难
            Purgatory,  // 炼狱
            Nightmare,  // 噩梦
            Abyss       // 深渊
        }

        [Serializable]
        public class DifficultySaveData
        {
            public int difficulty;
            public int chosen;
        }

        private static DifficultyManager _instance;
        public static DifficultyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<DifficultyManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DifficultyManager");
                        _instance = go.AddComponent<DifficultyManager>();
                    }
                }
                return _instance;
            }
        }

        private Difficulty currentDifficulty = Difficulty.Normal;
        private bool chosen = false;
        private bool startCursesApplied = false; // 开局诅咒已发放标记（跨场景/读档防重复发放）
        private GameObject panelGo;
        private Action pendingOnChosen; // 面板确认后的回调（首页选择完成后进入主场景等）

        // 冒险须知分页面板（难度面板内的覆盖层）
        private GameObject guideGo;
        private TMP_Text guideText;
        private TMP_Text guidePageLabel;
        private int guidePageIndex;

        /// <summary>难度已选定事件（UI/音效可监听）。</summary>
        public event Action<Difficulty> OnDifficultyChosen;

        public Difficulty CurrentDifficulty => currentDifficulty;
        public bool HasChosen => chosen;

        // ================= 难度参数 =================

        /// <summary>开局直接携带的诅咒数量（炼狱以上开局即受压）。</summary>
        public static int GetStartCurseCount(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Purgatory: return 1;
                case Difficulty.Nightmare: return 2;
                case Difficulty.Abyss: return 3;
                default: return 0;
            }
        }

        /// <summary>每层诅咒降临概率（0~1）。</summary>
        public static float GetFloorCurseChance(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Normal: return 0.15f;
                case Difficulty.Hard: return 0.30f;
                case Difficulty.Purgatory: return 0.45f;
                case Difficulty.Nightmare: return 0.60f;
                case Difficulty.Abyss: return 0.75f;
                default: return 0f;
            }
        }

        /// <summary>每层最多降临的诅咒数量（低难度至多 1 个，高难度才出现一层多个）。</summary>
        public static int GetMaxCursesPerFloor(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Purgatory:
                case Difficulty.Nightmare: return 2;
                case Difficulty.Abyss: return 3;
                case Difficulty.Hard:
                case Difficulty.Normal: return 1;
                default: return 0;
            }
        }

        public static string GetDisplayName(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Simple: return "简单";
                case Difficulty.Hard: return "困难";
                case Difficulty.Purgatory: return "炼狱";
                case Difficulty.Nightmare: return "噩梦";
                case Difficulty.Abyss: return "深渊";
                default: return "普通";
            }
        }

        public static string GetDisplayDesc(Difficulty d)
        {
            int chance = Mathf.RoundToInt(GetFloorCurseChance(d) * 100f);
            switch (d)
            {
                case Difficulty.Simple: return "无诅咒 · 轻松游玩";
                case Difficulty.Normal: return $"每层 {chance}% 概率诅咒 · 至多 1 个";
                case Difficulty.Hard: return $"每层 {chance}% 概率诅咒 · 至多 1 个";
                case Difficulty.Purgatory: return $"起始 1 诅咒 · 每层 {chance}% · 至多 2 个";
                case Difficulty.Nightmare: return $"起始 2 诅咒 · 每层 {chance}% · 至多 2 个";
                case Difficulty.Abyss: return $"起始 3 诅咒 · 每层 {chance}% · 至多 3 个";
                default: return "无诅咒 · 标准体验";
            }
        }

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景（首页→主场景）保留难度选择状态
            SaveService.Instance.Register(this);
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>本局尚未选择难度时弹出选择面板并暂停游戏（GameManager.Start 末尾调用）。</summary>
        public void EnsureSelected()
        {
            if (chosen) return;
            ShowSelectionPanel();
        }

        /// <summary>重新选择难度（首页"开始游戏"时清除已选状态，弹出选择面板）。</summary>
        public void ResetChosen()
        {
            chosen = false;
            startCursesApplied = false;
            if (panelGo != null)
            {
                Destroy(panelGo);
                panelGo = null;
            }
            Time.timeScale = 1f;
        }

        /// <summary>新一局开始：重置开局诅咒发放标记（难度选择本身保留，用于补发首页阶段未发放的开局诅咒）。</summary>
        public void ResetRunStartCurseFlag()
        {
            startCursesApplied = false;
        }

        /// <summary>选择难度：发放起始诅咒、补抽当前层诅咒、关闭面板恢复游戏并触发回调。</summary>
        public void ChooseDifficulty(Difficulty d)
        {
            currentDifficulty = d;
            chosen = true;

            if (panelGo != null)
            {
                Destroy(panelGo);
                panelGo = null;
            }
            Time.timeScale = 1f;

            ApplyRunStartCurses();

            // 主场景内直接选择时补抽当前层诅咒（GameManager.Start 的抽签在选择前因未选难度而跳过）
            if (FindObjectOfType<GameManager>() != null)
                RollFloorCurses();

            GameLogger.Log($"[难度] 本局难度：{GetDisplayName(d)}（{GetDisplayDesc(d)}）");

            // 诅咒发放后刷新地图迷雾（迷雾诅咒改变类型隐匿规则）
            MutationChess.Map.MapGenerator mg = FindObjectOfType<MutationChess.Map.MapGenerator>();
            if (mg != null) mg.UpdateFogOfWar();

            OnDifficultyChosen?.Invoke(d);

            Action cb = pendingOnChosen;
            pendingOnChosen = null;
            cb?.Invoke();
        }

        /// <summary>按当前难度发放开局诅咒（先清除上一局残留诅咒再重新抽签，黑烛免疫则全部拦截）。
        /// 标记防重复：首页阶段无遗物管理器时跳过，进入主场景后由 GameManager 补发。</summary>
        public void ApplyRunStartCurses()
        {
            if (startCursesApplied) return;
            int count = GetStartCurseCount(currentDifficulty);

            RelicManager rm = RelicManager.Instance;
            if (rm == null) return; // 首页等无遗物管理器场景：暂缓发放

            // 清除旧诅咒（难度变化/新一局时重新抽签）
            foreach (var relic in rm.GetAllRelics())
            {
                if (relic != null && CurseSystem.IsCurseId(relic.relicId))
                {
                    rm.RemoveRelic(relic.relicId);
                    GameLogger.Log($"[难度] 旧诅咒已清除：{relic.relicId}");
                }
            }

            if (count > 0)
                CurseSystem.GrantRandomCurses(rm, count, "开局诅咒");

            startCursesApplied = true;
        }

        /// <summary>
        /// 楼层诅咒抽签：每层按难度概率降临（黑烛免疫全拦、净秽香炉概率减半），
        /// 高难度允许一层降临多个。返回本层实际降临数量。
        /// </summary>
        public int RollFloorCurses()
        {
            if (!chosen) return 0;

            RelicManager rm = RelicManager.Instance;
            if (rm == null) return 0;

            if (CurseSystem.IsImmune(rm))
            {
                GameLogger.Log("[诅咒] 黑烛护体，本层诅咒无法降临");
                return 0;
            }

            float chance = GetFloorCurseChance(currentDifficulty);
            if (rm.HasRelic(RelicIds.Shop_CleansingCenser))
            {
                chance *= 0.5f;
                GameLogger.Log($"[诅咒] 净秽香炉焚香净秽：本层概率降至 {Mathf.RoundToInt(chance * 100f)}%");
            }
            if (chance <= 0f) return 0;

            if (UnityEngine.Random.value > chance)
            {
                GameLogger.Log($"[诅咒] 本层抽签未中（概率 {Mathf.RoundToInt(chance * 100f)}%），平安无事");
                return 0;
            }

            int maxCount = GetMaxCursesPerFloor(currentDifficulty);
            int count = maxCount <= 1 ? 1 : UnityEngine.Random.Range(1, maxCount + 1);
            int granted = CurseSystem.GrantRandomCurses(rm, count, "楼层诅咒");

            // 迷雾诅咒降临立即刷新地图迷雾
            if (granted > 0)
            {
                MutationChess.Map.MapGenerator mg = FindObjectOfType<MutationChess.Map.MapGenerator>();
                if (mg != null) mg.UpdateFogOfWar();
            }
            return granted;
        }

        // ================= 存档接口 =================

        public string SaveKey => "difficulty";

        public string SerializeState()
        {
            return JsonUtility.ToJson(new DifficultySaveData
            {
                difficulty = (int)currentDifficulty,
                chosen = chosen ? 1 : 0
            });
        }

        public void DeserializeState(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                DifficultySaveData d = JsonUtility.FromJson<DifficultySaveData>(json);
                if (d == null) return;
                currentDifficulty = (Difficulty)Mathf.Clamp(d.difficulty, 0, (int)Difficulty.Abyss);
                chosen = d.chosen == 1;
                // 读档恢复：诅咒已随遗物列表恢复，标记已发放避免 GameManager 补发时清空重抽
                startCursesApplied = chosen;
                GameLogger.Log($"[存档] 恢复难度：{GetDisplayName(currentDifficulty)}");
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[存档] difficulty 反序列化失败：{e.Message}");
            }
        }

        // ================= 运行时选择面板（视觉升级版） =================

        /// <summary>面板选择状态（闭包共享）。</summary>
        private class DifficultyPanelState
        {
            public Difficulty selected = Difficulty.Normal;
            public bool hasSelection;
            public readonly List<CardSelectionVisuals> cards = new List<CardSelectionVisuals>();
            public TMP_Text selectedText;
            public Button confirmBtn;
            public TMP_Text confirmLabel;
            public Image confirmImg;
            public int wheelIndex = 1; // 滚轮当前居中卡片下标（0=简单 … 5=深渊）
        }

        private class CardSelectionVisuals
        {
            public Difficulty difficulty;
            public Image border;
            public Image accent;
            public Color baseAccent;
            public TMP_Text tag;
            public Transform cardRoot; // 选中缩放动画对象
        }

        /// <summary>弹出难度选择面板（可选确认后回调，如首页选择完成后进入主场景）。</summary>
        public void ShowSelectionPanel(Action onChosen = null)
        {
            // 防重入：已弹出则先销毁旧面板
            if (panelGo != null)
            {
                Destroy(panelGo);
                panelGo = null;
            }
            pendingOnChosen = onChosen;
            BuildSelectionPanel();
        }

        private void BuildSelectionPanel()
        {
            Time.timeScale = 0f; // 选择前暂停游戏

            // 保险：场景缺失 EventSystem 时自动创建（MainScene 已有，首页等场景兜底）
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                DontDestroyOnLoad(es);
            }

            panelGo = new GameObject("DifficultySelectPanel", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = panelGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            CanvasScaler scaler = panelGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // 半透明暗底
            GameObject bgGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(panelGo.transform, false);
            RectTransform bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bgGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            // 金边外框（衬在面板底板后形成描边）
            GameObject borderOuter = new GameObject("GoldBorder", typeof(RectTransform), typeof(Image));
            borderOuter.transform.SetParent(panelGo.transform, false);
            RectTransform borderOuterRt = borderOuter.GetComponent<RectTransform>();
            borderOuterRt.anchorMin = borderOuterRt.anchorMax = new Vector2(0.5f, 0.5f);
            borderOuterRt.sizeDelta = new Vector2(1488f, 868f);
            borderOuter.GetComponent<Image>().color = new Color(0.62f, 0.5f, 0.24f, 1f);

            // 面板底板（复用获胜奖励图集背景，缺失回退暗色）
            GameObject frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(panelGo.transform, false);
            RectTransform frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(1472f, 852f);
            Image frameImg = frameGo.GetComponent<Image>();
            Sprite innerBg = Resources.Load<Sprite>("InterfaceUI/获胜奖励面板底层内嵌背景");
            if (innerBg != null)
            {
                frameImg.sprite = innerBg;
                frameImg.color = Color.white;
            }
            else frameImg.color = new Color(0.09f, 0.08f, 0.1f, 0.99f);

            TMP_FontAsset font = UiFonts.Load();

            // 标题
            TMP_Text title = CreatePanelText(frameGo.transform, "Title", font, 46, TextAlignmentOptions.Center, new Color(0.92f, 0.8f, 0.42f));
            title.fontStyle = FontStyles.Bold;
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -30f);
            titleRt.sizeDelta = new Vector2(800f, 56f);
            title.text = "选择本局难度";

            // 副标题
            TMP_Text subtitle = CreatePanelText(frameGo.transform, "Subtitle", font, 20, TextAlignmentOptions.Center, new Color(0.62f, 0.6f, 0.55f));
            RectTransform subRt = subtitle.rectTransform;
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -92f);
            subRt.sizeDelta = new Vector2(900f, 30f);
            subtitle.text = "诅咒将随楼层降临 · 难度越高，深渊越近 · 选定后本局不可更改";

            // 六档难度滚轮（滑轮框）：横排滚轮 + 居中吸附，鼠标滚轮/拖拽/点击均可切换
            DifficultyPanelState state = new DifficultyPanelState();
            DifficultyWheel wheel = BuildDifficultyWheel(frameGo.transform, font, state);

            // 底部：已选难度提示 + 确认开始
            GameObject bottomGo = new GameObject("BottomBar", typeof(RectTransform));
            bottomGo.transform.SetParent(frameGo.transform, false);
            RectTransform bottomRt = bottomGo.GetComponent<RectTransform>();
            bottomRt.anchorMin = bottomRt.anchorMax = new Vector2(0.5f, 0f);
            bottomRt.pivot = new Vector2(0.5f, 0f);
            bottomRt.anchoredPosition = new Vector2(0f, 26f);
            bottomRt.sizeDelta = new Vector2(1200f, 120f);

            state.selectedText = CreatePanelText(bottomGo.transform, "SelectedText", font, 21, TextAlignmentOptions.Center, new Color(0.7f, 0.68f, 0.62f));
            RectTransform selTextRt = state.selectedText.rectTransform;
            selTextRt.anchorMin = selTextRt.anchorMax = new Vector2(0.5f, 1f);
            selTextRt.pivot = new Vector2(0.5f, 1f);
            selTextRt.anchoredPosition = new Vector2(0f, -6f);
            selTextRt.sizeDelta = new Vector2(1100f, 34f);
            state.selectedText.text = "尚未选择难度 —— 点击上方卡牌选择";

            GameObject confirmGo = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            confirmGo.transform.SetParent(bottomGo.transform, false);
            RectTransform confirmRt = confirmGo.GetComponent<RectTransform>();
            confirmRt.anchorMin = confirmRt.anchorMax = new Vector2(0.5f, 0f);
            confirmRt.pivot = new Vector2(0.5f, 0f);
            confirmRt.anchoredPosition = new Vector2(160f, 8f);
            confirmRt.sizeDelta = new Vector2(300f, 60f);
            state.confirmImg = confirmGo.GetComponent<Image>();
            state.confirmImg.color = new Color(0.24f, 0.21f, 0.16f, 1f); // 未选：暗灰
            state.confirmLabel = CreatePanelText(confirmGo.transform, "Label", font, 27, TextAlignmentOptions.Center, new Color(0.55f, 0.52f, 0.45f));
            StretchPanelFull(state.confirmLabel.rectTransform);
            state.confirmLabel.text = "请先选择难度";
            state.confirmBtn = confirmGo.GetComponent<Button>();
            state.confirmBtn.targetGraphic = state.confirmImg;
            state.confirmBtn.transition = Selectable.Transition.None;
            state.confirmBtn.onClick.AddListener(() =>
            {
                if (!state.hasSelection)
                {
                    AudioManager.Instance?.PlayUIClick(0.25f);
                    return;
                }
                ChooseDifficulty(state.selected);
            });
            UiFeel.ApplyButton(state.confirmBtn);

            // 冒险须知按钮（确认按钮左侧）：翻开游戏常识/隐藏效果分页
            GameObject guideBtnGo = new GameObject("GuideButton", typeof(RectTransform), typeof(Image), typeof(Button));
            guideBtnGo.transform.SetParent(bottomGo.transform, false);
            RectTransform guideBtnRt = guideBtnGo.GetComponent<RectTransform>();
            guideBtnRt.anchorMin = guideBtnRt.anchorMax = new Vector2(0.5f, 0f);
            guideBtnRt.pivot = new Vector2(0.5f, 0f);
            guideBtnRt.anchoredPosition = new Vector2(-160f, 8f);
            guideBtnRt.sizeDelta = new Vector2(240f, 60f);
            Image guideBtnImg = guideBtnGo.GetComponent<Image>();
            guideBtnImg.color = new Color(0.2f, 0.2f, 0.24f, 1f);
            TMP_Text guideBtnLabel = CreatePanelText(guideBtnGo.transform, "Label", font, 24, TextAlignmentOptions.Center, new Color(0.8f, 0.82f, 0.85f));
            StretchPanelFull(guideBtnLabel.rectTransform);
            guideBtnLabel.text = "冒险须知";
            Button guideBtn = guideBtnGo.GetComponent<Button>();
            guideBtn.targetGraphic = guideBtnImg;
            guideBtn.transition = Selectable.Transition.None;
            guideBtn.onClick.AddListener(ShowGuide);
            UiFeel.ApplyButton(guideBtn);

            // 滚轮初始定位：居中"普通"，刷新已选提示与确认按钮（须在底部栏建成后调用）
            if (wheel != null)
                wheel.SnapTo(1, false);

            // 面板弹入动画
            UiFeel.AnimatePanelIn(frameGo);

            GameLogger.Log("[难度] 已弹出难度选择面板（视觉升级版，游戏暂停）");
        }

        /// <summary>
        /// 构建难度滚轮（滑轮框）：横向一排六档难度卡牌（视口遮罩裁剪两侧），
        /// 居中卡牌即为所选，支持鼠标滚轮滚动、拖拽吸附、直接点击。
        /// 拖拽/滚轮事件由 DifficultyWheel 自行处理（不用 ScrollRect，避免同物体事件竞争）。
        /// </summary>
        private DifficultyWheel BuildDifficultyWheel(Transform parent, TMP_FontAsset font, DifficultyPanelState state)
        {
            const float cardW = 300f, cardH = 320f, spacing = 24f;
            float slotWidth = cardW + spacing;
            const int count = 6;
            const float padding = 450f; // 首尾卡牌滚到正中央时视口两侧不露空（视口半宽 560 + 边距余量）
            float contentW = padding * 2f + count * cardW + (count - 1) * spacing;
            // 内容锚定视口中心 + 两侧留白：第 i 张卡居中时 content.x = centerOffset - i*slotWidth
            float centerOffset = padding + cardW / 2f;

            // 滚轮操作提示
            TMP_Text wheelHint = CreatePanelText(parent, "WheelHint", font, 19, TextAlignmentOptions.Center, new Color(0.6f, 0.58f, 0.52f));
            RectTransform hintRt = wheelHint.rectTransform;
            hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 1f);
            hintRt.pivot = new Vector2(0.5f, 1f);
            hintRt.anchoredPosition = new Vector2(0f, -128f);
            hintRt.sizeDelta = new Vector2(900f, 28f);
            wheelHint.text = "滚动鼠标滚轮 / 拖拽卡牌选择 · 居中卡牌即为所选难度";

            // 视口（遮罩裁剪两侧卡牌；拖拽/滚轮事件由 DifficultyWheel 自行处理——
            // 不用 ScrollRect：它与 DifficultyWheel 同挂一物体时，事件系统只调用
            // 组件顺序靠前的 ScrollRect 的拖拽接口，吸附逻辑将永远收不到松手事件）
            GameObject viewportGo = new GameObject("WheelViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(DifficultyWheel));
            viewportGo.transform.SetParent(parent, false);
            RectTransform viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = viewportRt.anchorMax = new Vector2(0.5f, 1f);
            viewportRt.pivot = new Vector2(0.5f, 1f);
            viewportRt.anchoredPosition = new Vector2(0f, -168f);
            viewportRt.sizeDelta = new Vector2(1120f, 372f);
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // 全透明：仅保留射线接收（拖拽命中），色块不再叠在卡牌下层

            // 内容横排（六个卡位 + 左右留白）
            GameObject contentGo = new GameObject("WheelContent", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(contentW, cardH);
            contentRt.anchoredPosition = new Vector2(centerOffset - slotWidth, 0f); // 初始居中"普通"（下标 1）
            HorizontalLayoutGroup hlg = contentGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.padding = new RectOffset((int)padding, (int)padding, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // 六档难度卡牌（简单 → 深渊）
            Difficulty[] order = { Difficulty.Simple, Difficulty.Normal, Difficulty.Hard, Difficulty.Purgatory, Difficulty.Nightmare, Difficulty.Abyss };
            Color[] colors =
            {
                new Color(0.36f, 0.6f, 0.38f),  // 简单 绿
                new Color(0.45f, 0.47f, 0.55f), // 普通 灰蓝
                new Color(0.65f, 0.45f, 0.2f),  // 困难 橙铜
                new Color(0.55f, 0.32f, 0.62f), // 炼狱 紫
                new Color(0.6f, 0.2f, 0.24f),   // 噩梦 暗红
                new Color(0.18f, 0.11f, 0.26f)  // 深渊 近黑紫
            };
            for (int i = 0; i < order.Length; i++)
                CreateWheelCard(contentGo.transform, font, order[i], colors[i], i, state);

            DifficultyWheel wheel = viewportGo.GetComponent<DifficultyWheel>();
            wheel.content = contentRt;
            wheel.slotWidth = slotWidth;
            wheel.centerOffset = centerOffset;
            wheel.state = state;
            wheel.owner = this;
            OnWheelCardClicked = idx => wheel.SnapTo(idx, true); // 点击卡牌 → 吸附选中
            return wheel;
        }

        /// <summary>滚轮内单张难度卡牌（300×320，选中时点亮描边+放大）。</summary>
        private void CreateWheelCard(Transform content, TMP_FontAsset font, Difficulty d, Color accent, int index, DifficultyPanelState state)
        {
            // 卡牌本体（金边由下层 Border 提供）
            GameObject cardGo = new GameObject($"Card_{d}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGo.transform.SetParent(content, false);
            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(300f, 320f);
            Image cardImg = cardGo.GetComponent<Image>();
            cardImg.color = new Color(accent.r * 0.22f + 0.03f, accent.g * 0.22f + 0.03f, accent.b * 0.22f + 0.03f, 0.96f);

            // 金色描边（先建，位于卡牌后层；选中时点亮）
            GameObject borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(cardGo.transform, false);
            borderGo.transform.SetAsFirstSibling();
            RectTransform borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-4f, -4f);
            borderRt.offsetMax = new Vector2(4f, 4f);
            Image borderImg = borderGo.GetComponent<Image>();
            borderImg.color = new Color(0.92f, 0.78f, 0.38f, 0f);
            borderImg.raycastTarget = false;

            // 左侧强调条
            GameObject accentGo = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            accentGo.transform.SetParent(cardGo.transform, false);
            RectTransform accentRt = accentGo.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 0.08f);
            accentRt.anchorMax = new Vector2(0f, 0.92f);
            accentRt.pivot = new Vector2(0f, 0.5f);
            accentRt.anchoredPosition = Vector2.zero;
            accentRt.sizeDelta = new Vector2(10f, 0f);
            Image accentImg = accentGo.GetComponent<Image>();
            accentImg.color = accent;
            accentImg.raycastTarget = false;

            // 难度名（大）
            TMP_Text nameTxt = CreatePanelText(cardGo.transform, "Name", font, 30, TextAlignmentOptions.TopLeft, Color.Lerp(accent, Color.white, 0.3f));
            nameTxt.fontStyle = FontStyles.Bold;
            RectTransform nameRt = nameTxt.rectTransform;
            nameRt.anchorMin = nameRt.anchorMax = new Vector2(0f, 1f);
            nameRt.pivot = new Vector2(0f, 1f);
            nameRt.anchoredPosition = new Vector2(24f, -16f);
            nameRt.sizeDelta = new Vector2(250f, 44f);
            nameTxt.text = GetDisplayName(d);

            // 数值三行
            int chance = Mathf.RoundToInt(GetFloorCurseChance(d) * 100f);
            TMP_Text stats = CreatePanelText(cardGo.transform, "Stats", font, 19, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.84f, 0.8f));
            RectTransform statsRt = stats.rectTransform;
            statsRt.anchorMin = statsRt.anchorMax = new Vector2(0f, 1f);
            statsRt.pivot = new Vector2(0f, 1f);
            statsRt.anchoredPosition = new Vector2(24f, -74f);
            statsRt.sizeDelta = new Vector2(262f, 92f);
            stats.text = $"起始诅咒：{GetStartCurseCount(d)} 个\n每层诅咒概率：{chance}%\n每层至多降临：{GetMaxCursesPerFloor(d)} 个";

            // 风味描述
            TMP_Text flavor = CreatePanelText(cardGo.transform, "Flavor", font, 17, TextAlignmentOptions.TopLeft, new Color(0.55f, 0.53f, 0.5f));
            RectTransform flavorRt = flavor.rectTransform;
            flavorRt.anchorMin = flavorRt.anchorMax = new Vector2(0f, 1f);
            flavorRt.pivot = new Vector2(0f, 1f);
            flavorRt.anchoredPosition = new Vector2(24f, -176f);
            flavorRt.sizeDelta = new Vector2(264f, 32f);
            flavor.text = GetFlavorText(d);

            // 难度排序（罗马数字）
            TMP_Text orderTxt = CreatePanelText(cardGo.transform, "Order", font, 22, TextAlignmentOptions.TopRight, new Color(0.75f, 0.72f, 0.66f, 0.8f));
            RectTransform orderRt = orderTxt.rectTransform;
            orderRt.anchorMin = orderRt.anchorMax = new Vector2(1f, 1f);
            orderRt.pivot = new Vector2(1f, 1f);
            orderRt.anchoredPosition = new Vector2(-20f, -18f);
            orderRt.sizeDelta = new Vector2(60f, 34f);
            orderTxt.text = new[] { "Ⅰ", "Ⅱ", "Ⅲ", "Ⅳ", "Ⅴ", "Ⅵ" }[index];

            // "✓ 已选"角标（默认透明）
            TMP_Text tag = CreatePanelText(cardGo.transform, "Tag", font, 20, TextAlignmentOptions.TopRight, new Color(0.95f, 0.82f, 0.42f, 0f));
            tag.fontStyle = FontStyles.Bold;
            RectTransform tagRt = tag.rectTransform;
            tagRt.anchorMin = tagRt.anchorMax = new Vector2(1f, 1f);
            tagRt.pivot = new Vector2(1f, 1f);
            tagRt.anchoredPosition = new Vector2(-20f, -58f);
            tagRt.sizeDelta = new Vector2(120f, 34f);
            tag.text = "✓ 已选";

            state.cards.Add(new CardSelectionVisuals
            {
                difficulty = d,
                border = borderImg,
                accent = accentImg,
                baseAccent = accent,
                tag = tag,
                cardRoot = cardRt
            });

            Button btn = cardGo.GetComponent<Button>();
            btn.targetGraphic = cardImg;
            btn.transition = Selectable.Transition.None;
            int captured = index;
            btn.onClick.AddListener(() =>
            {
                // 点击卡牌：滚轮吸附到该卡（BuildDifficultyWheel 里已接线到滚轮组件）
                OnWheelCardClicked?.Invoke(captured);
            });
        }

        /// <summary>点击滚轮卡牌事件（BuildDifficultyWheel 里接线到滚轮吸附）。</summary>
        private System.Action<int> OnWheelCardClicked;

        /// <summary>滚轮选中刷新：点亮居中卡牌描边+放大，同步底部已选提示与确认按钮。</summary>
        private void ApplyWheelSelection(int index, DifficultyPanelState state, bool playSound)
        {
            if (state == null || index < 0 || index >= state.cards.Count) return;

            Difficulty d = state.cards[index].difficulty;
            state.selected = d;
            state.hasSelection = true;
            state.wheelIndex = index;

            for (int i = 0; i < state.cards.Count; i++)
            {
                var v = state.cards[i];
                bool isSel = i == index;
                v.border.color = new Color(0.92f, 0.78f, 0.38f, isSel ? 1f : 0f);
                v.accent.color = isSel ? new Color(0.95f, 0.8f, 0.4f) : v.baseAccent;
                v.tag.color = new Color(0.95f, 0.82f, 0.42f, isSel ? 1f : 0f);
                if (v.cardRoot != null)
                    v.cardRoot.localScale = isSel ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one; // 只放大选中卡，未选卡保持原尺寸（不对称缩放显脏）
            }

            if (state.selectedText != null)
                state.selectedText.text = $"已选难度：{GetDisplayName(d)} —— {GetDisplayDesc(d)}";
            if (state.confirmImg != null)
                state.confirmImg.color = new Color(0.58f, 0.47f, 0.22f, 1f);
            if (state.confirmLabel != null)
            {
                state.confirmLabel.text = $"确认开始 · {GetDisplayName(d)}";
                state.confirmLabel.color = new Color(1f, 0.92f, 0.6f);
            }

            if (playSound)
                AudioManager.Instance?.PlayUIClick(0.35f);
        }

        /// <summary>
        /// 难度滚轮控制器：挂在滚轮视口上，自实现拖拽/滚轮/吸附（不用 ScrollRect，
        /// 避免同物体多组件抢事件：ExecuteEvents 只调用层级上首个实现者）。
        /// 鼠标滚轮切换相邻难度、拖拽松手/点击后吸附到最近的居中卡位
        /// （unscaled 时间动画，面板打开时 timeScale=0 也不受影响），居中卡牌自动成为所选难度。
        /// </summary>
        private class DifficultyWheel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
        {
            public RectTransform content;
            public float slotWidth;
            public float centerOffset; // 第 0 张卡居中时的 content.x（padding + cardW/2）
            public DifficultyPanelState state;
            public DifficultyManager owner;
            private Canvas canvas;
            private bool snapping;
            private int targetIndex;

            void Awake()
            {
                canvas = GetComponentInParent<Canvas>();
            }

            private float TargetXForIndex(int index)
            {
                return centerOffset - index * slotWidth;
            }

            /// <summary>吸附到指定卡位并刷新选中视觉（可静默，供初始定位）。</summary>
            public void SnapTo(int index, bool playSound)
            {
                targetIndex = Mathf.Clamp(index, 0, state != null ? state.cards.Count - 1 : 5);
                snapping = true;
                if (owner != null)
                    owner.ApplyWheelSelection(targetIndex, state, playSound);
            }

            void Update()
            {
                if (!snapping || content == null) return;

                // 吸附动画（面板暂停时 timeScale=0，必须用 unscaled 时间）
                float targetX = TargetXForIndex(targetIndex);
                float cur = content.anchoredPosition.x;
                float next = Mathf.Lerp(cur, targetX, Mathf.Clamp01(12f * Time.unscaledDeltaTime));
                if (Mathf.Abs(next - targetX) < 0.5f)
                {
                    next = targetX;
                    snapping = false;
                }
                content.anchoredPosition = new Vector2(next, content.anchoredPosition.y);
            }

            void LateUpdate()
            {
                // 拖拽过程中实时高亮最近的居中卡牌（无音效，避免滚动时连响）
                if (snapping || content == null || state == null || owner == null) return;
                int nearest = GetNearestIndex();
                if (nearest != state.wheelIndex)
                    owner.ApplyWheelSelection(nearest, state, false);
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                snapping = false; // 用户接管滚轮，取消吸附动画
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (content == null) return;
                // 屏幕像素位移换算到画布单位（ScaleWithScreenSize 缩放因子）
                float scale = canvas != null && canvas.scaleFactor > 0.001f ? canvas.scaleFactor : 1f;
                float x = content.anchoredPosition.x + eventData.delta.x / scale;
                int maxIndex = state != null ? state.cards.Count - 1 : 5;
                x = Mathf.Clamp(x, TargetXForIndex(maxIndex), TargetXForIndex(0));
                content.anchoredPosition = new Vector2(x, content.anchoredPosition.y);
            }

            public void OnEndDrag(PointerEventData eventData)
            {
                SnapTo(GetNearestIndex(), true);
            }

            public void OnScroll(PointerEventData eventData)
            {
                int dir = eventData.scrollDelta.y > 0.01f ? -1 : eventData.scrollDelta.y < -0.01f ? 1 : 0;
                if (dir == 0) return;
                SnapTo(state.wheelIndex + dir, true);
            }

            private int GetNearestIndex()
            {
                int idx = Mathf.RoundToInt((centerOffset - content.anchoredPosition.x) / slotWidth);
                return Mathf.Clamp(idx, 0, state != null ? state.cards.Count - 1 : 5);
            }
        }

        private static string GetFlavorText(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Simple: return "微风拂过，诅咒不临";
                case Difficulty.Normal: return "平衡之道，险象初现";
                case Difficulty.Hard: return "步步惊心，深渊窥伺";
                case Difficulty.Purgatory: return "开局即受诅咒，烈焰焚身";
                case Difficulty.Nightmare: return "双咒缠身，行走刀锋";
                case Difficulty.Abyss: return "三咒开局，踏入即深渊";
                default: return "";
            }
        }

        // ================= 冒险须知分页 =================

        /// <summary>翻开冒险须知：懒加载构建覆盖页，分页展示游戏常识/隐藏效果（GameTips）。</summary>
        private void ShowGuide()
        {
            if (panelGo == null) return;
            if (guideGo == null) BuildGuidePage();
            guidePageIndex = 0;
            RefreshGuidePage();
            guideGo.SetActive(true);
            AudioManager.Instance?.PlayUIClick(0.3f);
        }

        private void HideGuide()
        {
            if (guideGo != null)
                guideGo.SetActive(false);
        }

        private void BuildGuidePage()
        {
            TMP_FontAsset font = UiFonts.Load();

            guideGo = new GameObject("GuidePage", typeof(RectTransform));
            guideGo.transform.SetParent(panelGo.transform, false);
            RectTransform guideRt = guideGo.GetComponent<RectTransform>();
            guideRt.anchorMin = Vector2.zero;
            guideRt.anchorMax = Vector2.one;
            guideRt.offsetMin = Vector2.zero;
            guideRt.offsetMax = Vector2.zero;

            // 全屏暗底（盖住滚轮）
            GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(guideGo.transform, false);
            RectTransform dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

            // 金边外框
            GameObject border = new GameObject("GoldBorder", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(guideGo.transform, false);
            RectTransform borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = borderRt.anchorMax = new Vector2(0.5f, 0.5f);
            borderRt.sizeDelta = new Vector2(1224f, 764f);
            border.GetComponent<Image>().color = new Color(0.62f, 0.5f, 0.24f, 1f);

            // 面板底板
            GameObject frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(guideGo.transform, false);
            RectTransform frameRt = frame.GetComponent<RectTransform>();
            frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(1208f, 748f);
            Image frameImg = frame.GetComponent<Image>();
            Sprite innerBg = Resources.Load<Sprite>("InterfaceUI/获胜奖励面板底层内嵌背景");
            if (innerBg != null)
            {
                frameImg.sprite = innerBg;
                frameImg.color = Color.white;
            }
            else frameImg.color = new Color(0.08f, 0.075f, 0.1f, 0.99f);

            // 标题
            TMP_Text title = CreatePanelText(frame.transform, "Title", font, 40, TextAlignmentOptions.Center, new Color(0.92f, 0.8f, 0.42f));
            title.fontStyle = FontStyles.Bold;
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -28f);
            titleRt.sizeDelta = new Vector2(700f, 52f);
            title.text = "冒 险 须 知";

            // 副标题
            TMP_Text subtitle = CreatePanelText(frame.transform, "Subtitle", font, 19, TextAlignmentOptions.Center, new Color(0.6f, 0.58f, 0.52f));
            RectTransform subRt = subtitle.rectTransform;
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -86f);
            subRt.sizeDelta = new Vector2(900f, 28f);
            subtitle.text = "深渊中的常识与隐藏效果 · 细读可少走弯路";

            // 分页正文
            guideText = CreatePanelText(frame.transform, "GuideText", font, 23, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.88f, 0.8f));
            RectTransform textRt = guideText.rectTransform;
            textRt.anchorMin = textRt.anchorMax = new Vector2(0.5f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.anchoredPosition = new Vector2(0f, -132f);
            textRt.sizeDelta = new Vector2(1020f, 440f);
            guideText.lineSpacing = 14f;

            // 页码
            guidePageLabel = CreatePanelText(frame.transform, "PageLabel", font, 18, TextAlignmentOptions.Center, new Color(0.6f, 0.58f, 0.52f));
            RectTransform pageRt = guidePageLabel.rectTransform;
            pageRt.anchorMin = pageRt.anchorMax = new Vector2(0.5f, 0f);
            pageRt.pivot = new Vector2(0.5f, 0f);
            pageRt.anchoredPosition = new Vector2(0f, 116f);
            pageRt.sizeDelta = new Vector2(300f, 30f);

            // 上一页 / 下一页
            CreateGuideNavButton(frame.transform, font, "PrevButton", "◀ 上一页", new Vector2(-140f, 46f), () =>
            {
                guidePageIndex = Mathf.Max(0, guidePageIndex - 1);
                RefreshGuidePage();
            });
            CreateGuideNavButton(frame.transform, font, "NextButton", "下一页 ▶", new Vector2(140f, 46f), () =>
            {
                guidePageIndex = Mathf.Min(GameTips.PageCount - 1, guidePageIndex + 1);
                RefreshGuidePage();
            });

            // 返回难度选择
            GameObject backGo = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            backGo.transform.SetParent(frame.transform, false);
            RectTransform backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = backRt.anchorMax = new Vector2(0.5f, 0f);
            backRt.pivot = new Vector2(0.5f, 0f);
            backRt.anchoredPosition = new Vector2(0f, 26f);
            backRt.sizeDelta = new Vector2(320f, 58f);
            Image backImg = backGo.GetComponent<Image>();
            backImg.color = new Color(0.24f, 0.21f, 0.16f, 1f);
            TMP_Text backLabel = CreatePanelText(backGo.transform, "Label", font, 26, TextAlignmentOptions.Center, new Color(0.93f, 0.86f, 0.66f));
            StretchPanelFull(backLabel.rectTransform);
            backLabel.text = "返回难度选择";
            Button backBtn = backGo.GetComponent<Button>();
            backBtn.targetGraphic = backImg;
            backBtn.transition = Selectable.Transition.None;
            backBtn.onClick.AddListener(HideGuide);
            UiFeel.ApplyButton(backBtn);

            UiFeel.AnimatePanelIn(frame);
            guideGo.SetActive(false);
            GameLogger.Log("[难度] 冒险须知页已构建");
        }

        private void CreateGuideNavButton(Transform parent, TMP_FontAsset font, string goName, string label, Vector2 pos, System.Action onClick)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(240f, 54f);
            Image img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.24f, 1f);
            TMP_Text txt = CreatePanelText(go.transform, "Label", font, 23, TextAlignmentOptions.Center, new Color(0.8f, 0.82f, 0.85f));
            StretchPanelFull(txt.rectTransform);
            txt.text = label;
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick());
            UiFeel.ApplyButton(btn);
        }

        private void RefreshGuidePage()
        {
            if (guideText == null) return;
            guidePageIndex = Mathf.Clamp(guidePageIndex, 0, GameTips.PageCount - 1);
            guideText.text = GameTips.GetPageText(guidePageIndex);
            if (guidePageLabel != null)
                guidePageLabel.text = $"{guidePageIndex + 1} / {GameTips.PageCount}";
        }

        private static TMP_Text CreatePanelText(Transform parent, string goName, TMP_FontAsset font, int fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            return tmp;
        }

        private static void StretchPanelFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
