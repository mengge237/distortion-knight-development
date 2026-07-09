using UnityEngine;

namespace MutationChess.Map
{
    [CreateAssetMenu(fileName = "NodeBlueprint", menuName = "MutationChess/Node Blueprint")]
    public class NodeBlueprint : ScriptableObject
    {
        public NodeType nodeType;
        public Color color = Color.white;
        public string displayName = "节点";
        public GameObject prefab;
        public Material material;
    }
}