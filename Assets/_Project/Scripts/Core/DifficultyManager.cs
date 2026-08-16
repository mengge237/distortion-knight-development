using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MutationChess.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 困难度系统——六档难度：
    ///   简单   无诅咒
    ///   普通   每层 15% 概率降临诅咒 · 至多 1 个
    ///   困难   每层 30% 概率降临诅咒 · 至多 1 个
    ///   炼狱   起始 1 个诅咒 · 每层 45% 概率 · 至多 2 个
    ///   噩梦   起始 2 个诅咒 · 每层 60% 概率 · 至多 2 个
    ///   深渊   起始 3 个诅咒 · 每层 75% 概率 · 至多 3 个
    /// 每层诅咒由 GameManager 在楼层推进时调用 RollFloorCurses() 按概率抽签，
    /// 持有黑烛免疫一切诅咒，持有净秽香炉则概率减半。
    /// 选择面板：优先绑定场景内可编辑实体（HomeSceneSetup 生成进 HomeScene 的
    /// DifficultySelectPanel，编辑器可直接手动调整），场景缺失（主场景直启等）时
    /// 回退运行时自建——两路径共用 DifficultyPanelBuilder 同一构建器，结构完全一致。
    /// 本局尚未选择难度时，EnsureSelected() 弹出选择面板（暂停游戏）。
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
        private GameObject panelGo;           // 运行时自建面板（关闭时销毁）
        private GameObject scenePanelGo;      // 场景实体面板（HomeSceneSetup 生成，隐藏/显示复用不销毁）
        private bool scenePanelBound;         // 场景面板绑定尝试标记（每域只试一次）
        private DifficultyPanelState panelState; // 当前面板选择状态（场景/自建两路径共用）
        private DifficultyWheel boundWheel;   // 当前面板滚轮（两路径共用）
        private Transform boundFrame;         // 当前面板底板（弹入动画对象）
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

            // 首页场景里存在可编辑难度面板实体时：立即绑定并隐藏（场景内默认激活供编辑，
            // 游戏加载首帧前收起，等待 ShowSelectionPanel 弹出复用；无则运行时自建）
            TryBindScenePanel();
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
            CloseActivePanel();
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

            CloseActivePanel();
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
            pendingOnChosen = onChosen;
            Time.timeScale = 0f; // 选择前暂停游戏

            // 优先复用场景内可编辑面板（HomeSceneSetup 生成进 HomeScene 的场景实体）；
            // 场景缺失（主场景直启等）回退运行时自建
            if (TryBindScenePanel() && scenePanelGo != null)
            {
                scenePanelGo.SetActive(true);
                // 滚轮初始定位：居中"普通"，刷新已选提示与确认按钮
                if (boundWheel != null)
                    boundWheel.SnapTo(1, false);
                if (boundFrame != null)
                    UiFeel.AnimatePanelIn(boundFrame.gameObject);
                GameLogger.Log("[难度] 已弹出难度选择面板（场景实体版，游戏暂停）");
                return;
            }

            // 防重入：已弹出则先销毁旧面板
            if (panelGo != null)
            {
                Destroy(panelGo);
                panelGo = null;
            }
            panelState = new DifficultyPanelState();
            BuildSelectionPanel();
        }

        private void BuildSelectionPanel()
        {
            // 保险：场景缺失 EventSystem 时自动创建（MainScene 已有，首页等场景兜底）
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
                DontDestroyOnLoad(es);
            }

            panelGo = DifficultyPanelBuilder.CreateCanvasRoot("DifficultySelectPanel");
            DifficultyPanelHandle handle = DifficultyPanelBuilder.Build(panelGo.transform);
            BindPanelHandle(handle);

            // 滚轮初始定位：居中"普通"，刷新已选提示与确认按钮（须在底部栏建成后调用）
            if (boundWheel != null)
                boundWheel.SnapTo(1, false);

            // 面板弹入动画
            UiFeel.AnimatePanelIn(handle.Frame.gameObject);

            GameLogger.Log("[难度] 已弹出难度选择面板（运行时自建，游戏暂停）");
        }

        /// <summary>
        /// 尝试绑定场景内可编辑难度选择面板（HomeSceneSetup 生成的 DifficultySelectPanel
        /// 场景实体）。只在当前活动场景存在该节点时成功；绑定后立即隐藏（场景里默认激活
        /// 供编辑器查看），待 ShowSelectionPanel 弹出复用。主场景直启等无此节点时返回
        /// false，走运行时自建兜底。
        /// </summary>
        public bool TryBindScenePanel()
        {
            // 已绑且实体存活直接复用；实体随场景切换销毁（scenePanelGo == null）时允许重试，
            // 这样返回首页重新加载出的面板实体能被再次绑定
            if (scenePanelBound && scenePanelGo != null) return true;
            scenePanelBound = true;

            GameObject root = GameObject.Find("DifficultySelectPanel");
            if (root == null) return false;

            DifficultyPanelHandle handle = DifficultyPanelBuilder.GetHandle(root);
            if (handle == null || handle.Wheel == null || handle.Cards == null || handle.Cards.Count == 0)
                return false;

            scenePanelGo = root;
            BindPanelHandle(handle);
            scenePanelGo.SetActive(false);
            GameLogger.Log("[难度] 已绑定场景内可编辑难度选择面板（DifficultySelectPanel），运行时复用场景实体");
            return true;
        }

        /// <summary>关闭当前弹出的面板：场景实体只隐藏（保留实体可复用），运行时自建则销毁。</summary>
        private void CloseActivePanel()
        {
            HideGuide();
            if (scenePanelGo != null && scenePanelGo.activeSelf)
            {
                scenePanelGo.SetActive(false);
                return;
            }
            if (panelGo != null)
            {
                Destroy(panelGo);
                panelGo = null;
            }
        }

        /// <summary>
        /// 面板控件接线（场景实体与运行时自建共用）：卡牌点击→滚轮吸附、确认→选定难度、
        /// 须知→翻页、滚轮选中→刷新视觉。构建器只搭结构不接监听，监听统一在此绑定。
        /// </summary>
        private void BindPanelHandle(DifficultyPanelHandle handle)
        {
            if (panelState == null) panelState = new DifficultyPanelState();
            panelState.cards.Clear();
            foreach (DifficultyCardRef cr in handle.Cards)
            {
                panelState.cards.Add(new CardSelectionVisuals
                {
                    difficulty = cr.difficulty,
                    border = cr.border,
                    accent = cr.accent,
                    baseAccent = cr.baseAccent,
                    tag = cr.tag,
                    cardRoot = cr.cardRoot
                });
            }
            panelState.selectedText = handle.SelectedText;
            panelState.confirmBtn = handle.ConfirmBtn;
            panelState.confirmLabel = handle.ConfirmLabel;
            panelState.confirmImg = handle.ConfirmImg;

            DifficultyWheel wheel = handle.Wheel;
            boundWheel = wheel;
            boundFrame = handle.Frame;

            for (int i = 0; i < handle.Cards.Count; i++)
            {
                Button b = handle.Cards[i].button;
                if (b == null) continue;
                int captured = i;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => wheel.SnapTo(captured, true)); // 点击卡牌 → 吸附选中
            }

            if (handle.ConfirmBtn != null)
            {
                handle.ConfirmBtn.onClick.RemoveAllListeners();
                handle.ConfirmBtn.onClick.AddListener(() =>
                {
                    if (!panelState.hasSelection)
                    {
                        AudioManager.Instance?.PlayUIClick(0.25f);
                        return;
                    }
                    ChooseDifficulty(panelState.selected);
                });
            }
            if (handle.GuideBtn != null)
            {
                handle.GuideBtn.onClick.RemoveAllListeners();
                handle.GuideBtn.onClick.AddListener(ShowGuide);
            }

            // 几何兜底：场景里若手滑清空序列化字段，用构建默认值回填
            if (wheel.content == null) wheel.content = handle.Content;
            if (wheel.slotWidth <= 0.01f) wheel.slotWidth = DifficultyPanelBuilder.SlotWidth;
            if (wheel.centerOffset <= 0.01f) wheel.centerOffset = DifficultyPanelBuilder.CenterOffset;
            wheel.cardCount = panelState.cards.Count;
            wheel.onSelectionChanged = (idx, snd) => ApplyWheelSelection(idx, panelState, snd);
        }

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

        /// <summary>难度风味描述（DifficultyPanelBuilder 构建卡牌时使用）。</summary>
        public static string GetFlavorText(Difficulty d)
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

        /// <summary>当前弹出中的面板根（场景实体优先）。</summary>
        private Transform ActivePanelRoot
        {
            get
            {
                if (scenePanelGo != null && scenePanelGo.activeSelf) return scenePanelGo.transform;
                return panelGo != null ? panelGo.transform : null;
            }
        }

        /// <summary>翻开冒险须知：懒加载构建覆盖页，分页展示游戏常识/隐藏效果（GameTips）。</summary>
        private void ShowGuide()
        {
            if (ActivePanelRoot == null) return;
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
            guideGo.transform.SetParent(ActivePanelRoot, false);
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
            TMP_Text title = DifficultyPanelBuilder.CreateText(frame.transform, "Title", font, 40, TextAlignmentOptions.Center, new Color(0.92f, 0.8f, 0.42f));
            title.fontStyle = FontStyles.Bold;
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -28f);
            titleRt.sizeDelta = new Vector2(700f, 52f);
            title.text = "冒 险 须 知";

            // 副标题
            TMP_Text subtitle = DifficultyPanelBuilder.CreateText(frame.transform, "Subtitle", font, 19, TextAlignmentOptions.Center, new Color(0.6f, 0.58f, 0.52f));
            RectTransform subRt = subtitle.rectTransform;
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -86f);
            subRt.sizeDelta = new Vector2(900f, 28f);
            subtitle.text = "深渊中的常识与隐藏效果 · 细读可少走弯路";

            // 分页正文
            guideText = DifficultyPanelBuilder.CreateText(frame.transform, "GuideText", font, 23, TextAlignmentOptions.TopLeft, new Color(0.9f, 0.88f, 0.8f));
            RectTransform textRt = guideText.rectTransform;
            textRt.anchorMin = textRt.anchorMax = new Vector2(0.5f, 1f);
            textRt.pivot = new Vector2(0.5f, 1f);
            textRt.anchoredPosition = new Vector2(0f, -132f);
            textRt.sizeDelta = new Vector2(1020f, 440f);
            guideText.lineSpacing = 14f;

            // 页码
            guidePageLabel = DifficultyPanelBuilder.CreateText(frame.transform, "PageLabel", font, 18, TextAlignmentOptions.Center, new Color(0.6f, 0.58f, 0.52f));
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
            TMP_Text backLabel = DifficultyPanelBuilder.CreateText(backGo.transform, "Label", font, 26, TextAlignmentOptions.Center, new Color(0.93f, 0.86f, 0.66f));
            DifficultyPanelBuilder.StretchFull(backLabel.rectTransform);
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
            TMP_Text txt = DifficultyPanelBuilder.CreateText(go.transform, "Label", font, 23, TextAlignmentOptions.Center, new Color(0.8f, 0.82f, 0.85f));
            DifficultyPanelBuilder.StretchFull(txt.rectTransform);
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
    }
}
