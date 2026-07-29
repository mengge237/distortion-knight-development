using UnityEngine;
using MutationChess.Map;
using MutationChess.Battle;
using MutationChess.Core;
using MutationChess.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Core References")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject playerPrefab;

    [Header("Battle")]
    [SerializeField] private BattleManager battleManager;

    [Header("Camera Follow")]
    [SerializeField] private CameraFollowController cameraFollow;

    [Header("Map View")]
    [SerializeField] private MapView mapView;

    [Header("Player Offset")]
    [SerializeField] private float playerYOffset = 0.5f;

    [Header("=== UI ===")]
    [SerializeField] private ShopPanel shopPanel;

    [Header("=== Gold Ranges ===")]
    [SerializeField] private Vector2Int commonGoldRange = new Vector2Int(10, 25);
    [SerializeField] private Vector2Int eliteGoldRange = new Vector2Int(20, 40);
    [SerializeField] private Vector2Int bossGoldRange = new Vector2Int(50, 80);

    [Header("=== Floor ===")]
    [SerializeField] private int startFloor = 1;
    [SerializeField] private int maxFloor = 3;
    [SerializeField] private float goldBonusPerFloor = 0.15f;
    [SerializeField] private float relicRarityBonusPerFloor = 0.15f;

    private bool isMoving = false;
    private bool isInBattle = false;
    private EnemyType currentEnemyType = EnemyType.Normal;
    private int currentFloor = 1;

    public event System.Action<int> OnFloorChanged;
    public event System.Action OnGameComplete;

    void Start()
    {
        //
        MutationChess.Debug.DebugConsole.EnsureExists();

        var dataManager = PlayerDataManager.Instance;
        if (dataManager == null)
        {
            GameLogger.LogError("[GameManager] PlayerDataManager instance not found. Please ensure there is a PlayerDataManager in the scene.");
            return;
        }

        // Override defaults from GameConfig if values still match originals (so Inspector overrides are respected)
        var config = GameConfig.Instance;
        if (config != null)
        {
            if (maxFloor == 3) maxFloor = config.maxFloor;
            if (Mathf.Approximately(goldBonusPerFloor, 0.15f)) goldBonusPerFloor = config.goldBonusPerFloor;
            if (Mathf.Approximately(relicRarityBonusPerFloor, 0.15f)) relicRarityBonusPerFloor = config.relicRarityBonusPerFloor;
        }

        currentFloor = Mathf.Max(1, startFloor);
        GameLogger.Log($"[GameManager] Start floor: {currentFloor}, max floor: {maxFloor}");

        if (mapGenerator == null)
            mapGenerator = FindObjectOfType<MapGenerator>();

        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();

        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CameraFollowController>();

        if (mapView == null)
            mapView = FindObjectOfType<MapView>();

        if (mapGenerator == null)
            GameLogger.LogError("[GameManager] MapGenerator instance not found. Please add a MapGenerator component to the scene.");

        if (mapGenerator != null)
        {
            mapGenerator.OnNodeClickedAction += OnNodeClicked;
            mapGenerator.OnNodeReached += OnNodeReached;
        }

        if (battleManager != null)
            battleManager.OnBattleEnd += OnBattleEnd;

        SetupPlayer();

        if (mapView != null)
            mapView.RefreshAllNodes();
    }

    void SetupPlayer()
    {
        Vector3 startPos = Vector3.zero;
        MapNode startNode = null;

        if (mapGenerator != null && mapGenerator.AllLayers.Count > 0)
        {
            startNode = mapGenerator.AllLayers[0][0];
            if (startNode.nodeObject != null)
            {
                startPos = mapGenerator.GetNodeWorldPosition(startNode);
                startPos.y += playerYOffset;
            }
        }

        if (cameraFollow != null && startNode != null)
        {
            cameraFollow.TeleportToNode(startNode);
        }
        else
        {
            Camera cam = Camera.main;
            if (cam != null && startNode != null && startNode.nodeObject != null)
            {
                Vector3 targetPos = startNode.position + new Vector3(0, 8, -8);
                cam.transform.position = targetPos;
                cam.transform.LookAt(startNode.position);
            }
        }

        if (playerController == null)
        {
            PlayerController existing = FindObjectOfType<PlayerController>();
            if (existing != null) playerController = existing;
            else if (playerPrefab != null)
            {
                GameObject go = Instantiate(playerPrefab, startPos, Quaternion.identity);
                playerController = go.GetComponent<PlayerController>();
                if (playerController == null) playerController = go.AddComponent<PlayerController>();
            }
        }
        if (playerController != null)
            playerController.TeleportToNode(startPos);
    }

    public void OnNodeClicked(MapNode node)
    {
        if (isMoving || isInBattle)
        {
            return;
        }

        if (node == null || !node.isReachable || node.isVisited)
        {
            return;
        }

        bool isValidMove = false;
        if (mapGenerator.CurrentNode != null)
        {
            foreach (var conn in mapGenerator.CurrentNode.connections)
            {
                if (conn == node)
                {
                    isValidMove = true;
                    break;
                }
            }
        }

        if (!isValidMove)
        {
            return;
        }

        isMoving = true;

        if (cameraFollow != null)
        {
            cameraFollow.MoveToNode(node, () =>
            {
                isMoving = false;
                mapGenerator.ConfirmReachNode(node);
                if (mapView != null)
                    mapView.RefreshAllNodes();

                if (playerController != null)
                {
                    Vector3 targetPos = mapGenerator.GetNodeWorldPosition(node);
                    targetPos.y += playerYOffset;
                    playerController.TeleportToNode(targetPos);
                }
            });
        }
        else
        {
            if (playerController != null)
            {
                Vector3 target = mapGenerator.GetNodeWorldPosition(node);
                target.y += playerYOffset;

                playerController.MoveToNode(target, () =>
                {
                    isMoving = false;
                    mapGenerator.ConfirmReachNode(node);
                    if (mapView != null)
                        mapView.RefreshAllNodes();
                });
            }
            else
            {
                isMoving = false;
                GameLogger.LogError("[GameManager] Failed to start node movement: PlayerController or CameraFollowController not assigned.");
            }
        }
    }

    void OnNodeReached(MapNode node)
    {
        if (mapView == null)
        {
            mapView = FindObjectOfType<MapView>();
            if (mapView != null)
                GameLogger.Log("[GameManager] OnNodeReached: late-assigned MapView");
        }

        if (mapGenerator == null)
        {
            mapGenerator = FindObjectOfType<MapGenerator>();
            if (mapGenerator != null)
                GameLogger.Log("[GameManager] OnNodeReached: late-assigned MapGenerator");
        }

        if (mapView != null)
            mapView.RefreshAllNodes();

        var dataManager = PlayerDataManager.Instance;
        if (dataManager == null) return;

        switch (node.nodeType)
        {
            case NodeType.NormalMonster:
                currentEnemyType = EnemyType.Normal;
                if (battleManager != null)
                {
                    isInBattle = true;
                    Enemy randomEnemy = GetRandomNormalEnemy();
                    battleManager.StartBattle(randomEnemy, dataManager.GetPlayerData());
                }
                else
                {
                    GameLogger.LogError("[GameManager] Cannot start normal battle: BattleManager not assigned in scene.");
                }
                break;

            case NodeType.EliteMonster:
                currentEnemyType = EnemyType.Elite;
                if (battleManager != null)
                {
                    isInBattle = true;
                    Enemy randomElite = GetRandomEliteEnemy();
                    battleManager.StartBattle(randomElite, dataManager.GetPlayerData());
                }
                else
                {
                    GameLogger.LogError("[GameManager] Cannot start elite battle: BattleManager not assigned in scene.");
                }
                break;

            case NodeType.Boss:
                currentEnemyType = EnemyType.Boss;
                if (battleManager != null)
                {
                    isInBattle = true;
                    battleManager.StartBattle(Enemy.CreateAbyssLord(), dataManager.GetPlayerData());
                }
                else
                {
                    GameLogger.LogError("[GameManager] Cannot start boss battle: BattleManager not assigned in scene.");
                }
                break;

            case NodeType.MysteryEvent:
                if (UnityEngine.Random.value < 0.5f)
                    dataManager.AddGold(UnityEngine.Random.Range(20, 51));
                else
                    dataManager.TakeDamage(UnityEngine.Random.Range(5, 13));
                break;

            case NodeType.Shop:
                GameLogger.Log("[GameManager] Entering shop node");
                if (shopPanel != null)
                {
                    GameLogger.Log("[GameManager] Opening ShopPanel...");
                    shopPanel.OpenShop();
                }
                else
                {
                    GameLogger.LogError("[GameManager] ShopPanel is not assigned. Please drag the Shop Panel object into the GameManager's shopPanel field in the Inspector.");
                }
                break;

            case NodeType.Treasure:
                dataManager.AddGold(50);
                break;

            case NodeType.Rest:
                dataManager.Heal(20);
                break;

            default:
                break;
        }
    }

    private Enemy GetRandomNormalEnemy()
    {
        int index = UnityEngine.Random.Range(0, 4);
        switch (index)
        {
            case 0:
                return Enemy.CreateCorruptedSoldier();
            case 1:
                return Enemy.CreateMutantHound();
            case 2:
                return Enemy.CreatePlagueAcolyte();
            case 3:
                return Enemy.CreateAbyssGrub();
            default:
                return Enemy.CreateCorruptedSoldier();
        }
    }

    private Enemy GetRandomEliteEnemy()
    {
        int index = UnityEngine.Random.Range(0, 4);
        switch (index)
        {
            case 0:
                return Enemy.CreateCorruptedKnight();
            case 1:
                return Enemy.CreateHellInquisitor();
            case 2:
                return Enemy.CreateVoidWizard();
            case 3:
                return Enemy.CreateCorruptedGolem();
            default:
                return Enemy.CreateCorruptedKnight();
        }
    }

    void OnBattleEnd(bool victory)
    {
        bool wasBossBattle = (currentEnemyType == EnemyType.Boss);
        isInBattle = false;
        var dataManager = PlayerDataManager.Instance;

        if (victory)
        {
            GameLogger.Log($"[GameManager] Battle won. Enemy type: {currentEnemyType}");

            //
            if (dataManager != null)
            {
                int healAmount = Mathf.RoundToInt(dataManager.GetPlayerData().maxHealth * 0.2f);
                dataManager.Heal(healAmount);
                GameLogger.Log($"[GameManager] {healAmount} ");
            }

            if (wasBossBattle)
            {
                AdvanceToNextFloor();
            }
        }
        else
        {
            GameLogger.Log("[GameManager] Battle lost.");
        }

        //
        if (mapView == null)
        {
            mapView = FindObjectOfType<MapView>();
            if (mapView != null)
                GameLogger.Log("[GameManager] OnBattleEnd: late-assigned MapView");
        }

        if (mapGenerator == null)
        {
            mapGenerator = FindObjectOfType<MapGenerator>();
            if (mapGenerator != null)
                GameLogger.Log("[GameManager] OnBattleEnd: late-assigned MapGenerator");
        }

        if (mapView != null)
            mapView.RefreshAllNodes();

        //
        if (dataManager != null)
            dataManager.UpdateUI();
    }

    public void AdvanceToNextFloor()
    {
        if (currentFloor >= maxFloor)
        {
            GameLogger.Log($"[GameManager] === All floors cleared! Max floor reached: {maxFloor} ===");
            OnGameComplete?.Invoke();
            return;
        }

        currentFloor++;
        GameLogger.Log($"[GameManager] === Advancing to floor {currentFloor}/{maxFloor} ===");
        OnFloorChanged?.Invoke(currentFloor);

        if (mapGenerator != null)
        {
            mapGenerator.GenerateMap();
            GameLogger.Log("[GameManager] New floor map regenerated.");

            SetupPlayer();

            if (mapView != null)
                mapView.RefreshAllNodes();
        }
        else
        {
            GameLogger.LogWarning("[GameManager] MapGenerator is null, cannot regenerate next floor map.");
        }
    }

    public int GetCurrentFloor() => currentFloor;
    public int GetMaxFloor() => maxFloor;
    public float GetFloorProgress() => Mathf.Clamp01((float)(currentFloor - 1) / Mathf.Max(1, maxFloor - 1));
    public float GetGoldBonusPerFloor() => goldBonusPerFloor;

    public float GetGoldMultiplier()
    {
        return 1f + (currentFloor - 1) * goldBonusPerFloor;
    }

    public Vector2Int GetGoldRangeForEnemy(EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.Normal:
                return commonGoldRange;
            case EnemyType.Elite:
                return eliteGoldRange;
            case EnemyType.Boss:
                return bossGoldRange;
            default:
                return commonGoldRange;
        }
    }

    public bool IsInBattle() => isInBattle;
    public bool IsMoving() => isMoving;
    public EnemyType GetCurrentEnemyType() => currentEnemyType;
}

