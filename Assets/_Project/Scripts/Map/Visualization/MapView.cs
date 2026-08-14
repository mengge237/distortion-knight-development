using System.Collections.Generic;
using MutationChess.Core;
using UnityEngine;

namespace MutationChess.Map
{
    public class MapView : MonoBehaviour
    {
        [Header("节点颜色")]
        [SerializeField] private Color startColor = new Color(0.6f, 0.2f, 0.8f);
        [SerializeField] private Color normalMonsterColor = new Color(0.7f, 0.7f, 0.7f, 1f);
        [SerializeField] private Color eliteMonsterColor = new Color(1f, 0.4f, 0.1f, 1f);
        [SerializeField] private Color mysteryEventColor = new Color(1f, 0.85f, 0f, 1f);
        [SerializeField] private Color shopColor = new Color(0.2f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color treasureColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color restColor = new Color(0.2f, 0.7f, 1f, 1f);
        [SerializeField] private Color bossColor = new Color(1f, 0.1f, 0.1f, 1f);

        [Header("访问状态颜色")]
        [SerializeField] private Color visitedColor = new Color(0.3f, 0.3f, 0.35f, 1f);

        private MapGenerator mapGenerator;

        void Start()
        {
            mapGenerator = FindObjectOfType<MapGenerator>();
            if (mapGenerator == null)
            {
                GameLogger.LogError("[MapView] 未找到 MapGenerator");
                return;
            }

            Invoke(nameof(RefreshAllNodes), 0.2f);
        }

        Color GetNodeColor(NodeType type)
        {
            switch (type)
            {
                case NodeType.Start: return startColor;
                case NodeType.NormalMonster: return normalMonsterColor;
                case NodeType.EliteMonster: return eliteMonsterColor;
                case NodeType.MysteryEvent: return mysteryEventColor;
                case NodeType.Shop: return shopColor;
                case NodeType.Treasure: return treasureColor;
                case NodeType.Rest: return restColor;
                case NodeType.Boss: return bossColor;
                default: return Color.white;
            }
        }

        public void RefreshAllNodes()
        {

            if (mapGenerator == null)
            {
                mapGenerator = FindObjectOfType<MapGenerator>();
                if (mapGenerator == null)
                {
                    GameLogger.LogError("[MapView] 未找到 MapGenerator");
                    return;
                }
                GameLogger.Log("[MapView] 已找到 MapGenerator");
            }

            if (mapGenerator.AllLayers == null)
            {
                GameLogger.LogWarning("[MapView] MapGenerator.AllLayers 为空");
                return;
            }

            MapNode currentNode = mapGenerator.CurrentNode;

            foreach (var layer in mapGenerator.AllLayers)
            {
                foreach (var node in layer)
                {
                    UpdateNodeVisual(node, currentNode);
                }
            }

            mapGenerator.UpdateLineColors();
            mapGenerator.UpdateAllMapDisplays();
        }

        void UpdateNodeVisual(MapNode node, MapNode currentNode)
        {
            if (node == null || node.nodeObject == null) return;

            MeshRenderer mr = node.nodeObject.GetComponent<MeshRenderer>();
            if (mr == null) return;

            Color typeColor = GetNodeColor(node.nodeType);
            Color finalColor;

            if (node.isVisited)
            {
                finalColor = visitedColor;
            }
            else if (node.isReachable)
            {
                finalColor = typeColor;
                finalColor = Color.Lerp(finalColor, Color.white, 0.3f);
                finalColor.a = 1f;
            }
            else
            {
                finalColor = typeColor;
                finalColor.a = 1f;
            }

            mr.material.color = finalColor;
        }

        public void RefreshNode(MapNode node)
        {
            if (node == null || node.nodeObject == null) return;

            if (mapGenerator == null)
            {
                mapGenerator = FindObjectOfType<MapGenerator>();
                if (mapGenerator == null) return;
            }

            MapNode currentNode = mapGenerator.CurrentNode;
            UpdateNodeVisual(node, currentNode);
            mapGenerator.UpdateLineColors();
            mapGenerator.UpdateAllMapDisplays();
        }

        public void Initialize(MapData data)
        {
            mapGenerator = FindObjectOfType<MapGenerator>();
            if (mapGenerator != null)
            {
                Invoke(nameof(RefreshAllNodes), 0.2f);
            }
            else
            {
                GameLogger.LogError("[MapView] MapGenerator 未初始化");
            }
        }
    }
}


