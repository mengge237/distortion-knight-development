using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;

namespace MutationChess.UI
{
    /// <summary>
    /// 以撒式存档选择页：3 个存档位卡片 + 全成就展示区（图鉴收集进度占位）。
    /// 两种模式：
    ///  - 继续游戏：仅已有存档的槽位可点，点击 → 设定活动槽位 + 标记待读档 → 加载主场景；
    ///  - 开始新游戏：空槽位直接开始；已有存档的槽位需"再点一次"确认覆盖（或先删除）。
    /// 点击结果通过回调交给 HomeScreen 处理（HomeScreen 负责 SetActiveSlot/SetPendingLoad/难度面板/加载屏）。
    /// 运行时自建 Canvas（sortingOrder 910，压过首页 500/图鉴 700/难度面板 900，低于加载屏 950）。
    /// </summary>
    public class SaveSlotPanel : MonoBehaviour
    {
        private static SaveSlotPanel _instance;

        public static SaveSlotPanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveSlotPanel>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveSlotPanel");
                        _instance = go.AddComponent<SaveSlotPanel>();
                    }
                }
                return _instance;
            }
        }

        private Canvas canvas;
        private GameObject panelGo;
        private TMP_FontAsset font;

        private bool continueMode;
        private System.Action<int> onSlotPicked;
        private System.Action onBack;

        private TMP_Text[] slotTitle = new TMP_Text[3];
        private TMP_Text[] slotBody = new TMP_Text[3];
        private TMP_Text[] slotActionLabel = new TMP_Text[3];
        private TMP_Text[] deleteLabel = new TMP_Text[3];
        private Button[] slotButtons = new Button[3];
        private GameObject[] deleteGos = new GameObject[3];
        private TMP_Text achievementText;
        private TMP_Text achievementPlaceholder;

        private int pendingConfirmSlot = 0; // 新游戏模式：覆盖二次确认
        private int pendingDeleteSlot = 0;  // 删除二次确认

        /// <summary>显示存档选择页。onSlotPicked(slot) 在玩家选定槽位后回调；onBack 点击返回。</summary>
        public static void Show(bool continueMode, System.Action<int> onSlotPicked, System.Action onBack)
        {
            SaveSlotPanel panel = Instance;
            panel.continueMode = continueMode;
            panel.onSlotPicked = onSlotPicked;
            panel.onBack = onBack;

            if (panel.panelGo == null)
                panel.BuildPanel();
            panel.RefreshSlots();
            panel.RefreshAchievements();
            panel.panelGo.SetActive(true);
            panel.gameObject.SetActive(true);
        }

        public static void Hide()
        {
            if (_instance != null && _instance.panelGo != null)
                _instance.panelGo.SetActive(false);
        }

        public static bool IsShowing() => _instance != null && _instance.panelGo != null && _instance.panelGo.activeSelf;

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // ================= UI 构建 =================

        private void BuildPanel()
        {
            GameObject go = gameObject;
            canvas = go.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = go.AddComponent<Canvas>();
                CanvasScaler scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                go.AddComponent<GraphicRaycaster>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 910;

            font = UiFonts.Load();

            panelGo = new GameObject("Panel", typeof(RectTransform));
            panelGo.transform.SetParent(go.transform, false);
            RectTransform panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            // 全屏暗底（拦截点击）
            GameObject dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            dim.transform.SetParent(panelGo.transform, false);
            RectTransform dimRt = dim.GetComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            dim.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.05f, 0.94f);
            dim.GetComponent<Image>().raycastTarget = true;

            // 金边外框 + 底板
            GameObject border = new GameObject("GoldBorder", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(panelGo.transform, false);
            RectTransform borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = borderRt.anchorMax = new Vector2(0.5f, 0.5f);
            borderRt.sizeDelta = new Vector2(1288f, 868f);
            border.GetComponent<Image>().color = new Color(0.62f, 0.5f, 0.24f, 1f);

            GameObject frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(panelGo.transform, false);
            RectTransform frameRt = frame.GetComponent<RectTransform>();
            frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(1272f, 852f);
            frame.GetComponent<Image>().color = new Color(0.08f, 0.075f, 0.1f, 0.99f);

            // 标题
            TMP_Text title = CreateText(frame.transform, "Title", 42, TextAlignmentOptions.Center, new Color(0.92f, 0.8f, 0.42f));
            title.fontStyle = FontStyles.Bold;
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -28f);
            titleRt.sizeDelta = new Vector2(700f, 56f);
            title.text = "选 择 存 档";

            TMP_Text subtitle = CreateText(frame.transform, "Subtitle", 19, TextAlignmentOptions.Center, new Color(0.6f, 0.58f, 0.52f));
            RectTransform subRt = subtitle.rectTransform;
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -92f);
            subRt.sizeDelta = new Vector2(800f, 28f);
            subtitle.text = "尘封的旅途与未竟的深渊";

            // 三个存档位卡片
            float[] cardXs = { -420f, 0f, 420f };
            for (int i = 0; i < 3; i++)
            {
                BuildSlotCard(frame.transform, i, cardXs[i]);
            }

            // 全成就展示区（图鉴收集进度占位）
            BuildAchievementArea(frame.transform);

            // 返回按钮
            CreateButton(frame.transform, "BackButton", new Vector2(320f, 60f), new Vector2(0f, 44f), "返 回",
                new Color(0.24f, 0.22f, 0.18f), new Color(0.9f, 0.86f, 0.72f), () =>
                {
                    pendingConfirmSlot = 0;
                    pendingDeleteSlot = 0;
                    panelGo.SetActive(false);
                    onBack?.Invoke();
                });

            panelGo.SetActive(false);
            GameLogger.Log("[SaveSlotPanel] 以撒式存档选择页已构建");
        }

        private void BuildSlotCard(Transform parent, int index, float x)
        {
            GameObject card = new GameObject($"SlotCard_{index + 1}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(x, 60f);
            cardRt.sizeDelta = new Vector2(380f, 430f);
            card.GetComponent<Image>().color = new Color(0.12f, 0.11f, 0.14f, 1f);

            // 槽位标题
            TMP_Text titleTmp = CreateText(card.transform, "SlotTitle", 26, TextAlignmentOptions.Center, new Color(0.85f, 0.78f, 0.55f));
            titleTmp.fontStyle = FontStyles.Bold;
            RectTransform tRt = titleTmp.rectTransform;
            tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -16f);
            tRt.sizeDelta = new Vector2(340f, 40f);
            slotTitle[index] = titleTmp;

            // 存档信息
            TMP_Text bodyTmp = CreateText(card.transform, "Body", 19, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.82f, 0.75f));
            bodyTmp.lineSpacing = 6f;
            RectTransform bRt = bodyTmp.rectTransform;
            bRt.anchorMin = bRt.anchorMax = new Vector2(0.5f, 1f);
            bRt.pivot = new Vector2(0.5f, 1f);
            bRt.anchoredPosition = new Vector2(0f, -62f);
            bRt.sizeDelta = new Vector2(340f, 250f);
            slotBody[index] = bodyTmp;

            // 主操作按钮
            var action = CreateButton(card.transform, "SlotButton", new Vector2(300f, 56f), new Vector2(0f, 70f), "",
                new Color(0.35f, 0.28f, 0.15f), new Color(0.95f, 0.9f, 0.78f), () => OnSlotClicked(index + 1));
            slotButtons[index] = action.button;
            slotActionLabel[index] = action.label;

            // 删除按钮（仅已有存档时显示）
            var del = CreateButton(card.transform, "DeleteButton", new Vector2(200f, 36f), new Vector2(0f, 26f), "删除存档",
                new Color(0.3f, 0.12f, 0.12f), new Color(0.85f, 0.6f, 0.55f), () => OnDeleteClicked(index + 1));
            deleteGos[index] = del.go;
            deleteLabel[index] = del.label;
        }

        private void BuildAchievementArea(Transform parent)
        {
            GameObject area = new GameObject("AchievementArea", typeof(RectTransform), typeof(Image));
            area.transform.SetParent(parent, false);
            RectTransform areaRt = area.GetComponent<RectTransform>();
            areaRt.anchorMin = areaRt.anchorMax = new Vector2(0.5f, 0f);
            areaRt.pivot = new Vector2(0.5f, 0f);
            areaRt.anchoredPosition = new Vector2(0f, 118f);
            areaRt.sizeDelta = new Vector2(1160f, 150f);
            area.GetComponent<Image>().color = new Color(0.1f, 0.095f, 0.12f, 1f);

            TMP_Text title = CreateText(area.transform, "AreaTitle", 22, TextAlignmentOptions.Center, new Color(0.75f, 0.68f, 0.45f));
            RectTransform tRt = title.rectTransform;
            tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.anchoredPosition = new Vector2(0f, -10f);
            tRt.sizeDelta = new Vector2(1100f, 32f);
            title.text = "◆ 全成就展示区（图鉴收集进度）◆";

            achievementText = CreateText(area.transform, "Progress", 21, TextAlignmentOptions.Center, new Color(0.85f, 0.8f, 0.66f));
            RectTransform aRt = achievementText.rectTransform;
            aRt.anchorMin = aRt.anchorMax = new Vector2(0.5f, 1f);
            aRt.pivot = new Vector2(0.5f, 1f);
            aRt.anchoredPosition = new Vector2(0f, -52f);
            aRt.sizeDelta = new Vector2(1100f, 34f);

            achievementPlaceholder = CreateText(area.transform, "Placeholder", 17, TextAlignmentOptions.Center, new Color(0.45f, 0.43f, 0.4f));
            RectTransform pRt = achievementPlaceholder.rectTransform;
            pRt.anchorMin = pRt.anchorMax = new Vector2(0.5f, 1f);
            pRt.pivot = new Vector2(0.5f, 1f);
            pRt.anchoredPosition = new Vector2(0f, -92f);
            pRt.sizeDelta = new Vector2(1100f, 26f);
            achievementPlaceholder.text = "以撒式全成就页面占位 · 成就系统尚未实装，暂以图鉴收集进度展示";
        }

        private TMP_Text CreateText(Transform parent, string goName, int fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private (GameObject go, Button button, TMP_Text label) CreateButton(Transform parent, string goName, Vector2 size, Vector2 anchoredPos, string labelText, Color bgColor, Color textColor, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.color = bgColor;
            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onClick);
            UiFeel.ApplyButton(btn);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform lRt = labelGo.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;
            TMP_Text label = labelGo.GetComponent<TextMeshProUGUI>();
            if (font != null) label.font = font;
            label.fontSize = 21;
            label.alignment = TextAlignmentOptions.Center;
            label.color = textColor;
            label.raycastTarget = false;
            label.text = labelText;

            return (go, btn, label);
        }

        // ================= 交互 =================

        private void OnSlotClicked(int slot)
        {
            if (continueMode)
            {
                if (!SaveService.Instance.HasSave(slot))
                {
                    AudioManager.Instance?.PlayUIClick(0.25f);
                    return;
                }
                onSlotPicked?.Invoke(slot);
                return;
            }

            // 开始新游戏模式
            if (!SaveService.Instance.HasSave(slot))
            {
                onSlotPicked?.Invoke(slot);
                return;
            }

            if (pendingConfirmSlot != slot)
            {
                pendingConfirmSlot = slot;
                slotActionLabel[slot - 1].text = "⚠ 再点一次覆盖此存档";
                AudioManager.Instance?.PlayUIClick(0.25f);
                return;
            }
            pendingConfirmSlot = 0;
            onSlotPicked?.Invoke(slot);
        }

        private void OnDeleteClicked(int slot)
        {
            if (!SaveService.Instance.HasSave(slot)) return;

            if (pendingDeleteSlot != slot)
            {
                pendingDeleteSlot = slot;
                deleteLabel[slot - 1].text = "⚠ 再点一次删除";
                AudioManager.Instance?.PlayUIClick(0.25f);
                return;
            }

            pendingDeleteSlot = 0;
            SaveService.Instance.DeleteSave(slot);
            GameLogger.Log($"[SaveSlotPanel] 已删除槽位 {slot} 存档");
            RefreshSlots();
        }

        // ================= 刷新 =================

        private void RefreshSlots()
        {
            pendingConfirmSlot = 0;
            pendingDeleteSlot = 0;

            int activeSlot = SaveService.GetActiveSlot();
            for (int i = 1; i <= 3; i++)
            {
                int idx = i - 1;
                bool has = SaveService.Instance.HasSave(i);
                SaveService.SaveSlotMeta meta = has ? SaveService.Instance.GetMeta(i) : null;
                bool isActive = i == activeSlot;

                slotTitle[idx].text = $"{(isActive ? "★ " : "")}槽位 {i}";

                if (has && meta != null)
                {
                    int minutes = (int)(meta.playtimeSeconds / 60f);
                    slotBody[idx].text =
                        $"难度：{meta.difficulty}\n" +
                        $"进度：第 {meta.floor} 层\n" +
                        $"生命：{meta.hp} / {meta.maxHp}\n" +
                        $"金币：{meta.gold}\n" +
                        $"游玩：{minutes} 分钟\n" +
                        $"保存于 {meta.savedAt}";
                }
                else
                {
                    slotBody[idx].text = "空 存 档 位\n\n\n尚未有冒险在此留下痕迹\n点击开始新的旅途";
                }

                slotActionLabel[idx].text = continueMode
                    ? "继 续 冒 险"
                    : (has ? "覆盖并开始新游戏" : "开始新冒险");

                slotButtons[idx].interactable = continueMode ? has : true;
                deleteGos[idx].SetActive(has);
                deleteLabel[idx].text = "删除存档";
            }
        }

        private void RefreshAchievements()
        {
            CodexProgress codex = CodexProgress.Instance;
            if (codex == null)
            {
                achievementText.text = "图鉴收集进度：--";
                return;
            }

            int cSeen = codex.SeenCount(CodexCategory.Card);
            int cTotal = codex.TotalCount(CodexCategory.Card);
            int rSeen = codex.SeenCount(CodexCategory.Relic);
            int rTotal = codex.TotalCount(CodexCategory.Relic);
            int pSeen = codex.SeenCount(CodexCategory.Potion);
            int pTotal = codex.TotalCount(CodexCategory.Potion);
            int totalSeen = cSeen + rSeen + pSeen;
            int totalAll = cTotal + rTotal + pTotal;

            achievementText.text =
                $"卡牌 {cSeen}/{cTotal} · 遗物 {rSeen}/{rTotal} · 药水 {pSeen}/{pTotal} · 总计 {totalSeen}/{totalAll}";
        }
    }
}
