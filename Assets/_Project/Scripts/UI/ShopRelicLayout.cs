using UnityEngine;
using UnityEngine.UI;

namespace MutationChess.UI
{
    /// <summary>
    ///
    /// ?? relicContainer 4 ?? ?? 2 
    ///
    /// </summary>
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ShopRelicLayout : MonoBehaviour
    {
        [Header("")]
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
        ///
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

            //
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columns;

            //
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = spacing;

            //
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;

            //
            gridLayout.childAlignment = TextAnchor.UpperCenter;

            // 
            gridLayout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);

            //
            float idealWidth = (cellSize.x + spacing.x) * columns - spacing.x + paddingLeft + paddingRight;
            float idealHeight = (cellSize.y + spacing.y) * maxRows - spacing.y + paddingTop + paddingBottom;

            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, idealWidth);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, idealHeight);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            //
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    ApplyLayout();
            };
        }
#endif
    }
}
