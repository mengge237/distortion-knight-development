using UnityEngine;

namespace MutationChess.Map
{
    public static class LineRendererHelper
    {
        public static LineRenderer CreateLine(
            Vector3 start,
            Vector3 end,
            GameObject prefab = null,
            Transform parent = null,
            float width = 0.1f,
            Color? color = null)
        {
            GameObject lineObj;
            if (prefab != null)
                lineObj = Object.Instantiate(prefab, parent);
            else
                lineObj = new GameObject("Line", typeof(LineRenderer));

            LineRenderer lr = lineObj.GetComponent<LineRenderer>();
            if (lr == null) lr = lineObj.AddComponent<LineRenderer>();

            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startWidth = width;
            lr.endWidth = width;

            if (color.HasValue)
            {
                lr.startColor = color.Value;
                lr.endColor = color.Value;
            }

            return lr;
        }

        public static void UpdateLinePositions(LineRenderer lr, Vector3 start, Vector3 end)
        {
            if (lr == null) return;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }

        public static void SetLineColor(LineRenderer lr, Color color)
        {
            if (lr == null) return;
            lr.startColor = color;
            lr.endColor = color;
        }
    }
}