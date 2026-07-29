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
            GameLogger.Log($"[RelicManager] : {relic.relicName} ({relic.GetRarityName()})");
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
                    GameLogger.LogWarning($"[RelicManager] : {effectEntry.effectId}");
                    continue;
                }
                var capturedEntry = effectEntry;
                var capturedRelic = relic;
                var capturedEffect = effect;

                effectManager.Register(capturedEntry.trigger, (ctx) =>
                {
                    if (!ownedRelics.Contains(capturedRelic)) return;
                    capturedEffect.Execute(ctx);
                });


                relic.relicEffects.Add(new RelicEffectInstance
                {
                    effect = capturedEffect,
                    trigger = capturedEntry.trigger
                });

                GameLogger.Log($"[RelicManager] {relic.relicName} : {capturedEffect.GetType().Name} ({capturedEntry.trigger})");
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

                    GameLogger.Log($"[RelicManager]  {newRelic.relicName} 滻 {existing.relicName}");
                    toRemove.Add(existing);
                }
                else
                {

                    if (UnityEngine.Random.value < 0.5f)
                    {
                        GameLogger.Log($"[RelicManager]  {newRelic.relicName} 滻 {existing.relicName}");
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
                GameLogger.Log($"[RelicManager] : {relic.relicName}");
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

                effectManager.Register(capturedEntry.trigger, (ctx) =>
                {
                    if (!ownedRelics.Contains(capturedRelic)) return;
                    capturedEntry.effect.Execute(ctx);
                });

                GameLogger.Log($"[RelicManager] {relic.relicName} : {capturedEntry.effect.GetType().Name} ({capturedEntry.trigger})");
            }
        }

        /// <summary>

        /// </summary>
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
                    relic.relicEffects.Add(new RelicEffectInstance
                    {
                        effect = effect,
                        trigger = entry.trigger
                    });
                }
                else
                {
                    GameLogger.LogWarning($"[RelicManager] : {entry.effectId}");
                }
            }
        }

        private void UnregisterRelicEffects(Relic relic)
        {
            if (relic == null) return;
            registeredRelicIds.Remove(relic.relicId);
            relicsUsedThisBattle.Remove(relic.relicId);
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
                    relic.relicEffects.Add(new RelicEffectInstance
                    {
                        effect = effect,
                        trigger = entry.trigger
                    });
                }
                else
                {
                    GameLogger.LogWarning($"[RelicManager] : {entry.effectId}");
                }
            }

            if (!string.IsNullOrEmpty(asset.iconPath))
            {
                relic.icon = Resources.Load<Sprite>(asset.iconPath);
                if (relic.icon == null)
                    GameLogger.LogWarning($"[RelicManager] : {asset.iconPath}");
            }

            return relic;
        }

        private CardEffect LoadEffect(string effectId)
        {
            string effectPath = $"Effects/{effectId}";
            CardEffect effect = Resources.Load<CardEffect>(effectPath);

            if (effect == null)
                effect = Resources.Load<CardEffect>($"CardEffects/{effectId}");

            if (effect == null)
                effect = Resources.Load<CardEffect>(effectId);

            if (effect == null)
            {
                GameLogger.LogError(
                    $"[RelicManager] 遗物效果加载失败：找不到 effectId='{effectId}' 的资源。" +
                    $"请确认以下路径存在 .asset 文件：Resources/Effects/{effectId} " +
                    $"或 Resources/CardEffects/{effectId} 或 Resources/{effectId}"
                );
            }

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
                    GameLogger.Log($"[RelicManager] : {relic.relicName}");
                return relic;
            }
            return null;
        }

        public Relic GetEliteMonsterRelicDrop()
        {
            var pool = LoadAllObtainableRelicAssets();
            Relic relic = GenerateRandomRelic(pool);
            if (relic != null)
                GameLogger.Log($"[RelicManager] : {relic.relicName}");
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


