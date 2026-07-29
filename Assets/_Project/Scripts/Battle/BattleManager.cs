using MutationChess.Core;
using MutationChess.Map;
using MutationChess.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MutationChess.Battle
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("UI???")]
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject handPanel;

 [Header("")]
        [SerializeField] private Button toggleViewButton;

 [Header("")]
        [SerializeField] private Button endTurnButton;

 [Header("")]
        [SerializeField] private EnemyIntentUI enemyIntentUI;

 [Header("")]
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private Image enemyImage;

 [Header("")]
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text playerBlockText;
        [SerializeField] private Image playerImage;

 [Header("")]
        [SerializeField] private TMP_Text battleLogText;
        [SerializeField] private BattleIntroUI battleIntroUI;

        [Header("Debuff")]
        [SerializeField] private TMP_Text playerDebuffText;

 [Header("")]
        [SerializeField] private TMP_Text actionHintText;

 [Header("")]
        [SerializeField] private RewardPanel rewardPanel;

 [Header("")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private BackgroundConfig backgroundConfig;

        public event Action<bool> OnBattleEnd;

        private bool isInBattle = false;
        private bool isViewingMap = false;
        private Enemy currentEnemy;
        private PlayerData playerData;
        private int playerBlock = 0;
        private bool isBattleEnding = false;
        private bool waitingForPlayerInput = false;
        private bool isEnemyTurn = false;

        private EnemyIntentType currentIntent;
        private int currentIntentValue;
        private EnemyAction currentAction;

        private GameManager gameManager;
        private Relic pendingRelicReward;
        private Relic pendingBonusRelic;
        private Card pendingBossFactionCard;
        private Potion pendingPotionReward;
        private int bossRewardGold;
        private EffectManager effectManager;

        public Relic PendingRelicReward => pendingRelicReward;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            gameManager = FindObjectOfType<GameManager>();
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>(true);
            }

            if (gameManager == null)
            {
 GameLogger.LogWarning("BattleManager: ??? GameManager");
            }

            effectManager = EffectManager.Instance;

            if (battlePanel != null) battlePanel.SetActive(false);
            if (handPanel != null) handPanel.SetActive(false);

            LoadPlayerImage();

            if (toggleViewButton != null)
            {
                toggleViewButton.gameObject.SetActive(false);
                toggleViewButton.onClick.RemoveAllListeners();
                toggleViewButton.onClick.AddListener(ToggleView);
            }

            if (endTurnButton != null)
            {
                endTurnButton.gameObject.SetActive(false);
                endTurnButton.onClick.RemoveAllListeners();
                endTurnButton.onClick.AddListener(OnEndTurnClicked);
            }

            var turnManager = TurnManager.Instance;
            if (turnManager != null)
            {
                turnManager.OnPlayerTurnStart += OnPlayerTurnStart;
                turnManager.OnPlayerTurnEnd += OnPlayerTurnEnd;
                turnManager.OnEnemyTurnStart += OnEnemyTurnStart;
                turnManager.OnEnemyTurnEnd += OnEnemyTurnEnd;
            }
        }

        private void LoadPlayerImage()
        {
            if (playerImage == null) return;

            Sprite playerSprite = Resources.Load<Sprite>("PlayerSprites/Player");

            if (playerSprite == null)
            {
                playerSprite = Resources.Load<Sprite>("Player");
            }

            if (playerSprite != null)
            {
                playerImage.sprite = playerSprite;
                playerImage.gameObject.SetActive(true);
            }
            else
            {
                playerImage.gameObject.SetActive(false);
 GameLogger.LogWarning("");
            }
        }

        void OnDestroy()
        {
            var turnManager = TurnManager.Instance;
            if (turnManager != null)
            {
                turnManager.OnPlayerTurnStart -= OnPlayerTurnStart;
                turnManager.OnPlayerTurnEnd -= OnPlayerTurnEnd;
                turnManager.OnEnemyTurnStart -= OnEnemyTurnStart;
                turnManager.OnEnemyTurnEnd -= OnEnemyTurnEnd;
            }
        }

        void Update()
        {
            if (isInBattle && waitingForPlayerInput && !isBattleEnding && !isViewingMap && !isEnemyTurn)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    OnPlayerAction("attack");
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                    OnPlayerAction("defend");
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                    OnPlayerAction("skill");
            }
        }

        void OnPlayerTurnStart()
        {
            waitingForPlayerInput = true;
            isEnemyTurn = false;

            if (playerData != null)
            {
                playerData.OnTurnStart();
            }

            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(this);
                effectManager.Trigger(EffectTrigger.PlayerTurnStart, ctx);
            }

            if (endTurnButton != null)
                endTurnButton.gameObject.SetActive(true);

            if (actionHintText != null)
            {
 actionHintText.text = " ";
                actionHintText.color = Color.white;
            }

            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.OnNewTurn();
            }

            if (currentEnemy != null)
            {
                currentEnemy.OnTurnStart();
                GenerateAndStoreEnemyIntent();
                ShowStoredEnemyIntent();
            }

            RefreshAllUI();

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
                dataManager.UpdateUI();

            UpdatePlayerDebuffDisplay();
        }

        void OnPlayerTurnEnd()
        {
            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(this);
                effectManager.Trigger(EffectTrigger.PlayerTurnEnd, ctx);
            }
        }

        void OnEnemyTurnStart()
        {
            waitingForPlayerInput = false;
            isEnemyTurn = true;

            if (endTurnButton != null)
                endTurnButton.gameObject.SetActive(false);

            if (actionHintText != null)
            {
 actionHintText.text = "...";
                actionHintText.color = Color.gray;
            }

            StartCoroutine(ExecuteStoredEnemyIntentRoutine());
        }

        void OnEnemyTurnEnd()
        {
            isEnemyTurn = false;
        }

        void OnEndTurnClicked()
        {
            if (!isInBattle || isBattleEnding || isEnemyTurn) return;

            var handManager = HandManager.Instance;
            if (handManager != null)
            {
                handManager.OnEndTurn();
            }

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
                dataManager.UpdateUI();

            var turnManager = TurnManager.Instance;
            if (turnManager != null)
                turnManager.EndPlayerTurn();
        }

        void GenerateAndStoreEnemyIntent()
        {
            if (currentEnemy == null) return;

            currentAction = currentEnemy.GetNextAction();
            currentIntent = currentAction.intentType;
            currentIntentValue = currentAction.GetFinalValue();

            if (currentIntent != EnemyIntentType.Wait)
            {
                switch (currentIntent)
                {
                    case EnemyIntentType.Attack:
                        currentIntentValue = currentEnemy.GetAttackDamage();
                        break;
                    case EnemyIntentType.Defend:
                        currentIntentValue = Mathf.Max(1, currentIntentValue);
                        break;
                    case EnemyIntentType.Special:
                        currentIntentValue = Mathf.Max(1, currentIntentValue);
                        break;
                    case EnemyIntentType.Buff:
                        currentIntentValue = Mathf.Max(1, currentIntentValue);
                        break;
                }
            }
        }

        void ShowStoredEnemyIntent()
        {
            if (enemyIntentUI != null)
            {
                int displayValue = currentIntentValue;
                if (currentIntent == EnemyIntentType.Attack || currentIntent == EnemyIntentType.Special)
                {
                    displayValue = currentEnemy.GetAttackDamage();
                }
                enemyIntentUI.ShowIntent(currentIntent, displayValue);
            }
            else
            {
                GameLogger.LogWarning("EnemyIntentUI ???");
            }
        }

        void HideEnemyIntent()
        {
            if (enemyIntentUI != null)
                enemyIntentUI.HideIntent();
        }

        IEnumerator ExecuteStoredEnemyIntentRoutine()
        {
            yield return new WaitForSeconds(0.8f);

            if (currentEnemy == null || currentEnemy.IsDead())
            {
                EndBattle(true);
                yield break;
            }

            switch (currentIntent)
            {
                case EnemyIntentType.Attack:
                    yield return ExecuteAttack();
                    break;

                case EnemyIntentType.Defend:
                    yield return ExecuteDefend();
                    break;

                case EnemyIntentType.Special:
                    yield return ExecuteSpecial();
                    break;

                case EnemyIntentType.Buff:
                    yield return ExecuteBuff();
                    break;

                case EnemyIntentType.Wait:
 AddLog($"{currentEnemy.enemyName} ...");
                    yield return new WaitForSeconds(0.5f);
                    break;
            }

            currentEnemy.OnTurnEnd();

            RefreshAllUI();

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
            {
                dataManager.UpdateUI();
            }

            if (playerData.currentHealth <= 0)
            {
                EndBattle(false);
                yield break;
            }

            if (currentEnemy.IsDead())
            {
                EndBattle(true);
                yield break;
            }

            HideEnemyIntent();

            var turnManager = TurnManager.Instance;
            if (turnManager != null)
            {
                turnManager.EndEnemyTurn();
            }
        }

        IEnumerator ExecuteAttack()
        {
            int dmg = currentIntentValue;
            int actual = Mathf.Max(0, dmg - playerBlock);

            if (playerBlock > 0)
                AddLog($"??? {Mathf.Min(dmg, playerBlock)}???");

            playerBlock = 0;

            if (actual > 0)
            {
 // ??? EffectManager DamageReductionEffect ???
                actual = CalculatePlayerDamage(actual);
                playerData.TakeDamage(actual);
 AddLog($"{currentEnemy.enemyName} {actual} ");
                currentEnemy.PlayAttack();
            }
            else
            {
 AddLog("");
                currentEnemy.PlayHurt();
            }

            RefreshAllUI();
            UpdatePlayerDebuffDisplay();
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator ExecuteDefend()
        {
            int healAmount = currentIntentValue;
            currentEnemy.currentHealth = Mathf.Min(currentEnemy.maxHealth, currentEnemy.currentHealth + healAmount);
 AddLog($"{currentEnemy.enemyName} {healAmount} ");
            currentEnemy.PlayIdle();

            RefreshAllUI();
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator ExecuteSpecial()
        {
            int specialDmg = currentIntentValue;
            int actualSpecial = Mathf.Max(0, specialDmg - playerBlock / 2);
            playerBlock = 0;

            if (actualSpecial > 0)
            {
 // ??? EffectManager 
                actualSpecial = CalculatePlayerDamage(actualSpecial);
                playerData.TakeDamage(actualSpecial);
 AddLog($"{currentEnemy.enemyName} {actualSpecial} ");
                currentEnemy.PlayAttack();
            }
            else
            {
 AddLog("");
                currentEnemy.PlayHurt();
            }

            RefreshAllUI();
            UpdatePlayerDebuffDisplay();
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator ExecuteBuff()
        {
            int buffAmount = currentIntentValue;
            currentEnemy.currentAttackDamage += buffAmount;
 AddLog($"{currentEnemy.enemyName} {buffAmount}");
            currentEnemy.PlayHurt();
            yield return new WaitForSeconds(0.5f);
        }

        public void ShowBattleView()
        {
            if (battlePanel != null) battlePanel.SetActive(true);
            if (handPanel != null) handPanel.SetActive(true);
            if (toggleViewButton != null) toggleViewButton.gameObject.SetActive(true);
            isViewingMap = false;
        }

        public void ShowMapView()
        {
            if (battlePanel != null) battlePanel.SetActive(false);
            if (handPanel != null) handPanel.SetActive(false);
            if (toggleViewButton != null) toggleViewButton.gameObject.SetActive(true);
            isViewingMap = true;
        }

        public void ToggleView()
        {
            if (!isInBattle) return;

            if (isViewingMap)
            {
                ShowBattleView();
                RefreshAllUI();
                if (actionHintText != null)
                {
                    var turnManager = TurnManager.Instance;
 actionHintText.text = (turnManager != null && turnManager.IsPlayerTurn) ? " " : "...";
                    actionHintText.color = (turnManager != null && turnManager.IsPlayerTurn) ? Color.white : Color.gray;
                }
 AddLog("");
            }
            else
            {
                ShowMapView();
                AddLog("???...");
            }
        }

        public void StartBattle(Enemy enemy, PlayerData player)
        {
            currentEnemy = enemy;
            playerData = player;

            if (playerData != null)
            {
                playerData.ClearBuffs();
            }

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
            {
                dataManager.UpdateUI();
            }

            if (battleIntroUI != null)
            {
                string enemyName = enemy != null ? enemy.enemyName : "Unknown";
                int enemyHp = enemy != null ? enemy.currentHealth : 0;
                int enemyMaxHp = enemy != null ? enemy.maxHealth : 0;
                Sprite enemySprite = enemy != null ? enemy.GetSprite() : null;

                battleIntroUI.ShowIntro("Player",
                    player.currentHealth, player.maxHealth,
                    enemyName, enemyHp, enemyMaxHp,
                    enemySprite, () => StartBattlePhase2());
            }
            else
            {
                StartBattlePhase2();
            }
        }

        private void StartBattlePhase2()
        {
            if (BattleLogManager.Instance != null) BattleLogManager.Instance.ClearLogs();

            isInBattle = true;
            isBattleEnding = false;
            isViewingMap = false;
            isEnemyTurn = false;
            playerBlock = 0;
            waitingForPlayerInput = false;
            currentAction = null;

 // RelicManager 
            var relicManager = RelicManager.Instance;
            if (relicManager != null)
            {
                relicManager.OnBattleStart();
            }

            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(this);
                effectManager.Trigger(EffectTrigger.BattleStart, ctx);
            }

            ContinueBattleStartAfterEffects();
        }

        private void ContinueBattleStartAfterEffects()
        {
            if (enemyImage != null)
            {
                Sprite sprite = currentEnemy.GetSprite();
                if (sprite != null)
                {
                    enemyImage.sprite = sprite;
                    enemyImage.gameObject.SetActive(true);
                }
                else
                {
                    enemyImage.gameObject.SetActive(false);
                    GameLogger.LogWarning("Enemy has no image: " + currentEnemy.enemyName);
                }
            }

            SetBattleBackground(currentEnemy.enemyType, currentEnemy.enemyName);
            ShowBattleView();

            if (battleLogText != null) battleLogText.text = "";
            if (actionHintText != null)
            {
                actionHintText.text = "Click card or End Turn";
                actionHintText.color = Color.white;
            }

            RefreshAllUI();

            AddLog("=== Battle Start ===");
            AddLog("Encounter: " + currentEnemy.enemyName);
            AddLog("Player HP: " + playerData.currentHealth + "/" + playerData.maxHealth);
            AddLog("Enemy HP: " + currentEnemy.currentHealth + "/" + currentEnemy.maxHealth);

            var handManager = HandManager.Instance;
            if (handManager != null) handManager.StartBattle();

            var turnManager = TurnManager.Instance;
            if (turnManager != null) turnManager.StartBattle();

            waitingForPlayerInput = true;
        }

        private void SetBattleBackground(EnemyType enemyType, string enemyName)
        {
            if (backgroundImage == null)
            {
 GameLogger.LogWarning("");
                return;
            }

            if (backgroundConfig == null)
            {
 GameLogger.LogWarning("");
                return;
            }

            Sprite bgSprite = backgroundConfig.GetBackground(enemyType);

            if (bgSprite == null)
            {
                bgSprite = backgroundConfig.GetBackgroundByName(enemyName);
            }

            if (bgSprite != null)
            {
                backgroundImage.sprite = bgSprite;
                backgroundImage.enabled = true;
            }
            else
            {
                backgroundImage.enabled = false;
 GameLogger.LogWarning($" {enemyName} ");
            }
        }

        void OnPlayerAction(string action)
        {
            if (!waitingForPlayerInput || isBattleEnding || isViewingMap || isEnemyTurn) return;
            waitingForPlayerInput = false;

            switch (action)
            {
                case "attack":
 AddLog("");
                    PlayerAttack(8 + UnityEngine.Random.Range(0, 5));
                    break;
                case "defend":
                    int block = 5 + UnityEngine.Random.Range(0, 4);
                    PlayerBlock(block);
                    break;
                case "skill":
                    int skillDmg = 12 + UnityEngine.Random.Range(0, 6);
 AddLog(" -" + skillDmg + "HP");
                    if (currentEnemy != null)
                    {
                        currentEnemy.TakeDamage(skillDmg);
                    }
                    RefreshAllUI();
                    waitingForPlayerInput = true;
                    break;
            }
        }

        public void PlayerAttack(int damage)
        {
            if (currentEnemy == null)
            {
 GameLogger.LogWarning($"PlayerAttack: currentEnemy {damage} ??");
                waitingForPlayerInput = true;
                return;
            }

            if (currentEnemy.IsDead())
            {
                EndBattle(true);
                return;
            }

            int finalDamage = Mathf.Max(1, damage + UnityEngine.Random.Range(-1, 2));

            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(this);
                finalDamage = effectManager.CalculateModifiedValue(
                    EffectTrigger.CalculateAttackDamage, ctx, finalDamage);
            }

            int weak = playerData.GetBuffAmount(BuffType.Weak);
            if (weak > 0)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * (1 - weak * 0.2f));
            }

            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(this);
                effectManager.Trigger(EffectTrigger.PlayerAttack, ctx);
            }

            currentEnemy.TakeDamage(finalDamage);

 AddLog($" {finalDamage} ");

            RefreshAllUI();

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
                dataManager.UpdateUI();

            if (currentEnemy.IsDead())
            {
                EndBattle(true);
                return;
            }

            waitingForPlayerInput = true;
        }

        public void PlayerBlock(int blockAmount)
        {
            if (currentEnemy == null || currentEnemy.IsDead()) return;

            int finalBlock = playerData.GetModifiedBlock(blockAmount);

 // ??? EffectManager BlockLockNextTurnEffect ???
            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(this);
                finalBlock = effectManager.CalculateModifiedValue(
                    EffectTrigger.CalculateBlock, ctx, finalBlock);
            }

            playerBlock += finalBlock;
            AddLog($"??? {finalBlock} ?? (???: {playerBlock})");

            RefreshAllUI();
            waitingForPlayerInput = true;
        }

        void EndBattle(bool victory)
        {
            if (isBattleEnding) return;
            isBattleEnding = true;
            isInBattle = false;
            waitingForPlayerInput = false;
            isEnemyTurn = false;

            var turnManager = TurnManager.Instance;
            if (turnManager != null) turnManager.EndBattle();
            var dataManager = PlayerDataManager.Instance;

            AddLog(victory ? "=== ??? ===" : "=== ??? ===");

            if (actionHintText != null)
            {
                actionHintText.text = victory ? "???!" : "???...";
                actionHintText.color = victory ? Color.green : Color.red;
            }

            if (endTurnButton != null)
                endTurnButton.gameObject.SetActive(false);

            HideEnemyIntent();

            var handManager = HandManager.Instance;
            if (handManager != null) handManager.EndBattle();

            if (dataManager != null)
                dataManager.UpdateUI();

            if (victory)
            {
                if (effectManager != null)
                {
                    EffectContext ctx = new EffectContext(this);
                    effectManager.Trigger(EffectTrigger.Victory, ctx);
                }

                if (rewardPanel == null)
                {
 GameLogger.LogError("BattleManager: rewardPanel ");
                    OnBattleEnd?.Invoke(victory);
                    StartCoroutine(DelayedExit());
                    return;
                }

                if (gameManager == null)
                {
                    gameManager = FindObjectOfType<GameManager>();
                    if (gameManager == null)
                    {
                        gameManager = FindObjectOfType<GameManager>(true);
                    }
                }

                EnemyType enemyType = gameManager != null
                    ? gameManager.GetCurrentEnemyType()
                    : EnemyType.Normal;

                if (enemyType == EnemyType.Boss)
                {
                    HandleBossVictory();
                }
                else
                {
                    HandleNormalVictory(enemyType);
                }

                Time.timeScale = 0f;
                return;
            }

            OnBattleEnd?.Invoke(victory);
            StartCoroutine(DelayedExit());
        }

        private List<Card> GetDefaultCardRewards()
        {
            List<Card> result = new List<Card>();

            Card attack = CardData.CreateCard(CardName.¹¥»÷);
            Card defend = CardData.CreateCard(CardName.·ÀÓù);
            Card bash = CardData.CreateCard(CardName.Í´»÷);

            if (attack != null) result.Add(attack);
            if (defend != null) result.Add(defend);
            if (bash != null) result.Add(bash);

            return result;
        }

        private List<Card> GetRewardCardsForEnemy(EnemyType enemyType)
        {
            List<Card> cards = new List<Card>();

            var poolConfig = RewardPoolManager.Config;
            if (poolConfig == null) return cards;

            var unlockService = FactionUnlockService.Instance;

            int cardCount = 3;
            float rareChance;
            float uncommonChance;

 // 0~1??
            float floorProgress = GetFloorProgressFromManager();

            float nodeProgress = GetNodeProgress();


            float rareFloorBonus = floorProgress * 0.20f;

            float rareNodeBonus = nodeProgress * 0.15f;
            float uncommonNodeBonus = nodeProgress * 0.10f;

            switch (enemyType)
            {
                case EnemyType.Elite:
 // 45% + 
                    rareChance = 0.45f + rareFloorBonus + rareNodeBonus;
                    uncommonChance = 0.35f + uncommonNodeBonus;
                    break;
                case EnemyType.Boss:

                    rareChance = 1f;
                    uncommonChance = 0f;
                    break;
                default:
 // 8% + 
                    rareChance = 0.08f + rareFloorBonus + rareNodeBonus;
                    uncommonChance = 0.25f + uncommonNodeBonus;
                    break;
            }

            rareChance = Mathf.Clamp01(rareChance);
            uncommonChance = Mathf.Clamp01(uncommonChance);

 GameLogger.Log($"[BattleManager] - :{enemyType} :{floorProgress:F2} :{nodeProgress:F2} ???:{rareChance:P0} :{uncommonChance:P0}");

            var allCommon = poolConfig.GetColoredCardsByRarity(CardRarity.Common)
                .Where(a => a.faction == CardFaction.None || unlockService == null || unlockService.IsFactionUnlocked(a.faction))
                .ToList();
            var allUncommon = poolConfig.GetColoredCardsByRarity(CardRarity.Uncommon)
                .Where(a => a.faction == CardFaction.None || unlockService == null || unlockService.IsFactionUnlocked(a.faction))
                .ToList();
            var allRare = poolConfig.GetColoredCardsByRarity(CardRarity.Rare)
                .Where(a => a.faction == CardFaction.None || unlockService == null || unlockService.IsFactionUnlocked(a.faction))
                .ToList();

            for (int i = 0; i < cardCount; i++)
            {
                float roll = UnityEngine.Random.value;
                List<CardDataAsset> pool;

                if (roll < rareChance)
                    pool = allRare;
                else if (roll < rareChance + uncommonChance)
                    pool = allUncommon;
                else
                    pool = allCommon;

                if (pool.Count == 0)
                {
                    if (allCommon.Count > 0) pool = allCommon;
                    else if (allUncommon.Count > 0) pool = allUncommon;
                    else pool = allRare;
                }

                if (pool.Count > 0)
                {
                    var asset = pool[UnityEngine.Random.Range(0, pool.Count)];
                    CardName cardName;
                    if (System.Enum.TryParse(asset.name, out cardName))
                    {
                        var card = CardData.CreateCard(cardName);
                        if (card != null) cards.Add(card);
                    }
                }
            }

            return cards;
        }

        private float GetFloorProgress()
        {
            return GetFloorProgressFromManager();
        }

        private float GetFloorProgressFromManager()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
                if (gameManager == null) return 0f;
            }
            return gameManager.GetFloorProgress();
        }

        private float GetNodeProgress()
        {
            var mapGen = FindObjectOfType<Map.MapGenerator>();
            if (mapGen == null || mapGen.CurrentNode == null) return 0f;

            int totalRows = 8;
            var mapConfig = Resources.Load<Map.MapConfig>("MapConfig");
            if (mapConfig != null)
                totalRows = Mathf.Max(1, mapConfig.rows);

            return Mathf.Clamp01((float)mapGen.CurrentNode.point.y / totalRows);
        }

        private void HandleBossVictory()
        {
            BossRewardService bossService = BossRewardService.Instance;
            if (bossService == null || bossService.Config == null)
            {
 GameLogger.LogWarning("[BattleManager] BossRewardService ");
                HandleNormalVictory(EnemyType.Boss);
                return;
            }

            var bossReward = bossService.GenerateBossRewards();

 // BOSS
            int baseBossGold = bossReward.goldAmount;
            float goldMultiplier = 1f;
            if (gameManager != null)
                goldMultiplier = gameManager.GetGoldMultiplier();
            int finalBossGold = Mathf.RoundToInt(baseBossGold * goldMultiplier);
 GameLogger.Log($"[BattleManager] BOSS??? - :{baseBossGold} :{goldMultiplier:F2} :{finalBossGold}");

            pendingRelicReward = bossReward.factionUnlockRelic;
            pendingBonusRelic = bossReward.bonusRelic;
            pendingBossFactionCard = bossReward.factionCard;
            bossRewardGold = finalBossGold;

 // Boss GeneratePotion TryDropPotion??
            pendingPotionReward = null;
            var potionService = PotionDropService.Instance;
            if (potionService != null && gameManager != null)
            {
                int currentFloor = gameManager.GetCurrentFloor();
                int maxFloor = gameManager.GetMaxFloor();
                float nodeProgress = 1f;
                pendingPotionReward = potionService.GeneratePotion(currentFloor, maxFloor, nodeProgress);
            }

            List<Card> cardRewards = GetRewardCardsForEnemy(EnemyType.Boss);

            if (bossReward.factionCard != null)
            {
                cardRewards.Add(bossReward.factionCard);
            }

            rewardPanel.ShowRewards(
                finalBossGold,
                cardRewards,
                bossReward.factionUnlockRelic,
                pendingPotionReward,
                OnRewardsConfirmed,
                OnPanelClosed
            );
        }

        private void HandleNormalVictory(EnemyType enemyType)
        {
            int goldReward = 0;
            if (gameManager != null)
            {
                Vector2Int goldRange = gameManager.GetGoldRangeForEnemy(enemyType);
                int baseGold = UnityEngine.Random.Range(goldRange.x, goldRange.y + 1);
 // +15% ???
                float goldMultiplier = gameManager.GetGoldMultiplier();
                goldReward = Mathf.RoundToInt(baseGold * goldMultiplier);
 GameLogger.Log($"[BattleManager] - :{enemyType} :{baseGold} :{goldMultiplier:F2} :{goldReward}");
            }
            else
            {
                goldReward = UnityEngine.Random.Range(10, 30);
            }

            List<Card> cardRewards = GetRewardCardsForEnemy(enemyType);


            var relicMgrChk = RelicManager.Instance;
            if (relicMgrChk != null && enemyType == EnemyType.Elite && relicMgrChk.HasRelic("Shop_TreasureChest"))
            {
                List<Card> extraCards = GetRewardCardsForEnemy(enemyType);
                if (extraCards != null && extraCards.Count > 0)
                {
                    GameLogger.Log($"[BattleManager] ¶îÍâ¿¨ÅÆ½±Àø {extraCards.Count} ÕÅ");
                    cardRewards.AddRange(extraCards);
                }
            }

            pendingRelicReward = null;
            pendingPotionReward = null;
            var relicMgr = RelicManager.Instance;
            if (relicMgr != null)
            {
                switch (enemyType)
                {
                    case EnemyType.Normal:
                        pendingRelicReward = relicMgr.TryNormalMonsterRelicDrop();
                        break;
                    case EnemyType.Elite:
                        pendingRelicReward = relicMgr.GetEliteMonsterRelicDrop();
                        break;
                }
            }

 // 
            pendingPotionReward = TryDropPotion(enemyType);

            rewardPanel.ShowRewards(
                goldReward,
                cardRewards,
                pendingRelicReward,
                pendingPotionReward,
                OnRewardsConfirmed,
                OnPanelClosed
            );
        }

        private Potion TryDropPotion(EnemyType enemyType)
        {
 // Boss BossRewardService 
            if (enemyType == EnemyType.Boss) return null;

            var potionService = PotionDropService.Instance;
            if (potionService == null)
            {
 GameLogger.LogWarning("[BattleManager] PotionDropService ");
                return null;
            }

            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
                if (gameManager == null) return null;
            }

            int currentFloor = gameManager.GetCurrentFloor();
            int maxFloor = gameManager.GetMaxFloor();
            float nodeProgress = GetNodeProgress();

            return potionService.TryDropPotion(enemyType, currentFloor, maxFloor, nodeProgress);
        }

        private void OnRewardsConfirmed(int gold, Card card)
        {
            var dataManager = PlayerDataManager.Instance;

            Time.timeScale = 1f;

            if (gold > 0 && dataManager != null)
            {
                dataManager.AddGold(gold);
                AddLog($"??? {gold} ???");
            }

            if (card != null && dataManager != null)
            {
                dataManager.AddCardToDeck(card);
 AddLog($": {card.cardName}");
            }

            if (pendingRelicReward != null)
            {
                var relicManager = RelicManager.Instance;
                if (relicManager != null)
                {
                    relicManager.AddRelic(pendingRelicReward);
 AddLog($": {pendingRelicReward.relicName}");

                    if (pendingRelicReward.faction != CardFaction.None)
                    {
                        var unlockService = FactionUnlockService.Instance;
                        if (unlockService != null)
                        {
                            unlockService.UnlockFaction(pendingRelicReward.faction);
 AddLog($": {unlockService.GetFactionDisplayName(pendingRelicReward.faction)}");
                        }
                    }
                }
                pendingRelicReward = null;
            }

            if (pendingBonusRelic != null)
            {
                var relicManager = RelicManager.Instance;
                if (relicManager != null)
                {
                    relicManager.AddRelic(pendingBonusRelic);
 AddLog($": {pendingBonusRelic.relicName}");
                }
                pendingBonusRelic = null;
            }

            if (pendingPotionReward != null)
            {
                if (dataManager != null)
                {
                    bool added = dataManager.AddPotion(pendingPotionReward);
                    if (added)
                    {
 AddLog($": {pendingPotionReward.potionName}");
                    }
                    else
                    {
 // 
                        int refund = pendingPotionReward.price;
                        dataManager.AddGold(refund);
 AddLog($"{pendingPotionReward.potionName} {refund} ???");
                    }
                }
                pendingPotionReward = null;
            }

            pendingBossFactionCard = null;
            bossRewardGold = 0;

            OnBattleEnd?.Invoke(true);
            StartCoroutine(DelayedExit());
        }

        private void OnPanelClosed()
        {
            Time.timeScale = 1f;
        }

        IEnumerator DelayedExit()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            if (battlePanel != null) battlePanel.SetActive(false);
            if (handPanel != null) handPanel.SetActive(false);
            if (toggleViewButton != null) toggleViewButton.gameObject.SetActive(false);
            if (endTurnButton != null) endTurnButton.gameObject.SetActive(false);
            HideEnemyIntent();
            isViewingMap = false;
            isInBattle = false;
            isBattleEnding = false;

            if (rewardPanel != null)
                rewardPanel.ClosePanel();
        }

        void RefreshAllUI()
        {
            UpdatePlayerUI();
            UpdateEnemyUI();
            UpdatePlayerDebuffDisplay();
        }

        void UpdatePlayerUI()
        {
            if (playerHpText != null && playerData != null)
                playerHpText.text = $"{playerData.currentHealth}/{playerData.maxHealth}";

            if (playerBlockText != null)
            {
                playerBlockText.gameObject.SetActive(playerBlock > 0);
                if (playerBlock > 0)
                    playerBlockText.text = $"??: {playerBlock}";
            }
        }

        void UpdateEnemyUI()
        {
            if (enemyNameText != null && currentEnemy != null)
                enemyNameText.text = currentEnemy.enemyName;

            if (enemyHpText != null && currentEnemy != null)
            {
                enemyHpText.text = $"{currentEnemy.currentHealth}/{currentEnemy.maxHealth}";
            }
        }

        void UpdatePlayerDebuffDisplay()
        {
            if (playerDebuffText == null || playerData == null) return;

            List<string> debuffStrings = new List<string>();
            var buffs = playerData.GetBuffs();

            foreach (var buff in buffs)
            {
                if (buff.duration > 0)
                {
                    switch (buff.type)
                    {
                        case BuffType.Vulnerability:
 debuffStrings.Add($" {buff.amount}({buff.duration})");
                            break;
                        case BuffType.Weak:
 debuffStrings.Add($" {buff.amount}({buff.duration})");
                            break;
                        case BuffType.Frail:
 debuffStrings.Add($" {buff.amount}({buff.duration})");
                            break;
                        case BuffType.Poison:
                            debuffStrings.Add($"?? {buff.amount}({buff.duration})");
                            break;
                    }
                }
            }

            if (debuffStrings.Count > 0)
            {
                playerDebuffText.text = string.Join(" ", debuffStrings);
                playerDebuffText.gameObject.SetActive(true);
            }
            else
            {
                playerDebuffText.gameObject.SetActive(false);
            }
        }

        void AddLog(string msg)
        {
            if (battleLogText != null) battleLogText.text += msg + "\n";
            if (BattleLogManager.Instance != null) BattleLogManager.Instance.AddLog(msg);
        }

        public void AddBattleLog(string msg)
        {
            AddLog(msg);
        }

        public bool IsInBattle() => isInBattle;
        public bool IsViewingMap() => isViewingMap;
        public Enemy GetCurrentEnemy() => currentEnemy;
        public int GetPlayerBlock() => playerBlock;

        /// <summary>
 /// 
        /// </summary>
        public void ConsumePlayerBlock(int amount)
        {
            if (amount <= 0) return;
            int consumed = Mathf.Min(amount, playerBlock);
            playerBlock -= consumed;
 GameLogger.Log($"[BattleManager] {consumed} {playerBlock}");
            RefreshAllUI();
        }
        public PlayerData GetPlayerData() => playerData;

        /// <summary>


 /// ??? EffectTrigger.CalculatePlayerDamage /
        /// </summary>
        public int CalculatePlayerDamage(int baseDamage)
        {
            if (effectManager == null) return baseDamage;

            EffectContext ctx = new EffectContext(this);
            int finalDamage = effectManager.CalculateModifiedValue(
                EffectTrigger.CalculatePlayerDamage, ctx, baseDamage);

            if (finalDamage != baseDamage)
            {
 GameLogger.Log($"[BattleManager] : {baseDamage} ?? {finalDamage}");
            }

            return Mathf.Max(0, finalDamage);
        }
    }
}


