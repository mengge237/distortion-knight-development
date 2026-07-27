using UnityEngine;
using MutationChess.Map;
using MutationChess.Battle;
using MutationChess.Core;
using MutationChess.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("核心属性")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject playerPrefab;

    [Header("战斗")]
    [SerializeField] private BattleManager battleManager;

    [Header("摄像机跟随")]
    [SerializeField] private CameraFollowController cameraFollow;

    [Header("地图视图")]
    [SerializeField] private MapView mapView;

    [Header("玩家参数")]
    [SerializeField] private float playerYOffset = 0.5f;

    [Header("=== 奖励池组件 (Inspector中设置) ===")]
    [SerializeField] private RewardPool commonRewardPool;
    [SerializeField] private RewardPool eliteRewardPool;
    [SerializeField] private RewardPool bossRewardPool;

    [Header("=== 商店 ===")]
    [SerializeField] private ShopPanel shopPanel;

    [Header("=== 金币掉落范围 ===")]
    [SerializeField] private Vector2Int commonGoldRange = new Vector2Int(10, 25);
    [SerializeField] private Vector2Int eliteGoldRange = new Vector2Int(20, 40);
    [SerializeField] private Vector2Int bossGoldRange = new Vector2Int(50, 80);

    private bool isMoving = false;
    private bool isInBattle = false;
    private EnemyType currentEnemyType = EnemyType.Normal;

    void Start()
    {
        var dataManager = PlayerDataManager.Instance;
        if (dataManager == null)
        {
            Debug.LogError("PlayerDataManager 未设置！请在场景中添加 PlayerDataManager 组件");
            return;
        }

        RewardPoolManager.InitializeAllPools(commonRewardPool, eliteRewardPool, bossRewardPool);

        if (mapGenerator == null)
            mapGenerator = FindObjectOfType<MapGenerator>();

        if (battleManager == null)
            battleManager = FindObjectOfType<BattleManager>();

        if (cameraFollow == null)
            cameraFollow = FindObjectOfType<CameraFollowController>();

        if (mapView == null)
            mapView = FindObjectOfType<MapView>();

        if (mapGenerator == null)
            Debug.LogError("[GameManager] MapGenerator 未设置！请在场景中搜索 MapGenerator 对象...");

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
                Debug.LogError("场景中找不到PlayerController 或 CameraFollowController 组件");
            }
        }
    }

    void OnNodeReached(MapNode node)
    {
        if (mapView == null)
        {
            mapView = FindObjectOfType<MapView>();
            if (mapView != null)
                Debug.Log("[GameManager] 进入场景 切换到 MapView");
        }

        if (mapGenerator == null)
        {
            mapGenerator = FindObjectOfType<MapGenerator>();
            if (mapGenerator != null)
                Debug.Log("[GameManager] 切换到场景 MapGenerator");
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
                    Debug.LogError("BattleManager 为空！场景中必须有 BattleManager 组件");
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
                    Debug.LogError("BattleManager 为空！场景中必须有 BattleManager 组件");
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
                    Debug.LogError("BattleManager 为空！场景中必须有 BattleManager 组件");
                }
                break;

            case NodeType.MysteryEvent:
                if (Random.value < 0.5f)
                    dataManager.AddGold(Random.Range(20, 51));
                else
                    dataManager.TakeDamage(Random.Range(5, 13));
                break;

            case NodeType.Shop:
                Debug.Log("[GameManager] 进入战斗模式");
                if (shopPanel != null)
                {
                    Debug.Log("[GameManager] 正在打开 ShopPanel...");
                    shopPanel.OpenShop();
                }
                else
                {
                    Debug.LogError("[GameManager] ShopPanel 未设置！请在 Inspector 中给 GameManager 的 Shop Panel 字段拖入 ShopPanel 对象");
                }
                break;

            case NodeType.Treasure:
                dataManager.AddGold(50);
                break;

            case NodeType.Rest:
                dataManager.Heal(20);
                break;

            case NodeType.Start:
                break;

            default:
                break;
        }
    }

    private Enemy GetRandomNormalEnemy()
    {
        int index = Random.Range(0, 4);
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
        int index = Random.Range(0, 4);
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
        isInBattle = false;
        var dataManager = PlayerDataManager.Instance;

        if (victory)
        {
        }
        else
        {
            Debug.Log("游戏结束");
        }

        if (mapView == null)
        {
            mapView = FindObjectOfType<MapView>();
            if (mapView != null)
                Debug.Log("[GameManager] 游戏结束 切换回场景 MapView");
        }

        if (mapGenerator == null)
        {
            mapGenerator = FindObjectOfType<MapGenerator>();
            if (mapGenerator != null)
                Debug.Log("[GameManager] 游戏结束 切换回场景 MapGenerator");
        }

        if (mapView != null)
            mapView.RefreshAllNodes();
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

    public RewardPool GetRewardPoolForEnemy(EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.Normal:
                return commonRewardPool;
            case EnemyType.Elite:
                return eliteRewardPool;
            case EnemyType.Boss:
                return bossRewardPool;
            default:
                return commonRewardPool;
        }
    }

    public bool IsInBattle() => isInBattle;
    public bool IsMoving() => isMoving;
    public EnemyType GetCurrentEnemyType() => currentEnemyType;
}
