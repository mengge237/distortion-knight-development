using UnityEngine;
using MutationChess.Map;
using MutationChess.Battle;
using MutationChess.Core;
using MutationChess.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("地图与玩家")]
    [SerializeField] private MapGenerator mapGenerator;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject playerPrefab;

    [Header("战斗")]
    [SerializeField] private BattleManager battleManager;

    [Header("视图")]
    [SerializeField] private MapView mapView;

    [Header("玩家偏移")]
    [SerializeField] private float playerYOffset = 0.5f;

    [Header("=== 卡牌奖励池（拖入 Inspector） ===")]
    [SerializeField] private RewardPool commonRewardPool;
    [SerializeField] private RewardPool eliteRewardPool;
    [SerializeField] private RewardPool bossRewardPool;

    [Header("=== 金币奖励范围 ===")]
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
            Debug.LogError("PlayerDataManager 未找到！请在场景中添加");
            return;
        }

        RewardPoolManager.InitializeAllPools(commonRewardPool, eliteRewardPool, bossRewardPool);

        if (mapGenerator == null)
            mapGenerator = FindObjectOfType<MapGenerator>();

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
        if (mapGenerator != null && mapGenerator.AllLayers.Count > 0)
        {
            var startNode = mapGenerator.AllLayers[0][0];
            if (startNode.nodeObject != null)
            {
                startPos = mapGenerator.GetNodeWorldPosition(startNode);
                startPos.y += playerYOffset;
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

        if (playerController == null)
        {
            Debug.LogError("PlayerController 为空");
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
        Vector3 target = mapGenerator.GetNodeWorldPosition(node);
        target.y += playerYOffset;

        if (playerController != null)
        {
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
            Debug.LogError("无法移动：PlayerController 为空");
        }
    }

    void OnNodeReached(MapNode node)
    {
        if (mapView != null)
            mapView.RefreshAllNodes();

        var dataManager = PlayerDataManager.Instance;

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
                break;
            case NodeType.EliteMonster:
                currentEnemyType = EnemyType.Elite;
                if (battleManager != null)
                {
                    isInBattle = true;
                    Enemy randomElite = GetRandomEliteEnemy();
                    battleManager.StartBattle(randomElite, dataManager.GetPlayerData());
                }
                break;
            case NodeType.Boss:
                currentEnemyType = EnemyType.Boss;
                if (battleManager != null)
                {
                    isInBattle = true;
                    battleManager.StartBattle(Enemy.CreateAbyssLord(), dataManager.GetPlayerData());
                }
                break;
            case NodeType.MysteryEvent:
                if (Random.value < 0.5f)
                    dataManager.AddGold(Random.Range(20, 51));
                else
                    dataManager.TakeDamage(Random.Range(5, 13));
                break;
            case NodeType.Shop:
                if (dataManager.RemoveGold(50))
                    dataManager.Heal(20);
                break;
            case NodeType.Treasure:
                dataManager.AddGold(50);
                break;
            case NodeType.Rest:
                dataManager.Heal(20);
                break;
            case NodeType.Start:
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
