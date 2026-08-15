using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
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
        }

        private class CardSelectionVisuals
        {
            public Difficulty difficulty;
            public Image border;
            public Image accent;
            public Color baseAccent;
            public TMP_Text tag;
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

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SIMSUN SDF");

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

            // 六档难度卡牌（左列低难度 / 右列高难度）
            Difficulty[] leftCol = { Difficulty.Simple, Difficulty.Normal, Difficulty.Hard };
            Difficulty[] rightCol = { Difficulty.Purgatory, Difficulty.Nightmare, Difficulty.Abyss };
            Color[] leftColors =
            {
                new Color(0.36f, 0.6f, 0.38f),  // 简单 绿
                new Color(0.45f, 0.47f, 0.55f), // 普通 灰蓝
                new Color(0.65f, 0.45f, 0.2f)   // 困难 橙铜
            };
            Color[] rightColors =
            {
                new Color(0.55f, 0.32f, 0.62f), // 炼狱 紫
                new Color(0.6f, 0.2f, 0.24f),   // 噩梦 暗红
                new Color(0.18f, 0.11f, 0.26f)  // 深渊 近黑紫
            };

            DifficultyPanelState state = new DifficultyPanelState();
            CreateDifficultyColumn(frameGo.transform, font, leftCol, leftColors, -380f, state);
            CreateDifficultyColumn(frameGo.transform, font, rightCol, rightColors, 380f, state);

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
            confirmRt.anchoredPosition = new Vector2(0f, 8f);
            confirmRt.sizeDelta = new Vector2(360f, 60f);
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

            // 面板弹入动画
            UiFeel.AnimatePanelIn(frameGo);

            GameLogger.Log("[难度] 已弹出难度选择面板（视觉升级版，游戏暂停）");
        }

        private void CreateDifficultyColumn(Transform parent, TMP_FontAsset font, Difficulty[] column, Color[] colors, float x, DifficultyPanelState state)
        {
            for (int i = 0; i < column.Length; i++)
            {
                Difficulty d = column[i];
                Color accent = colors[i];
                Vector2 cardPos = new Vector2(x, -218f - i * 216f);
                Vector2 cardSize = new Vector2(430f, 190f);

                // 金色描边（先建，位于卡牌后层；选中时点亮）
                GameObject borderGo = new GameObject($"Border_{d}", typeof(RectTransform), typeof(Image));
                borderGo.transform.SetParent(parent, false);
                RectTransform borderRt = borderGo.GetComponent<RectTransform>();
                borderRt.anchorMin = borderRt.anchorMax = new Vector2(0.5f, 1f);
                borderRt.pivot = new Vector2(0.5f, 0.5f);
                borderRt.anchoredPosition = cardPos;
                borderRt.sizeDelta = cardSize + new Vector2(8f, 8f);
                Image borderImg = borderGo.GetComponent<Image>();
                borderImg.color = new Color(0.92f, 0.78f, 0.38f, 0f);
                borderImg.raycastTarget = false;

                // 卡牌本体
                GameObject cardGo = new GameObject($"Card_{d}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGo.transform.SetParent(parent, false);
                RectTransform cardRt = cardGo.GetComponent<RectTransform>();
                cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 1f);
                cardRt.pivot = new Vector2(0.5f, 0.5f);
                cardRt.anchoredPosition = cardPos;
                cardRt.sizeDelta = cardSize;
                Image cardImg = cardGo.GetComponent<Image>();
                cardImg.color = new Color(accent.r * 0.22f + 0.03f, accent.g * 0.22f + 0.03f, accent.b * 0.22f + 0.03f, 0.96f);

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
                TMP_Text nameTxt = CreatePanelText(cardGo.transform, "Name", font, 32, TextAlignmentOptions.TopLeft, Color.Lerp(accent, Color.white, 0.3f));
                nameTxt.fontStyle = FontStyles.Bold;
                RectTransform nameRt = nameTxt.rectTransform;
                nameRt.anchorMin = nameRt.anchorMax = new Vector2(0f, 1f);
                nameRt.pivot = new Vector2(0f, 1f);
                nameRt.anchoredPosition = new Vector2(26f, -18f);
                nameRt.sizeDelta = new Vector2(280f, 42f);
                nameTxt.text = GetDisplayName(d);

                // 数值三行
                int chance = Mathf.RoundToInt(GetFloorCurseChance(d) * 100f);
                TMP_Text stats = CreatePanelText(cardGo.transform, "Stats", font, 19, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.84f, 0.8f));
                RectTransform statsRt = stats.rectTransform;
                statsRt.anchorMin = statsRt.anchorMax = new Vector2(0f, 1f);
                statsRt.pivot = new Vector2(0f, 1f);
                statsRt.anchoredPosition = new Vector2(26f, -64f);
                statsRt.sizeDelta = new Vector2(360f, 84f);
                stats.text = $"起始诅咒：{GetStartCurseCount(d)} 个\n每层诅咒概率：{chance}%\n每层至多降临：{GetMaxCursesPerFloor(d)} 个";

                // 风味描述
                TMP_Text flavor = CreatePanelText(cardGo.transform, "Flavor", font, 17, TextAlignmentOptions.TopLeft, new Color(0.55f, 0.53f, 0.5f));
                RectTransform flavorRt = flavor.rectTransform;
                flavorRt.anchorMin = flavorRt.anchorMax = new Vector2(0f, 1f);
                flavorRt.pivot = new Vector2(0f, 1f);
                flavorRt.anchoredPosition = new Vector2(26f, -150f);
                flavorRt.sizeDelta = new Vector2(380f, 30f);
                flavor.text = GetFlavorText(d);

                // "✓ 已选"角标（默认透明）
                TMP_Text tag = CreatePanelText(cardGo.transform, "Tag", font, 20, TextAlignmentOptions.TopRight, new Color(0.95f, 0.82f, 0.42f, 0f));
                tag.fontStyle = FontStyles.Bold;
                RectTransform tagRt = tag.rectTransform;
                tagRt.anchorMin = tagRt.anchorMax = new Vector2(1f, 1f);
                tagRt.pivot = new Vector2(1f, 1f);
                tagRt.anchoredPosition = new Vector2(-18f, -16f);
                tagRt.sizeDelta = new Vector2(120f, 34f);
                tag.text = "✓ 已选";

                CardSelectionVisuals vis = new CardSelectionVisuals
                {
                    difficulty = d,
                    border = borderImg,
                    accent = accentImg,
                    baseAccent = accent,
                    tag = tag
                };
                state.cards.Add(vis);

                Difficulty captured = d;
                Button btn = cardGo.GetComponent<Button>();
                btn.targetGraphic = cardImg;
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => SelectDifficulty(captured, state));
                UiFeel.ApplyButton(btn);
            }
        }

        private void SelectDifficulty(Difficulty d, DifficultyPanelState state)
        {
            state.selected = d;
            state.hasSelection = true;

            foreach (var v in state.cards)
            {
                bool isSel = v.difficulty == d;
                v.border.color = new Color(0.92f, 0.78f, 0.38f, isSel ? 1f : 0f);
                v.accent.color = isSel ? new Color(0.95f, 0.8f, 0.4f) : v.baseAccent;
                v.tag.color = new Color(0.95f, 0.82f, 0.42f, isSel ? 1f : 0f);
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

            AudioManager.Instance?.PlayUIClick(0.35f);
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
