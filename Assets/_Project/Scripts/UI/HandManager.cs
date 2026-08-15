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

        [Header("UI引用")]
        [SerializeField] private GameObject handPanel;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private RectTransform cardsContainer;

        [Header("牌堆信息栏")]
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text drawPileCountText;
        [SerializeField] private TMP_Text discardPileCountText;
        private TMP_Text nextDrawPeekText; // 观星镜：抽牌堆下一张预览（运行时自建）

        [Header("Fan Layout")]
        [SerializeField] private float fanRadius = 2000f;
        [SerializeField] private float maxFanAngle = 35f;
        [SerializeField][Range(0f, 1f)] private float fanRotationStrength = 1f;

        [Header("牌组配置")]
        [SerializeField] private DeckData deckData;

        [Header("抽牌参数")]
        [SerializeField] private int maxHandSize = 10;
        [SerializeField] private int cardsPerTurn = 5;
        [SerializeField] private int startingHandSize = 5;

        [Header("动画参数")]
        [SerializeField] private float drawAnimationDelay = 0.05f;

        [Header("能量系统")]
        [SerializeField] private int maxEnergy = 3;
        private int currentEnergy = 3;
        private int pendingNextTurnEnergy = 0;

        [Header("牌堆（运行时）")]
        [SerializeField] private List<Card> drawPile = new List<Card>();
        [SerializeField] private List<Card> handCards = new List<Card>();
        [SerializeField] private List<Card> discardPile = new List<Card>();
        [SerializeField] private List<Card> exhaustPile = new List<Card>();

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

        void OnDestroy()
        {
            // 清理所有卡牌 UI 上残留的 DOTween 动画
            foreach (var cardUI in cardUIs)
            {
                if (cardUI != null && cardUI.GetRectTransform() != null)
                {
                    DOTween.Kill(cardUI.GetRectTransform());
                }
            }
            cardUIs.Clear();
        }

        void Start()
        {
            // 从 GameConfig 加载默认值（仅当使用默认值时，允许 Inspector 覆盖）
            var config = GameConfig.Instance;
            if (config != null)
            {
                if (maxHandSize == 10) maxHandSize = config.maxHandSize;
                if (cardsPerTurn == 5) cardsPerTurn = config.cardsPerTurn;
                if (startingHandSize == 5) startingHandSize = config.startingHandSize;
                if (maxEnergy == 3) maxEnergy = config.maxEnergy;
            }

            currentEnergy = maxEnergy;
            UpdateEnergyUI();
            UpdatePileCountUI();

            // 阵营主题能量 UI：持有阵营 Boss 遗物时才替换数字能量显示
            FactionEnergyUI.EnsureExists(this);
        }

        public void StartBattle()
        {
            battleManager = FindObjectOfType<BattleManager>();
            if (battleManager == null)
            {
                GameLogger.LogError("HandManager: 无法找到 BattleManager");
            }

            isFirstTurn = true;
            isAnimating = false;

            // 修复 Bug16：跨战斗状态清理，避免上一场战斗的临时累积泄漏到下一场
            pendingNextTurnEnergy = 0;

            InitializeDeckFromConfig();
            ClearHand();

            ShuffleDrawPile();

            StartCoroutine(DrawCardsRoutine(startingHandSize, GetDrawPilePosition()));

            currentEnergy = maxEnergy;
            // 虚弱诅咒：每场战斗开始能量-1
            RelicManager rm = RelicManager.Instance;
            if (rm != null && rm.HasRelic(RelicIds.Curse_Weakness))
            {
                currentEnergy = Mathf.Max(0, currentEnergy - 1);
                GameLogger.Log("[HandManager] 虚弱诅咒：本场战斗初始能量 -1");
            }
            // 能量变化统一由 UpdateEnergyUI 广播
            UpdateEnergyUI();
            UpdatePileCountUI();
        }

        public void EndBattle()
        {
            foreach (var card in handCards)
                discardPile.Add(card);
            handCards.Clear();

            foreach (var card in exhaustPile)
                discardPile.Add(card);
            exhaustPile.Clear();

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
                currentEnergy = maxEnergy + pendingNextTurnEnergy;
                pendingNextTurnEnergy = 0;
                // 能量变化统一由 UpdateEnergyUI 广播
                UpdateEnergyUI();

                // 回合开始时检查馈赠
                TriggerGiftsAtTurnStart();
                return;
            }

            currentEnergy = maxEnergy + pendingNextTurnEnergy;
            pendingNextTurnEnergy = 0;
            // 能量变化统一由 UpdateEnergyUI 广播
            UpdateEnergyUI();

            if (drawPile.Count == 0 && discardPile.Count > 0)
            {
                ReshuffleDiscard();
            }

            // 回合开始时检查馈赠
            TriggerGiftsAtTurnStart();


            int effectiveDraw = GetEffectiveCardsPerTurn();
            StartCoroutine(DrawCardsRoutine(effectiveDraw, GetDrawPilePosition()));
        }

        public void OnEndTurn()
        {
            // 回合结束时检查馈赠
            TriggerGiftsAtTurnEnd();

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
            exhaustPile.Clear();

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
                if (handCards.Count >= GetEffectiveMaxHandSize())
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
                GameLogger.LogWarning("CardPrefab 或 CardsContainer 为空");
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

                    bool canPlay = CanPlayCard(card);
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

            if (!CanPlayCard(card))
            {
                GameLogger.Log($"[HandManager] 无法打出此卡: {card.cardName}");
                return;
            }

            if (!handCards.Contains(card))
            {
                return;
            }

            PayCardCost(card);


            if (card.cardType != CardType.Curse)
                TriggerCurseDevourEffects();

            // 记录出牌前的手牌索引：Remove 后 handCards.IndexOf(card) 恒为 -1，
            // 相邻牌判定（史莱姆系列效果）依赖此索引
            card.lastHandIndex = handCards.IndexOf(card);

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
                RectTransform playedRt = targetCardUI.GetRectTransform();
                playedRt.DOAnchorPosY(200f, 0.3f)
                    .SetEase(Ease.OutQuad);
                playedRt.DOScale(Vector3.one * 1.5f, 0.2f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => {
                        if (targetCardUI == null) return;
                        playedRt.DOScale(Vector3.zero, 0.15f)
                            .SetEase(Ease.InQuad)
                            .OnComplete(() => {
                                if (targetCardUI != null) Destroy(targetCardUI.gameObject);
                            });
                    });
            }

            // 修复：传入实际玩家数据，避免部分效果因 targetPlayer 为 null 静默失败
            var dataMgr = PlayerDataManager.Instance;
            PlayerData playerData = dataMgr != null ? dataMgr.GetPlayerData() : null;
            CombatContext context = new CombatContext(
                battleManager,
                battleManager?.GetCurrentEnemy(),
                playerData,
                card
            );

            card.ExecuteEffects(context);

            bool isExhausted = ConversionModifier.ShouldExhaust(card);
            if (isExhausted)
            {
                exhaustPile.Add(card);

                var effectManager = EffectManager.Instance;
                if (effectManager != null)
                {
                    EffectContext exhaustCtx = new EffectContext(battleManager);
                    exhaustCtx.combat = context;
                    exhaustCtx.tag = card;
                    effectManager.Trigger(EffectTrigger.CardExhausted, exhaustCtx);
                }
            }
            else
            {
                discardPile.Add(card);
            }

            OnCardPlayed?.Invoke(card);

            RefreshAllUI();
        }

        public int GetModifiedCardCost(Card card)
        {
            if (card == null) return 0;

            int baseCost = card.cost;
            var effectManager = EffectManager.Instance;
            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(battleManager);
                ctx.tag = card;
                return effectManager.CalculateModifiedValue(EffectTrigger.CalculateCardCost, ctx, baseCost);
            }
            return baseCost;
        }

        /// <summary>
        /// 判断卡牌费用是否可以支付。
        /// 优先使用能量，不足部分按顺序用血→格挡补足（混合转换模式）。
        /// </summary>
        public bool CanPayCardCost(Card card)
        {
            if (card == null) return false;

            int modifiedCost = GetModifiedCardCost(card);
            int energyShortfall = Mathf.Max(0, modifiedCost - currentEnergy);

            if (energyShortfall == 0) return true;

            var dataManager = PlayerDataManager.Instance;
            if (dataManager == null) return false;
            PlayerData playerData = dataManager.GetPlayerData();
            if (playerData == null) return false;

            if (!card.UsesSpecialCost) return false;

            int currentBlock = battleManager != null ? battleManager.GetPlayerBlock() : 0;

            return card.CanPayWithMixedConversion(currentEnergy, playerData.currentHealth, currentBlock, modifiedCost);
        }

        /// <summary>
        /// 支付卡牌费用。按顺序：先计算转换消耗量 → 扣能量 → 扣血量 → 扣格挡。
        /// 混合模式：鲜血(3血=1能量)优先，寒霜(5格挡=1能量)补足剩余。
        /// </summary>
        private void PayCardCost(Card card)
        {
            if (card == null) return;

            int modifiedCost = GetModifiedCardCost(card);

            var dataManager = PlayerDataManager.Instance;
            PlayerData playerData = dataManager != null ? dataManager.GetPlayerData() : null;
            int currentBlock = battleManager != null ? battleManager.GetPlayerBlock() : 0;

            // === 先计算混合转换消耗量（基于扣除能量前的状态，使用 modifiedCost 保证费用减免正确传递到转换阶段）===
            int bloodToPay = 0;
            int blockToPay = 0;

            if (card.UsesSpecialCost && playerData != null)
            {
                var (bloodCost, blockCost) = card.CalculateMixedConversionCosts(
                    currentEnergy, playerData.currentHealth, currentBlock, modifiedCost);
                bloodToPay = bloodCost;
                blockToPay = blockCost;
            }

            // === 再扣能量 ===
            int energyToPay = Mathf.Min(modifiedCost, currentEnergy);
            if (energyToPay > 0)
            {
                currentEnergy -= energyToPay;
                // 能量变化统一由 UpdateEnergyUI 广播
                UpdateEnergyUI();
            }

            int energyShortfall = Mathf.Max(0, modifiedCost - energyToPay);

            // === 先扣血量（鲜血优先）===
            if (bloodToPay > 0 && playerData != null)
            {
                if (playerData.currentHealth > bloodToPay)
                {
                    playerData.TakeDamage(bloodToPay);
                    GameLogger.Log($"[HandManager] {card.cardName} 血量转换: {energyShortfall}能量 <- {bloodToPay}血量");
                    dataManager.UpdateUI();
                }
            }

            // === 再扣格挡（寒霜补足）===
            if (blockToPay > 0 && battleManager != null)
            {
                if (battleManager.GetPlayerBlock() >= blockToPay)
                {
                    battleManager.ConsumePlayerBlock(blockToPay);
                    GameLogger.Log($"[HandManager] {card.cardName} 格挡转换: {energyShortfall}能量 <- {blockToPay}格挡");
                }
            }
        }

        /// <summary>
        /// 判断卡牌是否可以打出。
        /// </summary>
        public bool CanPlayCard(Card card)
        {
            if (card == null) return false;
            if (card.cardType == CardType.Curse) return false;
            return CanPayCardCost(card);
        }

        /// <summary>
        /// 回合开始时触发馈赠效果。
        /// </summary>
        private void TriggerGiftsAtTurnStart()
        {
            var triggered = GiftEffect.CheckAndTriggerGifts(GiftEffect.GiftTriggerTime.TurnStart);
            if (triggered.Count > 0)
            {
                RefreshAllUI();
            }
        }

        /// <summary>
    /// 回合结束时触发馈赠效果，并处理诅咒卡效果。
    /// </summary>
    private void TriggerGiftsAtTurnEnd()
    {
        var triggered = GiftEffect.CheckAndTriggerGifts(GiftEffect.GiftTriggerTime.TurnEnd);
        if (triggered.Count > 0)
        {
            RefreshAllUI();
        }


        TriggerCurseDecayEffects();
    }

    /// <summary>

    /// </summary>
    private void TriggerCurseDecayEffects()
    {
        var dataManager = PlayerDataManager.Instance;
        PlayerData playerData = dataManager != null ? dataManager.GetPlayerData() : null;
        if (playerData == null) return;

        int totalHpLoss = 0;
        foreach (var card in handCards)
        {
            if (card == null || card.cardType != CardType.Curse) continue;
            foreach (var effect in card.effects)
            {
                if (effect is CurseDecayEffect decay)
                {
                    totalHpLoss += decay.hpLossPerTurn;
                }
            }
        }

        if (totalHpLoss > 0)
        {
            playerData.TakeDamage(totalHpLoss);
            GameLogger.Log($"[HandManager] 诅咒衰败触发：损失 {totalHpLoss} HP");
            dataManager.UpdateUI();
        }
    }

    /// <summary>
    /// 获取当前手牌中所有指定类型的诅咒效果。
    /// </summary>
    private List<T> GetCurseEffects<T>() where T : CurseEffect
    {
        List<T> result = new List<T>();
        foreach (var card in handCards)
        {
            if (card == null || card.cardType != CardType.Curse) continue;
            foreach (var effect in card.effects)
            {
                if (effect is T typed)
                    result.Add(typed);
            }
        }
        return result;
    }

    /// <summary>

    /// </summary>
    private int GetEffectiveMaxHandSize()
    {
        int reduction = 0;
        foreach (var fog in GetCurseEffects<CurseFogEffect>())
            reduction += fog.handSizeReduction;
        return Mathf.Max(1, maxHandSize - reduction);
    }

    /// <summary>

    /// </summary>
    private int GetEffectiveCardsPerTurn()
    {
        int reduction = 0;
        foreach (var chains in GetCurseEffects<CurseChainsEffect>())
            reduction += chains.drawReduction;
        return Mathf.Max(0, cardsPerTurn - reduction);
    }

    /// <summary>

    /// </summary>
    private void TriggerCurseDevourEffects()
    {
        var dataManager = PlayerDataManager.Instance;
        PlayerData playerData = dataManager != null ? dataManager.GetPlayerData() : null;
        if (playerData == null) return;

        int totalHpLoss = 0;
        foreach (var devour in GetCurseEffects<CurseDevourEffect>())
            totalHpLoss += devour.hpLossPerCard;

        if (totalHpLoss > 0)
        {
            playerData.TakeDamage(totalHpLoss);
            GameLogger.Log($"[HandManager] 诅咒噬命触发：损失 {totalHpLoss} HP");
            dataManager.UpdateUI();
        }
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

        public List<Card> GetDiscardPile() => discardPile;

        public List<Card> GetExhaustPile() => exhaustPile;

        public void RemoveCardFromDrawPile(int index)
        {
            if (index >= 0 && index < drawPile.Count)
            {
                drawPile.RemoveAt(index);
                UpdatePileCountUI();
            }
        }

        /// <summary>
        /// 添加卡牌到抽牌堆顶部。
        /// </summary>
        public void AddToDrawPileTop(Card card)
        {
            if (card == null) return;
            drawPile.Insert(0, card);
            UpdatePileCountUI();
        }

        /// <summary>
        /// 添加卡牌到抽牌堆底部。
        /// </summary>
        public void AddToDrawPileBottom(Card card)
        {
            if (card == null) return;
            drawPile.Add(card);
            UpdatePileCountUI();
        }

        /// <summary>
        /// 随机插入卡牌到抽牌堆。
        /// </summary>
        public void AddToDrawPileRandom(Card card)
        {
            if (card == null) return;
            int index = Random.Range(0, drawPile.Count + 1);
            drawPile.Insert(index, card);
            UpdatePileCountUI();
        }

        /// <summary>
        /// 添加卡牌到弃牌堆。
        /// </summary>
        public void AddToDiscardPile(Card card)
        {
            if (card == null) return;
            discardPile.Add(card);
            UpdatePileCountUI();
        }

        /// <summary>
        /// 添加卡牌到消耗堆。
        /// </summary>
        public void AddToExhaustPile(Card card)
        {
            if (card == null) return;
            exhaustPile.Add(card);
            UpdatePileCountUI();
        }

        public void AddCardToHand(Card card)
        {
            if (card == null) return;

            if (handCards.Count < GetEffectiveMaxHandSize())
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
                GameLogger.Log($"手牌已满，{card.cardName} 放入弃牌堆");
                UpdatePileCountUI();
            }
        }

        void RefreshAllUI()
        {
            UpdateHandUI();
            UpdateEnergyUI();
            UpdatePileCountUI();
        }

        public void UpdateHandUI()
        {
            CreateCardUIs(Vector3.zero);
        }

        void UpdateEnergyUI()
        {
            if (energyText != null)
                energyText.text = $"能量: {currentEnergy}/{maxEnergy}";
            // 统一从此处广播能量变化（阵营主题能量 UI 等监听者）
            OnEnergyChanged?.Invoke(currentEnergy);
        }

        public void UpdatePileCountUI()
        {
            if (drawPileCountText != null)
                drawPileCountText.text = $"抽牌: {drawPile.Count}";
            if (discardPileCountText != null)
                discardPileCountText.text = $"弃牌: {discardPile.Count}";
            UpdateNextDrawPeek();
        }

        /// <summary>观星镜基础效果：显示抽牌堆下一张卡牌预览（持有才显示）。</summary>
        void UpdateNextDrawPeek()
        {
            RelicManager rm = RelicManager.Instance;
            bool hasPeek = rm != null && rm.HasRelic(RelicIds.Shop_Astrolabe);

            if (!hasPeek)
            {
                if (nextDrawPeekText != null)
                    nextDrawPeekText.gameObject.SetActive(false);
                return;
            }

            if (nextDrawPeekText == null)
                CreateNextDrawPeekUI();

            if (nextDrawPeekText != null)
            {
                nextDrawPeekText.gameObject.SetActive(true);
                nextDrawPeekText.text = drawPile.Count > 0
                    ? $"观星: 下一抽「{drawPile[0].cardName}」"
                    : "观星: 抽牌堆已空";
            }
        }

        void CreateNextDrawPeekUI()
        {
            TMP_Text anchor = drawPileCountText != null ? drawPileCountText : energyText;
            if (anchor == null) return;

            GameObject go = new GameObject("NextDrawPeek", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(anchor.transform.parent, false);
            nextDrawPeekText = go.GetComponent<TextMeshProUGUI>();

            RectTransform rt = nextDrawPeekText.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchor.rectTransform.anchoredPosition + new Vector2(0f, -34f);
            rt.sizeDelta = new Vector2(420f, 32f);

            nextDrawPeekText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/SIMSUN SDF");
            nextDrawPeekText.fontSize = 20f;
            nextDrawPeekText.alignment = TextAlignmentOptions.Center;
            nextDrawPeekText.color = new Color(0.86f, 0.72f, 0.35f, 0.95f);
        }

        void OnCardUIClicked(Card card)
        {
            if (card != null && CanPayCardCost(card))
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

        /// <summary>
        /// 消耗手牌全部卡牌到消耗堆（用于黑暗契约药水等效果）。
        /// 返回被消耗的卡牌数量。
        /// </summary>
        public int ExhaustHand()
        {
            if (handCards.Count == 0) return 0;

            int count = handCards.Count;
            foreach (var card in handCards)
            {
                if (card != null) exhaustPile.Add(card);
            }

            handCards.Clear();
            foreach (var cardUI in cardUIs)
            {
                if (cardUI != null) Destroy(cardUI.gameObject);
            }
            cardUIs.Clear();

            UpdatePileCountUI();
            RefreshAllUI();
            GameLogger.Log($"[HandManager] 消耗全部手牌: {count} 张");
            return count;
        }

        /// <summary>
        /// 弃掉手牌全部卡牌到弃牌堆（回合结束调用）。
        /// </summary>
        public void DiscardHand()
        {
            if (handCards.Count == 0) return;

            foreach (var card in handCards)
            {
                if (card != null) discardPile.Add(card);
            }

            handCards.Clear();
            foreach (var cardUI in cardUIs)
            {
                if (cardUI != null) Destroy(cardUI.gameObject);
            }
            cardUIs.Clear();

            UpdatePileCountUI();
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
        public int GetExhaustPileSize() => exhaustPile.Count;
        public int GetCurrentEnergy() => currentEnergy;
        public int GetMaxEnergy() => maxEnergy;

        /// <summary>花瓣能量 UI 使用的只读属性。</summary>
        public int CurrentEnergy => currentEnergy;
        public int MaxEnergy => maxEnergy;
        public TMP_Text EnergyText => energyText;

        /// <summary>永久提升能量上限（遗物奖励等）——花瓣数量随之增加。</summary>
        public void AddMaxEnergy(int amount)
        {
            maxEnergy += amount;
            UpdateEnergyUI();
        }

        public void ResetEnergy()
        {
            currentEnergy = maxEnergy;
            UpdateEnergyUI();
            UpdateHandUI();
        }

        public void RestoreEnergy(int amount)
        {
            currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
            UpdateEnergyUI();
            UpdateHandUI();
        }

        public void AddPendingNextTurnEnergy(int amount)
        {
            pendingNextTurnEnergy += amount;
        }

        public bool IsInBattle() => handPanel != null && handPanel.activeSelf;
    }
}