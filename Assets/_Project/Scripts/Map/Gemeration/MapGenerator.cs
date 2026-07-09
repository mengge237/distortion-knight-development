using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MutationChess.Map
{
    public class MapGenerator : MonoBehaviour
    {
        [Header("布局参数")]
        [SerializeField] private int rows = 8;
        [SerializeField] private int maxNodesPerRow = 4;
        [SerializeField] private float horizontalSpacing = 3.0f;
        [SerializeField] private float verticalSpacing = 4.0f;
        [SerializeField] private float nodeYOffset = -0.5f;
        [SerializeField] private float positionOffsetX = 0.8f;
        [SerializeField] private float positionOffsetY = 0.3f;
        [SerializeField] private int extraBranches = 2;

        [Header("节点预制体")]
        [SerializeField] private GameObject nodePrefab;

        [Header("连线材质")]
        [SerializeField] private Material lineMaterial;

        [Header("特殊层规则")]
        [SerializeField] private bool bossLayerHasRestBefore = true;
        [SerializeField] private int treasureLayerIndex = 6;

        private List<List<MapNode>> allLayers = new List<List<MapNode>>();
        private MapNode currentNode;
        private System.Random rand = new System.Random();

        private List<LineConnectionData> lineConnections = new List<LineConnectionData>();

        public MapNode CurrentNode => currentNode;
        public List<List<MapNode>> AllLayers => allLayers;
        public float NodeYOffset => nodeYOffset;

        public System.Action<MapNode> OnNodeReached;
        public System.Action<MapNode> OnNodeClickedAction;

        private Material defaultLineMaterial;
        private Transform linesParent;

        void Start()
        {
            if (Camera.main != null && Camera.main.GetComponent<PhysicsRaycaster>() == null)
                Camera.main.gameObject.AddComponent<PhysicsRaycaster>();

            defaultLineMaterial = CreateDefaultLineMaterial();
            GenerateMap();
        }

        private Material CreateDefaultLineMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            Material mat = new Material(shader);
            mat.color = Color.white;
            return mat;
        }

        void GenerateMap()
        {
            allLayers.Clear();
            lineConnections.Clear();

            if (linesParent != null) Destroy(linesParent.gameObject);
            linesParent = new GameObject("Lines").transform;
            linesParent.SetParent(transform);

            // 计算每层的节点数量（单起点单终点）
            List<int> layerCounts = new List<int>();
            int maxCount = maxNodesPerRow;

            for (int row = 0; row < rows; row++)
            {
                int count;
                if (row == 0)
                    count = 1;
                else if (row == rows - 1)
                    count = 1;
                else
                {
                    float progress = (float)row / (rows - 1);
                    float curve = Mathf.Sin(progress * Mathf.PI);
                    count = Mathf.RoundToInt(1 + curve * (maxCount - 1));
                    count = Mathf.Clamp(count, 1, maxCount);
                }
                layerCounts.Add(count);
            }

            for (int row = 0; row < rows; row++)
            {
                List<MapNode> layer = new List<MapNode>();
                int count = layerCounts[row];

                for (int col = 0; col < count; col++)
                {
                    NodeType type = GetNodeType(row, col, count);
                    Vector3 pos = CalculatePosition(row, col, count);
                    MapNode node = CreateNode(pos, type, row, col);
                    node.position = pos;
                    layer.Add(node);
                }
                allLayers.Add(layer);
            }

            BuildFullConnections();

            EnsureAllNodesReachableFromBottom();

            UpdateStartAndBossVisuals();

            InitializeStartNodes();

            DrawAllLines();

        }

        Vector3 CalculatePosition(int row, int col, int count)
        {
            float totalWidth = (count - 1) * horizontalSpacing;
            float x = col * horizontalSpacing - totalWidth / 2f;
            float y = row * verticalSpacing + nodeYOffset;

            float ox = Random.Range(-positionOffsetX, positionOffsetX) * 0.5f;
            float oy = Random.Range(-positionOffsetY, positionOffsetY) * 0.5f;
            if (col == 0 || col == count - 1)
                ox *= 0.3f;

            return new Vector3(x + ox, y + oy, 0);
        }

        NodeType GetNodeType(int row, int col, int count)
        {
            if (row == 0) return NodeType.Start;
            if (row == rows - 1) return NodeType.Boss;
            if (row == treasureLayerIndex && row > 0 && row < rows - 1)
                return NodeType.Treasure;
            if (row == rows - 2 && bossLayerHasRestBefore)
                return NodeType.Rest;

            float r = (float)rand.NextDouble();
            if (r < 0.40f) return NodeType.NormalMonster;
            if (r < 0.55f) return NodeType.EliteMonster;
            if (r < 0.75f) return NodeType.MysteryEvent;
            if (r < 0.90f) return NodeType.Shop;
            return NodeType.Treasure;
        }

        MapNode CreateNode(Vector3 pos, NodeType type, int row, int col)
        {
            GameObject obj = Instantiate(nodePrefab, pos, Quaternion.identity, transform);
            obj.name = $"Node_{type}_{row}_{col}";
            MapNode node = new MapNode(new Vector2Int(col, row), type);
            node.nodeObject = obj;
            node.position = pos;

            if (obj.GetComponent<Collider>() == null)
                obj.AddComponent<BoxCollider>();

            NodeClickHandler handler = obj.GetComponent<NodeClickHandler>();
            if (handler == null)
                handler = obj.AddComponent<NodeClickHandler>();
            handler.Initialize(node);

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Standard");
                Material mat = new Material(shader);
                mat.color = GetNodeTypeColor(type);
                mr.material = mat;
            }

            return node;
        }

        Color GetNodeTypeColor(NodeType type)
        {
            switch (type)
            {
                case NodeType.Start: return new Color(0.6f, 0.2f, 0.8f);
                case NodeType.NormalMonster: return new Color(0.7f, 0.7f, 0.7f);
                case NodeType.EliteMonster: return new Color(1f, 0.4f, 0.1f);
                case NodeType.MysteryEvent: return new Color(1f, 0.85f, 0f);
                case NodeType.Shop: return new Color(0.2f, 0.9f, 0.2f);
                case NodeType.Treasure: return new Color(1f, 0.8f, 0.2f);
                case NodeType.Rest: return new Color(0.2f, 0.7f, 1f);
                case NodeType.Boss: return new Color(1f, 0.1f, 0.1f);
                default: return Color.white;
            }
        }

        void BuildFullConnections()
        {
            foreach (var layer in allLayers)
                foreach (var node in layer)
                    node.ClearConnections();

            Vector2Int finalPoint = GetFinalNode();
            List<List<Vector2Int>> paths = GeneratePaths(finalPoint);

            foreach (List<Vector2Int> path in paths)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    MapNode fromNode = GetNode(path[i]);
                    MapNode toNode = GetNode(path[i + 1]);
                    if (fromNode != null && toNode != null)
                    {
                        fromNode.AddConnection(toNode);
                    }
                }
            }

            // 确保每个节点都有出边
            for (int row = 0; row < rows - 1; row++)
            {
                var currentLayer = allLayers[row];
                var nextLayer = allLayers[row + 1];

                foreach (var node in currentLayer)
                {
                    if (node.connections.Count == 0 && nextLayer.Count > 0)
                    {
                        var closest = nextLayer
                            .OrderBy(n => Vector3.Distance(n.position, node.position))
                            .First();
                        node.AddConnection(closest);
                    }
                }
            }

            // 确保每个节点都有入边
            for (int row = 1; row < rows; row++)
            {
                var currentLayer = allLayers[row];
                var prevLayer = allLayers[row - 1];

                foreach (var node in currentLayer)
                {
                    if (node.incoming.Count == 0 && prevLayer.Count > 0)
                    {
                        var closest = prevLayer
                            .OrderBy(n => Vector3.Distance(n.position, node.position))
                            .First();
                        closest.AddConnection(node);
                    }
                }
            }

            // 确保起点有出边
            if (allLayers.Count > 0 && allLayers[0].Count > 0)
            {
                var start = allLayers[0][0];
                if (start.connections.Count == 0 && allLayers.Count > 1)
                {
                    var nextLayer = allLayers[1];
                    if (nextLayer.Count > 0)
                    {
                        var closest = nextLayer
                            .OrderBy(n => Vector3.Distance(n.position, start.position))
                            .First();
                        start.AddConnection(closest);
                    }
                }
            }

            // 确保Boss有入边
            if (allLayers.Count > 0)
            {
                var bossLayer = allLayers[rows - 1];
                if (bossLayer.Count > 0)
                {
                    var boss = bossLayer[0];
                    if (boss.incoming.Count == 0 && rows > 1)
                    {
                        var prevLayer = allLayers[rows - 2];
                        if (prevLayer.Count > 0)
                        {
                            var closest = prevLayer
                                .OrderBy(n => Vector3.Distance(n.position, boss.position))
                                .First();
                            closest.AddConnection(boss);
                        }
                    }
                }
            }

            // 关键步骤：移除交叉连接（参考杀戮尖塔算法）
            RemoveCrossConnections();

        }

        /// <summary>
        /// 移除交叉连接 - 杀戮尖塔标准算法
        /// 检测并修复交叉：添加平行连接，然后随机移除交叉连接
        /// </summary>
        void RemoveCrossConnections()
        {
            for (int row = 0; row < rows - 1; row++)
            {
                for (int col = 0; col < allLayers[row].Count - 1; col++)
                {
                    MapNode leftNode = GetNode(new Vector2Int(col, row));
                    if (leftNode == null || leftNode.connections.Count == 0) continue;

                    MapNode rightNode = GetNode(new Vector2Int(col + 1, row));
                    if (rightNode == null || rightNode.connections.Count == 0) continue;

                    MapNode topLeft = GetNode(new Vector2Int(col, row + 1));
                    MapNode topRight = GetNode(new Vector2Int(col + 1, row + 1));
                    if (topLeft == null || topRight == null) continue;

                    // 交叉条件：左边节点连接到右上，右边节点连接到左上
                    bool hasCross = leftNode.connections.Contains(topRight) &&
                                   rightNode.connections.Contains(topLeft);

                    if (hasCross)
                    {
                        // 1. 添加平行连接（确保有替代路径）
                        if (!leftNode.connections.Contains(topLeft))
                            leftNode.AddConnection(topLeft);
                        if (!rightNode.connections.Contains(topRight))
                            rightNode.AddConnection(topRight);

                        float rnd = Random.Range(0f, 1f);
                        if (rnd < 0.2f)
                        {
                            // 20% 概率：移除两条交叉连接
                            leftNode.RemoveConnection(topRight);
                            rightNode.RemoveConnection(topLeft);
                        }
                        else if (rnd < 0.6f)
                        {
                            // 40% 概率：只移除第一条交叉连接（左->右上）
                            leftNode.RemoveConnection(topRight);
                        }
                        else
                        {
                            // 40% 概率：只移除第二条交叉连接（右->左上）
                            rightNode.RemoveConnection(topLeft);
                        }
                    }
                }
            }
        }

        void EnsureAllNodesReachableFromBottom()
        {
            HashSet<MapNode> reachableFromStart = new HashSet<MapNode>();
            Queue<MapNode> queue = new Queue<MapNode>();

            var startNode = allLayers[0][0];
            queue.Enqueue(startNode);
            reachableFromStart.Add(startNode);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                foreach (var conn in cur.connections)
                {
                    if (!reachableFromStart.Contains(conn))
                    {
                        reachableFromStart.Add(conn);
                        queue.Enqueue(conn);
                    }
                }
            }

            for (int row = rows - 1; row >= 1; row--)
            {
                var currentLayer = allLayers[row];
                var prevLayer = allLayers[row - 1];

                foreach (var node in currentLayer)
                {
                    if (!reachableFromStart.Contains(node))
                    {
                        var candidates = prevLayer.Where(n => reachableFromStart.Contains(n)).ToList();
                        if (candidates.Count > 0)
                        {
                            var closest = candidates
                                .OrderBy(n => Vector3.Distance(n.position, node.position))
                                .First();
                            closest.AddConnection(node);
                            reachableFromStart.Add(node);
                        }
                        else if (prevLayer.Count > 0)
                        {
                            var closest = prevLayer
                                .OrderBy(n => Vector3.Distance(n.position, node.position))
                                .First();
                            closest.AddConnection(node);
                            reachableFromStart.Add(node);
                        }
                    }
                }
            }

            int unreachableCount = 0;
            for (int row = 1; row < rows; row++)
            {
                foreach (var node in allLayers[row])
                {
                    if (!reachableFromStart.Contains(node))
                    {
                        unreachableCount++;
                        var prevLayer = allLayers[row - 1];
                        if (prevLayer.Count > 0)
                        {
                            var closest = prevLayer
                                .OrderBy(n => Vector3.Distance(n.position, node.position))
                                .First();
                            closest.AddConnection(node);
                            reachableFromStart.Add(node);
                        }
                    }
                }
            }

        }

        Vector2Int GetFinalNode()
        {
            return new Vector2Int(0, rows - 1);
        }

        List<List<Vector2Int>> GeneratePaths(Vector2Int finalNode)
        {
            var paths = new List<List<Vector2Int>>();
            Vector2Int startPoint = new Vector2Int(0, 0);
            int numOfBranches = 2 + extraBranches;

            int preBossRow = rows - 2;
            int preBossCount = allLayers[preBossRow].Count;

            if (preBossCount < numOfBranches)
                numOfBranches = preBossCount;

            if (numOfBranches < 1)
                numOfBranches = 1;

            List<int> availableCols = new List<int>();
            for (int i = 0; i < preBossCount; i++)
                availableCols.Add(i);

            ShuffleList(availableCols);

            // 存储已使用的节点，避免多条路径使用相同节点
            HashSet<Vector2Int> usedNodes = new HashSet<Vector2Int>();
            usedNodes.Add(startPoint);

            for (int i = 0; i < numOfBranches; i++)
            {
                int targetCol = availableCols[i % availableCols.Count];
                Vector2Int endNode = new Vector2Int(targetCol, preBossRow);
                List<Vector2Int> path = GeneratePath(startPoint, endNode, i, usedNodes);
                path.Add(finalNode);
                paths.Add(path);

                for (int j = 1; j < path.Count - 1; j++)
                {
                    usedNodes.Add(path[j]);
                }
            }

            if (paths.Count == 0)
            {
                Vector2Int endNode = new Vector2Int(0, preBossRow);
                List<Vector2Int> path = GeneratePath(startPoint, endNode, 0, usedNodes);
                path.Add(finalNode);
                paths.Add(path);
            }


            return paths;
        }

        void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        List<Vector2Int> GeneratePath(Vector2Int fromPoint, Vector2Int toPoint, int pathIndex, HashSet<Vector2Int> usedNodes)
        {
            int toRow = toPoint.y;
            int toCol = toPoint.x;
            int lastNodeCol = fromPoint.x;

            List<Vector2Int> path = new List<Vector2Int> { fromPoint };

            for (int row = 1; row <= toRow; row++)
            {
                int currentLayerCount = allLayers[row].Count;

                // 只允许从当前列移动 -1, 0, +1（杀戮尖塔标准）
                List<int> candidateCols = new List<int>();

                int forwardCol = lastNodeCol;
                if (forwardCol >= 0 && forwardCol < currentLayerCount)
                    candidateCols.Add(forwardCol);

                int leftCol = lastNodeCol - 1;
                if (leftCol >= 0 && leftCol < currentLayerCount)
                    candidateCols.Add(leftCol);

                int rightCol = lastNodeCol + 1;
                if (rightCol >= 0 && rightCol < currentLayerCount)
                    candidateCols.Add(rightCol);

                if (row == toRow)
                {
                    path.Add(toPoint);
                    lastNodeCol = toCol;
                    continue;
                }

                // 如果候选列太少（边缘情况），添加所有列
                if (candidateCols.Count == 0)
                {
                    for (int col = 0; col < currentLayerCount; col++)
                        candidateCols.Add(col);
                }

                // 优先选择未被使用的节点（避免路径重叠）
                var unusedCandidates = candidateCols
                    .Where(col => !usedNodes.Contains(new Vector2Int(col, row)))
                    .ToList();

                List<int> finalCandidates;
                if (unusedCandidates.Count > 0)
                {
                    finalCandidates = unusedCandidates;
                }
                else
                {
                    finalCandidates = candidateCols;
                }

                finalCandidates = finalCandidates
                    .OrderBy(col => Mathf.Abs(col - toCol))
                    .ThenBy(col => Mathf.Abs(col - lastNodeCol))
                    .ToList();

                int candidateIndex;
                if (finalCandidates.Count > 1)
                {
                    // 70%概率选择最优，30%随机
                    if (Random.Range(0f, 1f) < 0.7f)
                        candidateIndex = 0;
                    else
                        candidateIndex = Random.Range(0, finalCandidates.Count);
                }
                else
                {
                    candidateIndex = 0;
                }

                int candidateCol = finalCandidates[candidateIndex];
                Vector2Int nextPoint = new Vector2Int(candidateCol, row);

                path.Add(nextPoint);
                lastNodeCol = candidateCol;
            }

            if (path[path.Count - 1] != toPoint)
            {
                path[path.Count - 1] = toPoint;
            }

            return path;
        }

        MapNode GetNode(Vector2Int p)
        {
            if (p.y >= allLayers.Count) return null;
            if (p.x >= allLayers[p.y].Count) return null;
            return allLayers[p.y][p.x];
        }

        void UpdateStartAndBossVisuals()
        {
            if (allLayers.Count > 0 && allLayers[0].Count > 0)
                allLayers[0][0].nodeType = NodeType.Start;
            if (allLayers.Count > 0 && allLayers[allLayers.Count - 1].Count > 0)
                allLayers[allLayers.Count - 1][0].nodeType = NodeType.Boss;
        }

        void InitializeStartNodes()
        {
            if (allLayers.Count == 0 || allLayers[0].Count == 0) return;

            var firstStart = allLayers[0][0];
            firstStart.isVisited = true;
            firstStart.isReachable = true;
            currentNode = firstStart;

            foreach (var conn in firstStart.connections)
            {
                conn.isReachable = true;
            }

        }

        public void ConfirmReachNode(MapNode node)
        {
            if (node == null || node.isVisited) return;

            currentNode = node;
            node.isVisited = true;

            foreach (var layer in allLayers)
                foreach (var n in layer)
                    n.isReachable = false;

            foreach (var conn in currentNode.connections)
            {
                conn.isReachable = true;
            }

            UpdateLineColors();
            OnNodeReached?.Invoke(node);
        }

        public bool CanReachNode(MapNode node)
        {
            if (node == null || node.isVisited) return false;
            return node.isReachable;
        }

        void DrawAllLines()
        {
            ClearAllLines();
            lineConnections.Clear();

            int lineIndex = 0;
            foreach (var layer in allLayers)
            {
                foreach (var node in layer)
                {
                    if (node.nodeObject == null) continue;
                    if (node.connections.Count == 0) continue;

                    foreach (var target in node.connections)
                    {
                        if (target.nodeObject == null) continue;

                        Vector3 start = node.nodeObject.transform.position;
                        Vector3 end = target.nodeObject.transform.position;

                        LineRenderer lr = LineRendererHelper.CreateLine(
                            start: start,
                            end: end,
                            parent: linesParent,
                            width: 0.15f,
                            color: new Color(0.4f, 0.4f, 0.4f, 0.6f)
                        );

                        if (lineMaterial != null)
                            lr.material = lineMaterial;
                        else
                            lr.material = defaultLineMaterial;

                        lr.sortingOrder = 10;
                        lr.name = $"Line_{lineIndex}";

                        lineConnections.Add(new LineConnectionData
                        {
                            lineRenderer = lr,
                            fromNode = node,
                            toNode = target
                        });

                        lineIndex++;
                    }
                }
            }

        }

        void ClearAllLines()
        {
            if (linesParent != null)
            {
                for (int i = linesParent.childCount - 1; i >= 0; i--)
                {
                    Destroy(linesParent.GetChild(i).gameObject);
                }
            }
            lineConnections.Clear();
        }

        public void UpdateLineColors()
        {
            if (currentNode == null) return;

            foreach (var conn in lineConnections)
            {
                if (conn.lineRenderer == null) continue;

                Color color;
                if (conn.fromNode == currentNode && conn.toNode.isReachable)
                {
                    color = new Color(0.2f, 1f, 0.2f, 0.8f);
                }
                else if (conn.fromNode.isVisited && conn.toNode.isVisited)
                {
                    color = new Color(0.4f, 0.4f, 0.5f, 0.6f);
                }
                else
                {
                    color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                }

                LineRendererHelper.SetLineColor(conn.lineRenderer, color);
            }
        }

        int CountAllNodes()
        {
            int count = 0;
            foreach (var layer in allLayers)
                count += layer.Count;
            return count;
        }

        int CountAllConnections()
        {
            int count = 0;
            foreach (var layer in allLayers)
                foreach (var node in layer)
                    count += node.connections.Count;
            return count;
        }

        public Vector3 GetNodeWorldPosition(MapNode node)
        {
            if (node == null || node.nodeObject == null) return Vector3.zero;
            return node.nodeObject.transform.position;
        }

        public List<MapNode> GetReachableNodes()
        {
            List<MapNode> result = new List<MapNode>();
            if (currentNode == null) return result;

            foreach (var conn in currentNode.connections)
            {
                if (!conn.isVisited)
                    result.Add(conn);
            }
            return result;
        }

        private class LineConnectionData
        {
            public LineRenderer lineRenderer;
            public MapNode fromNode;
            public MapNode toNode;
        }
    }

    public static class ListExtensions
    {
        private static System.Random rng = new System.Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
