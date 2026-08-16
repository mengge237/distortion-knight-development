using UnityEngine;
using UnityEngine.EventSystems;

namespace MutationChess.UI
{
    /// <summary>
    /// 难度滚轮控制器：挂在滚轮视口上，自实现拖拽/滚轮/吸附（不用 ScrollRect，
    /// 避免同物体多组件抢事件：ExecuteEvents 只调用层级上首个实现者）。
    /// 鼠标滚轮切换相邻难度、拖拽松手/点击后吸附到最近的居中卡位
    /// （unscaled 时间动画，面板打开时 timeScale=0 也不受影响），居中卡牌自动成为所选难度。
    /// 场景实体（DifficultyPanelBuilder 生成进 HomeScene）与运行时自建共用本组件：
    /// 几何参数（content/slotWidth/centerOffset）在编辑器里可直接改，
    /// 选中回调 onSelectionChanged 由 DifficultyManager 运行时绑定。
    /// </summary>
    public class DifficultyWheel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public RectTransform content;
        public float slotWidth = 324f;      // 卡宽 300 + 间距 24
        public float centerOffset = 600f;   // 第 0 张卡居中时的 content.x（padding 450 + cardW/2 150）
        [Range(1, 12)] public int cardCount = 6;

        /// <summary>居中卡牌变化回调（下标, 是否播放音效），由 DifficultyManager 绑定刷新选中视觉。</summary>
        public System.Action<int, bool> onSelectionChanged;

        private Canvas canvas;
        private bool snapping;
        private int targetIndex;
        private int currentIndex;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        private float TargetXForIndex(int index)
        {
            return centerOffset - index * slotWidth;
        }

        /// <summary>吸附到指定卡位并触发选中回调（可静默，供初始定位）。</summary>
        public void SnapTo(int index, bool playSound)
        {
            targetIndex = Mathf.Clamp(index, 0, cardCount - 1);
            snapping = true;
            ApplySelection(targetIndex, playSound);
        }

        private void ApplySelection(int index, bool playSound)
        {
            currentIndex = index;
            onSelectionChanged?.Invoke(index, playSound);
        }

        void Update()
        {
            if (!snapping || content == null) return;

            // 吸附动画（面板暂停时 timeScale=0，必须用 unscaled 时间）
            float targetX = TargetXForIndex(targetIndex);
            float cur = content.anchoredPosition.x;
            float next = Mathf.Lerp(cur, targetX, Mathf.Clamp01(12f * Time.unscaledDeltaTime));
            if (Mathf.Abs(next - targetX) < 0.5f)
            {
                next = targetX;
                snapping = false;
            }
            content.anchoredPosition = new Vector2(next, content.anchoredPosition.y);
        }

        void LateUpdate()
        {
            // 拖拽过程中实时高亮最近的居中卡牌（无音效，避免滚动时连响）
            if (snapping || content == null || onSelectionChanged == null) return;
            int nearest = GetNearestIndex();
            if (nearest != currentIndex)
                ApplySelection(nearest, false);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            snapping = false; // 用户接管滚轮，取消吸附动画
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (content == null) return;
            // 屏幕像素位移换算到画布单位（ScaleWithScreenSize 缩放因子）
            float scale = canvas != null && canvas.scaleFactor > 0.001f ? canvas.scaleFactor : 1f;
            float x = content.anchoredPosition.x + eventData.delta.x / scale;
            x = Mathf.Clamp(x, TargetXForIndex(cardCount - 1), TargetXForIndex(0));
            content.anchoredPosition = new Vector2(x, content.anchoredPosition.y);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            SnapTo(GetNearestIndex(), true);
        }

        public void OnScroll(PointerEventData eventData)
        {
            int dir = eventData.scrollDelta.y > 0.01f ? -1 : eventData.scrollDelta.y < -0.01f ? 1 : 0;
            if (dir == 0) return;
            SnapTo(currentIndex + dir, true);
        }

        private int GetNearestIndex()
        {
            int idx = Mathf.RoundToInt((centerOffset - content.anchoredPosition.x) / slotWidth);
            return Mathf.Clamp(idx, 0, cardCount - 1);
        }
    }
}
