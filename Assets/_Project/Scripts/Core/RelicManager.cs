using System;
using MutationChess.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    public class RelicManager : MonoBehaviour
    {
        private static RelicManager _instance;
        public static RelicManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindObjectOfType<RelicManager>();
                return _instance;
            }
        }

        [Header(" & ")]
        [SerializeField] [Range(0f, 1f)] private float normalRelicDropChance = 0.35f;

        private List<Relic> ownedRelics = new List<Relic>();
        private HashSet<string> relicsUsedThisBattle = new HashSet<string>();
        private HashSet<string> registeredRelicIds = new HashSet<string>();

        private HashSet<string> activatedHiddenRelicIds = new HashSet<string>();

        private EffectManager effectManager;

        // RelicBalanceConfig.CreateDefaultConfig() 每次调用都会重建整份配置，缓存一份供查询
        private static RelicBalanceConfig _cachedBalanceConfig;
        private static RelicBalanceConfig CachedBalanceConfig
        {
            get
            {
                if (_cachedBalanceConfig == null)
                    _cachedBalanceConfig = RelicBalanceConfig.CreateDefaultConfig();
                return _cachedBalanceConfig;
            }
        }

        public event Action OnRelicsChanged;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        void Start()
        {
            effectManager = EffectManager.Instance;
            EnsureHandlersRegistered();

            RefreshAllHiddenEffects();
        }

        void OnValidate()
        {
            normalRelicDropChance = Mathf.Clamp01(normalRelicDropChance);
        }

        public void AddRelic(Relic relic)
        {
            if (relic == null) return;


            TryDeduplicateRelics(relic);

            ownedRelics.Add(relic);
            RegisterRelicEffects(relic);
            GameLogger.Log($"[RelicManager] 获得遗物：{relic.relicName} ({relic.GetRarityName()})");
            OnRelicsChanged?.Invoke();


            var mergeService = RelicMergeService.Instance;
            if (mergeService != null)
            {
                mergeService.OnRelicAdded(relic);
            }



            RelicBalanceConfig.RelicBalanceEntry entryConfig = RelicBalanceConfig.CreateDefaultConfig().GetEntry(relic.relicId);
            bool newIsBoss = (relic.rarity == RelicRarity.Starting) ||
                              (entryConfig != null && entryConfig.isBossRelic);
            if (newIsBoss)
            {
                GameLogger.Log($"[RelicManager] Boss {relic.relicName} ...");
                RefreshAllHiddenEffects();
            }
            else
            {

                TryActivateHiddenEffectsForRelic(relic);
            }
        }

        /// <summary>

        /// </summary>
        private void RefreshAllHiddenEffects()
        {
            foreach (var relic in ownedRelics)
            {
                TryActivateHiddenEffectsForRelic(relic);
            }
        }

        /// <summary>

        /// </summary>
        private void TryActivateHiddenEffectsForRelic(Relic relic)
        {
            if (relic == null) return;
            if (activatedHiddenRelicIds.Contains(relic.relicId)) return;


            var cfg = RelicBalanceConfig.CreateDefaultConfig().GetEntry(relic.relicId);
            if (cfg == null || string.IsNullOrEmpty(cfg.hiddenActivatorRelicId)) return;


            if (!HasRelic(cfg.hiddenActivatorRelicId)) return;


            GameLogger.Log($"[RelicManager]  {relic.relicName}  {cfg.hiddenActivatorRelicId} ");
            activatedHiddenRelicIds.Add(relic.relicId);

            if (effectManager == null) effectManager = EffectManager.Instance;
            if (effectManager == null) return;

            foreach (var effectEntry in cfg.hiddenEffectIds)
            {
                if (effectEntry == null || string.IsNullOrEmpty(effectEntry.effectId)) continue;
                CardEffect effect = LoadEffect(effectEntry.effectId);
                if (effect == null)
                {
                    GameLogger.LogWarning($"[RelicManager] 遗物效果加载失败：{effectEntry.effectId}");
                    continue;
                }
                ApplyConfigValue(effect, effectEntry);
                var capturedRelic = relic;
                var capturedEffect = effect;

                RelicEffectInstance instance = new RelicEffectInstance
                {
                    effect = capturedEffect,
                    trigger = effectEntry.trigger
                };

                if (IsValueModifierTrigger(instance.trigger))
                {
                    instance.valueModifier = (ctx, currentValue) =>
                    {
                        if (!ownedRelics.Contains(capturedRelic)) return currentValue;
                        ctx.baseValue = currentValue;
                        ctx.finalValue = currentValue;
                        capturedEffect.Execute(ctx);
                        return ctx.finalValue;
                    };
                    effectManager.RegisterValueModifier(instance.trigger, instance.valueModifier);
                }
                else
                {
                    instance.handler = (ctx) =>
                    {
                        if (!ownedRelics.Contains(capturedRelic)) return;
                        capturedEffect.Execute(ctx);
                    };
                    effectManager.Register(instance.trigger, instance.handler);
                }

                relic.relicEffects.Add(instance);

                GameLogger.Log($"[RelicManager] {relic.relicName} : {capturedEffect.GetType().Name} ({instance.trigger})");
            }
        }

        /// <summary>


        /// </summary>
        private void TryDeduplicateRelics(Relic newRelic)
        {
            if (newRelic == null) return;

            if (newRelic.faction == CardFaction.None) return;

            var newSignatures = GetEffectSignature(newRelic);
            if (newSignatures.Count == 0) return;

            List<Relic> toRemove = new List<Relic>();

            foreach (var existing in ownedRelics)
            {
                if (existing == null || existing == newRelic) continue;
                var existingSignatures = GetEffectSignature(existing);
                bool hasOverlap = false;
                foreach (var sig in newSignatures)
                {
                    if (existingSignatures.Contains(sig))
                    {
                        hasOverlap = true;
                        break;
                    }
                }
                if (!hasOverlap) continue;

                if (existing.faction == CardFaction.None)
                {
                    if (existing.rarity == RelicRarity.Legendary)
                        continue;

                    GameLogger.Log($"[RelicManager]  {newRelic.relicName} ?I {existing.relicName}");
                    toRemove.Add(existing);
                }
                else
                {

                    if (UnityEngine.Random.value < 0.5f)
                    {
                        GameLogger.Log($"[RelicManager]  {newRelic.relicName} ?I {existing.relicName}");
                        toRemove.Add(existing);
                    }
                }
            }

            foreach (var relic in toRemove)
            {
                RemoveRelic(relic.relicId);
            }
        }

        /// <summary>

        /// </summary>
        private HashSet<string> GetEffectSignature(Relic relic)
        {
            HashSet<string> signatures = new HashSet<string>();
            if (relic?.relicEffects == null) return signatures;

            foreach (var entry in relic.relicEffects)
            {
                if (entry?.effect == null) continue;
                string signature = $"{entry.trigger}:{entry.effect.GetType().Name}";
                signatures.Add(signature);
            }
            return signatures;
        }

        public void RemoveRelic(string relicId)
        {
            Relic relic = ownedRelics.FirstOrDefault(r => r.relicId == relicId);
            if (relic != null)
            {
                UnregisterRelicEffects(relic);
                activatedHiddenRelicIds.Remove(relicId);
                ownedRelics.Remove(relic);
                GameLogger.Log($"[RelicManager] 移除遗物：{relic.relicName}");
                OnRelicsChanged?.Invoke();
            }
        }

        public bool HasRelic(string relicId)
        {
            return ownedRelics.Any(r => r.relicId == relicId);
        }

        public Relic GetRelic(string relicId)
        {
            return ownedRelics.FirstOrDefault(r => r.relicId == relicId);
        }

        public List<Relic> GetAllRelics()
        {
            return new List<Relic>(ownedRelics);
        }

        public int Count => ownedRelics.Count;

        public void OnBattleStart()
        {
            relicsUsedThisBattle.Clear();

            // 重置各遗物效果的战斗内状态（计数器/一次性标志），防止跨战斗泄漏
            foreach (var relic in ownedRelics)
            {
                if (relic?.relicEffects == null) continue;
                foreach (var entry in relic.relicEffects)
                {
                    if (entry?.effect != null)
                        entry.effect.ResetForBattle();
                }
            }

            EnsureHandlersRegistered();
        }

        private void EnsureHandlersRegistered()
        {
            if (effectManager == null) effectManager = EffectManager.Instance;
            if (effectManager == null) return;

            foreach (var relic in ownedRelics)
            {
                if (!registeredRelicIds.Contains(relic.relicId))
                {
                    RegisterRelicEffects(relic);
                }
            }
        }

        private void RegisterRelicEffects(Relic relic)
        {
            if (relic == null) return;
            if (registeredRelicIds.Contains(relic.relicId)) return;
            if (effectManager == null) effectManager = EffectManager.Instance;
            if (effectManager == null) return;

            registeredRelicIds.Add(relic.relicId);


            if (relic.relicEffects == null || relic.relicEffects.Count == 0)
            {
                LoadBaseEffectsIntoRelic(relic);
            }

            if (relic.relicEffects == null || relic.relicEffects.Count == 0)
            {
                GameLogger.LogWarning($"[RelicManager]  {relic.relicName} ");
                return;
            }

            foreach (var entry in relic.relicEffects)
            {
                if (entry?.effect == null) continue;

                var capturedEntry = entry;
                var capturedRelic = relic;

                // 修正：资产里序列化的 trigger 多为 0（BattleStart），
                // 以 RelicBalanceConfig 中的设计触发时机为准（配置缺失时退回资产值）。
                capturedEntry.trigger = ResolveEffectTrigger(relic, capturedEntry);

                if (IsValueModifierTrigger(capturedEntry.trigger))
                {
                    capturedEntry.valueModifier = (ctx, currentValue) =>
                    {
                        if (!ownedRelics.Contains(capturedRelic)) return currentValue;
                        ctx.baseValue = currentValue;
                        ctx.finalValue = currentValue;
                        capturedEntry.effect.Execute(ctx);
                        return ctx.finalValue;
                    };
                    effectManager.RegisterValueModifier(capturedEntry.trigger, capturedEntry.valueModifier);
                }
                else
                {
                    capturedEntry.handler = (ctx) =>
                    {
                        if (!ownedRelics.Contains(capturedRelic)) return;
                        capturedEntry.effect.Execute(ctx);
                    };
                    effectManager.Register(capturedEntry.trigger, capturedEntry.handler);
                }

                GameLogger.Log($"[RelicManager] {relic.relicName} : {capturedEntry.effect.GetType().Name} ({capturedEntry.trigger})");
            }
        }

        /// <summary>
        /// 从 RelicBalanceConfig 解析效果的预期触发时机（按 effectId 匹配）。
        /// 遗物 .asset 中序列化的 trigger 大量为默认值 0（BattleStart），
        /// 配置中才有策划预期的正确 trigger；配置查不到时退回资产值。
        /// </summary>
        private EffectTrigger ResolveEffectTrigger(Relic relic, RelicEffectInstance entry)
        {
            if (entry?.effect == null || relic == null) return EffectTrigger.BattleStart;
            string effectId = entry.effect.name.Replace(" (Clone)", "");

            var cfg = CachedBalanceConfig;
            if (cfg == null) return entry.trigger;

            var relicEntry = cfg.GetEntry(relic.relicId);
            if (relicEntry == null) return entry.trigger;

            if (relicEntry.baseEffectIds != null)
                foreach (var e in relicEntry.baseEffectIds)
                    if (e != null && e.effectId == effectId) return e.trigger;

            if (relicEntry.hiddenEffectIds != null)
                foreach (var e in relicEntry.hiddenEffectIds)
                    if (e != null && e.effectId == effectId) return e.trigger;

            return entry.trigger;
        }

        /// <summary>

        /// </summary>
        /// <summary>
        /// 将配置条目中的 value1 应用到效果实例字段（配置值优先于资产默认值）。
        /// 覆盖所有带数值的合并效果类：
        ///   ApplyBlockEffect.blockAmount / GainBuffEffect.amount /
        ///   MultiStatBuffEffect.energy / DrawCardsEffect.drawCount /
        ///   GainEnergyEffect.energyGain / HealPlayerEffect.healAmount /
        ///   MaxHealthEffect.maxHealthGain
        /// （此类效果没有 sourceCard，必须用配置值代替卡牌属性）
        /// </summary>
        private void ApplyConfigValue(CardEffect effect, RelicEffectEntry configEntry)
        {
            if (effect == null || configEntry == null) return;
            if (Mathf.Approximately(configEntry.value1, 0f)) return;

            int v = Mathf.RoundToInt(configEntry.value1);
            switch (effect)
            {
                case ApplyBlockEffect applyBlock:
                    applyBlock.blockAmount = v;
                    break;
                case GainBuffEffect gainBuff:
                    gainBuff.amount = v;
                    break;
                case MultiStatBuffEffect multi:
                    // 多属性类 config 未提供 value1（各字段由资产配置）
                    break;
                case DrawCardsEffect draw:
                    draw.drawCount = v;
                    break;
                case GainEnergyEffect energy:
                    energy.energyGain = v;
                    break;
                case HealPlayerEffect heal:
                    heal.healAmount = v;
                    break;
                case MaxHealthEffect maxHealth:
                    maxHealth.maxHealthGain = v;
                    break;
            }
        }

        private void LoadBaseEffectsIntoRelic(Relic relic)
        {
            if (relic == null) return;
            var cfg = RelicBalanceConfig.CreateDefaultConfig().GetEntry(relic.relicId);
            if (cfg == null) return;
            foreach (var entry in cfg.baseEffectIds)
            {
                if (entry == null || string.IsNullOrEmpty(entry.effectId)) continue;
                CardEffect effect = LoadEffect(entry.effectId);
                if (effect != null)
                {
                    ApplyConfigValue(effect, entry);
                    relic.relicEffects.Add(new RelicEffectInstance
                    {
                        effect = effect,
                        trigger = entry.trigger
                    });
                }
                else
                {
                    GameLogger.LogWarning($"[RelicManager] 遗物效果加载失败(LoadBase)：{entry.effectId}，遗物：{relic.relicName}");
                }
            }
        }

        private void UnregisterRelicEffects(Relic relic)
        {
            if (relic == null) return;
            registeredRelicIds.Remove(relic.relicId);
            relicsUsedThisBattle.Remove(relic.relicId);

            // 从 EffectManager 真正注销 handler，避免闭包泄漏与重复注册累积
            if (effectManager == null) effectManager = EffectManager.Instance;
            if (effectManager == null) return;

            if (relic.relicEffects == null) return;
            foreach (var entry in relic.relicEffects)
            {
                if (entry == null) continue;
                if (entry.valueModifier != null)
                {
                    effectManager.UnregisterValueModifier(entry.trigger, entry.valueModifier);
                    entry.valueModifier = null;
                }
                if (entry.handler != null)
                {
                    effectManager.Unregister(entry.trigger, entry.handler);
                    entry.handler = null;
                }
            }
        }



        public int GetBonusDamage() => 0;
        public float GetVictoryGoldPercent() => 0;
        public string GetInstantKillRelicId() => null;
        public float GetHealPercentEachTurn() => 0;

        private bool IsFactionUnlocked(CardFaction faction)
        {
            if (faction == CardFaction.None) return true;
            var fus = FactionUnlockService.Instance;
            if (fus == null) return false;
            return fus.IsFactionUnlocked(faction);
        }

        /// <summary>

        /// </summary>
        /// <summary>
        /// 加载全部遗物资产（不过滤阵营解锁/Boss/起始等）。
        /// 仅供调试工具使用；商店与掉落请用 LoadAllObtainableRelicAssets。
        /// </summary>
        public List<RelicDataAsset> LoadAllRelicAssets()
        {
            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>("Relics");
            var result = new List<RelicDataAsset>(allAssets);
            result.Sort((x, y) => string.Compare(x.relicName, y.relicName, System.StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public List<RelicDataAsset> LoadAllObtainableRelicAssets()
        {
            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>("Relics");
            List<RelicDataAsset> result = new List<RelicDataAsset>();

            foreach (var asset in allAssets)
            {

                if (asset.rarity == RelicRarity.Starting || asset.isBossRelic || asset.isFactionUnlocker)
                    continue;
                if (!IsFactionUnlocked(asset.faction))
                    continue;
                result.Add(asset);
            }
            return result;
        }

        public List<RelicDataAsset> LoadShopRelicAssets()
        {
            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>("Relics");
            List<RelicDataAsset> result = new List<RelicDataAsset>();

            foreach (var asset in allAssets)
            {
                if (!asset.isShopRelic)
                    continue;
                if (!IsFactionUnlocked(asset.faction))
                    continue;
                result.Add(asset);
            }
            return result;
        }

        public List<RelicDataAsset> LoadNonShopRelicAssets()
        {
            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>("Relics");
            List<RelicDataAsset> result = new List<RelicDataAsset>();

            foreach (var asset in allAssets)
            {
                if (asset.rarity == RelicRarity.Starting || asset.isShopRelic || asset.isBossRelic)
                    continue;
                if (!IsFactionUnlocked(asset.faction))
                    continue;
                result.Add(asset);
            }
            return result;
        }

        public Relic CreateRelicFromAsset(RelicDataAsset asset)
        {
            if (asset == null) return null;

            Relic relic = new Relic(
                asset.relicId,
                asset.relicName,
                asset.rarity,
                asset.faction,
                asset.description,
                asset.price
            );


            bool hasActivator = !string.IsNullOrEmpty(asset.hiddenActivatorRelicId) && HasRelic(asset.hiddenActivatorRelicId);
            var activeEffects = asset.GetActiveEffects(hasActivator);

            foreach (var entry in activeEffects)
            {
                if (entry == null || string.IsNullOrEmpty(entry.effectId)) continue;
                CardEffect effect = LoadEffect(entry.effectId);
                if (effect != null)
                {
                    ApplyConfigValue(effect, entry);
                    relic.relicEffects.Add(new RelicEffectInstance
                    {
                        effect = effect,
                        trigger = entry.trigger
                    });
                }
                else
                {
                    GameLogger.LogWarning($"[RelicManager] 遗物效果加载失败：{entry.effectId}，遗物：{asset.relicName}");
                }
            }

            if (!string.IsNullOrEmpty(asset.iconPath))
            {
                relic.icon = Resources.Load<Sprite>(asset.iconPath);

                if (relic.icon == null && !string.IsNullOrEmpty(asset.relicName))
                {
                    relic.icon = Resources.Load<Sprite>($"RelicsArt/{asset.relicName}");
                }

                if (relic.icon == null)
                    GameLogger.LogWarning($"[RelicManager] 遗物图标加载失败：iconPath={asset.iconPath}，遗物：{asset.relicName}");
            }
            else if (!string.IsNullOrEmpty(asset.relicName))
            {
                relic.icon = Resources.Load<Sprite>($"RelicsArt/{asset.relicName}");
                if (relic.icon == null)
                    GameLogger.LogWarning($"[RelicManager] 遗物图标加载失败：iconPath为空，尝试relicName={asset.relicName}");
            }

            return relic;
        }

        private static bool IsValueModifierTrigger(EffectTrigger trigger)
        {
            switch (trigger)
            {
                case EffectTrigger.CalculateAttackDamage:
                case EffectTrigger.CalculateBlock:
                case EffectTrigger.CalculateCardCost:
                case EffectTrigger.CalculatePlayerDamage:
                case EffectTrigger.CalculatePotionDropChance:
                    return true;
                default:
                    return false;
            }
        }

        private CardEffect LoadEffect(string effectId)
        {
            string effectPath = $"Effects/{effectId}";
            CardEffect loaded = Resources.Load<CardEffect>(effectPath);

            if (loaded == null)
                loaded = Resources.Load<CardEffect>($"CardEffects/{effectId}");

            if (loaded == null)
                loaded = Resources.Load<CardEffect>(effectId);

            if (loaded == null)
            {
                GameLogger.LogError(
                    $"[RelicManager] effectId='{effectId}' " +
                    $".asset Resources/Effects/{effectId} " +
                    $"?? Resources/CardEffects/{effectId} ?? Resources/{effectId}"
                );
                return null;
            }

            // 每个遗物各自克隆一份效果实例：共享同一资产时，
            // NonSerialized 的运行时状态（计数器、一次性标志等）会在遗物间串扰
            CardEffect effect = Instantiate(loaded);
            effect.name = loaded.name; // 去掉 "(Clone)" 后缀，保证 ResolveEffectTrigger 的 effectId 匹配
            return effect;
        }

        public Relic GenerateRandomRelic(List<RelicDataAsset> pool)
        {
            if (pool == null || pool.Count == 0) return null;

            List<RelicDataAsset> available = pool
                .Where(a => !HasRelic(a.relicId))
                .ToList();

            if (available.Count == 0) return null;

            RelicDataAsset chosen = available[UnityEngine.Random.Range(0, available.Count)];
            return CreateRelicFromAsset(chosen);
        }

        public List<Relic> GenerateRandomRelics(List<RelicDataAsset> pool, int count)
        {
            List<Relic> result = new List<Relic>();
            List<RelicDataAsset> available = pool
                .Where(a => !HasRelic(a.relicId))
                .ToList();

            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, available.Count);
                result.Add(CreateRelicFromAsset(available[index]));
                available.RemoveAt(index);
            }

            return result;
        }

        public Relic TryNormalMonsterRelicDrop()
        {
            if (UnityEngine.Random.value <= normalRelicDropChance)
            {
                var pool = LoadAllObtainableRelicAssets();
                Relic relic = GenerateRandomRelic(pool);
                if (relic != null)
                    GameLogger.Log($"[RelicManager] 普通怪掉落遗物：{relic.relicName}");
                return relic;
            }
            return null;
        }

        public Relic GetEliteMonsterRelicDrop()
        {
            var pool = LoadAllObtainableRelicAssets();
            Relic relic = GenerateRandomRelic(pool);
            if (relic != null)
                GameLogger.Log($"[RelicManager] 精英怪掉落遗物：{relic.relicName}");
            return relic;
        }

        public List<Relic> GenerateShopRelics(int count = 5)
        {
            var pool = LoadAllObtainableRelicAssets();
            return GenerateRandomRelics(pool, count);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}


