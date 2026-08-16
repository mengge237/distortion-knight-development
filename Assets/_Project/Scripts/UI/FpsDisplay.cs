using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MutationChess.Core;

namespace MutationChess.UI
{
    /// <summary>
    /// 场景角标式 FPS 显示：左上角小字实时帧数，随场景销毁。
    /// 主场景内已有场景接线 fpsText（SettingsManager 负责），本组件只在
    /// 没有 SettingsManager 的场景（首页等）创建，避免双份角标；
    /// 开关由 PlayerPrefs "ShowFPS" 驱动，与战斗内设置面板共享同一键。
    /// </summary>
    public class FpsDisplay : MonoBehaviour
    {
        private static FpsDisplay _instance;

        private TMP_Text fpsText;
        private float timer;
        private int frameCount;

        /// <summary>确保角标存在（幂等）：首页 Start 调用；战斗场景检测到 SettingsManager 时跳过。</summary>
        public static void EnsureExists()
        {
            if (_instance != null) return;

            // 战斗场景自带 fpsText（场景接线），不重复创建
            if (Object.FindObjectOfType<SettingsManager>() != null) return;

            GameObject go = new GameObject("FpsDisplay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 990; // 顶层角标，不遮挡交互（raycast 关闭）
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            go.GetComponent<GraphicRaycaster>().enabled = false;

            GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            RectTransform rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(16f, -10f);
            rt.sizeDelta = new Vector2(240f, 30f);
            TMP_Text tmp = textGo.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset font = UiFonts.Load();
            if (font != null) tmp.font = font;
            tmp.fontSize = 20f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = new Color(0.6f, 0.75f, 0.5f, 0.85f);
            tmp.text = "FPS: --";
            tmp.raycastTarget = false;

            _instance = go.AddComponent<FpsDisplay>();
            _instance.fpsText = tmp;
            _instance.SetVisibleInternal(PlayerPrefs.GetInt("ShowFPS", 0) == 1);
        }

        /// <summary>开关角标显示（无角标场景时无操作）。</summary>
        public static void SetVisible(bool visible)
        {
            if (_instance != null)
                _instance.SetVisibleInternal(visible);
        }

        private void SetVisibleInternal(bool visible)
        {
            if (fpsText != null)
                fpsText.gameObject.SetActive(visible);
        }

        void Update()
        {
            if (fpsText == null || !fpsText.gameObject.activeSelf) return;

            frameCount++;
            timer += Time.unscaledDeltaTime;
            if (timer >= 0.5f)
            {
                int fps = Mathf.RoundToInt(frameCount / timer);
                fpsText.text = $"FPS: {fps}";
                timer = 0f;
                frameCount = 0;
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
