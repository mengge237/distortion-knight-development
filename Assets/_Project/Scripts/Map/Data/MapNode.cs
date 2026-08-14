using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Map
{
    [System.Serializable]
    public partial class MapNode
    {
        public Vector2Int point;              // 网格坐标 (col, row)
        public NodeType nodeType;
        public bool isVisited = false;
        public bool isReachable = false;

        [System.NonSerialized] public List<MapNode> connections = new List<MapNode>(); // 出边连接
        [System.NonSerialized] public List<MapNode> incoming = new List<MapNode>();    // 入边连接

        // 可视化引用（运行时赋值，不序列化）
        [System.NonSerialized] public GameObject nodeObject;
        [System.NonSerialized] public GameObject mapDisplayObject;    // 地图显示子物体
        [System.NonSerialized] public Vector3 position;

        public MapNode(Vector2Int pos, NodeType type)
        {
            point = pos;
            nodeType = type;
            connections = new List<MapNode>();
            incoming = new List<MapNode>();
        }

        public void AddConnection(MapNode target)
        {
            if (!connections.Contains(target))
            {
                connections.Add(target);
                if (!target.incoming.Contains(this))
                    target.incoming.Add(this);
            }
        }

        public void RemoveConnection(MapNode target)
        {
            if (connections.Contains(target))
            {
                connections.Remove(target);
                if (target.incoming.Contains(this))
                    target.incoming.Remove(this);
            }
        }

        public void ClearConnections()
        {
            foreach (var conn in connections)
            {
                if (conn.incoming.Contains(this))
                    conn.incoming.Remove(this);
            }
            connections.Clear();
        }

        public bool HasConnectionTo(MapNode target)
        {
            return connections.Contains(target);
        }

        public bool HasNoConnections()
        {
            return connections.Count == 0 && incoming.Count == 0;
        }
    }
}
