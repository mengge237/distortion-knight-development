using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MutationChess.Core;

namespace MutationChess.UI
{
    /// <summary>
    /// 开局加载缓冲屏：全屏遮罩 + 标题 + 进度条 + 滚动常识提示（淡入淡出轮播）。
    /// 使用 LoadSceneAsync(allowSceneActivation=false) 在后台加载目标场景，
    /// 进度达到 90% 后激活切换；覆盖层随首页场景一同销毁，无需手动清理。
    /// 首页"开始游戏"（难度选定后）与"继续游戏"（选定存档位后）共用。
    /// </summary>
    public static class LoadingScreen
    {
        private static bool busy = false;

        public static bool IsShowing => busy;

        /// <summary>开始异步加载目标场景并显示缓冲屏（加载中重复调用忽略）。</summary>
        public static void ShowAndLoad(string sceneName)
        {
            if (busy) return;
            busy = true;

            GameObject runnerGo = new GameObject("LoadingScreenRunner");
            runnerGo.AddComponent<CoroutineRunner>().StartRoutine(Run(sceneName));
        }

        /// <summary>极简协程宿主（静态类需要挂载点）。</summary>
        private class CoroutineRunner : MonoBehaviour
        {
            public void StartRoutine(IEnumerator routine)
            {
                StartCoroutine(routine);
            }
        }

        private static IEnumerator Run(string sceneName)
        {
            // 时间流速兜底：难度面板关闭时已恢复，此处确保加载动画用 unscaled 时间照常播放
            Time.timeScale = 1f;

            // 保险：场景缺失 EventSystem 时自动创建（加载屏按钮/文本不需要交互，但保留兜底）
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            Canvas canvas = BuildOverlay();

            // 异步加载主场景：进度到 90% 前不激活（Unity 规定 allowSceneActivation=false 时进度封顶 0.9）
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                GameLogger.LogError($"[LoadingScreen] 场景 {sceneName} 加载失败，回退同步加载");
                SceneManager.LoadScene(sceneName);
                busy = false;
                yield break;
            }
            op.allowSceneActivation = false;

            TMP_Text tipTmp = canvas.transform.Find("Frame/Tip").GetComponent<TMP_Text>();
            Image fill = canvas.transform.Find("Frame/ProgressBar/Fill").GetComponent<Image>();
            TMP_Text percentTmp = canvas.transform.Find("Frame/ProgressBar/Percent").GetComponent<TMP_Text>();

            // 提示轮播状态（unscaled 时间：加载期间 timeScale 不受影响）
            int tipIndex = Random.Range(0, GameTips.All.Length);
            float tipElapsed = 0f;
            const float fadeIn = 0.5f, hold = 2.6f, fadeOut = 0.6f;
            const float cycle = fadeIn + hold + fadeOut;

            tipTmp.text = GameTips.All[tipIndex];

            while (op.progress < 0.9f)
            {
                tipElapsed += Time.unscaledDeltaTime;
                if (tipElapsed >= cycle)
                {
                    tipElapsed = 0f;
                    tipIndex = (tipIndex + 1) % GameTips.All.Length;
                    tipTmp.text = GameTips.All[tipIndex];
                }

                // 淡入 → 保持 → 淡出
                float alpha = 1f;
                if (tipElapsed < fadeIn) alpha = tipElapsed / fadeIn;
                else if (tipElapsed > fadeIn + hold) alpha = Mathf.Clamp01((cycle - tipElapsed) / fadeOut);
                tipTmp.color = new Color(0.9f, 0.86f, 0.75f, alpha);

                fill.fillAmount = Mathf.Clamp01(op.progress / 0.9f);
                if (percentTmp != null)
                    percentTmp.text = $"{Mathf.RoundToInt(fill.fillAmount * 100f)}%";
                yield return null;
            }

            fill.fillAmount = 1f;
            if (percentTmp != null) percentTmp.text = "100%";

            // 最后一拍让"踏入深渊"感停留半秒，再激活场景切换
            yield return new WaitForSecondsRealtime(0.4f);

            op.allowSceneActivation = true;
            busy = false;
        }

        /// <summary>构建全屏缓冲覆盖层（sortingOrder 950，压过首页 500/图鉴 700/难度面板 900）。</summary>
        private static Canvas BuildOverlay()
        {
            GameObject overlay = new GameObject("LoadingScreen", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;
            CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            TMP_FontAsset font = UiFonts.Load();

            // 全屏暗底
            GameObject bg = new GameObject("Dim", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(overlay.transform, false);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.03f, 0.03f, 0.05f, 0.98f);
            bg.GetComponent<Image>().raycastTarget = true; // 拦截点击，加载中不可操作旧界面

            // 金边外框
            GameObject border = new GameObject("GoldBorder", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(overlay.transform, false);
            RectTransform borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = borderRt.anchorMax = new Vector2(0.5f, 0.5f);
            borderRt.sizeDelta = new Vector2(1024f, 560f);
            border.GetComponent<Image>().color = new Color(0.62f, 0.5f, 0.24f, 1f);

            // 面板底板
            GameObject frame = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frame.transform.SetParent(overlay.transform, false);
            RectTransform frameRt = frame.GetComponent<RectTransform>();
            frameRt.anchorMin = frameRt.anchorMax = new Vector2(0.5f, 0.5f);
            frameRt.sizeDelta = new Vector2(1008f, 544f);
            Sprite innerBg = Resources.Load<Sprite>("InterfaceUI/获胜奖励面板底层内嵌背景");
            Image frameImg = frame.GetComponent<Image>();
            if (innerBg != null)
            {
                frameImg.sprite = innerBg;
                frameImg.color = Color.white;
            }
            else frameImg.color = new Color(0.08f, 0.075f, 0.1f, 0.99f);

            // 标题
            TMP_Text title = CreateText(frame.transform, "Title", font, 44, TextAlignmentOptions.Center, new Color(0.92f, 0.8f, 0.42f));
            title.fontStyle = FontStyles.Bold;
            RectTransform titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -36f);
            titleRt.sizeDelta = new Vector2(700f, 56f);
            title.text = "踏 入 深 渊";

            // 副标题
            TMP_Text subtitle = CreateText(frame.transform, "Subtitle", font, 20, TextAlignmentOptions.Center, new Color(0.6f, 0.58f, 0.52f));
            RectTransform subRt = subtitle.rectTransform;
            subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 1f);
            subRt.pivot = new Vector2(0.5f, 1f);
            subRt.anchoredPosition = new Vector2(0f, -100f);
            subRt.sizeDelta = new Vector2(800f, 30f);
            subtitle.text = "棋局正在展开 · 命运之线交错";

            // 滚动常识提示
            TMP_Text tip = CreateText(frame.transform, "Tip", font, 24, TextAlignmentOptions.Center, new Color(0.9f, 0.86f, 0.75f, 0f));
            RectTransform tipRt = tip.rectTransform;
            tipRt.anchorMin = tipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tipRt.pivot = new Vector2(0.5f, 0.5f);
            tipRt.anchoredPosition = new Vector2(0f, 28f);
            tipRt.sizeDelta = new Vector2(860f, 130f);
            tip.text = "◆ 黑烛护体可免疫一切诅咒降临";

            // 进度条：底板 + 金色填充
            GameObject barGo = new GameObject("ProgressBar", typeof(RectTransform));
            barGo.transform.SetParent(frame.transform, false);
            RectTransform barRt = barGo.GetComponent<RectTransform>();
            barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = new Vector2(0f, 64f);
            barRt.sizeDelta = new Vector2(640f, 18f);

            GameObject barBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(barGo.transform, false);
            RectTransform barBgRt = barBg.GetComponent<RectTransform>();
            barBgRt.anchorMin = Vector2.zero;
            barBgRt.anchorMax = Vector2.one;
            barBgRt.offsetMin = Vector2.zero;
            barBgRt.offsetMax = Vector2.zero;
            barBg.GetComponent<Image>().color = new Color(0.2f, 0.19f, 0.17f, 1f);

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(barGo.transform, false);
            RectTransform fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillGo.GetComponent<Image>().color = new Color(0.62f, 0.5f, 0.24f, 1f);

            // 进度百分比
            TMP_Text percent = CreateText(barGo.transform, "Percent", font, 18, TextAlignmentOptions.Center, new Color(0.8f, 0.76f, 0.65f));
            RectTransform percentRt = percent.rectTransform;
            percentRt.anchorMin = percentRt.anchorMax = new Vector2(0.5f, 1f);
            percentRt.pivot = new Vector2(0.5f, 1f);
            percentRt.anchoredPosition = new Vector2(0f, 6f);
            percentRt.sizeDelta = new Vector2(200f, 28f);
            percent.text = "0%";

            // 底部小字
            TMP_Text footer = CreateText(frame.transform, "Footer", font, 16, TextAlignmentOptions.Center, new Color(0.45f, 0.43f, 0.4f));
            RectTransform footerRt = footer.rectTransform;
            footerRt.anchorMin = footerRt.anchorMax = new Vector2(0.5f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.anchoredPosition = new Vector2(0f, 24f);
            footerRt.sizeDelta = new Vector2(800f, 26f);
            footer.text = "提示会在每局开始时滚动展示 · 冒险须知中可翻阅全部";

            return canvas;
        }

        private static TMP_Text CreateText(Transform parent, string goName, TMP_FontAsset font, int fontSize, TextAlignmentOptions align, Color color)
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
    }
}
