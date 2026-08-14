using UnityEngine;
using MutationChess.Core;

namespace MutationChess.Map
{
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        [SerializeField] private MapView mapView;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            InitializeMap();
        }

        void InitializeMap()
        {
            if (mapView != null)
            {
                MapGenerator gen = FindObjectOfType<MapGenerator>();
                if (gen != null)
                {
                    // 等待一帧让 MapGenerator 完成初始化
                    StartCoroutine(InitializeAfterGenerator());
                }
                else
                {
                    GameLogger.LogError("找不到 MapGenerator！");
                }
            }
        }

        System.Collections.IEnumerator InitializeAfterGenerator()
        {
            yield return null;

            MapGenerator gen = FindObjectOfType<MapGenerator>();
            if (gen != null && gen.AllLayers.Count > 0)
            {
                MapData mapData = new MapData();
                mapData.layers = gen.AllLayers;
                mapView.Initialize(mapData);
            }
        }

        public void RefreshMap()
        {
            if (mapView != null)
                mapView.RefreshAllNodes();
        }
    }
}
