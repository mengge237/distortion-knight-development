using System;
using MutationChess.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MutationChess.Battle;

namespace MutationChess.Core
{
    [Serializable]
    public class RelicsSaveData
    {
        public List<string> relicIds = new List<string>();
    }

    public class RelicManager : MonoBehaviour, ISaveable
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

        /// <summary>承咒之鼎已发放的最大生命加成（诅咒增减时重算差额，防止重复叠加）。</summary>
        private int curseVesselBonusApplied = 0;

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

            // 存档接口注册 + 承咒之鼎加成结算（读档恢复后重算）
            SaveService.Instance.Register(this);
            RecalculateCurseVesselBonus();
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
            // 图鉴"见过才解锁"：获得遗物即记录
            CodexProgress.MarkRelicSeenByAssetId(relic.relicId);
            RegisterRelicEffects(relic);
            GameLogger.Log($"[RelicManager] 获得遗物：{relic.relicName} ({relic.GetRarityName()})");
            OnRelicsChanged?.Invoke();
            AudioManager.Instance?.PlayRelicAcquired();


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
            else if (IsGoldenKingdomRelic(relic.relicId))
            {
                // 黄金王国遗物与 Boss 遗物同级：获得即重算全部隐藏效果（金银互为激活者）
                GameLogger.Log($"[RelicManager] 黄金王国遗物 {relic.relicName} 入库：王国共鸣重算");
                RefreshAllHiddenEffects();
            }
            else
            {

                TryActivateHiddenEffectsForRelic(relic);
            }

            // 遗物增减后检查共鸣组合（Isaac 式"化学反应"）
            RelicSynergyService.Instance?.RefreshCombos();

            // 黑烛拾取：立即驱散身上所有诅咒（烛火驱邪）
            if (relic.relicId == RelicIds.Shop_BlackCandle)
            {
                List<Relic> curses = ownedRelics
                    .Where(r => CurseSystem.IsCurseId(r.relicId))
                    .ToList();
                foreach (var c in curses)
                {
                    GameLogger.Log($"[RelicManager] 黑烛驱邪：诅咒「{c.relicName}」已消散");
                    RemoveRelic(c.relicId);
                }
            }

            // 承咒之鼎/诅咒增减：重算最大生命加成
            RecalculateCurseVesselBonus();
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

        /// <summary>黄金王国遗物（金/银）判定：机制级贪婪遗物，获得即重算全部隐藏效果。</summary>
        private static bool IsGoldenKingdomRelic(string relicId)
        {
            return relicId == RelicIds.Gold_GoldenKingdom_Gold || relicId == RelicIds.Gold_GoldenKingdom_Silver;
        }

        /// <summary>隐藏效果激活者判定：金银互为激活者（持有任一即视为持有对方）。</summary>
        private bool HasActivatorFor(string activatorId)
        {
            if (string.IsNullOrEmpty(activatorId)) return false;
            if (HasRelic(activatorId)) return true;
            if (activatorId == RelicIds.Gold_GoldenKingdom_Gold) return HasRelic(RelicIds.Gold_GoldenKingdom_Silver);
            if (activatorId == RelicIds.Gold_GoldenKingdom_Silver) return HasRelic(RelicIds.Gold_GoldenKingdom_Gold);
            return false;
        }

        /// <summary>

        /// </summary>
        private void TryActivateHiddenEffectsForRelic(Relic relic)
        {
            if (relic == null) return;
            if (activatedHiddenRelicIds.Contains(relic.relicId)) return;


            var cfg = RelicBalanceConfig.CreateDefaultConfig().GetEntry(relic.relicId);
            if (cfg == null || string.IsNullOrEmpty(cfg.hiddenActivatorRelicId)) return;


            if (!HasActivatorFor(cfg.hiddenActivatorRelicId)) return;


            GameLogger.Log($"[RelicManager]  {relic.relicName}  {cfg.hiddenActivatorRelicId} ");
            activatedHiddenRelicIds.Add(relic.relicId);
            AudioManager.Instance?.PlayHiddenAwaken();

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
                        AudioManager.Instance?.PlayRelicTick();
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
                        AudioManager.Instance?.PlayRelicTick();
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
                RecalculateCurseVesselBonus();
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

        /// <summary>
        /// 已装备的阵营组件（按获得顺序）：角色最多携带两个阵营（Boss 遗物/阵营遗物），
        /// 双阵营互相协同，并决定花瓣能量 UI 的主题花色。
        /// </summary>
        public List<CardFaction> GetEquippedFactions(int maxCount = 2)
        {
            List<CardFaction> result = new List<CardFaction>();
            foreach (var relic in ownedRelics)
            {
                if (relic == null || relic.faction == CardFaction.None) continue;
                if (result.Contains(relic.faction)) continue;
                result.Add(relic.faction);
                if (result.Count >= maxCount) break;
            }
            return result;
        }

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
                        AudioManager.Instance?.PlayRelicTick();
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
                        AudioManager.Instance?.PlayRelicTick();
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
        ///   MaxHealthEffect.maxHealthGain / VictoryGoldEffect.*（按资产默认值推断目标字段）
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
                case VictoryGoldEffect gold:
                    // value1 目标字段由资产默认值推断（三类金币资产各只有一个非零模式字段）：
                    // fixedGold>0 → 固定金币（原 Gain12GoldOnVictoryEffect 资产）
                    // goldBonusMultiplier>0 → 倍率（原 GoldBonusEffect 资产）
                    // 其余 → 百分比（原 VictoryGoldPercentEffect 资产）
                    if (gold.fixedGold > 0)
                        gold.fixedGold = v;
                    else if (gold.goldBonusMultiplier > 0f)
                        gold.goldBonusMultiplier = configEntry.value1;
                    else
                        gold.goldPercent = configEntry.value1;
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
            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics);
            var result = new List<RelicDataAsset>(allAssets);
            result.Sort((x, y) => string.Compare(x.relicName, y.relicName, System.StringComparison.OrdinalIgnoreCase));
            return result;
        }

        public List<RelicDataAsset> LoadAllObtainableRelicAssets()
        {
            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics);
            List<RelicDataAsset> result = new List<RelicDataAsset>();

            foreach (var asset in allAssets)
            {

                if (asset.rarity == RelicRarity.Starting || asset.isBossRelic || asset.isFactionUnlocker || asset.isCurse)
                    continue;
                if (IsGoldenKingdomRelic(asset.relicId))
                    continue; // 黄金王国·金/银有专属获取途径（时间/商店），不进常规掉落池
                if (!IsFactionUnlocked(asset.faction))
                    continue;
                result.Add(asset);
            }
            return result;
        }

        public List<RelicDataAsset> LoadShopRelicAssets()
        {
            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics);
            List<RelicDataAsset> result = new List<RelicDataAsset>();

            foreach (var asset in allAssets)
            {
                if (!asset.isShopRelic || asset.isCurse)
                    continue;
                if (asset.powerTier == PowerTier.Mechanic)
                    continue; // 机制级强力遗物不入商店（如反咒之镜）
                if (!IsFactionUnlocked(asset.faction))
                    continue;
                result.Add(asset);
            }
            return result;
        }

        public List<RelicDataAsset> LoadNonShopRelicAssets()
        {
            RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics);
            List<RelicDataAsset> result = new List<RelicDataAsset>();

            foreach (var asset in allAssets)
            {
                if (asset.rarity == RelicRarity.Starting || asset.isShopRelic || asset.isBossRelic || asset.isCurse)
                    continue;
                if (IsGoldenKingdomRelic(asset.relicId))
                    continue; // 黄金王国·金仅随时间获得，不进商店货架
                if (asset.powerTier == PowerTier.Mechanic)
                    continue; // 机制级强力遗物不进商店货架（仅极低概率掉落）
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


            bool hasActivator = !string.IsNullOrEmpty(asset.hiddenActivatorRelicId) && HasActivatorFor(asset.hiddenActivatorRelicId);
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
                    relic.icon = Resources.Load<Sprite>($"{ResourcePaths.RelicsArt}/{asset.relicName}");
                }

                if (relic.icon == null)
                    GameLogger.LogWarning($"[RelicManager] 遗物图标加载失败：iconPath={asset.iconPath}，遗物：{asset.relicName}");
            }
            else if (!string.IsNullOrEmpty(asset.relicName))
            {
                relic.icon = Resources.Load<Sprite>($"{ResourcePaths.RelicsArt}/{asset.relicName}");
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
            CardEffect loaded = Resources.Load<CardEffect>($"{ResourcePaths.Effects}/{effectId}");

            if (loaded == null)
            {
                GameLogger.LogError(
                    $"[RelicManager] effectId='{effectId}' 未找到：" +
                    $"Resources/{ResourcePaths.Effects}/{effectId}.asset"
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

            RelicDataAsset chosen = WeightedPickRelic(available);
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
                RelicDataAsset chosen = WeightedPickRelic(available);
                available.Remove(chosen);
                result.Add(CreateRelicFromAsset(chosen));
            }

            return result;
        }

        /// <summary>
        /// 按强度分级加权抽取遗物：机制级强力效果（改变游戏机制）在稀有度体系之上再度降权。
        /// </summary>
        private static RelicDataAsset WeightedPickRelic(List<RelicDataAsset> available)
        {
            float total = 0f;
            foreach (var a in available) total += GetRelicDropWeight(a);

            float roll = UnityEngine.Random.Range(0f, total);
            foreach (var a in available)
            {
                roll -= GetRelicDropWeight(a);
                if (roll <= 0f) return a;
            }
            return available[available.Count - 1];
        }

        /// <summary>遗物掉落权重：机制级 = 0.1（大幅降权），数值级 = 1.0。</summary>
        public static float GetRelicDropWeight(RelicDataAsset a)
        {
            if (a == null) return 0f;
            return a.powerTier == PowerTier.Mechanic ? PowerTierWeights.Mechanic : PowerTierWeights.Normal;
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
            // 商店货架不进机制级强力遗物（仅极低概率掉落）
            var pool = LoadAllObtainableRelicAssets()
                .Where(a => a.powerTier != PowerTier.Mechanic)
                .ToList();
            return GenerateRandomRelics(pool, count);
        }

        /// <summary>
        /// 承咒之鼎：每持有 1 个诅咒最大生命 +6。
        /// 诅咒/鼎增减时重算差额（desired - applied），防止重复叠加；
        /// 血上限下降时同步压低当前生命。
        /// </summary>
        private void RecalculateCurseVesselBonus()
        {
            PlayerDataManager pdm = PlayerDataManager.Instance;
            if (pdm == null)
            {
                curseVesselBonusApplied = 0; // 下次结算重试
                return;
            }

            int desired = HasRelic(RelicIds.Shop_CurseVessel)
                ? CurseSystem.HeldCurseCount(this) * 6
                : 0;
            int delta = desired - curseVesselBonusApplied;
            if (delta == 0) return;

            curseVesselBonusApplied = desired;
            pdm.AddMaxHealth(delta);

            if (delta < 0)
            {
                PlayerData data = pdm.GetPlayerData();
                if (data != null && data.currentHealth > data.maxHealth)
                    pdm.TakeDamage(data.currentHealth - data.maxHealth, true);
            }
            GameLogger.Log($"[RelicManager] 承咒之鼎结算：最大生命 {delta:+0;-0}");
        }

        // ================= 存档接口 =================

        public string SaveKey => "relics";

        public string SerializeState()
        {
            return JsonUtility.ToJson(new RelicsSaveData
            {
                relicIds = ownedRelics.Select(r => r.relicId).ToList()
            });
        }

        public void DeserializeState(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                RelicsSaveData d = JsonUtility.FromJson<RelicsSaveData>(json);
                if (d == null || d.relicIds == null) return;

                // 清空现有遗物后按存档重建
                foreach (var r in new List<Relic>(ownedRelics))
                    RemoveRelic(r.relicId);

                RelicDataAsset[] allAssets = Resources.LoadAll<RelicDataAsset>(ResourcePaths.Relics);
                foreach (var id in d.relicIds)
                {
                    RelicDataAsset asset = allAssets.FirstOrDefault(a => a != null && a.relicId == id);
                    if (asset == null)
                    {
                        GameLogger.LogWarning($"[存档] 遗物资产不存在，跳过：{id}");
                        continue;
                    }
                    Relic relic = CreateRelicFromAsset(asset);
                    if (relic != null) AddRelic(relic);
                }
                GameLogger.Log($"[存档] 恢复遗物 {ownedRelics.Count} 件");
            }
            catch (System.Exception e)
            {
                GameLogger.LogError($"[存档] relics 反序列化失败：{e.Message}");
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}


