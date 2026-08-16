using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MutationChess.Core;

namespace MutationChess.UI
{
    /// <summary>
    /// 全局 UI 手感助手：按钮按压缩放回弹 + 悬停微放大 + 点击音效，面板弹入动画。
    /// 场景接线与运行时构建的按钮/面板统一走这里，保证触碰质感一致。
    /// </summary>
    public static class UiFeel
    {
        /// <summary>为按钮附加按压/悬停/点击音效手感（重复调用安全）。</summary>
        public static void ApplyButton(Button btn, float pressScale = 0.92f)
        {
            if (btn == null) return;
            if (btn.GetComponent<UiPressFeedback>() != null) return;
            UiPressFeedback fb = btn.gameObject.AddComponent<UiPressFeedback>();
            fb.Init(pressScale);
        }

        /// <summary>递归为根节点下所有 Button 附加手感（含未激活子物体）。</summary>
        public static void ApplyToAllButtons(GameObject root)
        {
            if (root == null) return;
            foreach (var btn in root.GetComponentsInChildren<Button>(true))
                ApplyButton(btn);
        }

        /// <summary>面板弹入：缩放回弹 + 淡入，并播放面板音效。</summary>
        public static void AnimatePanelIn(GameObject panel, float fromScale = 0.86f, float duration = 0.22f)
        {
            if (panel == null) return;

            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            RectTransform rt = panel.GetComponent<RectTransform>();
            if (rt == null) rt = panel.AddComponent<RectTransform>();

            DOTween.Kill(panel, true); // 清理旧动画，避免残留
            rt.localScale = Vector3.one * fromScale;
            rt.DOScale(1f, duration).SetEase(Ease.OutBack).SetUpdate(true);
            cg.DOFade(1f, duration * 0.7f).SetEase(Ease.OutQuad).SetUpdate(true);
            AudioManager.Instance?.PlayUIPanel();
        }
    }

    /// <summary>按钮按压/悬停手感组件（由 UiFeel.ApplyButton 附加）。</summary>
    public class UiPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private RectTransform rt;
        private float pressScale = 0.92f;
        private bool pressed = false;
        private Vector3 baseScale = Vector3.one;
        private Tween scaleTween;

        void Awake()
        {
            // 场景实体路径自举：编辑期生成时 ApplyButton→Init 已把 rt/baseScale 置好，
            // 但私有字段不序列化，运行时加载后 rt 为空——悬停/按压会 NRE。
            // Awake 重取引用与基准缩放（运行时自建路径：AddComponent 时 Awake 先跑、
            // 随后 Init 再设 pressScale，二者不冲突）
            rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            baseScale = rt.localScale;
        }

        public void Init(float scale)
        {
            rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            baseScale = rt.localScale;
            pressScale = scale;
        }

        private bool CanInteract()
        {
            var btn = GetComponent<Button>();
            return btn == null || btn.interactable;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanInteract()) return;
            pressed = true;
            scaleTween?.Kill();
            scaleTween = rt.DOScale(baseScale * pressScale, 0.06f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!pressed) return;
            pressed = false;
            scaleTween?.Kill();
            scaleTween = rt.DOScale(baseScale, 0.16f).SetEase(Ease.OutBack).SetUpdate(true);
            if (CanInteract())
                AudioManager.Instance?.PlayUIClick();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanInteract()) return;
            scaleTween?.Kill();
            scaleTween = rt.DOScale(baseScale * 1.04f, 0.09f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (pressed) return;
            scaleTween?.Kill();
            scaleTween = rt.DOScale(baseScale, 0.12f).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        void OnDisable()
        {
            pressed = false;
            scaleTween?.Kill();
            scaleTween = null;
            if (rt != null)
                rt.localScale = baseScale;
        }
    }
}
