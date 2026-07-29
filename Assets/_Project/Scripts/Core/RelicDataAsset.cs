using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    [Serializable]
    public class RelicEffectEntry
    {
        [Tooltip("EffectResources Effects/TempStrength3")]
        public string effectId;
        [Tooltip("")]
        public EffectTrigger trigger;
        [Tooltip("1")]
        public float value1 = 0f;
        [Tooltip("2")]
        public float value2 = 0f;
    }

    [CreateAssetMenu(fileName = "RelicDataAsset", menuName = "MutationChess/Relic Data Asset")]
    public class RelicDataAsset : ScriptableObject
    {
        [Header("")]
        public string relicId;
        public string relicName;
        public RelicRarity rarity;
        public CardFaction faction;

        [Header("")]
        [Tooltip("")]
        public List<RelicEffectEntry> baseEffectIds = new List<RelicEffectEntry>();

        [Header("BossID")]
        [Tooltip("BossrelicId Boss_BloodVein磩")]
        public string hiddenActivatorRelicId = "";

        [Header("ActivatorActivator")]
        [Tooltip("Boss")]
        public List<RelicEffectEntry> hiddenEffectIds = new List<RelicEffectEntry>();

        [Header("baseEffectIds")]
        [Tooltip("baseEffectIds")]
        public List<RelicEffectEntry> relicEffects = new List<RelicEffectEntry>();

        [Header("")]
        [TextArea(2, 4)]
        public string description;

        [Header("")]
        [Tooltip("1:150 2:250 3:285-350 :320-400 Boss:350+")]
        public int price = 150;

        [Header("")]
        public bool isShopRelic = false;
        public bool isBossRelic = false;
        public bool isStartingRelic = false;
        public bool isSynthesisTarget = false;

        [Header("Boss")]
        [Tooltip("isBossRelic")]
        public bool isFactionUnlocker = false;
        public CardFaction unlockedFaction = CardFaction.None;

        [Header("")]
        public string iconPath;

        /// <summary>

        /// </summary>
        public List<RelicEffectEntry> GetActiveEffects(bool hasActivator)
        {
            List<RelicEffectEntry> result = new List<RelicEffectEntry>();

            if (baseEffectIds != null && baseEffectIds.Count > 0)
                result.AddRange(baseEffectIds);
            else if (relicEffects != null && relicEffects.Count > 0)
                result.AddRange(relicEffects);

            if (hasActivator && hiddenEffectIds != null && hiddenEffectIds.Count > 0)
                result.AddRange(hiddenEffectIds);

            return result;
        }

        private void OnValidate()
        {

            if ((relicEffects != null && relicEffects.Count > 0) &&
                (baseEffectIds == null || baseEffectIds.Count == 0))
            {
                baseEffectIds = new List<RelicEffectEntry>(relicEffects);
            }
        }
    }
}


