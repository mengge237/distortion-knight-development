using DG.Tweening;
using MutationChess.Battle;
using MutationChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MutationChess.UI
{
    public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("UI")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image borderImage;
        [SerializeField] private Image cardArt;
        [SerializeField] private TMP_Text cardNameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text descriptionText;

        [Header("")]
        [SerializeField] private Image costIcon;

        [Header("Hover")]
        [SerializeField] private float hoverScale = 1.08f;
        [SerializeField] private float hoverFloatAmount = 15f;
        [SerializeField] private float hoverDuration = 0.15f;
        [SerializeField] private float dragScale = 1.3f;
        [SerializeField] private float dragFloatAmount = 50f;
        [SerializeField] private float dragThreshold = 50f;

        [Header("")]
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

        [Header("")]
        [SerializeField] private GameObject bigCardPrefab;
        [SerializeField] private Transform bigCardParent;
        [SerializeField] private float longPressDelay = 0.5f;

        private Card cardData;
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private Vector3 originalPosition;
        private bool isInteractable = true;
        private bool isHovering = false;
        private bool isDragging = false;
        private bool isBigCardShowing = false;
        private Tween hoverTween;
        private float pressTimer = 0f;
        private bool isPointerDown = false;
        private GameObject bigCardInstance;
        private GameObject backgroundMask;
        private Vector3 dragStartPosition;
        private bool hasExceededThreshold = false;

        public System.Action<Card> OnCardClicked;
        public System.Action<Card> OnCardDragStart;
        public System.Action<Card> OnCardDragEnd;
        public System.Action<Card> OnCardPlayed;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                originalScale = rectTransform.localScale;
                originalPosition = rectTransform.anchoredPosition3D;
            }
            else
            {
                originalScale = Vector3.one;
                originalPosition = Vector3.zero;
            }
        }

        void OnDestroy()
        {
            // 终止所有关联的 DOTween 动画，避免销毁后仍访问 RectTransform
            hoverTween?.Kill();
            if (rectTransform != null) DOTween.Kill(rectTransform);
            if (bigCardInstance != null) DOTween.Kill(bigCardInstance.transform);
            if (backgroundMask != null) DOTween.Kill(backgroundMask.transform);
        }

        void Update()
        {
            if (isPointerDown && !isDragging && isInteractable && cardData != null)
            {
                pressTimer += Time.deltaTime;
                if (pressTimer >= longPressDelay && !isBigCardShowing)
                {
                    ShowBigCard();
                }
            }
        }

        public void Initialize(Card card)
        {
            cardData = card;

            if (cardNameText == null)
                GameLogger.LogError("CardUI: cardNameText ");
            if (costText == null)
                GameLogger.LogError("CardUI: costText ");
            if (descriptionText == null)
                GameLogger.LogError("CardUI: descriptionText ");
            if (borderImage == null)
                GameLogger.LogError("CardUI: borderImage ");
            if (cardBackground == null)
                GameLogger.LogError("CardUI: cardBackground ");
            if (cardArt == null)
                GameLogger.LogError("CardUI: cardArt ");
            if (costIcon == null)
                GameLogger.LogError("CardUI: costIcon ");

            UpdateUI();

            if (rectTransform != null)
            {
                rectTransform.localScale = originalScale;
                rectTransform.anchoredPosition3D = originalPosition;
            }
        }

        public void UpdateUI()
        {
            if (cardData == null) return;

            if (cardNameText != null)
                cardNameText.text = cardData.cardName;

            if (costText != null)
                costText.text = cardData.cost.ToString();

            if (descriptionText != null)
                descriptionText.text = cardData.GetDescription();

            if (borderImage != null)
            {
                Color rarityColor = CardVisualConfig.GetRarityColor(cardData.rarity);
                rarityColor.a = 1f;
                borderImage.color = rarityColor;
                borderImage.enabled = true;
            }

            if (cardBackground != null)
            {
                cardBackground.enabled = true;
            }

            if (costIcon != null)
            {
                costIcon.enabled = true;
            }

            if (cardArt != null)
            {
                if (cardData.cardArt != null)
                {
                    cardArt.sprite = cardData.cardArt;
                    cardArt.enabled = true;
                }
                else
                {
                    cardArt.enabled = false;
                }
            }

            SetInteractable(isInteractable);
        }

        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;

            if (borderImage != null && cardData != null)
            {
                Color color = interactable ? CardVisualConfig.GetRarityColor(cardData.rarity) : disabledColor;
                color.a = 1f;
                borderImage.color = color;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (cardData == null || !isInteractable || isDragging) return;
            isHovering = true;

            hoverTween?.Kill();

            Vector3 targetScale = originalScale * hoverScale;
            Vector3 targetPos = originalPosition + Vector3.up * hoverFloatAmount;

            hoverTween = DOTween.Sequence()
                .Join(rectTransform.DOScale(targetScale, hoverDuration).SetEase(Ease.OutQuad))
                .Join(rectTransform.DOAnchorPos3D(targetPos, hoverDuration).SetEase(Ease.OutQuad))
                .OnStart(() => {
                    transform.SetAsLastSibling();
                })
                .Play();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (cardData == null || isDragging) return;
            isHovering = false;

            hoverTween?.Kill();

            hoverTween = DOTween.Sequence()
                .Join(rectTransform.DOScale(originalScale, hoverDuration).SetEase(Ease.OutQuad))
                .Join(rectTransform.DOAnchorPos3D(originalPosition, hoverDuration).SetEase(Ease.OutQuad))
                .Play();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (cardData == null || !isInteractable) return;
            isPointerDown = true;
            pressTimer = 0f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
            pressTimer = 0f;

            if (isBigCardShowing)
            {
                HideBigCard();
                return;
            }

            if (!isDragging && !isBigCardShowing)
            {
                OnCardClicked?.Invoke(cardData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (cardData == null || !isInteractable) return;

            if (HandManager.Instance != null && cardData.cost > HandManager.Instance.GetCurrentEnergy())
            {
                return;
            }

            isDragging = true;
            hasExceededThreshold = false;
            isPointerDown = false;
            pressTimer = 0f;

            if (isBigCardShowing)
            {
                HideBigCard();
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);
            dragStartPosition = localPoint;

            rectTransform.DOScale(originalScale * dragScale, 0.1f).SetEase(Ease.OutQuad);
            rectTransform.DOAnchorPosY(originalPosition.y + dragFloatAmount, 0.1f).SetEase(Ease.OutQuad);

            transform.SetAsLastSibling();

            OnCardDragStart?.Invoke(cardData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            Vector2 mousePos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out mousePos);

            rectTransform.anchoredPosition = mousePos;

            float dragDistance = Vector2.Distance(mousePos, dragStartPosition);

            if (dragDistance > dragThreshold && !hasExceededThreshold)
            {
                hasExceededThreshold = true;
                if (cardData != null)
                {
                    OnCardPlayed?.Invoke(cardData);
                }
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            isDragging = false;

            if (!hasExceededThreshold)
            {
                rectTransform.DOScale(originalScale, 0.15f).SetEase(Ease.OutQuad);
                rectTransform.DOAnchorPos3D(originalPosition, 0.15f).SetEase(Ease.OutQuad);
            }

            OnCardDragEnd?.Invoke(cardData);
        }

        void ShowBigCard()
        {
            if (isBigCardShowing || cardData == null || cardData.cardArt == null) return;
            isBigCardShowing = true;

            Canvas topCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (topCanvas == null) return;

            backgroundMask = new GameObject("BigCardMask");
            backgroundMask.transform.SetParent(topCanvas.transform, false);
            RectTransform maskRt = backgroundMask.AddComponent<RectTransform>();
            maskRt.anchorMin = Vector2.zero;
            maskRt.anchorMax = Vector2.one;
            maskRt.offsetMin = Vector2.zero;
            maskRt.offsetMax = Vector2.zero;
            maskRt.SetAsFirstSibling();

            Image maskImage = backgroundMask.AddComponent<Image>();
            maskImage.color = new Color(0, 0, 0, 0.7f);
            maskImage.raycastTarget = true;

            Button maskButton = backgroundMask.AddComponent<Button>();
            maskButton.onClick.AddListener(HideBigCard);

            GameObject previewObj;
            if (bigCardPrefab != null)
            {
                previewObj = Instantiate(bigCardPrefab, topCanvas.transform);
                Image previewImage = previewObj.GetComponent<Image>();
                if (previewImage != null)
                {
                    previewImage.sprite = cardData.cardArt;
                    previewImage.preserveAspect = true;
                    previewImage.raycastTarget = false;
                }
            }
            else
            {
                previewObj = new GameObject("BigCardArt");
                previewObj.transform.SetParent(topCanvas.transform, false);
                Image previewImage = previewObj.AddComponent<Image>();
                previewImage.sprite = cardData.cardArt;
                previewImage.preserveAspect = true;
                previewImage.raycastTarget = false;
            }

            bigCardInstance = previewObj;
            RectTransform previewRt = previewObj.GetComponent<RectTransform>();
            if (previewRt != null)
            {
                previewRt.anchorMin = new Vector2(0.5f, 0.5f);
                previewRt.anchorMax = new Vector2(0.5f, 0.5f);
                previewRt.pivot = new Vector2(0.5f, 0.5f);
                previewRt.anchoredPosition = Vector2.zero;
                previewRt.sizeDelta = new Vector2(500, 700);
                previewRt.SetAsLastSibling();
            }

            previewObj.transform.localScale = Vector3.zero;
            previewObj.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        void HideBigCard()
        {
            if (!isBigCardShowing) return;
            isBigCardShowing = false;

            if (backgroundMask != null)
            {
                GameObject maskToDestroy = backgroundMask;
                backgroundMask = null;
                Destroy(maskToDestroy);
            }

            if (bigCardInstance != null)
            {
                GameObject objToDestroy = bigCardInstance;
                bigCardInstance = null;

                if (objToDestroy != null && objToDestroy.gameObject != null)
                {
                    objToDestroy.transform.DOScale(Vector3.zero, 0.15f)
                        .SetEase(Ease.InQuad)
                        .OnComplete(() => {
                            if (objToDestroy != null && objToDestroy.gameObject != null)
                            {
                                Destroy(objToDestroy);
                            }
                        });
                }
            }
        }

        private void ResetCardState()
        {
            if (borderImage != null)
            {
                borderImage.enabled = true;
                if (cardData != null)
                {
                    Color color = CardVisualConfig.GetRarityColor(cardData.rarity);
                    color.a = 1f;
                    borderImage.color = color;
                }
            }
            if (cardBackground != null)
            {
                cardBackground.enabled = true;
            }
            if (cardArt != null)
            {
                cardArt.enabled = true;
                cardArt.color = new Color(1f, 1f, 1f, 1f);
            }
            if (costIcon != null)
            {
                costIcon.enabled = true;
            }
            if (rectTransform != null)
            {
                rectTransform.localScale = originalScale;
                rectTransform.anchoredPosition3D = originalPosition;
            }
            isHovering = false;
            isDragging = false;
            isPointerDown = false;
            pressTimer = 0f;
            hasExceededThreshold = false;
            isBigCardShowing = false;
        }

        public void PlayDrawAnimation(Vector3 startPos, float delay = 0f)
        {
            if (rectTransform == null) return;

            rectTransform.anchoredPosition3D = startPos;
            rectTransform.localScale = Vector3.zero;

            DOTween.Sequence()
                .AppendInterval(delay)
                .Join(rectTransform.DOAnchorPos3D(originalPosition, 0.4f).SetEase(Ease.OutBack))
                .Join(rectTransform.DOScale(originalScale, 0.35f).SetEase(Ease.OutBack))
                .Play();
        }

        public void PlayDiscardAnimation(Vector3 targetPos, System.Action onComplete = null)
        {
            if (rectTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            DOTween.Sequence()
                .Append(rectTransform.DOScale(originalScale * 1.05f, 0.08f).SetEase(Ease.OutQuad))
                .Append(rectTransform.DOScale(originalScale * 0.9f, 0.08f).SetEase(Ease.InQuad))
                .Join(rectTransform.DOAnchorPos3D(originalPosition + Vector3.up * 10f, 0.08f).SetEase(Ease.OutQuad))
                .AppendInterval(0.05f)
                .Join(rectTransform.DOAnchorPos3D(targetPos, 0.35f).SetEase(Ease.InQuad))
                .Join(rectTransform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InQuad))
                .OnComplete(() => {
                    onComplete?.Invoke();
                })
                .Play();
        }

        public void ResetCard()
        {
            ResetCardState();
            if (isBigCardShowing)
            {
                HideBigCard();
            }
        }

        public Card GetCardData() => cardData;
        public int GetCost() => cardData?.cost ?? 0;
        public bool IsInteractable() => isInteractable;
        public bool IsHovering() => isHovering;
        public bool IsDragging() => isDragging;
        public RectTransform GetRectTransform() => rectTransform;
        public Vector3 GetOriginalPosition() => originalPosition;
        public void SetOriginalPosition(Vector3 pos) { originalPosition = pos; }
    }
}

