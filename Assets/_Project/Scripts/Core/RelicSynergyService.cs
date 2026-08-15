using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 遗物共鸣组合（Isaac 式"化学反应"）：特定遗物同时持有时触发一次性联动效果。
    /// </summary>
    [Serializable]
    public class RelicSynergyCombo
    {
        public string id;                 // 组合唯一 ID
        public string name;               // 组合名（中文）
        public string[] requiredIds;      // 所需遗物 ID（全部持有才触发）
        public int maxHpGain;             // 血上限加成
        public int goldGain;              // 金币奖励
        public string description;        // 触发公告文案
    }

    /// <summary>
    /// 遗物共鸣服务（运行时自动创建，无需场景接线）。
    /// RelicManager.AddRelic 后调用 RefreshCombos() 检查是否凑齐组合。
    /// 组合为一次性：本局触发过后不再重复触发。
    /// </summary>
    public class RelicSynergyService : MonoBehaviour
    {
        private static RelicSynergyService _instance;
        public static RelicSynergyService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<RelicSynergyService>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("RelicSynergyService");
                        _instance = go.AddComponent<RelicSynergyService>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>组合激活事件（UI 公告横幅可监听此事件）。</summary>
        public event Action<RelicSynergyCombo> OnComboActivated;

        private readonly HashSet<string> activatedCombos = new HashSet<string>();

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>全部共鸣组合表（跨阵营组合体现"化学反应"）。</summary>
        private static List<RelicSynergyCombo> BuildComboTable()
        {
            return new List<RelicSynergyCombo>
            {
                new RelicSynergyCombo
                {
                    id = "BloodMoonResonance",
                    name = "血月共鸣",
                    requiredIds = new[] { RelicIds.Boss_BloodVein, RelicIds.Blood_CrimsonAltar, RelicIds.Blood_BloodPact },
                    maxHpGain = 15,
                    goldGain = 50,
                    description = "鲜血三圣器共鸣：血上限 +15，金币 +50"
                },
                new RelicSynergyCombo
                {
                    id = "FrozenThrone",
                    name = "冰封王座",
                    requiredIds = new[] { RelicIds.Boss_FrostHeart, RelicIds.Frost_Permafrost, RelicIds.Frost_FrostGiant },
                    maxHpGain = 20,
                    goldGain = 0,
                    description = "极寒王座现世：血上限 +20"
                },
                new RelicSynergyCombo
                {
                    id = "AbyssalEnd",
                    name = "影之终焉",
                    requiredIds = new[] { RelicIds.Boss_MemoryLens, RelicIds.Shadow_AbyssGaze, RelicIds.Shadow_PhantomMask },
                    maxHpGain = 10,
                    goldGain = 100,
                    description = "深渊凝视者齐集：血上限 +10，金币 +100"
                },
                new RelicSynergyCombo
                {
                    id = "CorruptedGarden",
                    name = "腐化花园",
                    requiredIds = new[] { RelicIds.Boss_CorruptLiver, RelicIds.Corrupt_Necronomicon, RelicIds.Corrupt_DeadBranch },
                    maxHpGain = 10,
                    goldGain = 50,
                    description = "腐化之书翻动：血上限 +10，金币 +50"
                },
                new RelicSynergyCombo
                {
                    id = "SlimeEcosystem",
                    name = "史莱姆生态",
                    requiredIds = new[] { RelicIds.Boss_SlimeGland, RelicIds.Slime_SlimeHeart, RelicIds.Slime_AcidicCore },
                    maxHpGain = 10,
                    goldGain = 50,
                    description = "黏液群落成型：血上限 +10，金币 +50"
                },
                new RelicSynergyCombo
                {
                    id = "ReluctantEcho",
                    name = "执念回响",
                    requiredIds = new[] { RelicIds.Boss_ReluctantChain, RelicIds.Reluctant_EchoRing, RelicIds.Reluctant_Nostalgia },
                    maxHpGain = 10,
                    goldGain = 50,
                    description = "执念之链回响：血上限 +10，金币 +50"
                },
                // 跨阵营组合（真正的"化学反应"：不同体系的遗物互相激发）
                new RelicSynergyCombo
                {
                    id = "BloodAndFrost",
                    name = "血与冰",
                    requiredIds = new[] { RelicIds.Blood_VampireFang, RelicIds.Frost_Snowflake },
                    maxHpGain = 10,
                    goldGain = 0,
                    description = "炽血遇寒霜：血上限 +10"
                },
                new RelicSynergyCombo
                {
                    id = "CorruptedSlime",
                    name = "腐化粘液",
                    requiredIds = new[] { RelicIds.Corrupt_DeadBranch, RelicIds.Slime_StickyGlove },
                    maxHpGain = 0,
                    goldGain = 60,
                    description = "枯枝生出黏液：金币 +60"
                },
                new RelicSynergyCombo
                {
                    id = "ShadowsOfMemory",
                    name = "记忆之影",
                    requiredIds = new[] { RelicIds.Shadow_Cloak, RelicIds.Reluctant_EchoRing },
                    maxHpGain = 10,
                    goldGain = 40,
                    description = "往昔在暗影中回响：血上限 +10，金币 +40"
                },

                // ============ 双阵营融合（两个 Boss 遗物·六阵营两两配对，共 15 组）============
                new RelicSynergyCombo
                {
                    id = "Fusion_BloodFrost",
                    name = "血染霜华",
                    requiredIds = new[] { RelicIds.Boss_BloodVein, RelicIds.Boss_FrostHeart },
                    maxHpGain = 12,
                    goldGain = 0,
                    description = "热血与寒霜交融：血上限 +12"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_BloodCorrupt",
                    name = "腐血契约",
                    requiredIds = new[] { RelicIds.Boss_BloodVein, RelicIds.Boss_CorruptLiver },
                    maxHpGain = 8,
                    goldGain = 40,
                    description = "以腐为薪燃血：血上限 +8，金币 +40"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_BloodSlime",
                    name = "血之黏液",
                    requiredIds = new[] { RelicIds.Boss_BloodVein, RelicIds.Boss_SlimeGland },
                    maxHpGain = 10,
                    goldGain = 0,
                    description = "鲜血在黏液中不凝：血上限 +10"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_BloodReluctant",
                    name = "血誓回响",
                    requiredIds = new[] { RelicIds.Boss_BloodVein, RelicIds.Boss_ReluctantChain },
                    maxHpGain = 0,
                    goldGain = 70,
                    description = "旧誓以血为证：金币 +70"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_BloodShadow",
                    name = "血影双生",
                    requiredIds = new[] { RelicIds.Boss_BloodVein, RelicIds.Boss_MemoryLens },
                    maxHpGain = 10,
                    goldGain = 30,
                    description = "影随血行：血上限 +10，金币 +30"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_FrostCorrupt",
                    name = "寒冰腐土",
                    requiredIds = new[] { RelicIds.Boss_FrostHeart, RelicIds.Boss_CorruptLiver },
                    maxHpGain = 10,
                    goldGain = 0,
                    description = "冻土之下腐物蛰伏：血上限 +10"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_FrostSlime",
                    name = "冰封黏核",
                    requiredIds = new[] { RelicIds.Boss_FrostHeart, RelicIds.Boss_SlimeGland },
                    maxHpGain = 8,
                    goldGain = 40,
                    description = "黏液冻成晶核：血上限 +8，金币 +40"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_FrostReluctant",
                    name = "冰忆长廊",
                    requiredIds = new[] { RelicIds.Boss_FrostHeart, RelicIds.Boss_ReluctantChain },
                    maxHpGain = 0,
                    goldGain = 70,
                    description = "冰封的记忆长廊：金币 +70"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_FrostShadow",
                    name = "寒夜之影",
                    requiredIds = new[] { RelicIds.Boss_FrostHeart, RelicIds.Boss_MemoryLens },
                    maxHpGain = 12,
                    goldGain = 0,
                    description = "寒夜为影披霜：血上限 +12"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_CorruptSlime",
                    name = "腐沼之源",
                    requiredIds = new[] { RelicIds.Boss_CorruptLiver, RelicIds.Boss_SlimeGland },
                    maxHpGain = 10,
                    goldGain = 30,
                    description = "腐沼中孕育新群：血上限 +10，金币 +30"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_CorruptReluctant",
                    name = "腐朽之忆",
                    requiredIds = new[] { RelicIds.Boss_CorruptLiver, RelicIds.Boss_ReluctantChain },
                    maxHpGain = 0,
                    goldGain = 80,
                    description = "腐朽的往昔无价：金币 +80"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_CorruptShadow",
                    name = "腐影密谋",
                    requiredIds = new[] { RelicIds.Boss_CorruptLiver, RelicIds.Boss_MemoryLens },
                    maxHpGain = 8,
                    goldGain = 50,
                    description = "影与腐共谋：血上限 +8，金币 +50"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_SlimeReluctant",
                    name = "黏忆纠缠",
                    requiredIds = new[] { RelicIds.Boss_SlimeGland, RelicIds.Boss_ReluctantChain },
                    maxHpGain = 10,
                    goldGain = 0,
                    description = "回忆黏稠不散：血上限 +10"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_SlimeShadow",
                    name = "黏液暗影",
                    requiredIds = new[] { RelicIds.Boss_SlimeGland, RelicIds.Boss_MemoryLens },
                    maxHpGain = 0,
                    goldGain = 60,
                    description = "影中黏液生财：金币 +60"
                },
                new RelicSynergyCombo
                {
                    id = "Fusion_ReluctantShadow",
                    name = "影忆之隙",
                    requiredIds = new[] { RelicIds.Boss_ReluctantChain, RelicIds.Boss_MemoryLens },
                    maxHpGain = 8,
                    goldGain = 40,
                    description = "影与忆之间的缝隙：血上限 +8，金币 +40"
                },
            };
        }

        /// <summary>检查并激活所有新凑齐的共鸣组合（由 RelicManager 在遗物增减后调用）。</summary>
        public void RefreshCombos()
        {
            RelicManager rm = RelicManager.Instance;
            if (rm == null) return;

            foreach (RelicSynergyCombo combo in BuildComboTable())
            {
                if (combo == null || activatedCombos.Contains(combo.id)) continue;
                if (!HasAllRelics(rm, combo.requiredIds)) continue;

                activatedCombos.Add(combo.id);
                ApplyCombo(combo);
                GameLogger.Log($"[遗物共鸣] 「{combo.name}」激活！{combo.description}");
                AudioManager.Instance?.PlayComboActivated();
                OnComboActivated?.Invoke(combo);
            }
        }

        private static bool HasAllRelics(RelicManager rm, string[] ids)
        {
            foreach (string id in ids)
            {
                if (!rm.HasRelic(id)) return false;
            }
            return true;
        }

        private static void ApplyCombo(RelicSynergyCombo combo)
        {
            PlayerDataManager pdm = PlayerDataManager.Instance;
            if (pdm == null) return;
            if (combo.maxHpGain > 0)
                pdm.AddMaxHealth(combo.maxHpGain);
            if (combo.goldGain > 0)
                pdm.AddGold(combo.goldGain);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
