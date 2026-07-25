using UnityEngine;

namespace MutationChess.Map
{
    [CreateAssetMenu(fileName = "NodeBlueprint", menuName = "MutationChess/Node Blueprint")]
    public class NodeBlueprint : ScriptableObject
    {
        [Header("节点类型")]
        public NodeType nodeType;

        [Header("显示设置")]
        public string displayName = "节点";
        public Color color = Color.white;

        [Header("节点模型")]
        public GameObject prefab;           // 节点主体预制体

        [Header("材质")]
        public Material material;           // 节点材质

        [Header("地图显示（可选）")]
        public GameObject mapPrefab;        // 地图显示预制体
        public Texture2D mapTexture;        // 地图贴图（当没有 mapPrefab 时使用）
    }
}
