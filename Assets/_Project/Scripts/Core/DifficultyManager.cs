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
        private GameObject panelGo;

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

        /// <summary>选择难度：发放起始诅咒、关闭面板恢复游戏（楼层诅咒在 GameManager 楼层推进时抽签）。</summary>
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
            GameLogger.Log($"[难度] 本局难度：{GetDisplayName(d)}（{GetDisplayDesc(d)}）");

            // 诅咒发放后刷新地图迷雾（迷雾诅咒改变类型隐匿规则）
            MutationChess.Map.MapGenerator mg = FindObjectOfType<MutationChess.Map.MapGenerator>();
            if (mg != null) mg.UpdateFogOfWar();

            OnDifficultyChosen?.Invoke(d);
        }

        /// <summary>按当前难度发放开局诅咒（先清除上一局残留诅咒再重新抽签，黑烛免疫则全部拦截）。</summary>
        public void ApplyRunStartCurses()
        {
            int count = GetStartCurseCount(currentDifficulty);

            RelicManager rm = RelicManager.Instance;
            if (rm == null) return;

            // 清除旧诅咒（难度变化/新一局时重新抽签）
            foreach (var relic in rm.GetAllRelics())
            {
                if (relic != null && CurseSystem.IsCurseId(relic.relicId))
                {
                    rm.RemoveRelic(relic.relicId);
                    GameLogger.Log($"[难度] 旧诅咒已清除：{relic.relicId}");
                }
            }

            if (count <= 0) return;
            CurseSystem.GrantRandomCurses(rm, count, "开局诅咒");
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

            if (Random.value > chance)
            {
                GameLogger.Log($"[诅咒] 本层抽签未中（概率 {Mathf.RoundToInt(chance * 100f)}%），平安无事");
                return 0;
            }

            int maxCount = GetMaxCursesPerFloor(currentDifficulty);
            int count = maxCount <= 1 ? 1 : Random.Range(1, maxCount + 1);
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
                GameLogger.Log($"[存档] 恢复难度：{GetDisplayName(currentDifficulty)}");
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[存档] difficulty 反序列化失败：{e.Message}");
            }
        }

        // ================= 运行时选择面板 =================

        private void ShowSelectionPanel()
        {
            Time.timeScale = 0f; // 选择前暂停游戏

            // 保险：场景缺失 EventSystem 时自动创建（MainScene 已有，其他场景兜底）
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
            bgGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            // 面板底板
            GameObject frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(panelGo.transform, false);
            RectTransform frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(720f, 620f);
            frameGo.GetComponent<Image>().color = new Color(0.16f, 0.13f, 0.09f, 0.97f);

            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SIMSUN SDF");

            // 标题
            GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(frameGo.transform, false);
            RectTransform titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -48f);
            titleRt.sizeDelta = new Vector2(600f, 48f);
            TMP_Text title = titleGo.GetComponent<TextMeshProUGUI>();
            title.font = font;
            title.fontSize = 38f;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.86f, 0.72f, 0.35f);
            title.text = "选择本局难度";

            // 六档难度按钮（左列低难度 / 右列高难度）
            Difficulty[] leftCol = { Difficulty.Simple, Difficulty.Normal, Difficulty.Hard };
            Difficulty[] rightCol = { Difficulty.Purgatory, Difficulty.Nightmare, Difficulty.Abyss };
            Color[] leftColors =
            {
                new Color(0.30f, 0.52f, 0.32f), // 简单 绿
                new Color(0.38f, 0.38f, 0.46f), // 普通 灰蓝
                new Color(0.55f, 0.38f, 0.16f)  // 困难 橙铜
            };
            Color[] rightColors =
            {
                new Color(0.42f, 0.24f, 0.48f), // 炼狱 紫
                new Color(0.48f, 0.14f, 0.18f), // 噩梦 暗红
                new Color(0.10f, 0.06f, 0.16f)  // 深渊 近黑
            };

            CreateColumn(frameGo.transform, font, leftCol, leftColors, -185f);
            CreateColumn(frameGo.transform, font, rightCol, rightColors, 185f);

            // 面板弹入动画
            UiFeel.AnimatePanelIn(frameGo);

            GameLogger.Log("[难度] 已弹出难度选择面板（游戏暂停）");
        }

        private void CreateColumn(Transform parent, TMP_FontAsset font, Difficulty[] column, Color[] colors, float x)
        {
            for (int i = 0; i < column.Length; i++)
            {
                Difficulty d = column[i];
                GameObject btnGo = new GameObject($"Btn_{d}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(parent, false);
                RectTransform btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 1f);
                btnRt.anchoredPosition = new Vector2(x, -132f - i * 128f);
                btnRt.sizeDelta = new Vector2(330f, 104f);
                Image btnImg = btnGo.GetComponent<Image>();
                btnImg.color = colors[i];

                GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(btnGo.transform, false);
                RectTransform labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                TMP_Text label = labelGo.GetComponent<TextMeshProUGUI>();
                label.font = font;
                label.fontSize = 26f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(0.95f, 0.92f, 0.82f);
                label.text = $"<size=30>{GetDisplayName(d)}</size>\n<size=19>{GetDisplayDesc(d)}</size>";

                Difficulty captured = d;
                Button btn = btnGo.GetComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.onClick.AddListener(() => ChooseDifficulty(captured));
                UiFeel.ApplyButton(btn);
            }
        }
    }
}
