using UnityEngine;
using MutationChess.Core;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace MutationChess.UI
{
    public class BattleLogManager : MonoBehaviour
    {
        public static BattleLogManager Instance { get; private set; }

        [Header("Toast Template")]
        [SerializeField] private GameObject toastTemplate;
        [SerializeField] private RectTransform toastContainer;
        [SerializeField] private TMP_Text toastTextTemplate;
        [SerializeField] private Image toastBgTemplate;
        [SerializeField] private int maxVisibleToasts = 5;
        [SerializeField] private float toastSpacing = 64f;
        [SerializeField] private float shiftAnimationDuration = 0.4f;

        [Header("Toast Timing")]
        [SerializeField] private float toastDuration = 5f;
        [SerializeField] private float toastFadeInDuration = 0.5f;
        [SerializeField] private float toastFadeOutDuration = 0.6f;

        [Header("History Panel")]
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private TMP_Text historyText;
        [SerializeField] private ScrollRect historyScrollRect;

        [Header("Toggle Button")]
        [SerializeField] private Button toggleHistoryButton;

        private List<string> logHistory = new List<string>();
        private List<GameObject> activeToasts = new List<GameObject>();
        private Dictionary<GameObject, Sequence> toastSequences = new Dictionary<GameObject, Sequence>();
        private string previousFullText = "";

        // 模板组件位置缓存
        private bool toastTextIsRoot;
        private bool toastBgIsRoot;
        private int toastTextSiblingIndex = -1;
        private int toastBgSiblingIndex = -1;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            // 清理所有 toast 的动画，避免销毁后 DOTween 仍访问 RectTransform
            foreach (var toast in activeToasts)
            {
                KillToastTweens(toast);
            }
            activeToasts.Clear();
            toastSequences.Clear();
        }

        /// <summary>
        /// 终止指定 toast 上所有关联的 DOTween 动画（Sequence + 位移动画等）。
        /// </summary>
        private void KillToastTweens(GameObject toast)
        {
            if (toast == null) return;
            if (toastSequences.TryGetValue(toast, out var seq))
            {
                if (seq != null && seq.IsActive()) seq.Kill();
                toastSequences.Remove(toast);
            }
            // 终止所有以该 transform 为目标的独立动画（如位移 shift 动画）
            DOTween.Kill(toast.transform);
        }

        /// <summary>
        /// 安全销毁 toast：先终止所有动画，再销毁对象。
        /// </summary>
        private void DestroyToast(GameObject toast)
        {
            KillToastTweens(toast);
            if (toast != null) Destroy(toast);
        }

        private void Start()
        {
            if (historyPanel != null) historyPanel.SetActive(false);

            if (toastTemplate != null)
            {
                toastTemplate.SetActive(false);

                //
                if (toastTextTemplate != null)
                {
                    if (toastTextTemplate.transform == toastTemplate.transform)
                        toastTextIsRoot = true;
                    else
                        toastTextSiblingIndex = toastTextTemplate.transform.GetSiblingIndex();
                }
                if (toastBgTemplate != null)
                {
                    if (toastBgTemplate.transform == toastTemplate.transform)
                        toastBgIsRoot = true;
                    else
                        toastBgSiblingIndex = toastBgTemplate.transform.GetSiblingIndex();
                }
            }

            if (toggleHistoryButton != null)
                toggleHistoryButton.onClick.AddListener(ToggleHistoryPanel);
        }

        public void AddLog(string msg)
        {
            logHistory.Add(msg);
            UpdateHistoryText();
        }

        /// <summary>
        ///
        /// </summary>
        private static bool IsSymbolOnlyLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return true;
            foreach (char c in line)
            {
                if (char.IsLetterOrDigit(c)) return false;
                //
                if (c >= 0x4e00 && c <= 0x9fff) return false;
                if (c >= 0x3400 && c <= 0x4dbf) return false;
            }
            return true;
        }

        private void ShowToast(string msg)
        {
            if (toastTemplate == null || toastContainer == null)
            {
                GameLogger.LogWarning($"[BattleLogManager] ShowToast skipped - toastTemplate:{toastTemplate != null}, toastContainer:{toastContainer != null}");
                return;
            }

            // 超过最大显示数量时移除最旧的 toast
            while (activeToasts.Count >= maxVisibleToasts)
            {
                GameObject oldest = activeToasts[0];
                activeToasts.RemoveAt(0);
                DestroyToast(oldest);
            }

            // 已有 toast 向上移动
            foreach (var toast in activeToasts)
            {
                if (toast == null) continue;
                RectTransform rt = toast.GetComponent<RectTransform>();
                if (rt != null)
                {
                    DOTween.Kill(rt); // 终止旧的位移动画，避免叠加
                    rt.DOAnchorPosY(rt.anchoredPosition.y + toastSpacing, shiftAnimationDuration).SetEase(Ease.OutQuad);
                }
            }

            //
            GameObject newToast = Instantiate(toastTemplate, toastContainer);
            newToast.SetActive(true);

            RectTransform newRt = newToast.GetComponent<RectTransform>();
            CanvasGroup newCg = newToast.GetComponent<CanvasGroup>();
            TMP_Text newText = FindCloneComponent<TMP_Text>(newToast, toastTextIsRoot, toastTextSiblingIndex);
            Image newBg = FindCloneComponent<Image>(newToast, toastBgIsRoot, toastBgSiblingIndex);

            if (newRt != null)
            {
                newRt.anchorMin = new Vector2(0.5f, 0f);
                newRt.anchorMax = new Vector2(0.5f, 0f);
                newRt.pivot = new Vector2(0.5f, 0f);
                newRt.anchoredPosition = Vector2.zero;
            }

            if (newText != null)
                newText.text = msg;
            if (newCg != null)
                newCg.alpha = 0f;
            if (newBg != null)
                SetImageAlpha(newBg, 0f);

            activeToasts.Add(newToast);

            // 创建淡入→停留→淡出动画序列
            Sequence seq = DOTween.Sequence();

            // 淡入阶段
            if (newCg != null)
            {
                seq.Append(newCg.DOFade(1f, toastFadeInDuration));
                if (newBg != null)
                    seq.Join(newBg.DOFade(1f, toastFadeInDuration));
            }
            else if (newBg != null)
            {
                seq.Append(newBg.DOFade(1f, toastFadeInDuration));
            }

            // 等待提示显示
            seq.AppendInterval(toastDuration);

            // 淡出阶段
            if (newCg != null)
            {
                seq.Append(newCg.DOFade(0f, toastFadeOutDuration));
                if (newBg != null)
                    seq.Join(newBg.DOFade(0f, toastFadeOutDuration));
                if (newRt != null)
                    seq.Join(newRt.DOAnchorPosY(newRt.anchoredPosition.y + 40f, toastFadeOutDuration).SetEase(Ease.InQuad));
            }
            else if (newBg != null)
            {
                seq.Append(newBg.DOFade(0f, toastFadeOutDuration));
                if (newRt != null)
                    seq.Join(newRt.DOAnchorPosY(newRt.anchoredPosition.y + 40f, toastFadeOutDuration).SetEase(Ease.InQuad));
            }

            // 记录 Sequence 引用以便安全清理
            toastSequences[newToast] = seq;

            seq.OnComplete(() =>
            {
                activeToasts.Remove(newToast);
                toastSequences.Remove(newToast);
                if (newToast != null)
                {
                    DOTween.Kill(newToast.transform); // 终止残留的位移等动画
                    Destroy(newToast);
                }
            });
        }

        private void ToggleHistoryPanel()
        {
            if (historyPanel == null)
            {
                GameLogger.LogWarning("[BattleLogManager] ToggleHistoryPanel - historyPanel is null!");
                return;
            }

            bool isActive = historyPanel.activeSelf;
            historyPanel.SetActive(!isActive);
            GameLogger.Log($"[BattleLogManager] ToggleHistoryPanel - opening:{!isActive}, logCount:{logHistory.Count}");

            if (!isActive)
            {
                UpdateHistoryText();
                Canvas.ForceUpdateCanvases();
                if (historyScrollRect != null)
                    historyScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void UpdateHistoryText()
        {
            if (historyText == null)
            {
                GameLogger.LogWarning("[BattleLogManager] UpdateHistoryText - historyText is null!");
                return;
            }

            string newFullText = string.Join("\n", logHistory);

            //
            if (!string.IsNullOrEmpty(previousFullText))
            {
                string[] oldLines = previousFullText.Split('\n');
                string[] newLines = newFullText.Split('\n');

                for (int i = oldLines.Length; i < newLines.Length; i++)
                {
                    string line = newLines[i].Trim();
                    if (!IsSymbolOnlyLine(line))
                    {
                        ShowToast(line);
                    }
                }
            }
            else if (logHistory.Count > 0)
            {
                //
                string lastLine = logHistory[logHistory.Count - 1].Trim();
                if (!IsSymbolOnlyLine(lastLine))
                {
                    ShowToast(lastLine);
                }
            }

            previousFullText = newFullText;
            historyText.text = newFullText;
        }

        public void ClearLogs()
        {
            logHistory.Clear();
            UpdateHistoryText();
        }

        public void HideAllPanels()
        {
            if (historyPanel != null) historyPanel.SetActive(false);
            foreach (var toast in activeToasts)
            {
                DestroyToast(toast);
            }
            activeToasts.Clear();
        }

        /// <summary>
        ///
        /// </summary>
        private static T FindCloneComponent<T>(GameObject clone, bool isRoot, int siblingIndex) where T : Component
        {
            if (clone == null) return null;
            if (isRoot)
                return clone.GetComponent<T>();
            if (siblingIndex >= 0 && siblingIndex < clone.transform.childCount)
                return clone.transform.GetChild(siblingIndex).GetComponent<T>();
            //
            return clone.GetComponentInChildren<T>();
        }

        private static void SetImageAlpha(Image img, float alpha)
        {
            var c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }
}
