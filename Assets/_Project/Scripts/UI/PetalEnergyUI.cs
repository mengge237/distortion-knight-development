using DG.Tweening;
using MutationChess.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MutationChess.UI
{
    /// <summary>
    /// 花瓣能量 UI（中国元素）：能量不再以数字显示，而是一朵花——
    /// 花瓣数量 = 能量上限；当前能量 = 直立的花瓣；消耗能量 = 花瓣按压下去（缩小+内移+变暗）。
    /// 阵营主题：鲜血(血红)/寒霜(冰蓝)/腐化(腐紫)/粘液(黏液绿)/不舍(鎏金)/暗影(暗紫)；
    /// 双阵营（两个 Boss 遗物）时花瓣两色交替，花芯取两阵营融合色。
    /// 由 HandManager 在 Start 时 EnsureExists 创建（运行时自动构建 Canvas，无需场景接线）。
    /// </summary>
    public class PetalEnergyUI : MonoBehaviour
    {
        private static PetalEnergyUI _instance;
        public static PetalEnergyUI Instance => _instance;

        [System.Serializable]
        public class PetalTheme
        {
            public Color petal;
            public Color core;
        }

        private HandManager handManager;
        private Canvas canvas;
        private RectTransform flowerRoot;
        private Image coreImage;
        private TMP_Text factionLabel;
        private TMP_FontAsset chineseFont;

        private readonly List<Image> petals = new List<Image>();
        private readonly List<PetalTheme> petalThemes = new List<PetalTheme>();
        private int cachedMaxEnergy = -1;
        private int cachedCurrentEnergy = -1;
        private List<CardFaction> cachedFactions = new List<CardFaction>();

        private const int MAX_VISIBLE_PETALS = 12;
        private const float PETAL_RADIUS = 46f;

        /// <summary>阵营主题表（中国风配色，每个阵营一套花色）。</summary>
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

        /// <summary>由 HandManager 启动时调用：场景中不存在则创建（挂在独立 Canvas 上）。</summary>
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

            GameObject go = new GameObject("PetalEnergyUI");
            _instance = go.AddComponent<PetalEnergyUI>();
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
            // 能量/上限/阵营任一变化时刷新（HandManager 事件为主，这里做兜底轮询）
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
            if (flowerRoot != null) return;

            // Canvas（ScreenSpaceOverlay，运行时自建）
            GameObject canvasGo = new GameObject("PetalEnergyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // 花根：默认左下角，若场景有能量文本则对齐到该文本位置
            GameObject rootGo = new GameObject("FlowerRoot", typeof(RectTransform), typeof(CanvasGroup));
            rootGo.transform.SetParent(canvasGo.transform, false);
            flowerRoot = rootGo.GetComponent<RectTransform>();
            flowerRoot.anchorMin = flowerRoot.anchorMax = new Vector2(0.5f, 0.5f);
            flowerRoot.sizeDelta = new Vector2(150f, 150f);
            flowerRoot.anchoredPosition = new Vector2(-880f, -440f);

            // 花芯
            GameObject coreGo = new GameObject("Core", typeof(RectTransform), typeof(Image));
            coreGo.transform.SetParent(flowerRoot, false);
            RectTransform coreRt = coreGo.GetComponent<RectTransform>();
            coreRt.anchorMin = coreRt.anchorMax = new Vector2(0.5f, 0.5f);
            coreRt.sizeDelta = new Vector2(40f, 40f);
            coreImage = coreGo.GetComponent<Image>();
            coreImage.sprite = CreateCircleSprite(64, Color.white);
            coreImage.color = GetTheme(CardFaction.None).core;

            // 阵营名标签（花下方）
            GameObject labelGo = new GameObject("FactionLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(flowerRoot, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = new Vector2(0f, -86f);
            labelRt.sizeDelta = new Vector2(260f, 34f);
            factionLabel = labelGo.GetComponent<TextMeshProUGUI>();
            factionLabel.font = LoadFont();
            factionLabel.fontSize = 22f;
            factionLabel.alignment = TextAlignmentOptions.Center;
            factionLabel.color = new Color(0.30f, 0.24f, 0.16f, 0.85f);

            // 隐藏原数字能量文本（花瓣取代数字）
            if (handManager != null && handManager.EnergyText != null)
            {
                handManager.EnergyText.gameObject.SetActive(false);
                Vector2 screenPos = handManager.EnergyText.rectTransform.position;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    flowerRoot.parent as RectTransform, screenPos, null, out Vector2 localPos);
                flowerRoot.anchoredPosition = localPos;
            }
        }

        // ================= 刷新 =================

        void RefreshFrom(HandManager hm)
        {
            int maxE = hm.MaxEnergy;
            int curE = hm.CurrentEnergy;

            List<CardFaction> factions = new List<CardFaction>();
            RelicManager rm = RelicManager.Instance;
            if (rm != null) factions = rm.GetEquippedFactions(2);

            bool factionsChanged = !SameFactions(cachedFactions, factions);
            bool maxChanged = maxE != cachedMaxEnergy || factionsChanged;
            bool energyChanged = curE != cachedCurrentEnergy;

            if (!maxChanged && !energyChanged) return;

            cachedMaxEnergy = maxE;
            cachedCurrentEnergy = curE;
            cachedFactions = factions;

            int petalCount = Mathf.Clamp(maxE, 1, MAX_VISIBLE_PETALS);
            if (maxChanged)
                RebuildPetals(petalCount, factions);
            if (energyChanged || maxChanged)
                ApplyPetalStates(curE);

            UpdateLabel(factions);
        }

        bool SameFactions(List<CardFaction> a, List<CardFaction> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        void RebuildPetals(int count, List<CardFaction> factions)
        {
            foreach (Image p in petals)
            {
                if (p != null) Destroy(p.gameObject);
            }
            petals.Clear();
            petalThemes.Clear();

            for (int i = 0; i < count; i++)
            {
                CardFaction f = CardFaction.None;
                if (factions.Count == 1) f = factions[0];
                else if (factions.Count >= 2) f = (i % 2 == 0) ? factions[0] : factions[1];
                PetalTheme theme = GetTheme(f);

                GameObject petalGo = new GameObject($"Petal_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                petalGo.transform.SetParent(flowerRoot, false);
                Image img = petalGo.GetComponent<Image>();
                img.sprite = CreatePetalSprite(theme.petal);
                RectTransform rt = petalGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(64f, 96f);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

                // 花瓣绕花芯均匀分布：-90° 从正上方开始，顺时针
                float angleDeg = -90f + 360f * i / count;
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                rt.localRotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);
                rt.localScale = Vector3.zero;
                rt.anchoredPosition = dir * PETAL_RADIUS;

                petals.Add(img);
                petalThemes.Add(theme);
            }

            // 花芯取主阵营色（双阵营取融合色）
            if (factions.Count >= 2)
            {
                PetalTheme t1 = GetTheme(factions[0]);
                PetalTheme t2 = GetTheme(factions[1]);
                coreImage.color = (t1.core + t2.core) * 0.5f;
            }
            else if (factions.Count == 1)
                coreImage.color = GetTheme(factions[0]).core;
            else
                coreImage.color = GetTheme(CardFaction.None).core;
        }

        void ApplyPetalStates(int currentEnergy)
        {
            for (int i = 0; i < petals.Count; i++)
            {
                if (petals[i] == null) continue;
                bool raised = i < currentEnergy;
                Image img = petals[i];
                RectTransform rt = img.rectTransform;
                PetalTheme theme = petalThemes[i];

                float angleDeg = -90f + 360f * i / petals.Count;
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                if (raised)
                {
                    // 直立：饱满明亮，微微外弹
                    rt.DOKill();
                    rt.DOAnchorPos(dir * PETAL_RADIUS, 0.18f).SetEase(Ease.OutQuad);
                    rt.DOScale(1f, 0.18f).SetEase(Ease.OutBack);
                    rt.DORotate(angleDeg - 90f, 0.18f);
                    img.DOColor(theme.petal, 0.18f);
                }
                else
                {
                    // 按压：缩小、内移、下压变暗
                    rt.DOKill();
                    rt.DOAnchorPos(dir * PETAL_RADIUS * 0.55f, 0.18f).SetEase(Ease.InQuad);
                    rt.DOScale(0.68f, 0.18f).SetEase(Ease.InQuad);
                    rt.DORotate(angleDeg - 90f - 10f, 0.18f);
                    img.DOColor(theme.petal * new Color(0.45f, 0.45f, 0.45f, 0.85f), 0.18f);
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

        /// <summary>花瓣贴图：竖向花瓣轮廓（两头收尖、中部饱满），带叶脉与边缘羽化。</summary>
        Sprite CreatePetalSprite(Color baseColor)
        {
            int w = 64, h = 96;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w - 0.5f;   // -0.5 .. 0.5
                    float v = (y + 0.5f) / h;          // 0(底) .. 1(顶)
                    float halfWidth = 0.5f * Mathf.Sin(Mathf.PI * v); // 两头尖、中间宽
                    float dist = Mathf.Abs(u) - halfWidth;
                    float alpha = Mathf.Clamp01(0.5f - dist * w); // 边缘羽化

                    // 叶脉：中央纵向亮线 + 两侧微暗
                    float vein = Mathf.Exp(-(u * u) / 0.006f);
                    float lum = 0.82f + 0.30f * v + 0.25f * vein;
                    Color c = baseColor * lum;
                    c.a = alpha;
                    px[y * w + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            // 锚点放在花瓣底部，便于绕花芯旋转
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.06f), 100f);
        }

        /// <summary>纯色圆形贴图（花芯）。</summary>
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
                    // 中央略亮，形成立体花蕊
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
