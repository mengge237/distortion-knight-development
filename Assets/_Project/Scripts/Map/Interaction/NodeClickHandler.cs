using UnityEngine;
using UnityEngine.EventSystems;

namespace MutationChess.Map
{
    public class NodeClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private MapNode node;
        private GameManager gameManager;

        public void Initialize(MapNode mapNode)
        {
            node = mapNode;
            gameManager = FindObjectOfType<GameManager>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (node == null) return;

            if (!node.isReachable || node.isVisited)
            {
                return;
            }

            if (gameManager == null)
                gameManager = FindObjectOfType<GameManager>();

            if (gameManager != null)
            {
                gameManager.OnNodeClicked(node);
            }
            else
            {
                Debug.LogError("GameManager not found!");
            }
        }
    }
}
