using System.Collections.Generic;
using MutationChess.Core;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>

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
            [Tooltip("ID")]
            public List<string> materialRelicIds = new List<string>();
            [Tooltip("ID")]
            public string resultRelicId;
        }

        [Header("")]
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
                var recipe = new MergeRecipe
                {
                    materialRelicIds = new List<string> { "01", "06" },
                    resultRelicId = "relic_sword_core"
                };
                recipes.Add(recipe);
            }
        }

        /// <summary>

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

            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>("Relics");
            foreach (var asset in allAssets)
            {
                if (asset.relicId == recipe.resultRelicId)
                {
                    Relic mergedRelic = relicManager.CreateRelicFromAsset(asset);
                    if (mergedRelic != null)
                    {
                        relicManager.AddRelic(mergedRelic);
                        GameLogger.Log($"[RelicMergeService] : {mergedRelic.relicName}");
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


