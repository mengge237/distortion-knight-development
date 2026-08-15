using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MutationChess.Core
{
    /// <summary>
    /// 困难度系统（运行时自动创建，无需场景接线）：
    /// 简单/普通 = 无诅咒；困难 = 随机 1 个诅咒；噩梦 = 随机 2 个诅咒。
    /// 本局尚未选择难度时，EnsureSelected() 会弹出运行时自建的选择面板（暂停游戏）。
    /// 诅咒从 Resources/Relics 中 isCurse=true 的遗物资产中随机抽取，通过 RelicManager.AddRelic 发放。
    /// </summary>
    public class DifficultyManager : MonoBehaviour
    {
        public enum Difficulty
        {
            Simple,    // 简单
            Normal,    // 普通
            Hard,      // 困难
            Nightmare  // 噩梦
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
        public event System.Action<Difficulty> OnDifficultyChosen;

        public Difficulty CurrentDifficulty => currentDifficulty;
        public bool HasChosen => chosen;

        /// <summary>各难度附带的诅咒数量。</summary>
        public int GetCurseCount(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Hard: return 1;
                case Difficulty.Nightmare: return 2;
                default: return 0;
            }
        }

        public static string GetDisplayName(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Simple: return "简单";
                case Difficulty.Hard: return "困难";
                case Difficulty.Nightmare: return "噩梦";
                default: return "普通";
            }
        }

        public static string GetDisplayDesc(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Simple: return "无诅咒 · 轻松游玩";
                case Difficulty.Hard: return "随机携带 1 个诅咒";
                case Difficulty.Nightmare: return "随机携带 2 个诅咒 · 自求多福";
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
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>本局尚未选择难度时弹出选择面板并暂停游戏（GameManager.Start 末尾调用）。</summary>
        public void EnsureSelected()
        {
            if (chosen) return;
            ShowSelectionPanel();
        }

        /// <summary>选择难度：发放随机诅咒并关闭面板恢复游戏。</summary>
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
            GameLogger.Log($"[难度] 本局难度：{GetDisplayName(d)}（{GetCurseCount(d)} 个诅咒）");

            // 诅咒发放后刷新地图迷雾（迷雾诅咒改变类型隐匿规则）
            MutationChess.Map.MapGenerator mg = FindObjectOfType<MutationChess.Map.MapGenerator>();
            if (mg != null) mg.UpdateFogOfWar();

            OnDifficultyChosen?.Invoke(d);
        }

        /// <summary>按当前难度从诅咒池随机发放诅咒（先清除上一局残留的诅咒，再重新抽签）。</summary>
        public void ApplyRunStartCurses()
        {
            int count = GetCurseCount(currentDifficulty);

            RelicManager rm = RelicManager.Instance;
            if (rm == null) return;

            // 清除旧诅咒（难度变化/新一局时重新抽签）
            string[] allCurseIds =
            {
                RelicIds.Curse_FogOfWar, RelicIds.Curse_Greed, RelicIds.Curse_Weakness,
                RelicIds.Curse_Bloodthirst, RelicIds.Curse_Rust
            };
            foreach (string id in allCurseIds)
            {
                if (rm.HasRelic(id))
                {
                    rm.RemoveRelic(id);
                    GameLogger.Log($"[难度] 旧诅咒已清除：{id}");
                }
            }

            if (count <= 0) return;

            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics);
            List<RelicDataAsset> cursePool = allAssets
                .Where(a => a != null && a.isCurse && !rm.HasRelic(a.relicId))
                .ToList();

            int granted = 0;
            while (granted < count && cursePool.Count > 0)
            {
                int idx = Random.Range(0, cursePool.Count);
                RelicDataAsset curse = cursePool[idx];
                cursePool.RemoveAt(idx);

                Relic relic = rm.CreateRelicFromAsset(curse);
                if (relic == null) continue;
                rm.AddRelic(relic);
                GameLogger.Log($"[难度] 诅咒降临：「{curse.relicName}」——{curse.description}");
                granted++;
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
            titleRt.anchoredPosition = new Vector2(0f, -58f);
            titleRt.sizeDelta = new Vector2(500f, 56f);
            TMP_Text title = titleGo.GetComponent<TextMeshProUGUI>();
            title.font = font;
            title.fontSize = 40f;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.86f, 0.72f, 0.35f);
            title.text = "选择本局难度";

            // 四个难度按钮
            Difficulty[] order = { Difficulty.Simple, Difficulty.Normal, Difficulty.Hard, Difficulty.Nightmare };
            Color[] btnColors =
            {
                new Color(0.30f, 0.52f, 0.32f), // 简单 绿
                new Color(0.38f, 0.38f, 0.46f), // 普通 灰蓝
                new Color(0.55f, 0.38f, 0.16f), // 困难 橙铜
                new Color(0.48f, 0.14f, 0.18f), // 噩梦 暗红
            };
            for (int i = 0; i < order.Length; i++)
            {
                Difficulty d = order[i];
                GameObject btnGo = new GameObject($"Btn_{d}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(frameGo.transform, false);
                RectTransform btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 1f);
                btnRt.anchoredPosition = new Vector2(0f, -150f - i * 118f);
                btnRt.sizeDelta = new Vector2(560f, 92f);
                Image btnImg = btnGo.GetComponent<Image>();
                btnImg.color = btnColors[i];

                GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(btnGo.transform, false);
                RectTransform labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                TMP_Text label = labelGo.GetComponent<TextMeshProUGUI>();
                label.font = font;
                label.fontSize = 30f;
                label.alignment = TextAlignmentOptions.Center;
                label.color = new Color(0.95f, 0.92f, 0.82f);
                label.text = $"{GetDisplayName(d)} · {GetDisplayDesc(d)}";

                Difficulty captured = d;
                Button btn = btnGo.GetComponent<Button>();
                btn.targetGraphic = btnImg;
                btn.onClick.AddListener(() => ChooseDifficulty(captured));
            }

            GameLogger.Log("[难度] 已弹出难度选择面板（游戏暂停）");
        }
    }
}
