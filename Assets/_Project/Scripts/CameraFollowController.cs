using UnityEngine;
using DG.Tweening;

namespace MutationChess.Map
{
    public class CameraFollowController : MonoBehaviour
    {
        [Header("跟随设置")]
        [SerializeField] private float moveDuration = 0.6f;
        [SerializeField] private Ease moveEase = Ease.InOutQuad;
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 8, -8);
        [SerializeField] private Vector3 lookOffset = new Vector3(0, 0, 0);

        [Header("旋转设置")]
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private float rotationDuration = 0.5f;

        private Camera mainCamera;
        private bool isMoving = false;
        private Tween moveTween;
        private Tween rotationTween;

        public bool IsMoving => isMoving;
        public MapNode CurrentTargetNode { get; private set; }

        void Start()
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null) mainCamera = Camera.main;
        }

        public void MoveToNode(MapNode targetNode, System.Action onComplete = null)
        {
            if (targetNode == null || targetNode.nodeObject == null) return;
            if (isMoving) return;

            CurrentTargetNode = targetNode;
            isMoving = true;

            Vector3 targetPos = targetNode.position + cameraOffset;
            Vector3 lookTarget = targetNode.position + lookOffset;
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - targetPos);

            moveTween = mainCamera.transform
                .DOMove(targetPos, moveDuration)
                .SetEase(moveEase);

            if (enableRotation)
            {
                rotationTween = mainCamera.transform
                    .DORotateQuaternion(targetRot, rotationDuration)
                    .SetEase(Ease.InOutQuad);
            }

            moveTween.OnComplete(() => {
                isMoving = false;
                onComplete?.Invoke();
            });
        }

        public void TeleportToNode(MapNode targetNode)
        {
            if (targetNode == null || targetNode.nodeObject == null) return;

            moveTween?.Kill();
            rotationTween?.Kill();
            isMoving = false;

            Vector3 targetPos = targetNode.position + cameraOffset;
            Vector3 lookTarget = targetNode.position + lookOffset;
            Quaternion targetRot = Quaternion.LookRotation(lookTarget - targetPos);

            mainCamera.transform.position = targetPos;
            if (enableRotation)
                mainCamera.transform.rotation = targetRot;

            CurrentTargetNode = targetNode;
        }

        void OnDestroy()
        {
            moveTween?.Kill();
            rotationTween?.Kill();
        }
    }
}
