using DG.Tweening;
using MutationChess.Battle;
using MutationChess.Core;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MutationChess.UI
{
    public class HandManager : MonoBehaviour
    {
        public static HandManager Instance { get; private set; }

        [Header("=== UI引用 ===")]
        [SerializeField] private GameObject handPanel;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private RectTransform cardsContainer;

        [Header("=== 牌堆信息栏 ===")]
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text drawPileCountText;
        [SerializeField] private TMP_Text discardPileCountText;

        [Header("=== Fan Layout ===")]
        [SerializeField] private float fanRadius = 2000f;
        [SerializeField] private float maxFanAngle = 35f;
        [SerializeField][Range(0f, 1f)] private float fanRotationStrength = 1f;

        [Header("=== 卡组配置 ===")]
        [SerializeField] private DeckData deckData;

        [Header("=== 手牌设置 ===")]
        [SerializeField] private int maxHandSize = 10;
        [SerializeField] private int cardsPerTurn = 5;
        [SerializeField] private int startingHandSize = 5;

        [Header("=== 动画设置 ===")]
        [SerializeField] private float drawAnimationDelay = 0.05f;

        [Header("=== 能量系统 ===")]
        [SerializeField] private int maxEnergy = 3;
        private int currentEnergy = 3;

        [Header("=== 牌堆（运行时） ===")]
        [SerializeField] private List<Card> drawPile = new List<Card>();
        [SerializeField] private List<Card> handCards = new List<Card>();
        [SerializeField] private List<Card> discardPile = new List<Card>();

        private List<CardUI> cardUIs = new List<CardUI>();
        private BattleManager battleManager;
        private bool isFirstTurn = true;
        private bool isAnimating = false;

        public System.Action<int> OnEnergyChanged;
        public System.Action<Card> OnCardPlayed;
        public System.Action OnHandUpdated;

        void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        void Start()
        {
            currentEnergy = maxEnergy;
            UpdateEnergyUI();
            UpdatePileCountUI();
        }

        public void StartBattle()
        {
            battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                Debug.LogError("HandManager: 无法找到 BattleManager！");
            }

            isFirstTurn = true;
            isAnimating = false;

            InitializeDeckFromConfig();
            ClearHand();

            ShuffleDrawPile();

            StartCoroutine(DrawCardsRoutine(startingHandSize, GetDrawPilePosition()));

            currentEnergy = maxEnergy;
            OnEnergyChanged?.Invoke(currentEnergy);
            UpdateEnergyUI();
            UpdatePileCountUI();
        }

        public void EndBattle()
        {
            foreach (var card in handCards)
                discardPile.Add(card);
            handCards.Clear();

            foreach (var cardUI in cardUIs)
            {
                if (cardUI != null) Destroy(cardUI.gameObject);
            }
            cardUIs.Clear();

            RefreshAllUI();

            battleManager = null;
            isFirstTurn = true;
            isAnimating = false;
        }

        public void OnNewTurn()
        {
            if (isFirstTurn)
            {
                isFirstTurn = false;
                currentEnergy = maxEnergy;
                OnEnergyChanged?.Invoke(currentEnergy);
                UpdateEnergyUI();
                return;
            }

            currentEnergy = maxEnergy;
            OnEnergyChanged?.Invoke(currentEnergy);
            UpdateEnergyUI();

            if (drawPile.Count == 0 && discardPile.Count > 0)
            {
                ReshuffleDiscard();
            }

            StartCoroutine(DrawCardsRoutine(cardsPerTurn, GetDrawPilePosition()));
        }

        public void OnEndTurn()
        {
            foreach (var card in handCards)
                discardPile.Add(card);

            handCards.Clear();

            if (cardUIs.Count > 0)
            {
                Vector3 discardPos = GetDiscardPilePosition();
                int cardCount = cardUIs.Count;

                for (int i = 0; i < cardCount; i++)
                {
                    CardUI cardUI = cardUIs[i];
                    if (cardUI != null)
                    {
                        float delay = i * 0.04f;
                        Vector3 targetPos = discardPos + new Vector3(Random.Range(-40f, 40f), Random.Range(-20f, 20f), 0);
                        cardUI.PlayDiscardAnimation(targetPos, () => {
                            Destroy(cardUI.gameObject);
                        });
                    }
                }

                cardUIs.Clear();
            }

            RefreshAllUI();

            DOVirtual.DelayedCall(0.3f, () => {
                UpdatePileCountUI();
            });
        }

        void InitializeDeckFromConfig()
        {
            drawPile.Clear();
            discardPile.Clear();

            if (deckData == null)
            {
                CreateDefaultDeck();
                return;
            }

            drawPile = deckData.GetDeckCopy();
        }

        void CreateDefaultDeck()
        {
            Card attack = CardData.CreateCard(CardName.攻击);
            Card defend = CardData.CreateCard(CardName.防御);
            Card bash = CardData.CreateCard(CardName.痛击);

            if (attack != null)
            {
                for (int i = 0; i < 5; i++)
                    drawPile.Add(attack);
            }

            if (defend != null)
            {
                for (int i = 0; i < 4; i++)
                    drawPile.Add(defend);
            }

            if (bash != null)
                drawPile.Add(bash);
        }

        void ShuffleDrawPile()
        {
            for (int i = drawPile.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Card temp = drawPile[i];
                drawPile[i] = drawPile[j];
                drawPile[j] = temp;
            }
        }

        void ReshuffleDiscard()
        {
            if (discardPile.Count == 0)
            {
                return;
            }

            drawPile.AddRange(discardPile);
            discardPile.Clear();

            ShuffleDrawPile();

            UpdatePileCountUI();
        }

        Vector3 GetDrawPilePosition()
        {
            if (drawPileCountText != null)
            {
                RectTransform textRect = drawPileCountText.rectTransform;
                Vector3 pos = textRect.anchoredPosition;
                return pos + new Vector3(-80f, 0, 0);
            }
            return new Vector3(-400f, -150f, 0);
        }

        Vector3 GetDiscardPilePosition()
        {
            if (discardPileCountText != null)
            {
                RectTransform textRect = discardPileCountText.rectTransform;
                Vector3 pos = textRect.anchoredPosition;
                return pos + new Vector3(80f, 0, 0);
            }
            return new Vector3(400f, -150f, 0);
        }

        public void DrawCards(int count)
        {
            StartCoroutine(DrawCardsRoutine(count, GetDrawPilePosition()));
        }

        public void DrawCard() => DrawCards(1);

        IEnumerator DrawCardsRoutine(int count, Vector3 drawPilePos)
        {
            while (isAnimating)
                yield return null;

            isAnimating = true;

            int drawn = 0;
            List<Card> drawnCards = new List<Card>();

            for (int i = 0; i < count; i++)
            {
                if (handCards.Count >= maxHandSize)
                {
                    break;
                }

                if (drawPile.Count == 0)
                {
                    if (discardPile.Count > 0)
                    {
                        ReshuffleDiscard();
                    }
                    else
                    {
                        break;
                    }
                }

                Card drawnCard = drawPile[0];
                drawPile.RemoveAt(0);
                handCards.Add(drawnCard);
                drawnCards.Add(drawnCard);
                drawn++;

                if (drawn % 2 == 0 || drawn == count)
                {
                    UpdatePileCountUI();
                }

                yield return new WaitForSeconds(0.02f);
            }

            if (drawn > 0)
            {
                UpdateHandUIWithAnimation(drawPilePos, drawnCards);
                OnHandUpdated?.Invoke();
            }

            UpdatePileCountUI();
            isAnimating = false;
        }

        private Vector2 CalculateFanPosition(int index, int totalCount)
        {
            if (totalCount <= 1) return Vector2.zero;
            float t = (float)index / (totalCount - 1);
            float angle = (t - 0.5f) * maxFanAngle * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * fanRadius;
            float y = -(1f - Mathf.Cos(angle)) * fanRadius;
            return new Vector2(x, y);
        }

        void UpdateHandUIWithAnimation(Vector3 drawPilePos, List<Card> newCards)
        {
            CreateCardUIs(drawPilePos, newCards);
        }

        private void CreateCardUIs(Vector3 drawPilePos, List<Card> newCards = null)
        {
            foreach (var cardUI in cardUIs)
            {
                if (cardUI != null) Destroy(cardUI.gameObject);
            }
            cardUIs.Clear();

            if (handCards.Count == 0)
            {
                UpdateEnergyUI();
                UpdatePileCountUI();
                return;
            }

            if (cardPrefab == null || cardsContainer == null)
            {
                Debug.LogWarning("CardPrefab 和 CardsContainer 为空！");
                return;
            }

            List<Vector2> targetPositions = new List<Vector2>();
            List<float> targetRotations = new List<float>();
            for (int i = 0; i < handCards.Count; i++)
            {
                Vector2 fanPos = CalculateFanPosition(i, handCards.Count);
                targetPositions.Add(fanPos);
                float rot = -(i - handCards.Count / 2f + 0.5f) / (handCards.Count / 2f) * maxFanAngle * 0.5f * fanRotationStrength;
                targetRotations.Add(rot);
            }

            int newCardStartIndex = newCards != null ? handCards.Count - newCards.Count : -1;

            for (int i = 0; i < handCards.Count; i++)
            {
                Card card = handCards[i];
                GameObject cardObj = Instantiate(cardPrefab, cardsContainer);
                CardUI cardUI = cardObj.GetComponent<CardUI>();

                if (cardUI != null)
                {
                    cardUI.Initialize(card);
                    cardUI.OnCardClicked += OnCardUIClicked;
                    cardUI.OnCardPlayed += PlayCard;

                    bool canPlay = card.cost <= currentEnergy;
                    cardUI.SetInteractable(canPlay);

                    RectTransform rect = cardUI.GetRectTransform();

                    if (newCards != null && i >= newCardStartIndex)
                    {
                        float delay = (i - newCardStartIndex) * drawAnimationDelay;
                        cardUI.PlayDrawAnimation(drawPilePos, delay);
                    }

                    rect.anchoredPosition = targetPositions[i];
                    rect.localRotation = Quaternion.Euler(0f, 0f, targetRotations[i]);
                    cardUI.SetOriginalPosition(targetPositions[i]);
                }

                cardUIs.Add(cardUI);
            }

            UpdateEnergyUI();
            UpdatePileCountUI();
        }

        public void PlayCard(Card card)
        {
            if (card == null) return;

            if (card.cost > currentEnergy)
            {
                return;
            }

            if (!handCards.Contains(card))
            {
                return;
            }

            currentEnergy -= card.cost;
            OnEnergyChanged?.Invoke(currentEnergy);
            UpdateEnergyUI();

            handCards.Remove(card);

            CardUI targetCardUI = null;
            foreach (var cardUI in cardUIs)
            {
                if (cardUI != null && cardUI.GetCardData() == card)
                {
                    targetCardUI = cardUI;
                    break;
                }
            }

            if (targetCardUI != null)
            {
                cardUIs.Remove(targetCardUI);
                targetCardUI.GetRectTransform().DOAnchorPosY(200f, 0.3f)
                    .SetEase(Ease.OutQuad);
                targetCardUI.GetRectTransform().DOScale(Vector3.one * 1.5f, 0.2f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => {
                        targetCardUI.GetRectTransform().DOScale(Vector3.zero, 0.15f)
                            .SetEase(Ease.InQuad)
                            .OnComplete(() => {
                                Destroy(targetCardUI.gameObject);
                            });
                    });
            }

            CombatContext context = new CombatContext(
                battleManager,
                battleManager?.GetCurrentEnemy(),
                null,
                card
            );

            card.ExecuteEffects(context);

            discardPile.Add(card);

            OnCardPlayed?.Invoke(card);

            RefreshAllUI();
        }

        public void DiscardCard(Card card)
        {
            if (card == null || !handCards.Contains(card)) return;

            handCards.Remove(card);
            discardPile.Add(card);

            CardUI targetCardUI = null;
            foreach (var cardUI in cardUIs)
            {
                if (cardUI != null && cardUI.GetCardData() == card)
                {
                    targetCardUI = cardUI;
                    break;
                }
            }
            if (targetCardUI != null)
            {
                cardUIs.Remove(targetCardUI);
                Destroy(targetCardUI.gameObject);
            }

            RefreshAllUI();
        }

        public List<Card> GetHandCards() => new List<Card>(handCards);

        public List<Card> GetDrawPile() => drawPile;

        public void RemoveCardFromDrawPile(int index)
        {
            if (index >= 0 && index < drawPile.Count)
            {
                drawPile.RemoveAt(index);
                UpdatePileCountUI();
            }
        }

        public void AddCardToHand(Card card)
        {
            if (card == null) return;

            if (handCards.Count < maxHandSize)
            {
                handCards.Add(card);
                Vector3 drawPilePos = GetDrawPilePosition();
                List<Card> newCards = new List<Card> { card };
                UpdateHandUIWithAnimation(drawPilePos, newCards);
                UpdatePileCountUI();
            }
            else
            {
                discardPile.Add(card);
                Debug.Log($"手牌已满，{card.cardName} 进入弃牌堆");
                UpdatePileCountUI();
            }
        }

        void RefreshAllUI()
        {
            UpdateHandUI();
            UpdateEnergyUI();
            UpdatePileCountUI();
        }

        void UpdateHandUI()
        {
            CreateCardUIs(Vector3.zero);
        }

        void UpdateEnergyUI()
        {
            if (energyText != null)
                energyText.text = $"能量: {currentEnergy}/{maxEnergy}";
        }

        void UpdatePileCountUI()
        {
            if (drawPileCountText != null)
                drawPileCountText.text = $"抽牌: {drawPile.Count}";
            if (discardPileCountText != null)
                discardPileCountText.text = $"弃牌: {discardPile.Count}";
        }

        void OnCardUIClicked(Card card)
        {
            if (card != null && card.cost <= currentEnergy)
            {
                PlayCard(card);
            }
        }

        void ClearHand()
        {
            handCards.Clear();
            foreach (var cardUI in cardUIs)
            {
                if (cardUI != null) Destroy(cardUI.gameObject);
            }
            cardUIs.Clear();
        }

        public void AddCardToDrawPile(Card card)
        {
            if (card != null) drawPile.Add(card);
        }

        public void AddCardsToDrawPile(List<Card> cards)
        {
            drawPile.AddRange(cards);
        }

        public int GetHandSize() => handCards.Count;
        public int GetDrawPileSize() => drawPile.Count;
        public int GetDiscardPileSize() => discardPile.Count;
        public int GetCurrentEnergy() => currentEnergy;
        public int GetMaxEnergy() => maxEnergy;

        public void ResetEnergy()
        {
            currentEnergy = maxEnergy;
            OnEnergyChanged?.Invoke(currentEnergy);
            UpdateEnergyUI();
            UpdateHandUI();
        }

        public void RestoreEnergy(int amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
            OnEnergyChanged?.Invoke(currentEnergy);
            UpdateEnergyUI();
            UpdateHandUI();
        }

        public bool IsInBattle() => handPanel != null && handPanel.activeSelf;
    }
}
