using System.Collections.Generic;
using MutationChess.Core;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MutationChess.Map
{
    [System.Serializable]
    public class MapTextureListEntry
    {
        public NodeType nodeType;
        public List<Texture2D> textures = new List<Texture2D>();
    }

    public class MapGenerator : MonoBehaviour
    {
        [Header("地图布局参数")]
        [SerializeField] private int rows = 8;
        [SerializeField] private int maxNodesPerRow = 4;
        [SerializeField] private float horizontalSpacing = 3.0f;
        [SerializeField] private float verticalSpacing = 4.0f;
        [SerializeField] private float nodeYOffset = -0.5f;
        [SerializeField] private float positionOffsetX = 0.8f;
        [SerializeField] private float positionOffsetY = 0.3f;
        [SerializeField] private int extraBranches = 2;

        [Header("节点预制体")]
        [SerializeField] private GameObject nodePrefab;

        [Header("连线材质")]
        [SerializeField] private Material lineMaterial;

        [Header("节点蓝图")]
        [SerializeField] private List<NodeBlueprint> nodeBlueprints = new List<NodeBlueprint>();

        [Header("地图显示设置")]
        [SerializeField] private bool enableMapDisplay = true;
        [SerializeField] private List<MapTextureListEntry> mapTextureLists = new List<MapTextureListEntry>();
        [SerializeField] private Vector3 mapDisplayOffset = new Vector3(0, -0.35f, 0);
        [SerializeField] private float mapDisplayScale = 0.9f;
        [SerializeField] private Color mapTintColor = new Color(1, 1, 1, 0.8f);

        [Header("地图风格化设置")]
        [SerializeField] private bool enablePseudo3DTilt = true;
        [SerializeField] private float mapTiltAngleX = 22f;
        [SerializeField] private bool enableFogOfWar = true;
        [SerializeField] private bool hideNodeMeshOnMap = true;

        [Header("特殊层设置")]
        [SerializeField] private bool bossLayerHasRestBefore = true;
        [SerializeField] private int treasureLayerIndex = 6;

        private List<List<MapNode>> allLayers = new List<List<MapNode>>();
        private MapNode currentNode;
        private System.Random rand = new System.Random();

        private List<LineConnectionData> lineConnections = new List<LineConnectionData>();

        public MapNode CurrentNode => currentNode;
        public List<List<MapNode>> AllLayers => allLayers;
        public float NodeYOffset => nodeYOffset;

        public System.Action<MapNode> OnNodeReached;
        public System.Action<MapNode> OnNodeClickedAction;

        private Material defaultLineMaterial;
        private Material stylizedLineMaterial;
        private Transform contentRoot;
        private GameObject boardObject;
        private List<Renderer> fogRowObjects = new List<Renderer>();
        private Transform linesParent;
        private Dictionary<NodeType, NodeBlueprint> blueprintCache;
        private Dictionary<NodeType, List<Texture2D>> textureCache;

        void Start()
        {
            GameLogger.Log("=== MapGenerator.Start() 开始 ===");

            if (Camera.main != null && Camera.main.GetComponent<PhysicsRaycaster>() == null)
                Camera.main.gameObject.AddComponent<PhysicsRaycaster>();

            defaultLineMaterial = CreateDefaultLineMaterial();
            stylizedLineMaterial = CreateStylizedLineMaterial();
            EnsureContentRoot();


            BuildBlueprintCache();


            BuildTextureCache();

            GenerateMap();

            GameLogger.Log($"[MapGenerator] 地图显示: {(enableMapDisplay ? "启用" : "禁用")}");
            GameLogger.Log($"[MapGenerator] 蓝图缓存: {(blueprintCache != null ? blueprintCache.Count : 0)}");
            GameLogger.Log($"[MapGenerator] 纹理缓存: {(textureCache != null ? textureCache.Count : 0)}");

            if (blueprintCache != null)
            {
                foreach (var kvp in blueprintCache)
                {
                    GameLogger.Log($"  -  {kvp.Key}: {(kvp.Value.prefab != null ? kvp.Value.prefab.name : "")}");
                }
            }
            if (textureCache != null)
            {
                foreach (var kvp in textureCache)
                {
                    GameLogger.Log($"  -  {kvp.Key}: {kvp.Value.Count} ");
                }
            }
        }

        /// <summary>

        /// </summary>
        private void BuildBlueprintCache()
        {
            blueprintCache = new Dictionary<NodeType, NodeBlueprint>();

            if (nodeBlueprints != null && nodeBlueprints.Count > 0)
            {
                GameLogger.Log($"[MapGenerator]  {nodeBlueprints.Count} ");
                foreach (var bp in nodeBlueprints)
                {
                    if (bp != null && !blueprintCache.ContainsKey(bp.nodeType))
                    {
                        blueprintCache[bp.nodeType] = bp;
                        GameLogger.Log($"  - {bp.nodeType}: {bp.displayName} -> {(bp.prefab != null ? bp.prefab.name : "")}");
                    }
                }
            }
            else
            {
                GameLogger.Log("[MapGenerator] nodeBlueprints 未配置，使用默认颜色");
            }
        }

        /// <summary>

        /// </summary>
        private void BuildTextureCache()
        {
            textureCache = new Dictionary<NodeType, List<Texture2D>>();

            if (mapTextureLists != null && mapTextureLists.Count > 0)
            {
                GameLogger.Log($"[MapGenerator]  Inspector  {mapTextureLists.Count} ");
                foreach (var entry in mapTextureLists)
                {
                    if (entry.textures != null && entry.textures.Count > 0)
                    {
                        List<Texture2D> validTextures = entry.textures.Where(t => t != null).ToList();
                        if (validTextures.Count > 0)
                        {
                            textureCache[entry.nodeType] = validTextures;
                            GameLogger.Log($"  - {entry.nodeType}:  {validTextures.Count}  ( Inspector)");
                        }
                    }
                }
            }


            NodeType[] allTypes = System.Enum.GetValues(typeof(NodeType)) as NodeType[];
            foreach (NodeType type in allTypes)
            {
                if (!textureCache.ContainsKey(type) || textureCache[type] == null || textureCache[type].Count == 0)
                {
                    List<Texture2D> loadedTextures = LoadTexturesFromResources(type);
                    if (loadedTextures != null && loadedTextures.Count > 0)
                    {
                        textureCache[type] = loadedTextures;
                        GameLogger.Log($"  - {type}:  {loadedTextures.Count}  ( Resources)");
                    }
                    else
                    {
                        if (!textureCache.ContainsKey(type))
                            textureCache[type] = new List<Texture2D>();
                    }
                }
            }
        }

        /// <summary>

        /// </summary>
        private List<Texture2D> LoadTexturesFromResources(NodeType type)
        {
            List<Texture2D> result = new List<Texture2D>();
            string basePath = $"{ResourcePaths.MapTextures}/{type}";

            Texture2D[] allInFolder = Resources.LoadAll<Texture2D>($"{ResourcePaths.MapTextures}/{type}");
            if (allInFolder != null && allInFolder.Length > 0)
            {
                result.AddRange(allInFolder);
                return result;
            }

            Texture2D baseTex = Resources.Load<Texture2D>(basePath);
            if (baseTex != null)
                result.Add(baseTex);

            int index = 1;
            while (true)
            {
                string path = $"{basePath}_{index}";
                Texture2D tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                {
                    result.Add(tex);
                    index++;
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>

        /// </summary>
        private NodeBlueprint GetBlueprint(NodeType type)
        {
            if (blueprintCache != null && blueprintCache.ContainsKey(type))
                return blueprintCache[type];
            return null;
        }

        /// <summary>

        /// </summary>
        private GameObject GetNodePrefab(NodeType type)
        {
            NodeBlueprint bp = GetBlueprint(type);
            if (bp != null && bp.prefab != null)
                return bp.prefab;
            return nodePrefab;
        }

        /// <summary>

        /// </summary>
        private Color GetBlueprintColor(NodeType type)
        {
            NodeBlueprint bp = GetBlueprint(type);
            if (bp != null)
                return bp.color;
            return GetNodeTypeColor(type);
        }

        /// <summary>

        /// </summary>
        private Material GetBlueprintMaterial(NodeType type)
        {
            NodeBlueprint bp = GetBlueprint(type);
            if (bp != null && bp.material != null)
                return bp.material;
            return null;
        }

        /// <summary>

        /// </summary>
        private GameObject GetMapPrefabFromBlueprint(NodeType type)
        {
            NodeBlueprint bp = GetBlueprint(type);
            if (bp != null && bp.mapPrefab != null)
                return bp.mapPrefab;
            return null;
        }

        /// <summary>

        /// </summary>
        private Texture2D GetMapTextureFromBlueprint(NodeType type)
        {
            NodeBlueprint bp = GetBlueprint(type);
            if (bp != null && bp.mapTexture != null)
                return bp.mapTexture;
            return null;
        }

        private Material CreateDefaultLineMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null)
            {
                GameLogger.LogError("[MapGenerator] 所有 Shader 均未找到，线条将不可见！请将 URP/Unlit 添加到 Always Included Shaders。");
                shader = Shader.Find("Hidden/Internal-Colored");
            }
            Material mat = new Material(shader);
            mat.color = Color.white;
            return mat;
        }

        /// <summary>
        /// 创建手绘墨迹连线材质（邪恶冥刻风）。纹理缺失或 Shader 缺失时返回 null，走原版单色线回退。
        /// </summary>
        private Material CreateStylizedLineMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = defaultLineMaterial != null ? defaultLineMaterial.shader : null;
            if (shader == null) return null;

            Texture2D inkTex = Resources.Load<Texture2D>("MapTextures/Lines/墨迹连线");
            if (inkTex == null)
            {
                GameLogger.Log("[MapGenerator] 未找到墨迹连线纹理，使用单色连线");
                return null;
            }

            Material mat = new Material(shader);
            mat.mainTexture = inkTex;
            mat.color = new Color(1f, 1f, 1f, 0.9f);
            // 每条连线平铺 2 个纹理周期，横向笔刷连续不断裂
            mat.mainTextureScale = new Vector2(2f, 1f);
            return mat;
        }

        /// <summary>
        /// 伪 3D 内容根：整张地图（节点+连线+底板+迷雾）绕 X 轴倾斜，形成邪恶冥刻式透视。
        /// </summary>
        private void EnsureContentRoot()
        {
            if (!enablePseudo3DTilt) return;
            if (contentRoot == null)
            {
                contentRoot = new GameObject("MapContent").transform;
                contentRoot.SetParent(transform, false);
            }
            contentRoot.localRotation = Quaternion.Euler(mapTiltAngleX, 0f, 0f);
        }

        /// <summary>
        /// 羊皮纸底板：整幅大地图的背景画布，位于所有节点之后（renderQueue 1000）。
        /// </summary>
        private void CreateBoardVisual()
        {
            Texture2D boardTex = Resources.Load<Texture2D>("MapTextures/Board/羊皮纸底板");
            if (boardTex == null)
            {
                GameLogger.Log("[MapGenerator] 未找到羊皮纸底板纹理，跳过底板显示");
                return;
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return;

            float boardWidth = (maxNodesPerRow - 1) * horizontalSpacing + 7f;
            // 高度按实际内容行跨度计算（上下各留 3 单位边距），避免空边过大
            float boardHeight = (rows - 1) * verticalSpacing + 6f;
            float centerY = (rows - 1) * verticalSpacing / 2f + nodeYOffset;

            boardObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            boardObject.name = "Board_Parchment";
            Collider col = boardObject.GetComponent<Collider>();
            if (col != null) Destroy(col);
            boardObject.transform.SetParent(contentRoot != null ? contentRoot : transform, false);
            boardObject.transform.localPosition = new Vector3(0f, centerY, 0.5f);
            boardObject.transform.localRotation = Quaternion.identity;
            boardObject.transform.localScale = new Vector3(boardWidth, boardHeight, 1f);

            Material mat = new Material(shader);
            mat.mainTexture = boardTex;
            mat.color = new Color(1f, 1f, 1f, 0.85f);
            mat.renderQueue = 1000;
            Renderer renderer = boardObject.GetComponent<Renderer>();
            renderer.material = mat;
            renderer.enabled = true;
            GameLogger.Log("[MapGenerator] 羊皮纸底板已创建");
        }

        /// <summary>
        /// 迷雾遮罩：每行一张深色 Quad 覆盖未探索行（renderQueue 3060，压过节点图标）。
        /// 已到达行及下一行揭开（alpha 0），其余行保持 alpha 0.85。
        /// </summary>
        private void CreateFogOfWar()
        {
            ClearFogOfWar();

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) return;

            float fogWidth = (maxNodesPerRow - 1) * horizontalSpacing + 9f;
            float fogHeight = verticalSpacing * 0.95f;

            for (int row = 0; row < rows; row++)
            {
                float rowY = row * verticalSpacing + nodeYOffset;

                GameObject fog = GameObject.CreatePrimitive(PrimitiveType.Quad);
                fog.name = $"FogRow_{row}";
                Collider col = fog.GetComponent<Collider>();
                if (col != null) Destroy(col);
                fog.transform.SetParent(contentRoot != null ? contentRoot : transform, false);
                fog.transform.localPosition = new Vector3(0f, rowY, -0.35f);
                fog.transform.localRotation = Quaternion.identity;
                fog.transform.localScale = new Vector3(fogWidth, fogHeight, 1f);

                Material mat = new Material(shader);
                mat.color = new Color(0.04f, 0.03f, 0.05f, 0.85f);
                mat.renderQueue = 3060;
                Renderer r = fog.GetComponent<Renderer>();
                r.material = mat;
                fogRowObjects.Add(r);
            }

            UpdateFogOfWar();
        }

        /// <summary>按当前节点所在行揭开迷雾：到达行及下一行透明，其余保持遮罩。</summary>
        private void UpdateFogOfWar()
        {
            if (!enableFogOfWar || fogRowObjects == null) return;

            int revealedRow = 0;
            if (currentNode != null)
                revealedRow = currentNode.point.y + 1;

            for (int i = 0; i < fogRowObjects.Count; i++)
            {
                Renderer r = fogRowObjects[i];
                if (r == null) continue;
                Color c = r.material.color;
                c.a = i <= revealedRow ? 0f : 0.85f;
                r.material.color = c;
            }
        }

        /// <summary>销毁全部迷雾行（重新生成地图时调用）。</summary>
        private void ClearFogOfWar()
        {
            if (fogRowObjects == null) return;
            foreach (var r in fogRowObjects)
            {
                if (r != null)
                    Destroy(r.gameObject);
            }
            fogRowObjects.Clear();
        }

        public void ClearMap()
        {
            GameLogger.Log("[MapGenerator] ClearMap() 清空地图");

            foreach (var layer in allLayers)
            {
                if (layer == null) continue;
                foreach (var node in layer)
                {
                    if (node != null && node.nodeObject != null)
                    {
                        Destroy(node.nodeObject);
                        node.nodeObject = null;
                    }
                }
            }
            allLayers.Clear();
            lineConnections.Clear();
            currentNode = null;
        }

        public void GenerateMap()
        {
            GameLogger.Log("[MapGenerator] GenerateMap() 开始生成地图");

            ClearMap();

            allLayers.Clear();
            lineConnections.Clear();

            if (boardObject != null)
            {
                Destroy(boardObject);
                boardObject = null;
            }
            ClearFogOfWar();

            if (linesParent != null) Destroy(linesParent.gameObject);
            linesParent = new GameObject("Lines").transform;
            linesParent.SetParent(contentRoot != null ? contentRoot : transform, false);

            List<int> layerCounts = new List<int>();
            int maxCount = maxNodesPerRow;

            for (int row = 0; row < rows; row++)
            {
                int count;
                if (row == 0)
                    count = 1;
                else if (row == rows - 1)
                    count = 1;
                else
                {
                    float progress = (float)row / (rows - 1);
                    float curve = Mathf.Sin(progress * Mathf.PI);
                    count = Mathf.RoundToInt(1 + curve * (maxCount - 1));
                    count = Mathf.Clamp(count, 1, maxCount);
                }
                layerCounts.Add(count);
            }

            for (int row = 0; row < rows; row++)
            {
                List<MapNode> layer = new List<MapNode>();
                int count = layerCounts[row];

                for (int col = 0; col < count; col++)
                {
                    NodeType type = GetNodeType(row, col, count);
                    Vector3 pos = CalculatePosition(row, col, count);
                    MapNode node = CreateNode(pos, type, row, col);
                    node.position = pos;
                    layer.Add(node);
                }
                allLayers.Add(layer);
            }

            BuildFullConnections();
            EnsureAllNodesReachableFromBottom();
            UpdateStartAndBossVisuals();
            InitializeStartNodes();
            DrawAllLines();
            UpdateLineColors();

            CreateBoardVisual();
            if (enableFogOfWar)
                CreateFogOfWar();

            if (enableMapDisplay)
            {
                UpdateAllMapDisplays();
                GameLogger.Log($"[MapGenerator] 地图生成完成，共 {CountAllNodes()} 个节点");
            }
        }

        Vector3 CalculatePosition(int row, int col, int count)
        {
            float totalWidth = (count - 1) * horizontalSpacing;
            float x = col * horizontalSpacing - totalWidth / 2f;
            float y = row * verticalSpacing + nodeYOffset;

            float ox = Random.Range(-positionOffsetX, positionOffsetX) * 0.5f;
            float oy = Random.Range(-positionOffsetY, positionOffsetY) * 0.5f;
            if (col == 0 || col == count - 1)
                ox *= 0.3f;

            return new Vector3(x + ox, y + oy, 0);
        }

        NodeType GetNodeType(int row, int col, int count)
        {
            if (row == 0) return NodeType.Start;
            if (row == rows - 1) return NodeType.Boss;
            if (row == treasureLayerIndex && row > 0 && row < rows - 1)
                return NodeType.Treasure;
            if (row == rows - 2 && bossLayerHasRestBefore)
                return NodeType.Rest;

            float r = (float)rand.NextDouble();
            if (r < 0.40f) return NodeType.NormalMonster;
            if (r < 0.55f) return NodeType.EliteMonster;
            if (r < 0.75f) return NodeType.MysteryEvent;
            if (r < 0.90f) return NodeType.Shop;
            return NodeType.Treasure;
        }

        MapNode CreateNode(Vector3 pos, NodeType type, int row, int col)
        {

            GameObject prefab = GetNodePrefab(type);


            if (prefab == null)
                prefab = nodePrefab;


            if (prefab == null)
            {
                GameLogger.LogWarning($"[MapGenerator]  {type} 壬");
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fallback.transform.localScale = Vector3.one * 0.5f;
                prefab = fallback;
            }

            Transform nodeParent = contentRoot != null ? contentRoot : transform;
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, nodeParent);
            obj.name = $"Node_{type}_{row}_{col}";
            MapNode node = new MapNode(new Vector2Int(col, row), type);
            node.nodeObject = obj;
            node.position = pos;

            if (obj.GetComponent<Collider>() == null)
            {
                Collider existing = obj.GetComponentInChildren<Collider>();
                if (existing == null)
                    obj.AddComponent<BoxCollider>();
            }

            NodeClickHandler handler = obj.GetComponent<NodeClickHandler>();
            if (handler == null)
                handler = obj.AddComponent<NodeClickHandler>();
            handler.Initialize(node);

            MeshRenderer mr = obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {

                Material blueprintMat = GetBlueprintMaterial(type);
                if (blueprintMat != null)
                {
                    mr.material = blueprintMat;
                }
                else
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null) shader = Shader.Find("Standard");
                    if (shader == null) shader = Shader.Find("Sprites/Default");
                    if (shader == null)
                    {
                        GameLogger.LogError($"[MapGenerator] 节点 {type} 的 Shader 未找到，将使用粉色错误材质！");
                        shader = Shader.Find("Hidden/Internal-Colored");
                    }
                    Material mat = new Material(shader);

                    mat.color = GetBlueprintColor(type);
                    mr.material = mat;
                }
            }

            if (enableMapDisplay)
            {
                CreateMapDisplay(node, pos, type);
            }

            return node;
        }

        /// <summary>

        /// </summary>
        void CreateMapDisplay(MapNode node, Vector3 nodePos, NodeType type)
        {
            // 图标即节点本体：隐藏预制体自带网格(扁平圆柱盘)，避免盘体遮挡/穿插图标；
            // 点击命中图标Quad的碰撞体后事件沿层级冒泡到节点根部的 NodeClickHandler
            if (hideNodeMeshOnMap)
            {
                MeshRenderer[] nodeRenderers = node.nodeObject.GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer nr in nodeRenderers)
                    nr.enabled = false;
            }

            GameObject mapPrefab = GetMapPrefabFromBlueprint(type);
            if (mapPrefab != null)
            {
                GameObject mapObj = Instantiate(mapPrefab, nodePos + mapDisplayOffset, Quaternion.identity, node.nodeObject.transform);
                mapObj.name = $"MapDisplay_{type}";
                mapObj.transform.localScale = Vector3.one * mapDisplayScale;
                node.mapDisplayObject = mapObj;
                GameLogger.Log($"[MapGenerator]  {type}  (): {mapPrefab.name}");
                return;
            }


            Texture2D bpTexture = GetMapTextureFromBlueprint(type);
            if (bpTexture != null)
            {
                GameObject mapObj = CreateMapQuad(nodePos + mapDisplayOffset, bpTexture, node.nodeObject.transform, type);
                mapObj.name = $"MapDisplay_{type}";
                mapObj.transform.localScale = Vector3.one * mapDisplayScale;
                node.mapDisplayObject = mapObj;
                GameLogger.Log($"[MapGenerator]  {type}  (): {bpTexture.name}");
                return;
            }


            Texture2D texture = GetRandomMapTexture(type);
            if (texture != null)
            {
                GameObject mapObj = CreateMapQuad(nodePos + mapDisplayOffset, texture, node.nodeObject.transform, type);
                mapObj.name = $"MapDisplay_{type}";
                mapObj.transform.localScale = Vector3.one * mapDisplayScale;
                node.mapDisplayObject = mapObj;
                GameLogger.Log($"[MapGenerator]  {type}  (): {texture.name}");
                return;
            }


        }

        /// <summary>

        /// </summary>
        GameObject CreateMapQuad(Vector3 position, Texture2D texture, Transform parent, NodeType nodeType)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(parent);
            // 前置到盘面之上（-Z 朝向相机），避免与盘体/连线同层穿插
            quad.transform.position = new Vector3(position.x, position.y, position.z - 0.3f);
            // 立牌化：始终面向相机，图标不会被倾斜盘面透视压扁成"饼干"
            if (Camera.main != null)
                quad.transform.rotation = Camera.main.transform.rotation;
            else
                quad.transform.localRotation = Quaternion.identity;

            // 保留 Quad 自带碰撞体：图标即节点本体，点击命中后冒泡到 NodeClickHandler

            Renderer renderer = quad.GetComponent<Renderer>();
            if (renderer != null)
            {
                // 图标 PNG 带透明通道，优先 Sprites/Default 透明渲染
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null)
                {
                    GameLogger.LogError($"[MapGenerator] MapQuad 的 Shader 未找到，贴图将不可见！");
                    return quad;
                }

                Material mat = new Material(shader);
                mat.mainTexture = texture;
                mat.color = mapTintColor;
                mat.renderQueue = 3010;
                renderer.material = mat;
                renderer.enabled = true;
            }

            return quad;
        }

        /// <summary>

        /// </summary>
        Texture2D GetRandomMapTexture(NodeType type)
        {
            if (textureCache != null && textureCache.ContainsKey(type))
            {
                List<Texture2D> textures = textureCache[type];
                if (textures != null && textures.Count > 0)
                {
                    int index = Random.Range(0, textures.Count);
                    return textures[index];
                }
            }
            return null;
        }

        Color GetNodeTypeColor(NodeType type)
        {
            switch (type)
            {
                case NodeType.Start: return new Color(0.6f, 0.2f, 0.8f);
                case NodeType.NormalMonster: return new Color(0.7f, 0.7f, 0.7f);
                case NodeType.EliteMonster: return new Color(1f, 0.4f, 0.1f);
                case NodeType.MysteryEvent: return new Color(1f, 0.85f, 0f);
                case NodeType.Shop: return new Color(0.2f, 0.9f, 0.2f);
                case NodeType.Treasure: return new Color(1f, 0.8f, 0.2f);
                case NodeType.Rest: return new Color(0.2f, 0.7f, 1f);
                case NodeType.Boss: return new Color(1f, 0.1f, 0.1f);
                default: return Color.white;
            }
        }



        void BuildFullConnections()
        {
            foreach (var layer in allLayers)
                foreach (var node in layer)
                    node.ClearConnections();

            Vector2Int finalPoint = GetFinalNode();
            List<List<Vector2Int>> paths = GeneratePaths(finalPoint);

            foreach (List<Vector2Int> path in paths)
            {
                for (int i = 0; i < path.Count - 1; i++)
                {
                    MapNode fromNode = GetNode(path[i]);
                    MapNode toNode = GetNode(path[i + 1]);
                    if (fromNode != null && toNode != null)
                    {
                        fromNode.AddConnection(toNode);
                    }
                }
            }

            for (int row = 0; row < rows - 1; row++)
            {
                var currentLayer = allLayers[row];
                var nextLayer = allLayers[row + 1];

                foreach (var node in currentLayer)
                {
                    if (node.connections.Count == 0 && nextLayer.Count > 0)
                    {
                        var closest = nextLayer
                            .OrderBy(n => Vector3.Distance(n.position, node.position))
                            .First();
                        node.AddConnection(closest);
                    }
                }
            }

            for (int row = 1; row < rows; row++)
            {
                var currentLayer = allLayers[row];
                var prevLayer = allLayers[row - 1];

                foreach (var node in currentLayer)
                {
                    if (node.incoming.Count == 0 && prevLayer.Count > 0)
                    {
                        var closest = prevLayer
                            .OrderBy(n => Vector3.Distance(n.position, node.position))
                            .First();
                        closest.AddConnection(node);
                    }
                }
            }

            if (allLayers.Count > 0 && allLayers[0].Count > 0)
            {
                var start = allLayers[0][0];
                if (start.connections.Count == 0 && allLayers.Count > 1)
                {
                    var nextLayer = allLayers[1];
                    if (nextLayer.Count > 0)
                    {
                        var closest = nextLayer
                            .OrderBy(n => Vector3.Distance(n.position, start.position))
                            .First();
                        start.AddConnection(closest);
                    }
                }
            }

            if (allLayers.Count > 0)
            {
                var bossLayer = allLayers[rows - 1];
                if (bossLayer.Count > 0)
                {
                    var boss = bossLayer[0];
                    if (boss.incoming.Count == 0 && rows > 1)
                    {
                        var prevLayer = allLayers[rows - 2];
                        if (prevLayer.Count > 0)
                        {
                            var closest = prevLayer
                                .OrderBy(n => Vector3.Distance(n.position, boss.position))
                                .First();
                            closest.AddConnection(boss);
                        }
                    }
                }
            }

            RemoveCrossConnections();
            EnsureAllNodesCanReachBoss();
        }

        void EnsureAllNodesCanReachBoss()
        {
            if (allLayers.Count < 2) return;

            var bossLayer = allLayers[rows - 1];
            if (bossLayer.Count == 0) return;
            MapNode boss = bossLayer[0];

            for (int row = 0; row < rows - 1; row++)
            {
                var currentLayer = allLayers[row];
                var nextLayer = allLayers[row + 1];

                foreach (var node in currentLayer)
                {
                    if (node.connections.Count == 0 && nextLayer.Count > 0)
                    {
                        var closest = nextLayer
                            .OrderBy(n => Vector3.Distance(n.position, node.position))
                            .First();
                        node.AddConnection(closest);
                    }
                }
            }

            if (rows >= 2)
            {
                var preBossLayer = allLayers[rows - 2];
                foreach (var node in preBossLayer)
                {
                    if (!node.connections.Contains(boss))
                    {
                        node.AddConnection(boss);
                    }
                }
            }
        }

        void RemoveCrossConnections()
        {
            for (int row = 0; row < rows - 1; row++)
            {
                for (int col = 0; col < allLayers[row].Count - 1; col++)
                {
                    MapNode leftNode = GetNode(new Vector2Int(col, row));
                    if (leftNode == null || leftNode.connections.Count == 0) continue;

                    MapNode rightNode = GetNode(new Vector2Int(col + 1, row));
                    if (rightNode == null || rightNode.connections.Count == 0) continue;

                    MapNode topLeft = GetNode(new Vector2Int(col, row + 1));
                    MapNode topRight = GetNode(new Vector2Int(col + 1, row + 1));
                    if (topLeft == null || topRight == null) continue;

                    bool hasCross = leftNode.connections.Contains(topRight) &&
                                   rightNode.connections.Contains(topLeft);

                    if (hasCross)
                    {
                        if (!leftNode.connections.Contains(topLeft))
                            leftNode.AddConnection(topLeft);
                        if (!rightNode.connections.Contains(topRight))
                            rightNode.AddConnection(topRight);

                        float rnd = Random.Range(0f, 1f);
                        if (rnd < 0.2f)
                        {
                            leftNode.RemoveConnection(topRight);
                            rightNode.RemoveConnection(topLeft);
                        }
                        else if (rnd < 0.6f)
                        {
                            leftNode.RemoveConnection(topRight);
                        }
                        else
                        {
                            rightNode.RemoveConnection(topLeft);
                        }
                    }
                }
            }
        }

        void EnsureAllNodesReachableFromBottom()
        {
            HashSet<MapNode> reachableFromStart = new HashSet<MapNode>();
            Queue<MapNode> queue = new Queue<MapNode>();

            var startNode = allLayers[0][0];
            queue.Enqueue(startNode);
            reachableFromStart.Add(startNode);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                foreach (var conn in cur.connections)
                {
                    if (!reachableFromStart.Contains(conn))
                    {
                        reachableFromStart.Add(conn);
                        queue.Enqueue(conn);
                    }
                }
            }

            for (int row = rows - 1; row >= 1; row--)
            {
                var currentLayer = allLayers[row];
                var prevLayer = allLayers[row - 1];

                foreach (var node in currentLayer)
                {
                    if (!reachableFromStart.Contains(node))
                    {
                        var candidates = prevLayer.Where(n => reachableFromStart.Contains(n)).ToList();
                        if (candidates.Count > 0)
                        {
                            var closest = candidates
                                .OrderBy(n => Vector3.Distance(n.position, node.position))
                                .First();
                            closest.AddConnection(node);
                            reachableFromStart.Add(node);
                        }
                        else if (prevLayer.Count > 0)
                        {
                            var closest = prevLayer
                                .OrderBy(n => Vector3.Distance(n.position, node.position))
                                .First();
                            closest.AddConnection(node);
                            reachableFromStart.Add(node);
                        }
                    }
                }
            }

            for (int row = 1; row < rows; row++)
            {
                foreach (var node in allLayers[row])
                {
                    if (!reachableFromStart.Contains(node))
                    {
                        var prevLayer = allLayers[row - 1];
                        if (prevLayer.Count > 0)
                        {
                            var closest = prevLayer
                                .OrderBy(n => Vector3.Distance(n.position, node.position))
                                .First();
                            closest.AddConnection(node);
                            reachableFromStart.Add(node);
                        }
                    }
                }
            }
        }

        Vector2Int GetFinalNode()
        {
            return new Vector2Int(0, rows - 1);
        }

        List<List<Vector2Int>> GeneratePaths(Vector2Int finalNode)
        {
            var paths = new List<List<Vector2Int>>();
            Vector2Int startPoint = new Vector2Int(0, 0);
            int numOfBranches = 2 + extraBranches;

            int preBossRow = rows - 2;
            int preBossCount = allLayers[preBossRow].Count;

            if (preBossCount < numOfBranches)
                numOfBranches = preBossCount;

            if (numOfBranches < 1)
                numOfBranches = 1;

            List<int> availableCols = new List<int>();
            for (int i = 0; i < preBossCount; i++)
                availableCols.Add(i);

            ShuffleList(availableCols);

            HashSet<Vector2Int> usedNodes = new HashSet<Vector2Int>();
            usedNodes.Add(startPoint);

            for (int i = 0; i < numOfBranches; i++)
            {
                int targetCol = availableCols[i % availableCols.Count];
                Vector2Int endNode = new Vector2Int(targetCol, preBossRow);
                List<Vector2Int> path = GeneratePath(startPoint, endNode, i, usedNodes);
                path.Add(finalNode);
                paths.Add(path);

                for (int j = 1; j < path.Count - 1; j++)
                {
                    usedNodes.Add(path[j]);
                }
            }

            if (paths.Count == 0)
            {
                Vector2Int endNode = new Vector2Int(0, preBossRow);
                List<Vector2Int> path = GeneratePath(startPoint, endNode, 0, usedNodes);
                path.Add(finalNode);
                paths.Add(path);
            }

            return paths;
        }

        void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        List<Vector2Int> GeneratePath(Vector2Int fromPoint, Vector2Int toPoint, int pathIndex, HashSet<Vector2Int> usedNodes)
        {
            int toRow = toPoint.y;
            int toCol = toPoint.x;
            int lastNodeCol = fromPoint.x;

            List<Vector2Int> path = new List<Vector2Int> { fromPoint };

            for (int row = 1; row <= toRow; row++)
            {
                int currentLayerCount = allLayers[row].Count;

                List<int> candidateCols = new List<int>();

                int forwardCol = lastNodeCol;
                if (forwardCol >= 0 && forwardCol < currentLayerCount)
                    candidateCols.Add(forwardCol);

                int leftCol = lastNodeCol - 1;
                if (leftCol >= 0 && leftCol < currentLayerCount)
                    candidateCols.Add(leftCol);

                int rightCol = lastNodeCol + 1;
                if (rightCol >= 0 && rightCol < currentLayerCount)
                    candidateCols.Add(rightCol);

                if (row == toRow)
                {
                    path.Add(toPoint);
                    lastNodeCol = toCol;
                    continue;
                }

                if (candidateCols.Count == 0)
                {
                    for (int col = 0; col < currentLayerCount; col++)
                        candidateCols.Add(col);
                }

                var unusedCandidates = candidateCols
                    .Where(col => !usedNodes.Contains(new Vector2Int(col, row)))
                    .ToList();

                List<int> finalCandidates;
                if (unusedCandidates.Count > 0)
                {
                    finalCandidates = unusedCandidates;
                }
                else
                {
                    finalCandidates = candidateCols;
                }

                finalCandidates = finalCandidates
                    .OrderBy(col => Mathf.Abs(col - toCol))
                    .ThenBy(col => Mathf.Abs(col - lastNodeCol))
                    .ToList();

                int candidateIndex;
                if (finalCandidates.Count > 1)
                {
                    if (Random.Range(0f, 1f) < 0.7f)
                        candidateIndex = 0;
                    else
                        candidateIndex = Random.Range(0, finalCandidates.Count);
                }
                else
                {
                    candidateIndex = 0;
                }

                int candidateCol = finalCandidates[candidateIndex];
                Vector2Int nextPoint = new Vector2Int(candidateCol, row);

                path.Add(nextPoint);
                lastNodeCol = candidateCol;
            }

            if (path[path.Count - 1] != toPoint)
            {
                path[path.Count - 1] = toPoint;
            }

            return path;
        }

        MapNode GetNode(Vector2Int p)
        {
            if (p.y >= allLayers.Count) return null;
            if (p.x >= allLayers[p.y].Count) return null;
            return allLayers[p.y][p.x];
        }

        void UpdateStartAndBossVisuals()
        {
            if (allLayers.Count > 0 && allLayers[0].Count > 0)
                allLayers[0][0].nodeType = NodeType.Start;
            if (allLayers.Count > 0 && allLayers[allLayers.Count - 1].Count > 0)
                allLayers[allLayers.Count - 1][0].nodeType = NodeType.Boss;
        }

        void InitializeStartNodes()
        {
            if (allLayers.Count == 0 || allLayers[0].Count == 0) return;

            var firstStart = allLayers[0][0];
            firstStart.isVisited = true;
            firstStart.isReachable = true;
            currentNode = firstStart;

            foreach (var conn in firstStart.connections)
            {
                conn.isReachable = true;
            }
        }

        public void ConfirmReachNode(MapNode node)
        {
            if (node == null || node.isVisited) return;

            currentNode = node;
            node.isVisited = true;

            foreach (var layer in allLayers)
                foreach (var n in layer)
                    n.isReachable = false;

            foreach (var conn in currentNode.connections)
            {
                conn.isReachable = true;
            }

            UpdateLineColors();
            if (enableMapDisplay)
                UpdateAllMapDisplays();
            UpdateFogOfWar();
            OnNodeReached?.Invoke(node);
        }

        public bool CanReachNode(MapNode node)
        {
            if (node == null || node.isVisited) return false;
            return node.isReachable;
        }

        private Quaternion lastCameraRotation;

        /// <summary>相机旋转时刷新立牌朝向，图标始终面向相机（成本极低，仅相机旋转变化时执行）。</summary>
        void LateUpdate()
        {
            if (!enableMapDisplay || Camera.main == null || allLayers == null) return;
            if (Camera.main.transform.rotation == lastCameraRotation) return;
            lastCameraRotation = Camera.main.transform.rotation;

            foreach (var layer in allLayers)
            {
                if (layer == null) continue;
                foreach (var node in layer)
                {
                    if (node != null && node.mapDisplayObject != null)
                        node.mapDisplayObject.transform.rotation = Camera.main.transform.rotation;
                }
            }
        }

        void DrawAllLines()
        {
            ClearAllLines();
            lineConnections.Clear();

            int lineIndex = 0;
            foreach (var layer in allLayers)
            {
                foreach (var node in layer)
                {
                    if (node.nodeObject == null) continue;
                    if (node.connections.Count == 0) continue;

                    foreach (var target in node.connections)
                    {
                        if (target.nodeObject == null) continue;

                        Vector3 start = node.nodeObject.transform.position;
                        Vector3 end = target.nodeObject.transform.position;

                        LineRenderer lr = LineRendererHelper.CreateLine(
                            start: start,
                            end: end,
                            parent: linesParent,
                            width: 0.18f,
                            color: new Color(1f, 1f, 1f, 0.9f)
                        );

                        if (stylizedLineMaterial != null)
                        {
                            // 手绘墨迹纹理 + 平铺模式，营造邪恶冥刻风连线
                            lr.material = stylizedLineMaterial;
                            lr.textureMode = LineTextureMode.Tile;
                        }
                        else if (lineMaterial != null)
                            lr.material = lineMaterial;
                        else
                            lr.material = defaultLineMaterial;

                        lr.sortingOrder = 10;
                        lr.name = $"Line_{lineIndex}";

                        lineConnections.Add(new LineConnectionData
                        {
                            lineRenderer = lr,
                            fromNode = node,
                            toNode = target
                        });

                        lineIndex++;
                    }
                }
            }
        }

        void ClearAllLines()
        {
            if (linesParent != null)
            {
                for (int i = linesParent.childCount - 1; i >= 0; i--)
                {
                    Destroy(linesParent.GetChild(i).gameObject);
                }
            }
            lineConnections.Clear();
        }

        public void UpdateLineColors()
        {
            if (currentNode == null) return;

            foreach (var conn in lineConnections)
            {
                if (conn.lineRenderer == null) continue;

                Color color;
                if (conn.fromNode == currentNode && conn.toNode.isReachable)
                {
                    color = new Color(0.2f, 1f, 0.2f, 0.8f);
                }
                else if (conn.fromNode.isVisited && conn.toNode.isVisited)
                {
                    color = new Color(0.4f, 0.4f, 0.5f, 0.6f);
                }
                else
                {
                    color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
                }

                LineRendererHelper.SetLineColor(conn.lineRenderer, color);
            }
        }

        public void UpdateAllMapDisplays()
        {
            if (!enableMapDisplay)
            {
                return;
            }

            int displayCount = 0;
            foreach (var layer in allLayers)
            {
                foreach (var node in layer)
                {
                    if (node.mapDisplayObject == null)
                    {
                        CreateMapDisplay(node, node.position, node.nodeType);
                        if (node.mapDisplayObject == null)
                            continue;
                    }

                    Renderer renderer = node.mapDisplayObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = true;
                        Color color = renderer.material.color;
                        if (node.isVisited)
                        {
                            color.a = 0.4f;
                        }
                        else if (node.isReachable)
                        {
                            color.a = 0.9f;
                        }
                        else
                        {
                            color.a = 0.2f;
                        }
                        renderer.material.color = color;
                        displayCount++;
                    }
                }
            }
            if (displayCount > 0)
                GameLogger.Log($"[MapGenerator] UpdateAllMapDisplays 更新了 {displayCount} 个显示对象");
        }

        int CountAllNodes()
        {
            int count = 0;
            foreach (var layer in allLayers)
                count += layer.Count;
            return count;
        }

        int CountAllConnections()
        {
            int count = 0;
            foreach (var layer in allLayers)
                foreach (var node in layer)
                    count += node.connections.Count;
            return count;
        }

        public Vector3 GetNodeWorldPosition(MapNode node)
        {
            if (node == null || node.nodeObject == null) return Vector3.zero;
            return node.nodeObject.transform.position;
        }

        public List<MapNode> GetReachableNodes()
        {
            List<MapNode> result = new List<MapNode>();
            if (currentNode == null) return result;

            foreach (var conn in currentNode.connections)
            {
                if (!conn.isVisited)
                    result.Add(conn);
            }
            return result;
        }

        private class LineConnectionData
        {
            public LineRenderer lineRenderer;
            public MapNode fromNode;
            public MapNode toNode;
        }
    }
}


