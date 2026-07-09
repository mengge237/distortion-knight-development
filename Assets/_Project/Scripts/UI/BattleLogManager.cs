using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace MutationChess.UI
{
    public class BattleLogManager : MonoBehaviour
    {
        public static BattleLogManager Instance { get; private set; }

        [Header("=== Toast ===")]
        [SerializeField] private TMP_Text toastText;
        [SerializeField] private CanvasGroup toastCanvasGroup;
        [SerializeField] private float toastDuration = 1.2f;
        [SerializeField] private float toastFadeInDuration = 0.25f;
        [SerializeField] private float toastFadeOutDuration = 0.4f;

        [Header("=== History Panel ===")]
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private TMP_Text historyText;
        [SerializeField] private ScrollRect historyScrollRect;

        [Header("=== Toggle Button ===")]
        [SerializeField] private Button toggleHistoryButton;

        private List<string> logHistory = new List<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[BattleLogManager] Instance initialized");
        }

        private void Start()
        {
            if (historyPanel != null) historyPanel.SetActive(false);
            if (toastCanvasGroup != null) toastCanvasGroup.alpha = 0f;
            if (toastText != null) toastText.text = "";

            if (toggleHistoryButton != null)
                toggleHistoryButton.onClick.AddListener(ToggleHistoryPanel);

            Debug.Log($"[BattleLogManager] Start - toastText:{toastText != null}, toastCanvas:{toastCanvasGroup != null}, historyPanel:{historyPanel != null}, toggleBtn:{toggleHistoryButton != null}");
        }

        public void AddLog(string msg)
        {
            Debug.Log($"[BattleLogManager] AddLog: {msg}");
            logHistory.Add(msg);
            UpdateHistoryText();
            ShowToast(msg);
        }

        private void ShowToast(string msg)
        {
            if (toastText == null || toastCanvasGroup == null)
            {
                Debug.LogWarning($"[BattleLogManager] ShowToast skipped - toastText:{toastText != null}, toastCanvasGroup:{toastCanvasGroup != null}");
                return;
            }

            toastText.text = msg;
            toastCanvasGroup.alpha = 0f;
            toastText.transform.localScale = Vector3.one * 0.7f;

            Sequence seq = DOTween.Sequence();
            seq.Append(toastCanvasGroup.DOFade(1f, toastFadeInDuration));
            seq.Join(toastText.transform.DOScale(1f, toastFadeInDuration).SetEase(Ease.OutBack));

            seq.AppendInterval(toastDuration);

            seq.Append(toastCanvasGroup.DOFade(0f, toastFadeOutDuration));
            seq.Join(toastText.transform.DOScale(1.1f, toastFadeOutDuration).SetEase(Ease.InQuad));
            seq.Join(toastText.transform.DOLocalMoveY(toastText.transform.localPosition.y + 40f, toastFadeOutDuration).SetEase(Ease.InQuad));

            seq.OnComplete(() =>
            {
                toastText.transform.localScale = Vector3.one;
            });
        }

        private void ToggleHistoryPanel()
        {
            if (historyPanel == null)
            {
                Debug.LogWarning("[BattleLogManager] ToggleHistoryPanel - historyPanel is null!");
                return;
            }

            bool isActive = historyPanel.activeSelf;
            historyPanel.SetActive(!isActive);
            Debug.Log($"[BattleLogManager] ToggleHistoryPanel - opening:{!isActive}, logCount:{logHistory.Count}");

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
                Debug.LogWarning("[BattleLogManager] UpdateHistoryText - historyText is null!");
                return;
            }
            historyText.text = string.Join("\n", logHistory);
            Debug.Log($"[BattleLogManager] UpdateHistoryText - lines:{logHistory.Count}, text length:{historyText.text.Length}");
        }

        public void ClearLogs()
        {
            logHistory.Clear();
            UpdateHistoryText();
        }
    }
}
