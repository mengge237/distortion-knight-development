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

        [Header("UI引用")]
        [SerializeField] private GameObject battlePanel;
        [SerializeField] private GameObject handPanel;

        [Header("视图切换")]
        [SerializeField] private Button toggleViewButton;

        [Header("回合结束")]
        [SerializeField] private Button endTurnButton;

        [Header("敌人意图")]
        [SerializeField] private EnemyIntentUI enemyIntentUI;

        [Header("敌人名称")]
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text enemyHpText;
        [SerializeField] private Image enemyImage;

        [Header("玩家血量")]
        [SerializeField] private TMP_Text playerHpText;
        [SerializeField] private TMP_Text playerBlockText;
        [SerializeField] private Image playerImage;

        [Header("战斗日志")]
        [SerializeField] private TMP_Text battleLogText;
        [SerializeField] private BattleIntroUI battleIntroUI;

        [Header("Debuff")]
        [SerializeField] private TMP_Text playerDebuffText;

        [Header("行动提示")]
        [SerializeField] private TMP_Text actionHintText;

        [Header("奖励面板")]
        [SerializeField] private RewardPanel rewardPanel;
        [SerializeField] private BossRelicChoicePanel bossRelicChoicePanel;

        [Header("背景图片")]
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
                GameLogger.LogWarning("BattleManager: 缺少 GameManager");
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

            Sprite playerSprite = Resources.Load<Sprite>(ResourcePaths.PlayerSprites_Player);

            if (playerSprite == null)
            {
                playerSprite = Resources.Load<Sprite>(ResourcePaths.Player_player);
            }

            if (playerSprite != null)
            {
                playerImage.sprite = playerSprite;
                playerImage.gameObject.SetActive(true);
            }
            else
            {
                playerImage.gameObject.SetActive(false);
                GameLogger.LogWarning("玩家图片未找到");
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
                actionHintText.text = "出牌或结束回合";
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
                actionHintText.text = "敌人行动中...";
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
                GameLogger.LogWarning("EnemyIntentUI 未找到");
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

            if (playerBlock > 0)
            {
                int blocked = Mathf.Min(dmg, playerBlock);
                AddLog($"{currentEnemy.enemyName} 攻击！玩家格挡抵挡 {blocked} 点伤害");
            }

            int actual = Mathf.Max(0, dmg - playerBlock);
            playerBlock = 0;

            if (actual > 0)
            {
                actual = CalculatePlayerDamage(actual);
                playerData.TakeDamage(actual);
                AddLog($"{currentEnemy.enemyName} 对玩家造成 {actual} 点伤害");
                currentEnemy.PlayAttack();
            }
            else
            {
                AddLog("玩家完全格挡了本次攻击");
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
            AddLog($"{currentEnemy.enemyName} 防御并回复 {healAmount} 点生命");
            currentEnemy.PlayIdle();

            RefreshAllUI();
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator ExecuteSpecial()
        {
            int specialDmg = currentIntentValue;

            if (playerBlock > 0)
            {
                int blocked = Mathf.Min(specialDmg / 2, playerBlock);
                AddLog($"{currentEnemy.enemyName} 释放特殊攻击！玩家格挡抵挡 {blocked} 点伤害");
            }

            int actualSpecial = Mathf.Max(0, specialDmg - playerBlock / 2);
            playerBlock = 0;

            if (actualSpecial > 0)
            {
                actualSpecial = CalculatePlayerDamage(actualSpecial);
                playerData.TakeDamage(actualSpecial);
                AddLog($"{currentEnemy.enemyName} 的特殊攻击对玩家造成 {actualSpecial} 点伤害");
                currentEnemy.PlayAttack();
            }
            else
            {
                AddLog("玩家完全抵挡了本次特殊攻击");
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
            AddLog($"{currentEnemy.enemyName} 获得 {buffAmount} 点力量（攻击力提升）");
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
                    actionHintText.text = (turnManager != null && turnManager.IsPlayerTurn) ? "出牌或结束回合" : "敌人行动中...";
                    actionHintText.color = (turnManager != null && turnManager.IsPlayerTurn) ? Color.white : Color.gray;
                }
                AddLog("切换到战斗视图");
            }
            else
            {
                ShowMapView();
                AddLog("正在加载地图...");
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

            // 战斗边界重置：清掉上一场战斗泄漏的永久修正/Boss标志/计数器
            // （ResetTemporary 仍由 TurnManager 每回合调用；Boss标志由本次 BattleStart 重新设置）
            ConversionModifier.ResetAll();

            // RelicManager 战斗开始回调
            var relicManager = RelicManager.Instance;
            if (relicManager != null)
            {
                relicManager.OnBattleStart();

                // 嗜血/幻痛诅咒：每场战斗开始结算（反咒之镜反转后变为回血）
                PlayerDataManager pdm = PlayerDataManager.Instance;
                if (pdm != null)
                {
                    CurseMode bloodMode = CurseSystem.GetCurseMode(RelicIds.Curse_Bloodthirst);
                    if (bloodMode == CurseMode.Active)
                    {
                        int hpLoss = Mathf.Min(2, Mathf.Max(0, pdm.GetHealth() - 1));
                        if (hpLoss > 0)
                        {
                            pdm.TakeDamage(hpLoss, true);
                            GameLogger.Log($"[BattleManager] 嗜血诅咒：战斗开始损失 {hpLoss} 点生命");
                        }
                    }
                    else if (bloodMode == CurseMode.Inverted)
                    {
                        pdm.Heal(2);
                        GameLogger.Log("[BattleManager] 嗜血诅咒反转：战斗开始恢复 2 点生命");
                    }

                    CurseMode painMode = CurseSystem.GetCurseMode(RelicIds.Curse_PhantomPain);
                    if (painMode == CurseMode.Active)
                    {
                        int hpLoss = Mathf.Min(3, Mathf.Max(0, pdm.GetHealth() - 1));
                        if (hpLoss > 0)
                        {
                            pdm.TakeDamage(hpLoss, true);
                            GameLogger.Log($"[BattleManager] 幻痛诅咒：战斗开始受到 {hpLoss} 点伤害");
                        }
                    }
                    else if (painMode == CurseMode.Inverted)
                    {
                        pdm.Heal(3);
                        GameLogger.Log("[BattleManager] 幻痛诅咒反转：战斗开始恢复 3 点生命");
                    }
                }
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
                }
            }

            SetBattleBackground(currentEnemy.enemyType, currentEnemy.enemyName);
            ShowBattleView();

            if (battleLogText != null) battleLogText.text = "";
            if (actionHintText != null)
            {
                actionHintText.text = "出牌或结束回合";
                actionHintText.color = Color.white;
            }

            RefreshAllUI();

            AddLog("=== 战斗开始 ===");
            string encounterName = string.IsNullOrEmpty(currentEnemy.enemyName) ? currentEnemy.data?.aiPatternName ?? "Unknown" : currentEnemy.enemyName;
            AddLog("遭遇: " + encounterName);
            AddLog("玩家生命: " + playerData.currentHealth + "/" + playerData.maxHealth);
            AddLog("敌人生命: " + currentEnemy.currentHealth + "/" + currentEnemy.maxHealth);

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
                GameLogger.LogWarning("背景图片组件未设置");
                return;
            }

            if (backgroundConfig == null)
            {
                GameLogger.LogWarning("背景配置未设置");
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
                GameLogger.LogWarning($"未找到 {enemyName} 的背景图片");
            }
        }

        void OnPlayerAction(string action)
        {
            if (!waitingForPlayerInput || isBattleEnding || isViewingMap || isEnemyTurn) return;
            waitingForPlayerInput = false;

            switch (action)
            {
                case "attack":
                    int atkDmg = 8 + UnityEngine.Random.Range(0, 5);
                    AddLog($"玩家发动普通攻击");
                    PlayerAttack(atkDmg);
                    break;
                case "defend":
                    int block = 5 + UnityEngine.Random.Range(0, 4);
                    AddLog("玩家进入防御姿态");
                    PlayerBlock(block);
                    break;
                case "skill":
                    int skillDmg = 12 + UnityEngine.Random.Range(0, 6);
                    AddLog($"玩家释放技能，对敌人造成 {skillDmg} 点伤害");
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
                GameLogger.LogWarning($"PlayerAttack: 当前敌人不存在，攻击伤害={damage}");
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
                // 虚弱最多 4 层(-80%)，最终伤害保底 1，避免虚弱乘到 0 或负数
                finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage * (1 - weak * 0.2f)));
            }

            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(this);
                effectManager.Trigger(EffectTrigger.PlayerAttack, ctx);
            }

            currentEnemy.TakeDamage(finalDamage);

            AddLog($"玩家对 {currentEnemy.enemyName} 造成 {finalDamage} 点伤害");

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

            // 触发 EffectManager BlockLockNextTurnEffect 格挡锁定
            if (effectManager != null)
            {
                EffectContext ctx = new EffectContext(this);
                finalBlock = effectManager.CalculateModifiedValue(
                    EffectTrigger.CalculateBlock, ctx, finalBlock);
            }

            playerBlock += finalBlock;
            AddLog($"玩家获得 {finalBlock} 格挡 (总计格挡: {playerBlock})");

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
                    GameLogger.LogError("BattleManager: rewardPanel 未设置");
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

            Card attack = CardData.CreateCard((CardName)0);
            Card defend = CardData.CreateCard((CardName)1);
            Card bash = CardData.CreateCard((CardName)2);

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

            // 楼层进度 0~1
            float floorProgress = GetFloorProgressFromManager();

            float nodeProgress = GetNodeProgress();

            float rareFloorBonus = floorProgress * 0.20f;

            float rareNodeBonus = nodeProgress * 0.15f;
            float uncommonNodeBonus = nodeProgress * 0.10f;

            switch (enemyType)
            {
                case EnemyType.Elite:
                    // 精英怪稀有率 45% + 楼层/进度加成
                    rareChance = 0.45f + rareFloorBonus + rareNodeBonus;
                    uncommonChance = 0.35f + uncommonNodeBonus;
                    break;
                case EnemyType.Boss:
                    // Boss 必出稀有
                    rareChance = 1f;
                    uncommonChance = 0f;
                    break;
                default:
                    // 普通怪稀有率 8% + 楼层/进度加成
                    rareChance = 0.08f + rareFloorBonus + rareNodeBonus;
                    uncommonChance = 0.25f + uncommonNodeBonus;
                    break;
            }

            rareChance = Mathf.Clamp01(rareChance);
            uncommonChance = Mathf.Clamp01(uncommonChance);

            GameLogger.Log($"[BattleManager] 敌人:{enemyType} 楼层:{floorProgress:F2} 进度:{nodeProgress:F2} 稀有率:{rareChance:P0} 罕见率:{uncommonChance:P0}");

            var allCommon = poolConfig.GetColoredCardsByRarity(CardRarity.Common)
                .Where(a => a.faction == CardFaction.None || unlockService == null || unlockService.IsFactionUnlocked(a.faction))
                .ToList();
            var allUncommon = poolConfig.GetColoredCardsByRarity(CardRarity.Uncommon)
                .Where(a => a.faction == CardFaction.None || unlockService == null || unlockService.IsFactionUnlocked(a.faction))
                .ToList();
            var allRare = poolConfig.GetColoredCardsByRarity(CardRarity.Rare)
                .Where(a => a.faction == CardFaction.None || unlockService == null || unlockService.IsFactionUnlocked(a.faction))
                .ToList();

            // 同一批奖励中不允许出现相同的卡牌（按资产名去重）
            HashSet<string> usedCardAssets = new HashSet<string>();

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
                    var available = pool.Where(a => a != null && !usedCardAssets.Contains(a.name)).ToList();
                    if (available.Count == 0) available = pool; // 该稀有度池已抽完时回退到全池
                    var asset = available[UnityEngine.Random.Range(0, available.Count)];
                    usedCardAssets.Add(asset.name);
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
            var mapConfig = Resources.Load<Map.MapConfig>(ResourcePaths.MapConfig);
            if (mapConfig != null)
                totalRows = Mathf.Max(1, mapConfig.rows);

            return Mathf.Clamp01((float)mapGen.CurrentNode.point.y / totalRows);
        }

        private void HandleBossVictory()
        {
            BossRewardService bossService = BossRewardService.Instance;
            if (bossService == null)
            {
                GameLogger.LogWarning("[BattleManager] BossRewardService 未找到");
                HandleNormalVictory(EnemyType.Boss);
                return;
            }

            var bossReward = bossService.GenerateBossRewards();

            // BOSS 金币计算
            int baseBossGold = bossReward.goldAmount;
            float goldMultiplier = 1f;
            if (gameManager != null)
                goldMultiplier = gameManager.GetGoldMultiplier();
            int finalBossGold = Mathf.RoundToInt(baseBossGold * goldMultiplier);
            GameLogger.Log($"[BattleManager] BOSS金币 - 基础:{baseBossGold} 倍率:{goldMultiplier:F2} 最终:{finalBossGold}");

            pendingBonusRelic = bossReward.bonusRelic;
            pendingBossFactionCard = bossReward.factionCard;
            bossRewardGold = finalBossGold;

            // Boss 药水掉落
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
                // 与主奖励卡牌同名去重
                bool duplicated = cardRewards.Any(c => c != null && c.cardName == bossReward.factionCard.cardName);
                if (!duplicated)
                {
                    cardRewards.Add(bossReward.factionCard);
                }
                else
                {
                    GameLogger.Log($"[BattleManager] Boss阵营卡牌与主奖励重复，已跳过: {bossReward.factionCard.cardName}");
                }
            }

            // 第一步：优先弹出 Boss 遗物三选一面板，选择后再进入金币/卡牌奖励页
            List<Relic> relicChoices = bossService.GenerateBossRelicChoices(3);
            if (relicChoices.Count > 0)
            {
                pendingRelicReward = null;
                BossRelicChoicePanel choicePanel = GetBossRelicChoicePanel();
                if (choicePanel != null)
                {
                    GameLogger.Log($"[BattleManager] 打开 Boss 遗物选择面板（{relicChoices.Count} 个选项）");
                    choicePanel.Show(relicChoices, pickedRelic =>
                    {
                        GrantBossRelic(pickedRelic);
                        ShowBossRewardPanel(finalBossGold, cardRewards);
                    });
                    if (choicePanel.IsVisible)
                        return;
                }
            }

            // 回退：无可选遗物或面板缺失时，沿用旧的随机解锁遗物流程
            pendingRelicReward = bossReward.factionUnlockRelic;
            ShowBossRewardPanel(finalBossGold, cardRewards);
        }

        /// <summary>Boss 遗物选择后立即发放（含阵营解锁）；金币/卡牌/额外遗物/药水仍在奖励页确认时发放。</summary>
        private void GrantBossRelic(Relic relic)
        {
            if (relic == null) return;

            var relicManager = RelicManager.Instance;
            if (relicManager == null) return;

            relicManager.AddRelic(relic);
            AddLog($"获得 Boss 遗物: {relic.relicName}");

            if (relic.faction != CardFaction.None)
            {
                var unlockService = FactionUnlockService.Instance;
                if (unlockService != null)
                {
                    unlockService.UnlockFaction(relic.faction);
                    AddLog($"解锁阵营: {unlockService.GetFactionDisplayName(relic.faction)}");
                    AudioManager.Instance?.PlayFactionUnlocked();
                }
            }
        }

        /// <summary>Boss 奖励页（金币+卡牌+额外遗物+药水）：在遗物选择完成后弹出。</summary>
        private void ShowBossRewardPanel(int gold, List<Card> cards)
        {
            rewardPanel.ShowRewards(
                gold,
                cards,
                pendingRelicReward,
                pendingPotionReward,
                OnRewardsConfirmed,
                OnPanelClosed
            );
        }

        /// <summary>获取 Boss 遗物选择面板：场景接线优先，缺失时运行时自动构建。</summary>
        private BossRelicChoicePanel GetBossRelicChoicePanel()
        {
            if (bossRelicChoicePanel == null)
                bossRelicChoicePanel = BossRelicChoicePanel.Instance;
            if (bossRelicChoicePanel == null)
            {
                GameObject panelGo = new GameObject("BossRelicChoicePanel");
                bossRelicChoicePanel = panelGo.AddComponent<BossRelicChoicePanel>();
            }
            return bossRelicChoicePanel;
        }

        private void HandleNormalVictory(EnemyType enemyType)
        {
            int goldReward = 0;
            if (gameManager != null)
            {
                Vector2Int goldRange = gameManager.GetGoldRangeForEnemy(enemyType);
                int baseGold = UnityEngine.Random.Range(goldRange.x, goldRange.y + 1);
                // +15% 金币加成
                float goldMultiplier = gameManager.GetGoldMultiplier();
                goldReward = Mathf.RoundToInt(baseGold * goldMultiplier);
                GameLogger.Log($"[BattleManager] 敌人:{enemyType} 基础金币:{baseGold} 倍率:{goldMultiplier:F2} 最终金币:{goldReward}");
            }
            else
            {
                goldReward = UnityEngine.Random.Range(10, 30);
            }

            // 情报遗物基础效果 + 诅咒结算（常驻效果，与迷雾诅咒无关）
            RelicManager rmGold = RelicManager.Instance;
            if (rmGold != null)
            {
                if (rmGold.HasRelic(RelicIds.Shop_Compass))
                {
                    goldReward += 5;
                    GameLogger.Log("[BattleManager] 罗盘·司南引路：胜利金币 +5");
                }
                if (rmGold.HasRelic(RelicIds.Shop_StarChart))
                {
                    goldReward += 15;
                    GameLogger.Log("[BattleManager] 星图·星河巡礼：胜利金币 +15");
                }
                CurseMode rustMode = CurseSystem.GetCurseMode(RelicIds.Curse_Rust);
                if (rustMode == CurseMode.Active)
                {
                    int before = goldReward;
                    goldReward = Mathf.RoundToInt(goldReward * 0.8f);
                    GameLogger.Log($"[BattleManager] 锈蚀诅咒：胜利金币 {before} → {goldReward}");
                }
                else if (rustMode == CurseMode.Inverted)
                {
                    int before = goldReward;
                    goldReward = Mathf.RoundToInt(goldReward * 1.2f);
                    GameLogger.Log($"[BattleManager] 锈蚀诅咒反转：胜利金币 {before} → {goldReward}");
                }
            }

            List<Card> cardRewards = GetRewardCardsForEnemy(enemyType);

            var relicMgrChk = RelicManager.Instance;
            if (relicMgrChk != null && enemyType == EnemyType.Elite && relicMgrChk.HasRelic(RelicIds.Shop_TreasureChest))
            {
                List<Card> extraCards = GetRewardCardsForEnemy(enemyType);
                if (extraCards != null && extraCards.Count > 0)
                {
                    // 与主奖励做同名去重（宝箱额外奖励不追加重复卡牌）
                    int added = 0;
                    foreach (var extra in extraCards)
                    {
                        if (extra == null) continue;
                        if (cardRewards.Any(c => c != null && c.cardName == extra.cardName))
                        {
                            GameLogger.Log($"[BattleManager] 宝箱额外卡牌与主奖励重复，已跳过: {extra.cardName}");
                            continue;
                        }
                        cardRewards.Add(extra);
                        added++;
                    }
                    GameLogger.Log($"[BattleManager] 额外卡牌奖励 {added} 张");
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

            // 尝试掉落药水
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
            // Boss 药水由 BossRewardService 处理
            if (enemyType == EnemyType.Boss) return null;

            var potionService = PotionDropService.Instance;
            if (potionService == null)
            {
                GameLogger.LogWarning("[BattleManager] PotionDropService 未找到");
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
                AddLog($"获得 {gold} 金币");
            }

            if (card != null && dataManager != null)
            {
                dataManager.AddCardToDeck(card);
                AddLog($"获得卡牌: {card.cardName}");
            }

            if (pendingRelicReward != null)
            {
                var relicManager = RelicManager.Instance;
                if (relicManager != null)
                {
                    relicManager.AddRelic(pendingRelicReward);
                    AddLog($"获得遗物: {pendingRelicReward.relicName}");

                    if (pendingRelicReward.faction != CardFaction.None)
                    {
                        var unlockService = FactionUnlockService.Instance;
                        if (unlockService != null)
                        {
                            unlockService.UnlockFaction(pendingRelicReward.faction);
                            AddLog($"解锁阵营: {unlockService.GetFactionDisplayName(pendingRelicReward.faction)}");
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
                    AddLog($"获得额外遗物: {pendingBonusRelic.relicName}");
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
                        AddLog($"获得药水: {pendingPotionReward.potionName}");
                    }
                    else
                    {
                        int refund = pendingPotionReward.price;
                        dataManager.AddGold(refund);
                        AddLog($"药水瓶已满，{pendingPotionReward.potionName} 转换为 {refund} 金币");
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

        public void AddLog(string msg)
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
        /// 消耗玩家格挡值
        /// </summary>
        public void ConsumePlayerBlock(int amount)
        {
            if (amount <= 0) return;
            int consumed = Mathf.Min(amount, playerBlock);
            playerBlock -= consumed;
            GameLogger.Log($"[BattleManager] 消耗格挡 {consumed}，剩余 {playerBlock}");
            RefreshAllUI();
        }
        public PlayerData GetPlayerData() => playerData;

        /// <summary>
        /// 触发 EffectTrigger.CalculatePlayerDamage 伤害修正
        /// </summary>
        public int CalculatePlayerDamage(int baseDamage)
        {
            if (effectManager == null) return baseDamage;

            EffectContext ctx = new EffectContext(this);
            int finalDamage = effectManager.CalculateModifiedValue(
                EffectTrigger.CalculatePlayerDamage, ctx, baseDamage);

            // 幻影减伤：在遗物修饰器结算后统一扣除（见 PhantomAfter5CardsEffect）
            finalDamage = Mathf.Max(0, finalDamage - ConversionModifier.GetPhantomReduction());

            if (finalDamage != baseDamage)
            {
                GameLogger.Log($"[BattleManager] 伤害修正: {baseDamage} -> {finalDamage}");
            }

            return Mathf.Max(0, finalDamage);
        }
    }
}
