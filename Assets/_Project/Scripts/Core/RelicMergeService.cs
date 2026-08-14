using System.Collections.Generic;
using MutationChess.Core;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 遗物合成服务，检测合成配方并自动合成遗物
    /// </summary>
    public class RelicMergeService : MonoBehaviour
    {
        private static RelicMergeService _instance;
        public static RelicMergeService Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<RelicMergeService>();
                return _instance;
            }
        }

        [System.Serializable]
        public class MergeRecipe
        {
            [Tooltip("材料遗物ID列表")]
            public List<string> materialRelicIds = new List<string>();
            [Tooltip("合成结果遗物ID")]
            public string resultRelicId;
        }

        [Header("合成配方")]
        [SerializeField] private List<MergeRecipe> recipes = new List<MergeRecipe>();

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (recipes.Count == 0)
            {
                // 默认配方：剑刃碎片 + 剑柄碎片 → 剑核（ID 与 RelicBalanceConfig 保持一致）
                var recipe = new MergeRecipe
                {
                    materialRelicIds = new List<string> { RelicIds.Synth_SwordShard, RelicIds.Synth_HiltShard },
                    resultRelicId = RelicIds.Synth_SwordCore
                };
                recipes.Add(recipe);
            }
        }

        /// <summary>
        /// 检查所有配方并执行可合成的配方
        /// </summary>
        public void CheckAndMerge()
        {
            var relicManager = RelicManager.Instance;
            if (relicManager == null) return;

            foreach (var recipe in recipes)
            {
                if (CanMerge(recipe, relicManager))
                {
                    MergeRelics(recipe, relicManager);
                }
            }
        }

        /// <summary>
        /// 当新遗物添加时触发合成检查
        /// </summary>
        public void OnRelicAdded(Relic newRelic)
        {
            CheckAndMerge();
        }

        private bool CanMerge(MergeRecipe recipe, RelicManager relicManager)
        {
            if (recipe == null || recipe.materialRelicIds == null) return false;

            foreach (string materialId in recipe.materialRelicIds)
            {
                if (!relicManager.HasRelic(materialId))
                    return false;
            }

            if (relicManager.HasRelic(recipe.resultRelicId))
                return false;

            return true;
        }

        private void MergeRelics(MergeRecipe recipe, RelicManager relicManager)
        {
            foreach (string materialId in recipe.materialRelicIds)
            {
                relicManager.RemoveRelic(materialId);
            }

            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics);
            foreach (var asset in allAssets)
            {
                if (asset.relicId == recipe.resultRelicId)
                {
                    Relic mergedRelic = relicManager.CreateRelicFromAsset(asset);
                    if (mergedRelic != null)
                    {
                        relicManager.AddRelic(mergedRelic);
                        GameLogger.Log($"[RelicMergeService] 合成遗物：{mergedRelic.relicName}");
                    }
                    break;
                }
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}


