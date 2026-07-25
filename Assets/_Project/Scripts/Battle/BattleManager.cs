using MutationChess.Core;
using MutationChess.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace MutationChess.Battle
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [Header("=== UI面板 ===")]
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject handPanel;

        [Header("=== 切换视图按钮 ===")]
        [SerializeField] private Button toggleViewButton;

        [Header("=== 结束回合按钮 ===")]
        [SerializeField] private Button endTurnButton;

        [Header("=== 敌人意图 ===")]
        [SerializeField] private EnemyIntentUI enemyIntentUI;

        [Header("=== 敌人区域 ===")]
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private Image enemyImage;

        [Header("=== 玩家区域 ===")]
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text playerBlockText;
        [SerializeField] private Image playerImage;

        [Header("=== 战斗日志 ===")]
        [SerializeField] private TMP_Text battleLogText;
        [SerializeField] private BattleIntroUI battleIntroUI;

        [Header("=== 玩家Debuff显示 ===")]
        [SerializeField] private TMP_Text playerDebuffText;

        [Header("=== 操作提示 ===")]
        [SerializeField] private TMP_Text actionHintText;

        [Header("=== 战斗奖励 ===")]
        [SerializeField] private RewardPanel rewardPanel;

        [Header("=== 战斗背景 ===")]
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
                Debug.LogWarning("BattleManager: 未找到 GameManager！奖励面板可能无法显示。");
            }

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
                Debug.LogWarning("未找到玩家图片");
            }
        }

        void OnDestroy()
        {
            var turnManager = TurnManager.Instance;
            if (turnManager != null)
            {
                turnManager.OnPlayerTurnStart -= OnPlayerTurnStart;
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

            if (endTurnButton != null)
                endTurnButton.gameObject.SetActive(true);

            if (actionHintText != null)
            {
                actionHintText.text = "点击卡牌出牌 或 结束回合";
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

        void OnEnemyTurnStart()
        {
            waitingForPlayerInput = false;
            isEnemyTurn = true;

            if (endTurnButton != null)
                endTurnButton.gameObject.SetActive(false);

            if (actionHintText != null)
            {
                actionHintText.text = "敌人回合...";
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
                Debug.LogWarning("EnemyIntentUI 未设置！");
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
                    AddLog($"{currentEnemy.enemyName} 正在蓄力...");
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
                AddLog($"格挡抵消 {Mathf.Min(dmg, playerBlock)}");

            playerBlock = 0;

            if (actual > 0)
            {
                playerData.TakeDamage(actual);
                AddLog($"{currentEnemy.enemyName} 造成 {actual} 点伤害");
                currentEnemy.PlayAttack();
            }
            else
            {
                AddLog("攻击被完全格挡");
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
            AddLog($"{currentEnemy.enemyName} 恢复 {healAmount} 点生命");
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
                playerData.TakeDamage(actualSpecial);
                AddLog($"{currentEnemy.enemyName} 造成 {actualSpecial} 点特殊伤害");
                currentEnemy.PlayAttack();
            }
            else
            {
                AddLog("特殊攻击被部分格挡");
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
            AddLog($"{currentEnemy.enemyName} 攻击力提升 {buffAmount}");
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
                    actionHintText.text = (turnManager != null && turnManager.IsPlayerTurn) ? "点击卡牌出牌 或 结束回合" : "敌人回合...";
                    actionHintText.color = (turnManager != null && turnManager.IsPlayerTurn) ? Color.white : Color.gray;
                }
                AddLog("返回战斗");
            }
            else
            {
                ShowMapView();
                AddLog("查看地图");
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
                    Debug.LogWarning("Enemy has no image: " + currentEnemy.enemyName);
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
                Debug.LogWarning("背景图片组件未设置");
                return;
            }

            if (backgroundConfig == null)
            {
                Debug.LogWarning("背景配置未设置");
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
                Debug.LogWarning($"未找到敌人 {enemyName} 对应的背景图片");
            }
        }

        void OnPlayerAction(string action)
        {
            if (!waitingForPlayerInput || isBattleEnding || isViewingMap || isEnemyTurn) return;
            waitingForPlayerInput = false;

            switch (action)
            {
                case "attack":
                    AddLog("玩家攻击");
                    PlayerAttack(8 + UnityEngine.Random.Range(0, 5));
                    break;
                case "defend":
                    int block = 5 + UnityEngine.Random.Range(0, 4);
                    PlayerBlock(block);
                    break;
                case "skill":
                    int skillDmg = 12 + UnityEngine.Random.Range(0, 6);
                    AddLog("玩家使用技能 -" + skillDmg + "HP");
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
                Debug.LogWarning($"PlayerAttack: currentEnemy 为空！伤害 {damage} 未应用");
                waitingForPlayerInput = true;
                return;
            }

            if (currentEnemy.IsDead())
            {
                EndBattle(true);
                return;
            }

            int finalDamage = Mathf.Max(1, damage + UnityEngine.Random.Range(-1, 2));

            int weak = playerData.GetBuffAmount(BuffType.Weak);
            if (weak > 0)
            {
                finalDamage = Mathf.RoundToInt(finalDamage * (1 - weak * 0.2f));
            }

            currentEnemy.TakeDamage(finalDamage);

            AddLog($"玩家造成 {finalDamage} 点伤害");

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
            playerBlock += finalBlock;
            AddLog($"玩家获得 {finalBlock} 点格挡 (累计: {playerBlock})");

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

            AddLog(victory ? "=== 胜利 ===" : "=== 失败 ===");

            if (actionHintText != null)
            {
                actionHintText.text = victory ? "胜利!" : "失败...";
                actionHintText.color = victory ? Color.green : Color.red;
            }

            if (endTurnButton != null)
                endTurnButton.gameObject.SetActive(false);

            HideEnemyIntent();

            var handManager = HandManager.Instance;
            if (handManager != null) handManager.EndBattle();

            var dataManager = PlayerDataManager.Instance;
            if (dataManager != null)
                dataManager.UpdateUI();

            if (victory)
            {
                if (rewardPanel == null)
                {
                    Debug.LogError("BattleManager: rewardPanel 未设置！");
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

                if (gameManager == null)
                {
                    Debug.LogError("BattleManager: 无法找到 GameManager！使用默认奖励。");
                    int defaultGold = UnityEngine.Random.Range(10, 30);
                    List<Card> defaultCards = GetDefaultCardRewards();

                    rewardPanel.ShowRewards(
                        defaultGold,
                        defaultCards,
                        OnRewardsConfirmed,
                        OnPanelClosed
                    );

                    Time.timeScale = 0f;
                    return;
                }

                EnemyType enemyType = gameManager.GetCurrentEnemyType();

                Vector2Int goldRange = gameManager.GetGoldRangeForEnemy(enemyType);
                int goldReward = UnityEngine.Random.Range(goldRange.x, goldRange.y + 1);

                RewardPool pool = gameManager.GetRewardPoolForEnemy(enemyType);
                List<Card> cardRewards = new List<Card>();

                if (pool != null)
                {
                    cardRewards = pool.GetRewards();
                }
                else
                {
                    Debug.LogWarning("RewardPool 为空，使用默认卡牌");
                    cardRewards = GetDefaultCardRewards();
                }

                rewardPanel.ShowRewards(
                    goldReward,
                    cardRewards,
                    OnRewardsConfirmed,
                    OnPanelClosed
                );

                Time.timeScale = 0f;
                return;
            }

            OnBattleEnd?.Invoke(victory);
            StartCoroutine(DelayedExit());
        }

        private List<Card> GetDefaultCardRewards()
        {
            List<Card> result = new List<Card>();

            Card attack = CardData.CreateCard(CardName.攻击);
            Card defend = CardData.CreateCard(CardName.防御);
            Card bash = CardData.CreateCard(CardName.痛击);

            if (attack != null) result.Add(attack);
            if (defend != null) result.Add(defend);
            if (bash != null) result.Add(bash);

            return result;
        }

        private void OnRewardsConfirmed(int gold, Card card)
        {
            var dataManager = PlayerDataManager.Instance;

            Time.timeScale = 1f;

            if (gold > 0 && dataManager != null)
            {
                dataManager.AddGold(gold);
                AddLog($"获得 {gold} 金币");
            }

            if (card != null && dataManager != null)
            {
                dataManager.AddCardToDeck(card);
                AddLog($"获得卡牌: {card.cardName}");
            }

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
                    playerBlockText.text = $"格挡: {playerBlock}";
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
                            debuffStrings.Add($"易伤 {buff.amount}({buff.duration})");
                            break;
                        case BuffType.Weak:
                            debuffStrings.Add($"虚弱 {buff.amount}({buff.duration})");
                            break;
                        case BuffType.Frail:
                            debuffStrings.Add($"脆弱 {buff.amount}({buff.duration})");
                            break;
                        case BuffType.Poison:
                            debuffStrings.Add($"中毒 {buff.amount}({buff.duration})");
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

        public bool IsInBattle() => isInBattle;
        public bool IsViewingMap() => isViewingMap;
        public Enemy GetCurrentEnemy() => currentEnemy;
        public int GetPlayerBlock() => playerBlock;
    }
}
