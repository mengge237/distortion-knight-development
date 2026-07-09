using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Map
{
    [CreateAssetMenu(fileName = "MapConfig", menuName = "MutationChess/Map Config")]
    public class MapConfig : ScriptableObject
    {
        [Header("层数")]
        public int rows = 8;

        [Header("节点数量范围")]
        public Vector2Int startNodesRange = new Vector2Int(2, 3);
        public Vector2Int midNodesRange = new Vector2Int(2, 4);
        public Vector2Int extraPathsRange = new Vector2Int(0, 2);

        [Header("间距")]
        public float horizontalSpacing = 3.0f;
        public float verticalSpacing = 4.0f;
        public float nodeYOffset = -0.5f;

        [Header("随机偏移")]
        public float positionOffsetX = 0.8f;
        public float positionOffsetY = 0.3f;

        [Header("特殊层")]
        public int treasureLayerIndex = 6;
        public bool bossLayerHasRestBefore = true;

        [Header("节点蓝图")]
        public List<NodeBlueprint> nodeBlueprints = new List<NodeBlueprint>();
    }
}