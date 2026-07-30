using UnityEngine;
using UnityEngine.UI;

namespace MutationChess.UI
{
    /// <summary>
    /// 商店遗物布局组件，自动管理 relicContainer 的网格布局
    /// 默认 4 列、最多 2 行，可通过 Inspector 调整
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ShopRelicLayout : MonoBehaviour
    {
        [Header("网格布局")]
        [SerializeField] private int columns = 4;
        [SerializeField] private int maxRows = 2;
        [SerializeField] private Vector2 cellSize = new Vector2(240, 320);
        [SerializeField] private Vector2 spacing = new Vector2(25, 20);

        [Header("内边距")]
        [SerializeField] private int paddingLeft = 20;
        [SerializeField] private int paddingRight = 20;
        [SerializeField] private int paddingTop = 15;
        [SerializeField] private int paddingBottom = 15;

        private GridLayoutGroup gridLayout;
        private RectTransform rectTransform;

        void Awake()
        {
            ApplyLayout();
        }

        /// <summary>
        /// 刷新布局，强制重建 GridLayoutGroup
        /// </summary>
        public void RefreshLayout()
        {
            if (gridLayout == null)
                gridLayout = GetComponent<GridLayoutGroup>();
            if (gridLayout != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform ?? GetComponent<RectTransform>());
        }

        [ContextMenu("Apply Layout")]
        private void ApplyLayout()
        {
            gridLayout = GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
                gridLayout = gameObject.AddComponent<GridLayoutGroup>();

            rectTransform = GetComponent<RectTransform>();

            // 设置列数约束
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columns;

            // 设置单元格尺寸与间距
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = spacing;

            // 设置排列起点与轴向
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

            // 设置子对象对齐方式
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            // 设置内边距
            gridLayout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);

            // 计算并设置理想宽高
            float idealWidth = (cellSize.x + spacing.x) * columns - spacing.x + paddingLeft + paddingRight;
            float idealHeight = (cellSize.y + spacing.y) * maxRows - spacing.y + paddingTop + paddingBottom;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, idealWidth);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, idealHeight);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // 延迟调用以避免 OnValidate 中直接修改布局的警告
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    ApplyLayout();
            };
        }
#endif
    }
}
