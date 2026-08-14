using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace MutationChess.Map
{
    public class PlayerController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float moveDuration = 0.6f;
        [SerializeField] private float jumpPower = 2.5f;
        [SerializeField] private int jumpCount = 1;
        [SerializeField] private Ease moveEase = Ease.OutQuad;
        [SerializeField] private float rotationDuration = 0.15f;

        private bool isMoving = false;
        private Tween moveTween;
        private Tween rotationTween;

        public bool IsMoving => isMoving;

        public void MoveToNode(Vector3 targetPosition, System.Action onComplete)
        {
            if (isMoving) return;
            StartCoroutine(MoveRoutine(targetPosition, onComplete));
        }

        IEnumerator MoveRoutine(Vector3 target, System.Action callback)
        {
            isMoving = true;

            Vector3 direction = (target - transform.position).normalized;
            if (direction != Vector3.zero && direction != Vector3.forward)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                rotationTween?.Kill();
                rotationTween = transform.DORotateQuaternion(targetRotation, rotationDuration).SetEase(Ease.OutQuad);
            }

            // 使用 DOTween 跳跃移动（更丝滑）
            moveTween = transform
                .DOJump(target, jumpPower, jumpCount, moveDuration)
                .SetEase(moveEase)
                .OnComplete(() => {
                    isMoving = false;
                    callback?.Invoke();
                });

            yield return moveTween.WaitForCompletion();
        }

        public void TeleportToNode(Vector3 position)
        {
            moveTween?.Kill();
            rotationTween?.Kill();
            transform.position = position;
            isMoving = false;
        }

        /// <summary>
        /// 停止移动
        /// </summary>
        public void StopMoving()
        {
            moveTween?.Kill();
            rotationTween?.Kill();
            isMoving = false;
        }

        void OnDestroy()
        {
            moveTween?.Kill();
            rotationTween?.Kill();
        }
    }
}
