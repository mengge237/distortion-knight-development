using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;

namespace MutationChess.UI
{
    /// <summary>难度面板里的单张滚轮卡牌引用（构建/场景拾取共用）。</summary>
    public class DifficultyCardRef
    {
        public DifficultyManager.Difficulty difficulty;
        public Image border;            // 金色描边（选中点亮）
        public Image accent;            // 左侧强调条
        public Color baseAccent;
        public TMP_Text tag;            // "✓ 已选"角标
        public RectTransform cardRoot;  // 选中缩放动画对象
        public Button button;           // 点击吸附（运行时绑定）
    }

    /// <summary>难度选择面板句柄（构建/场景拾取共用，DifficultyManager 据此绑定控件逻辑）。</summary>
    public class DifficultyPanelHandle
    {
        public Transform Frame;         // 面板底板（弹入动画对象）
        public DifficultyWheel Wheel;
        public RectTransform Content;
        public List<DifficultyCardRef> Cards = new List<DifficultyCardRef>();
        public TMP_Text SelectedText;
        public Button ConfirmBtn;
        public TMP_Text ConfirmLabel;
        public Image ConfirmImg;
        public Button GuideBtn;
        public Image GuideBtnImg;
    }

    /// <summary>
    /// 难度选择面板统一构建器：编辑器场景生成（HomeSceneSetup.CreateDifficultyPanel）与
    /// 运行时兜底自建（DifficultyManager.BuildSelectionPanel）共用同一份构建代码，
    /// 场景实体与运行时结构完全一致，可直接在编辑器内手动调整；
    /// 控件逻辑（确认/须知/卡牌点击/滚轮选中）由 DifficultyManager.BindPanelHandle 运行时绑定
    /// ——构建器只搭结构不接监听。
    /// </summary>
    public static class DifficultyPanelBuilder
    {
        private const float CardW = 300f, CardH = 320f, CardSpacing = 24f;
        private const float Padding = 450f; // 首尾卡牌滚到正中央时视口两侧不露空（视口半宽 560 + 边距余量）
        public const float SlotWidth = CardW + CardSpacing;
        public const float CenterOffset = Padding + CardW / 2f;

        /// <summary>面板根（独立 900 层画布，盖在首页 500 层之上）。父级 null 时落到当前活动场景（编辑器）或根对象（运行时）。</summary>
        public static GameObject CreateCanvasRoot(string name)
        {
            GameObject root = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return root;
        }

        /// <summary>构建面板全结构（纯构建不接监听，监听由 DifficultyManager 绑定）。</summary>
        public static DifficultyPanelHandle Build(Transform panelRoot)
        {
            TMP_FontAsset font = UiFonts.Load();
            DifficultyPanelHandle handle = new DifficultyPanelHandle();

            // 半透明暗底
            GameObject bgGo = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(panelRoot, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            bgGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

            // 金边外框（衬在面板底板后形成描边）
            GameObject borderOuter = new GameObject("GoldBorder", typeof(RectTransform), typeof(Image));
            borderOuter.transform.SetParent(panelRoot, false);
            RectTransform borderOuterRt = borderOuter.GetComponent<RectTransform>();
            borderOuterRt.anchorMin = borderOuterRt.anchorMax = new Vector2(0.5f, 0.5f);
            borderOuterRt.sizeDelta = new Vector2(1488f, 868f);
            borderOuter.GetComponent<Image>().color = new Color(0.62f, 0.5f, 0.24f, 1f);

            // 面板底板（复用获胜奖励图集背景，缺失回退暗色）
            GameObject frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(panelRoot, false);
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
            handle.Frame = frameGo.transform;

            // 标题
            TMP_Text title = CreateText(frameGo.transform, "Title", font, 46, TextAlignmentOptions.Center, new Color(0.92f, 0.8f, 0.42f));
            title.fontStyle = FontStyles.Bold;
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(800f, 56f));
            title.text = "选择本局难度";

            // 副标题
            TMP_Text subtitle = CreateText(frameGo.transform, "Subtitle", font, 20, TextAlignmentOptions.Center, new Color(0.62f, 0.6f, 0.55f));
            SetRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(900f, 30f));
            subtitle.text = "诅咒将随楼层降临 · 难度越高，深渊越近 · 选定后本局不可更改";

            // 六档难度滚轮（滑轮框）
            BuildWheel(frameGo.transform, font, handle);

            // 底部：已选难度提示 + 确认开始 + 冒险须知
            GameObject bottomGo = new GameObject("BottomBar", typeof(RectTransform));
            bottomGo.transform.SetParent(frameGo.transform, false);
            SetRect(bottomGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1200f, 120f));

            handle.SelectedText = CreateText(bottomGo.transform, "SelectedText", font, 21, TextAlignmentOptions.Center, new Color(0.7f, 0.68f, 0.62f));
            SetRect(handle.SelectedText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(1100f, 34f));
            handle.SelectedText.text = "尚未选择难度 —— 点击上方卡牌选择";

            // 确认按钮（右侧；与左侧冒险须知分离 60px，避免二者视觉重叠）
            GameObject confirmGo = new GameObject("ConfirmButton", typeof(RectTransform), typeof(Image), typeof(Button));
            confirmGo.transform.SetParent(bottomGo.transform, false);
            SetRect(confirmGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(160f, 8f), new Vector2(300f, 60f));
            handle.ConfirmImg = confirmGo.GetComponent<Image>();
            handle.ConfirmImg.color = new Color(0.24f, 0.21f, 0.16f, 1f); // 未选：暗灰
            handle.ConfirmLabel = CreateText(confirmGo.transform, "Label", font, 27, TextAlignmentOptions.Center, new Color(0.55f, 0.52f, 0.45f));
            StretchFull(handle.ConfirmLabel.rectTransform);
            handle.ConfirmLabel.text = "请先选择难度";
            handle.ConfirmBtn = confirmGo.GetComponent<Button>();
            handle.ConfirmBtn.targetGraphic = handle.ConfirmImg;
            handle.ConfirmBtn.transition = Selectable.Transition.None;
            UiFeel.ApplyButton(handle.ConfirmBtn);

            // 冒险须知按钮（确认按钮左侧）：翻开游戏常识/隐藏效果分页
            GameObject guideBtnGo = new GameObject("GuideButton", typeof(RectTransform), typeof(Image), typeof(Button));
            guideBtnGo.transform.SetParent(bottomGo.transform, false);
            SetRect(guideBtnGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-160f, 8f), new Vector2(240f, 60f));
            handle.GuideBtnImg = guideBtnGo.GetComponent<Image>();
            handle.GuideBtnImg.color = new Color(0.2f, 0.2f, 0.24f, 1f);
            TMP_Text guideBtnLabel = CreateText(guideBtnGo.transform, "Label", font, 24, TextAlignmentOptions.Center, new Color(0.8f, 0.82f, 0.85f));
            StretchFull(guideBtnLabel.rectTransform);
            guideBtnLabel.text = "冒险须知";
            handle.GuideBtn = guideBtnGo.GetComponent<Button>();
            handle.GuideBtn.targetGraphic = handle.GuideBtnImg;
            handle.GuideBtn.transition = Selectable.Transition.None;
            UiFeel.ApplyButton(handle.GuideBtn);

            return handle;
        }

        /// <summary>构建六档难度滚轮（视口遮罩裁剪两侧卡牌；拖拽/滚轮事件由 DifficultyWheel 自行处理，不用 ScrollRect 避免同物体事件竞争）。</summary>
        private static void BuildWheel(Transform parent, TMP_FontAsset font, DifficultyPanelHandle handle)
        {
            // 滚轮操作提示
            TMP_Text wheelHint = CreateText(parent, "WheelHint", font, 19, TextAlignmentOptions.Center, new Color(0.6f, 0.58f, 0.52f));
            SetRect(wheelHint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -128f), new Vector2(900f, 28f));
            wheelHint.text = "滚动鼠标滚轮 / 拖拽卡牌选择 · 居中卡牌即为所选难度";

            // 视口（遮罩裁剪两侧卡牌；Image 近全透明仅保留射线接收，色块不再叠在卡牌下层）
            GameObject viewportGo = new GameObject("WheelViewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(DifficultyWheel));
            viewportGo.transform.SetParent(parent, false);
            SetRect(viewportGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -168f), new Vector2(1120f, 372f));
            viewportGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.002f);

            // 内容横排（六个卡位 + 左右留白）
            float contentW = Padding * 2f + 6 * CardW + (6 - 1) * CardSpacing;
            GameObject contentGo = new GameObject("WheelContent", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = new Vector2(contentW, CardH);
            contentRt.anchoredPosition = new Vector2(CenterOffset - SlotWidth, 0f); // 初始居中"普通"（下标 1）
            HorizontalLayoutGroup hlg = contentGo.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = CardSpacing;
            hlg.padding = new RectOffset((int)Padding, (int)Padding, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            handle.Content = contentRt;

            // 六档难度卡牌（简单 → 深渊）
            DifficultyManager.Difficulty[] order =
            {
                DifficultyManager.Difficulty.Simple, DifficultyManager.Difficulty.Normal, DifficultyManager.Difficulty.Hard,
                DifficultyManager.Difficulty.Purgatory, DifficultyManager.Difficulty.Nightmare, DifficultyManager.Difficulty.Abyss
            };
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
                handle.Cards.Add(CreateWheelCard(contentGo.transform, font, order[i], colors[i], i));

            DifficultyWheel wheel = viewportGo.GetComponent<DifficultyWheel>();
            wheel.content = contentRt;
            wheel.slotWidth = SlotWidth;
            wheel.centerOffset = CenterOffset;
            wheel.cardCount = order.Length;
            handle.Wheel = wheel;
        }

        /// <summary>滚轮内单张难度卡牌（300×320，选中时点亮描边+放大）。</summary>
        private static DifficultyCardRef CreateWheelCard(Transform content, TMP_FontAsset font, DifficultyManager.Difficulty d, Color accent, int index)
        {
            // 卡牌本体（金边由下层 Border 提供）
            GameObject cardGo = new GameObject($"Card_{d}", typeof(RectTransform), typeof(Image), typeof(Button));
            cardGo.transform.SetParent(content, false);
            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(CardW, CardH);
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
            TMP_Text nameTxt = CreateText(cardGo.transform, "Name", font, 30, TextAlignmentOptions.TopLeft, Color.Lerp(accent, Color.white, 0.3f));
            nameTxt.fontStyle = FontStyles.Bold;
            SetRect(nameTxt.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(250f, 44f));
            nameTxt.text = DifficultyManager.GetDisplayName(d);

            // 数值三行
            int chance = Mathf.RoundToInt(DifficultyManager.GetFloorCurseChance(d) * 100f);
            TMP_Text stats = CreateText(cardGo.transform, "Stats", font, 19, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.84f, 0.8f));
            SetRect(stats.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -74f), new Vector2(262f, 92f));
            stats.text = $"起始诅咒：{DifficultyManager.GetStartCurseCount(d)} 个\n每层诅咒概率：{chance}%\n每层至多降临：{DifficultyManager.GetMaxCursesPerFloor(d)} 个";

            // 风味描述
            TMP_Text flavor = CreateText(cardGo.transform, "Flavor", font, 17, TextAlignmentOptions.TopLeft, new Color(0.55f, 0.53f, 0.5f));
            SetRect(flavor.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -176f), new Vector2(264f, 32f));
            flavor.text = DifficultyManager.GetFlavorText(d);

            // 难度排序（罗马数字）
            TMP_Text orderTxt = CreateText(cardGo.transform, "Order", font, 22, TextAlignmentOptions.TopRight, new Color(0.75f, 0.72f, 0.66f, 0.8f));
            SetRect(orderTxt.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -18f), new Vector2(60f, 34f));
            orderTxt.text = new[] { "Ⅰ", "Ⅱ", "Ⅲ", "Ⅳ", "Ⅴ", "Ⅵ" }[index];

            // "✓ 已选"角标（默认透明）
            TMP_Text tag = CreateText(cardGo.transform, "Tag", font, 20, TextAlignmentOptions.TopRight, new Color(0.95f, 0.82f, 0.42f, 0f));
            tag.fontStyle = FontStyles.Bold;
            SetRect(tag.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -58f), new Vector2(120f, 34f));
            tag.text = "✓ 已选";

            Button btn = cardGo.GetComponent<Button>();
            btn.targetGraphic = cardImg;
            btn.transition = Selectable.Transition.None;
            UiFeel.ApplyButton(btn);

            return new DifficultyCardRef
            {
                difficulty = d,
                border = borderImg,
                accent = accentImg,
                baseAccent = accent,
                tag = tag,
                cardRoot = cardRt,
                button = btn
            };
        }

        /// <summary>从已建面板（场景实体或运行时面板）拾取句柄：按层级名称路径取引用。</summary>
        public static DifficultyPanelHandle GetHandle(GameObject panelRoot)
        {
            if (panelRoot == null) return null;
            Transform frame = panelRoot.transform.Find("Frame");
            if (frame == null) return null;
            Transform viewport = frame.Find("WheelViewport");
            if (viewport == null) return null;

            DifficultyPanelHandle handle = new DifficultyPanelHandle();
            handle.Frame = frame;
            handle.Wheel = viewport.GetComponent<DifficultyWheel>();
            Transform content = viewport.Find("WheelContent");
            handle.Content = content != null ? content.GetComponent<RectTransform>() : null;

            foreach (DifficultyManager.Difficulty d in System.Enum.GetValues(typeof(DifficultyManager.Difficulty)))
            {
                Transform card = content != null ? content.Find("Card_" + d) : null;
                if (card == null) continue;
                Transform border = card.Find("Border");
                Transform accent = card.Find("AccentBar");
                Transform tag = card.Find("Tag");
                Image accentImg = accent != null ? accent.GetComponent<Image>() : null;
                handle.Cards.Add(new DifficultyCardRef
                {
                    difficulty = d,
                    border = border != null ? border.GetComponent<Image>() : null,
                    accent = accentImg,
                    baseAccent = accentImg != null ? accentImg.color : Color.white,
                    tag = tag != null ? tag.GetComponent<TMP_Text>() : null,
                    cardRoot = card.GetComponent<RectTransform>(),
                    button = card.GetComponent<Button>()
                });
            }

            Transform bottom = frame.Find("BottomBar");
            if (bottom != null)
            {
                Transform sel = bottom.Find("SelectedText");
                handle.SelectedText = sel != null ? sel.GetComponent<TMP_Text>() : null;
                Transform confirm = bottom.Find("ConfirmButton");
                if (confirm != null)
                {
                    handle.ConfirmBtn = confirm.GetComponent<Button>();
                    handle.ConfirmImg = confirm.GetComponent<Image>();
                    Transform label = confirm.Find("Label");
                    handle.ConfirmLabel = label != null ? label.GetComponent<TMP_Text>() : null;
                }
                Transform guide = bottom.Find("GuideButton");
                if (guide != null)
                {
                    handle.GuideBtn = guide.GetComponent<Button>();
                    handle.GuideBtnImg = guide.GetComponent<Image>();
                }
            }
            return handle;
        }

        public static TMP_Text CreateText(Transform parent, string goName, TMP_FontAsset font, int fontSize, TextAlignmentOptions align, Color color)
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

        public static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }

        public static void StretchFull(RectTransform rt)
        {
            SetRect(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }
    }
}
