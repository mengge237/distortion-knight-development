using DG.Tweening;
using MutationChess.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MutationChess.UI
{
    /// <summary>
    /// 阵营主题能量 UI（中国元素）：默认保持数字能量显示；
    /// 持有阵营 Boss 遗物后，能量 UI 切换为该阵营的主题形象——
    /// 鲜血·血管（血液比例反馈能量）/ 寒霜·冰晶 / 腐化·藤蔓孢子 / 粘液·液泡 / 不舍·香烛 / 暗影·鬼火。
    /// 每个主题：单位数 = 能量上限，点亮数 = 当前能量；鼠标悬浮时显示 X/X 数字反馈。
    /// 双阵营取先获得的阵营主题，标签显示两个阵营名。
    /// 由 HandManager 在 Start 时 EnsureExists 创建（运行时自动构建 Canvas，无需场景接线）。
    /// （花瓣样式保留给未来的"花瓣"阵营，不再作为默认能力 UI。）
    /// </summary>
    public class FactionEnergyUI : MonoBehaviour
    {
        private static FactionEnergyUI _instance;
        public static FactionEnergyUI Instance => _instance;

        [System.Serializable]
        public class PetalTheme
        {
            public Color petal;
            public Color core;
        }

        private class Unit
        {
            public RectTransform rt;
            public Image main;
            public Image extra;      // 蜡烛火焰等副图
            public Vector2 basePos;
            public float baseAngle;
        }

        private HandManager handManager;
        private Canvas canvas;
        private RectTransform root;
        private Image vesselBody;
        private Image vesselFill;
        private TMP_Text factionLabel;
        private TMP_Text hoverTip;
        private TMP_FontAsset chineseFont;

        private readonly List<Unit> units = new List<Unit>();
        private CardFaction currentFaction = CardFaction.None;
        private int cachedMaxEnergy = -1;
        private int cachedCurrentEnergy = -1;

        private const int MAX_VISIBLE = 8;
        private const float ARC_RADIUS = 54f;
        private const float ROW_SPACING = 64f;

        /// <summary>阵营主题配色（中国风，每个阵营一套）。</summary>
        public static PetalTheme GetTheme(CardFaction faction)
        {
            switch (faction)
            {
                case CardFaction.Blood: return new PetalTheme { petal = new Color(0.84f, 0.20f, 0.26f), core = new Color(0.55f, 0.07f, 0.10f) };
                case CardFaction.Frost: return new PetalTheme { petal = new Color(0.60f, 0.82f, 0.96f), core = new Color(0.25f, 0.52f, 0.80f) };
                case CardFaction.Corrupt: return new PetalTheme { petal = new Color(0.58f, 0.42f, 0.76f), core = new Color(0.28f, 0.38f, 0.20f) };
                case CardFaction.Slime: return new PetalTheme { petal = new Color(0.58f, 0.84f, 0.34f), core = new Color(0.20f, 0.52f, 0.14f) };
                case CardFaction.Reluctant: return new PetalTheme { petal = new Color(0.94f, 0.74f, 0.34f), core = new Color(0.72f, 0.50f, 0.16f) };
                case CardFaction.Shadow: return new PetalTheme { petal = new Color(0.48f, 0.38f, 0.74f), core = new Color(0.17f, 0.11f, 0.40f) };
                default: return new PetalTheme { petal = new Color(0.82f, 0.70f, 0.52f), core = new Color(0.52f, 0.40f, 0.22f) };
            }
        }

        /// <summary>由 HandManager 启动时调用：场景中不存在则创建。</summary>
        public static void EnsureExists(HandManager hm)
        {
            if (hm == null) return;
            if (_instance != null)
            {
                _instance.handManager = hm;
                _instance.BuildUI();
                _instance.RefreshFrom(hm);
                return;
            }

            GameObject go = new GameObject("FactionEnergyUI");
            _instance = go.AddComponent<FactionEnergyUI>();
            _instance.handManager = hm;
            _instance.BuildUI();
            _instance.RefreshFrom(hm);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Update()
        {
            if (handManager == null)
            {
                HandManager hm = FindObjectOfType<HandManager>();
                if (hm != null) handManager = hm;
                else return;
            }
            RefreshFrom(handManager);
        }

        // ================= 构建 =================

        private TMP_FontAsset LoadFont()
        {
            if (chineseFont == null)
                chineseFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/SIMSUN SDF");
            return chineseFont;
        }

        private void BuildUI()
        {
            if (root != null) return;

            GameObject canvasGo = new GameObject("FactionEnergyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // 能量主题根：默认隐藏；对齐到原能量文本位置
            GameObject rootGo = new GameObject("EnergyThemeRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootGo.transform.SetParent(canvasGo.transform, false);
            root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(300f, 200f);
            root.anchoredPosition = new Vector2(-880f, -440f);
            root.gameObject.SetActive(false);

            // 悬浮提示：能量 X/X（悬浮时显示）
            GameObject tipGo = new GameObject("HoverTip", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            tipGo.transform.SetParent(root, false);
            RectTransform tipRt = tipGo.GetComponent<RectTransform>();
            tipRt.anchorMin = tipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tipRt.anchoredPosition = new Vector2(0f, 118f);
            tipRt.sizeDelta = new Vector2(200f, 34f);
            hoverTip = tipGo.GetComponent<TextMeshProUGUI>();
            hoverTip.font = LoadFont();
            hoverTip.fontSize = 24f;
            hoverTip.alignment = TextAlignmentOptions.Center;
            hoverTip.color = new Color(1f, 0.95f, 0.75f, 1f);
            hoverTip.gameObject.SetActive(false);

            // 阵营名标签
            GameObject labelGo = new GameObject("FactionLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(root, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = new Vector2(0f, -92f);
            labelRt.sizeDelta = new Vector2(260f, 30f);
            factionLabel = labelGo.GetComponent<TextMeshProUGUI>();
            factionLabel.font = LoadFont();
            factionLabel.fontSize = 20f;
            factionLabel.alignment = TextAlignmentOptions.Center;
            factionLabel.color = new Color(0.30f, 0.24f, 0.16f, 0.85f);

            // 悬浮事件：进入显示 X/X，离开隐藏
            EventTrigger trigger = rootGo.AddComponent<EventTrigger>();
            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => { if (hoverTip != null) hoverTip.gameObject.SetActive(true); });
            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => { if (hoverTip != null) hoverTip.gameObject.SetActive(false); });
            trigger.triggers.Add(enter);
            trigger.triggers.Add(exit);

            AlignToEnergyText();
        }

        void AlignToEnergyText()
        {
            if (handManager == null || handManager.EnergyText == null) return;
            Vector2 screenPos = handManager.EnergyText.rectTransform.position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                root.parent as RectTransform, screenPos, null, out Vector2 localPos);
            root.anchoredPosition = localPos;
        }

        // ================= 刷新 =================

        void RefreshFrom(HandManager hm)
        {
            int maxE = hm.MaxEnergy;
            int curE = hm.CurrentEnergy;

            CardFaction faction = CardFaction.None;
            List<CardFaction> factions = new List<CardFaction>();
            RelicManager rm = RelicManager.Instance;
            if (rm != null) factions = rm.GetEquippedFactions(2);
            if (factions.Count > 0) faction = factions[0];

            if (faction == CardFaction.None)
            {
                // 默认数字能量显示（悬浮即数字本身 X/Y）
                if (root.gameObject.activeSelf) root.gameObject.SetActive(false);
                if (hm.EnergyText != null && !hm.EnergyText.gameObject.activeSelf)
                    hm.EnergyText.gameObject.SetActive(true);
                currentFaction = CardFaction.None;
                cachedMaxEnergy = -1;
                return;
            }

            // 阵营主题显示
            if (hm.EnergyText != null && hm.EnergyText.gameObject.activeSelf)
                hm.EnergyText.gameObject.SetActive(false);
            if (!root.gameObject.activeSelf)
            {
                AlignToEnergyText();
                root.gameObject.SetActive(true);
            }

            int unitCount = Mathf.Clamp(maxE, 1, MAX_VISIBLE);
            bool rebuild = faction != currentFaction || maxE != cachedMaxEnergy;
            if (rebuild)
            {
                currentFaction = faction;
                cachedMaxEnergy = maxE;
                Rebuild(faction, unitCount);
            }

            if (curE != cachedCurrentEnergy || rebuild)
                ApplyStates(curE, unitCount);
            cachedCurrentEnergy = curE;

            if (hoverTip != null)
                hoverTip.text = $"能量 {curE}/{maxE}";
            UpdateLabel(factions);
        }

        // ================= 各阵营构建 =================

        void Rebuild(CardFaction faction, int count)
        {
            foreach (Unit u in units)
            {
                if (u.rt != null) Destroy(u.rt.gameObject);
            }
            units.Clear();
            if (vesselBody != null) { Destroy(vesselBody.gameObject); vesselBody = null; vesselFill = null; }

            switch (faction)
            {
                case CardFaction.Blood: BuildBloodVessel(count); break;
                case CardFaction.Frost: BuildArcUnits(count, faction, CreateCrystalSprite, 0.62f); break;
                case CardFaction.Corrupt: BuildArcUnits(count, faction, CreateSporeSprite, 0.6f); break;
                case CardFaction.Slime: BuildRowUnits(count, faction, CreateBlobSprite); break;
                case CardFaction.Reluctant: BuildCandles(count, faction); break;
                case CardFaction.Shadow: BuildArcUnits(count, faction, CreateWispSprite, 0.6f); break;
            }
        }

        /// <summary>鲜血·血管：横向血管 + 血液填充（比例反馈）+ 能量室灯。</summary>
        void BuildBloodVessel(int count)
        {
            PetalTheme theme = GetTheme(CardFaction.Blood);

            // 血管本体
            GameObject bodyGo = new GameObject("VesselBody", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(root, false);
            RectTransform bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.sizeDelta = new Vector2(250f, 58f);
            vesselBody = bodyGo.GetComponent<Image>();
            vesselBody.sprite = CreateTubeSprite(new Color(0.30f, 0.12f, 0.13f), new Color(0.14f, 0.05f, 0.06f));

            // 血液填充（Filled 水平）
            GameObject fillGo = new GameObject("BloodFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(bodyGo.transform, false);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0.5f);
            fillRt.anchorMax = new Vector2(0f, 0.5f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.sizeDelta = new Vector2(230f, 40f);
            fillRt.anchoredPosition = new Vector2(-115f, 0f);
            vesselFill = fillGo.GetComponent<Image>();
            vesselFill.sprite = CreateTubeSprite(new Color(0.80f, 0.16f, 0.20f), new Color(0.55f, 0.06f, 0.09f));
            vesselFill.type = Image.Type.Filled;
            vesselFill.fillMethod = Image.FillMethod.Horizontal;
            vesselFill.fillAmount = 0f;

            // 能量室灯：沿血管排列
            for (int i = 0; i < count; i++)
            {
                GameObject cellGo = new GameObject($"Cell_{i}", typeof(RectTransform), typeof(Image));
                cellGo.transform.SetParent(bodyGo.transform, false);
                RectTransform cellRt = cellGo.GetComponent<RectTransform>();
                cellRt.anchorMin = cellRt.anchorMax = new Vector2(0.5f, 0.5f);
                float x = -230f / 2f + 230f * (i + 0.5f) / count;
                cellRt.anchoredPosition = new Vector2(x, 0f);
                cellRt.sizeDelta = new Vector2(20f, 20f);
                Image cell = cellGo.GetComponent<Image>();
                cell.sprite = CreateCircleSprite(32, Color.white);
                cell.color = theme.core * 0.55f;
                Unit u = new Unit { rt = cellRt, main = cell };
                units.Add(u);
            }
        }

        /// <summary>弧形排列单位（冰晶/孢子/鬼火）。</summary>
        void BuildArcUnits(int count, CardFaction faction, System.Func<Sprite> spriteFactory, float dimScale)
        {
            PetalTheme theme = GetTheme(faction);
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"Unit_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                float angleDeg = -90f + 360f * i / count;
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                rt.localRotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);
                rt.anchoredPosition = dir * ARC_RADIUS;
                rt.sizeDelta = new Vector2(64f, 80f);
                rt.localScale = Vector3.one * dimScale;
                Image img = go.GetComponent<Image>();
                img.sprite = spriteFactory();
                img.color = theme.petal * 0.35f;
                units.Add(new Unit { rt = rt, main = img, basePos = dir * ARC_RADIUS, baseAngle = angleDeg - 90f });
            }
        }

        /// <summary>横向排列单位（粘液液泡）。</summary>
        void BuildRowUnits(int count, CardFaction faction, System.Func<Sprite> spriteFactory)
        {
            PetalTheme theme = GetTheme(faction);
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"Unit_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                float x = -ROW_SPACING * (count - 1) / 2f + ROW_SPACING * i;
                rt.anchoredPosition = new Vector2(x, 0f);
                rt.sizeDelta = new Vector2(58f, 48f);
                rt.localScale = new Vector3(1f, 0.55f, 1f);
                Image img = go.GetComponent<Image>();
                img.sprite = spriteFactory();
                img.color = theme.petal * 0.35f;
                units.Add(new Unit { rt = rt, main = img, basePos = new Vector2(x, 0f) });
            }
        }

        /// <summary>不舍·香烛：蜡烛身体 + 火焰子图。</summary>
        void BuildCandles(int count, CardFaction faction)
        {
            PetalTheme theme = GetTheme(faction);
            for (int i = 0; i < count; i++)
            {
                GameObject go = new GameObject($"Candle_{i}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(root, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                float x = -ROW_SPACING * (count - 1) / 2f + ROW_SPACING * i;
                rt.anchoredPosition = new Vector2(x, 0f);
                rt.sizeDelta = new Vector2(36f, 60f);
                Image body = go.GetComponent<Image>();
                body.sprite = CreateCandleSprite(new Color(0.88f, 0.78f, 0.56f));
                body.color = new Color(0.55f, 0.47f, 0.30f);

                // 火焰（点亮时可见）
                GameObject flameGo = new GameObject("Flame", typeof(RectTransform), typeof(Image));
                flameGo.transform.SetParent(go.transform, false);
                RectTransform flameRt = flameGo.GetComponent<RectTransform>();
                flameRt.anchorMin = flameRt.anchorMax = new Vector2(0.5f, 1f);
                flameRt.anchoredPosition = new Vector2(0f, 22f);
                flameRt.sizeDelta = new Vector2(22f, 30f);
                Image flame = flameGo.GetComponent<Image>();
                flame.sprite = CreateFlameSprite();
                flame.color = new Color(1f, 0.62f, 0.18f);
                flame.gameObject.SetActive(false);

                units.Add(new Unit { rt = rt, main = body, extra = flame, basePos = new Vector2(x, 0f) });
            }
        }

        // ================= 状态应用 =================

        void ApplyStates(int cur, int count)
        {
            switch (currentFaction)
            {
                case CardFaction.Blood:
                {
                    PetalTheme theme = GetTheme(CardFaction.Blood);
                    float ratio = count > 0 ? (float)cur / count : 0f;
                    if (vesselFill != null)
                        vesselFill.DOFillAmount(ratio, 0.25f).SetEase(Ease.OutQuad);
                    if (vesselBody != null)
                        vesselBody.rectTransform.DOPunchScale(new Vector3(0.06f, 0.04f, 0f), 0.25f, 4, 0.5f);
                    for (int i = 0; i < units.Count; i++)
                    {
                        bool filled = i < cur;
                        Image cell = units[i].main;
                        cell.DOColor(filled ? new Color(0.95f, 0.30f, 0.32f) : theme.core * 0.55f, 0.2f);
                        units[i].rt.DOScale(filled ? 1.15f : 0.8f, 0.2f);
                    }
                    break;
                }
                case CardFaction.Reluctant:
                {
                    PetalTheme theme = GetTheme(CardFaction.Reluctant);
                    for (int i = 0; i < units.Count; i++)
                    {
                        bool filled = i < cur;
                        Unit u = units[i];
                        if (u.extra != null) u.extra.gameObject.SetActive(filled);
                        u.main.DOColor(filled ? new Color(0.95f, 0.85f, 0.62f) : new Color(0.55f, 0.47f, 0.30f), 0.2f);
                        u.rt.DOScale(filled ? 1f : 0.88f, 0.2f);
                    }
                    break;
                }
                case CardFaction.Slime:
                {
                    PetalTheme theme = GetTheme(CardFaction.Slime);
                    for (int i = 0; i < units.Count; i++)
                    {
                        bool filled = i < cur;
                        Unit u = units[i];
                        u.main.DOColor(filled ? theme.petal : theme.petal * 0.30f, 0.2f);
                        u.rt.DOScale(filled ? new Vector3(1.05f, 1.05f, 1f) : new Vector3(0.8f, 0.42f, 1f), 0.2f).SetEase(Ease.OutQuad);
                    }
                    break;
                }
                default: // 寒霜冰晶 / 腐化孢子 / 暗影鬼火：点亮=明亮放大，熄灭=暗沉缩小
                {
                    PetalTheme theme = GetTheme(currentFaction);
                    for (int i = 0; i < units.Count; i++)
                    {
                        bool filled = i < cur;
                        Unit u = units[i];
                        u.main.DOColor(filled ? theme.petal : theme.petal * 0.30f, 0.2f);
                        float target = filled ? 1f : 0.62f;
                        u.rt.DOScale(target, 0.2f).SetEase(Ease.OutBack);
                    }
                    break;
                }
            }
        }

        void UpdateLabel(List<CardFaction> factions)
        {
            if (factionLabel == null) return;
            if (factions.Count == 0)
                factionLabel.text = "";
            else if (factions.Count == 1)
                factionLabel.text = FactionDisplayName(factions[0]);
            else
                factionLabel.text = $"{FactionDisplayName(factions[0])} · {FactionDisplayName(factions[1])}";
        }

        string FactionDisplayName(CardFaction f)
        {
            FactionUnlockService svc = FactionUnlockService.Instance;
            if (svc != null) return svc.GetFactionDisplayName(f);
            switch (f)
            {
                case CardFaction.Slime: return "粘液";
                case CardFaction.Reluctant: return "不舍";
                case CardFaction.Blood: return "鲜血";
                case CardFaction.Frost: return "寒霜";
                case CardFaction.Shadow: return "暗影";
                case CardFaction.Corrupt: return "腐化";
                default: return "";
            }
        }

        // ================= 程序化贴图 =================

        /// <summary>圆角管道（血管/血液）。</summary>
        Sprite CreateTubeSprite(Color body, Color edge)
        {
            int w = 128, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w;
                    float v = (y + 0.5f) / h;
                    float r = h / 2f - 2f;
                    float dx = Mathf.Abs(x + 0.5f - r - 1f);
                    float dy = Mathf.Abs(y + 0.5f - r - 1f);
                    float d;
                    if (x < r) d = Mathf.Sqrt(dx * dx + dy * dy); // 左圆头
                    else if (x > w - r) d = Mathf.Sqrt(Mathf.Abs(x + 0.5f - (w - r - 1f)) * Mathf.Abs(x + 0.5f - (w - r - 1f)) + dy * dy); // 右圆头
                    else d = Mathf.Abs(dy); // 直段
                    float alpha = Mathf.Clamp01(r - d);
                    // 纵向高光
                    float hi = Mathf.Clamp01(1f - Mathf.Abs(v - 0.38f) * 3f);
                    Color c = Color.Lerp(edge, body, 0.5f + 0.5f * hi * 0.8f);
                    c.a = alpha;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>冰晶：菱形晶体带切面。</summary>
        Sprite CreateCrystalSprite()
        {
            int w = 64, h = 80;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w - 0.5f;
                    float v = (y + 0.5f) / h - 0.5f;
                    // 菱形轮廓
                    float d = Mathf.Abs(u) / 0.5f + Mathf.Abs(v) / 0.5f;
                    float alpha = Mathf.Clamp01(1f - d);
                    // 中轴切面亮线
                    float facet = Mathf.Clamp01(1f - Mathf.Abs(u) * 5f);
                    float lum = 0.8f + 0.45f * facet;
                    Color c = Color.white * lum;
                    c.a = alpha;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>腐化孢子：圆球带斑点。</summary>
        Sprite CreateSporeSprite()
        {
            int s = 48;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color[] px = new Color[s * s];
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float dx = x + 0.5f - s * 0.5f;
                    float dy = y + 0.5f - s * 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float r = s * 0.5f - 2f;
                    float alpha = Mathf.Clamp01((r - d) / 2f);
                    float lum = 0.85f + 0.3f * Mathf.Max(0f, 1f - d / (r * 0.5f));
                    // 斑点
                    float dot = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Sin((x + 7) * 0.45f) * Mathf.Cos((y + 11) * 0.45f)) * 6f - d / r * 2f);
                    Color c = Color.white * lum * (0.9f + 0.1f * dot);
                    c.a = alpha;
                    px[y * s + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>粘液液泡：椭圆带顶部高光。</summary>
        Sprite CreateBlobSprite()
        {
            int w = 72, h = 56;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w - 0.5f;
                    float v = (y + 0.5f) / h - 0.5f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    float alpha = Mathf.Clamp01(0.5f - d);
                    float hi = Mathf.Clamp01(1f - Mathf.Abs((u + 0.12f) * 3.2f) - Mathf.Abs((v + 0.22f) * 2.4f));
                    Color c = Color.white * (0.85f + 0.35f * hi);
                    c.a = alpha;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>香烛：蜡柱带烛芯。</summary>
        Sprite CreateCandleSprite(Color wax)
        {
            int w = 32, h = 56;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = Mathf.Abs((x + 0.5f) / w - 0.5f);
                    float v = (y + 0.5f) / h;
                    float alpha = Mathf.Clamp01((0.5f - u) * w);
                    // 顶部融边 + 烛芯
                    float topMelt = v > 0.9f ? Mathf.Clamp01((0.5f - u) * w - (v - 0.9f) * 40f) : 1f;
                    float wick = (v > 0.93f && u < 0.06f) ? 0.55f : 1f;
                    Color c = wax * (0.9f + 0.1f * Mathf.Clamp01(1f - u * 4f)) * wick;
                    c.a = alpha * topMelt;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>烛焰：水滴形火焰带光晕。</summary>
        Sprite CreateFlameSprite()
        {
            int w = 24, h = 32;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w - 0.5f;
                    float v = (y + 0.5f) / h - 0.5f;
                    float d = Mathf.Abs(u) / 0.45f + Mathf.Abs(v) / 0.5f;
                    float alpha = Mathf.Clamp01(1f - d);
                    Color c = Color.white;
                    c.a = alpha;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>暗影鬼火：泪滴火焰带光晕拖尾。</summary>
        Sprite CreateWispSprite()
        {
            int w = 48, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w - 0.5f;
                    float v = (y + 0.5f) / h - 0.5f;
                    float d = Mathf.Abs(u) / 0.42f + Mathf.Abs(v) / 0.5f;
                    float alpha = Mathf.Clamp01(1f - d);
                    float glow = Mathf.Clamp01(0.6f - Mathf.Sqrt(u * u + v * v));
                    Color c = Color.white * (0.9f + 0.3f * glow);
                    c.a = alpha;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>纯色圆形贴图（能量室灯）。</summary>
        Sprite CreateCircleSprite(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] px = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - r;
                    float dy = y + 0.5f - r;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = Mathf.Clamp01((r - d) / 2f);
                    float lum = 1f + 0.4f * Mathf.Max(0f, 1f - d / (r * 0.6f));
                    Color c = color * lum;
                    c.a = edge;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
