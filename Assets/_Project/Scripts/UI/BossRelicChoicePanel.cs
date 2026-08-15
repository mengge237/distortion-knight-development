using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;

namespace MutationChess.UI
{
    /// <summary>
    /// Boss 遗物三选一面板：战胜 Boss 后优先弹出（先于金币/卡牌奖励页）。
    /// 场景可预接线（panelRoot / relicContainer / titleText），缺失时运行时自动构建，
    /// 视觉素材复用 InterfaceUI 获胜奖励图集 + 各遗物 RelicsArt 图标 + SIMSUN SDF 中文字体。
    /// </summary>
    public class BossRelicChoicePanel : MonoBehaviour
    {
        public static BossRelicChoicePanel Instance { get; private set; }

        [Header("场景接线（可选，缺失时运行时自动构建）")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Transform relicContainer;
        [SerializeField] private TextMeshProUGUI titleText;

        private Canvas canvas;
        private static TMP_FontAsset cachedFont;

        private const int CanvasOrder = 500;

        public bool IsVisible => panelRoot != null && panelRoot.activeSelf;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>弹出面板；用户点击某遗物后回调 onPicked 并自动隐藏。</summary>
        public void Show(List<Relic> relics, Action<Relic> onPicked)
        {
            if (panelRoot == null)
                BuildPanel();
            if (panelRoot == null) return;

            RefreshButtons(relics, onPicked);
            panelRoot.SetActive(true);

            // 重建画布，确保叠层渲染顺序正确
            if (canvas != null)
            {
                canvas.enabled = false;
                canvas.enabled = true;
            }

            // 面板弹入动画
            UiFeel.AnimatePanelIn(panelRoot);
        }

        public void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (canvas != null)
                canvas.enabled = false;
        }

        #region 运行时构建

        private void BuildPanel()
        {
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasOrder;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // 面板根（初始隐藏）
            var rootGo = new GameObject("PanelRoot", typeof(RectTransform));
            rootGo.transform.SetParent(transform, false);
            StretchFull(rootGo.GetComponent<RectTransform>());
            panelRoot = rootGo;
            panelRoot.SetActive(false);

            // 全屏遮罩
            var maskGo = new GameObject("Mask", typeof(RectTransform), typeof(Image));
            maskGo.transform.SetParent(panelRoot.transform, false);
            StretchFull(maskGo.GetComponent<RectTransform>());
            maskGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            // 中央面板：优先使用获胜奖励面板底层内嵌背景图，缺失时用深色底
            var centerGo = new GameObject("CenterPanel", typeof(RectTransform), typeof(Image));
            centerGo.transform.SetParent(panelRoot.transform, false);
            var centerRt = centerGo.GetComponent<RectTransform>();
            centerRt.anchorMin = centerRt.anchorMax = new Vector2(0.5f, 0.5f);
            centerRt.pivot = new Vector2(0.5f, 0.5f);
            centerRt.sizeDelta = new Vector2(1560, 760);
            var centerImg = centerGo.GetComponent<Image>();
            Sprite innerBg = LoadUISprite("获胜奖励面板底层内嵌背景");
            if (innerBg != null)
            {
                centerImg.sprite = innerBg;
                centerImg.color = Color.white;
            }
            else
            {
                centerImg.color = new Color(0.07f, 0.07f, 0.11f, 0.98f);
            }

            // 标题栏（获胜奖励标题背景图） + 标题文字
            var titleBar = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
            titleBar.transform.SetParent(centerGo.transform, false);
            var titleBarRt = titleBar.GetComponent<RectTransform>();
            titleBarRt.anchorMin = titleBarRt.anchorMax = new Vector2(0.5f, 1f);
            titleBarRt.pivot = new Vector2(0.5f, 1f);
            titleBarRt.anchoredPosition = new Vector2(0f, -30f);
            titleBarRt.sizeDelta = new Vector2(1000f, 100f);
            var titleBarImg = titleBar.GetComponent<Image>();
            Sprite titleBg = LoadUISprite("获胜奖励标题背景图");
            if (titleBg != null)
            {
                titleBarImg.sprite = titleBg;
                titleBarImg.color = Color.white;
            }
            else
            {
                titleBarImg.color = new Color(0.12f, 0.08f, 0.05f, 0.9f);
            }

            var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGo.transform.SetParent(titleBar.transform, false);
            StretchFull(titleGo.GetComponent<RectTransform>());
            titleText = titleGo.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset font = LoadFont();
            if (font != null) titleText.font = font;
            titleText.text = "选择 Boss 遗物";
            titleText.fontSize = 46;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.85f, 0.35f);

            // 遗物卡片容器（横向三张）
            var containerGo = new GameObject("RelicContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            containerGo.transform.SetParent(centerGo.transform, false);
            var containerRt = containerGo.GetComponent<RectTransform>();
            containerRt.anchorMin = Vector2.zero;
            containerRt.anchorMax = Vector2.one;
            containerRt.offsetMin = new Vector2(60f, 60f);
            containerRt.offsetMax = new Vector2(-60f, -160f);
            var layout = containerGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 40f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            relicContainer = containerRt;
        }

        private void RefreshButtons(List<Relic> relics, Action<Relic> onPicked)
        {
            if (relicContainer == null) return;

            for (int i = relicContainer.childCount - 1; i >= 0; i--)
                Destroy(relicContainer.GetChild(i).gameObject);

            TMP_FontAsset font = LoadFont();
            foreach (var relic in relics)
            {
                if (relic == null) continue;

                // 卡片按钮
                var btnGo = new GameObject("RelicButton_" + relic.relicName,
                    typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                btnGo.transform.SetParent(relicContainer, false);
                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.sizeDelta = new Vector2(420f, 560f);
                btnGo.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.24f, 0.96f);
                var le = btnGo.GetComponent<LayoutElement>();
                le.preferredWidth = 420f;
                le.preferredHeight = 560f;

                var innerLayout = btnGo.AddComponent<VerticalLayoutGroup>();
                innerLayout.spacing = 10f;
                innerLayout.padding = new RectOffset(20, 20, 24, 24);
                innerLayout.childAlignment = TextAnchor.UpperCenter;
                innerLayout.childForceExpandWidth = false;
                innerLayout.childForceExpandHeight = false;

                // 遗物图标（RelicsArt 素材，由 RelicManager 加载到 relic.icon）
                var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                iconGo.transform.SetParent(btnGo.transform, false);
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = relic.icon;
                iconImg.preserveAspect = true;
                iconImg.color = Color.white;
                var iconLe = iconGo.GetComponent<LayoutElement>();
                iconLe.preferredHeight = 240f;

                // 名称
                var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
                nameGo.transform.SetParent(btnGo.transform, false);
                var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
                if (font != null) nameTmp.font = font;
                nameTmp.text = relic.relicName;
                nameTmp.fontSize = 34f;
                nameTmp.alignment = TextAlignmentOptions.Center;
                nameTmp.color = new Color(1f, 0.92f, 0.6f);

                // 阵营 · 稀有度
                var metaGo = new GameObject("Meta", typeof(RectTransform), typeof(TextMeshProUGUI));
                metaGo.transform.SetParent(btnGo.transform, false);
                var metaTmp = metaGo.GetComponent<TextMeshProUGUI>();
                if (font != null) metaTmp.font = font;
                metaTmp.text = $"{relic.GetFactionName()} · {relic.GetRarityName()}";
                metaTmp.fontSize = 22f;
                metaTmp.alignment = TextAlignmentOptions.Center;
                metaTmp.color = new Color(0.7f, 0.7f, 0.75f);

                // 描述
                var descGo = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
                descGo.transform.SetParent(btnGo.transform, false);
                var descTmp = descGo.GetComponent<TextMeshProUGUI>();
                if (font != null) descTmp.font = font;
                descTmp.text = relic.description;
                descTmp.fontSize = 24f;
                descTmp.alignment = TextAlignmentOptions.Top;
                descTmp.color = new Color(0.85f, 0.85f, 0.88f);
                descTmp.enableWordWrapping = true;

                // 点击：隐藏并回调（选取瞬间播放遗物主题音效，可在设置中关闭）
                var button = btnGo.GetComponent<Button>();
                UiFeel.ApplyButton(button);
                var captured = relic;
                button.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayBossRelicPick(captured.relicId);
                    Hide();
                    onPicked?.Invoke(captured);
                });
            }
        }

        private static Sprite LoadUISprite(string name)
        {
            return Resources.Load<Sprite>("InterfaceUI/" + name);
        }

        private static TMP_FontAsset LoadFont()
        {
            if (cachedFont == null)
                cachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/SIMSUN SDF");
            return cachedFont;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        #endregion

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
