using System;
using System.Collections.Generic;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 图鉴进度（以撒式"见过才解锁"）：
    /// 卡牌/遗物/药水在获得或展示时记录 codexId，随存档持久化（SaveKey "codex"）。
    /// 跨场景常驻单例（懒加载自动创建），首次解锁时打日志。
    /// </summary>
    public class CodexProgress : MonoBehaviour, ISaveable
    {
        [Serializable]
        private class CodexSaveData
        {
            public List<int> cards = new List<int>();
            public List<int> relics = new List<int>();
            public List<int> potions = new List<int>();
        }

        private static CodexProgress _instance;
        public static CodexProgress Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CodexProgress>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CodexProgress");
                        _instance = go.AddComponent<CodexProgress>();
                    }
                }
                return _instance;
            }
        }

        private readonly HashSet<int> seenCards = new HashSet<int>();
        private readonly HashSet<int> seenRelics = new HashSet<int>();
        private readonly HashSet<int> seenPotions = new HashSet<int>();

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject); // 图鉴进度随存档持久化，跨场景保留
            SaveService.Instance.Register(this);
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        // ================= 标记见过 =================

        public static void MarkCardSeen(CardDataAsset asset)
        {
            if (asset != null && asset.codexId > 0)
                MarkCardSeen(asset.codexId);
        }

        public static void MarkCardSeen(int codexId)
        {
            if (!CodexIds.IsCardId(codexId)) return;
            if (Instance.seenCards.Add(codexId))
                GameLogger.Log($"[图鉴] 解锁卡牌 No.{codexId}");
        }

        public static void MarkCardSeenByName(string cardName)
        {
            MarkCardSeen(CodexIdRegistry.FindCardByName(cardName));
        }

        public static void MarkRelicSeen(RelicDataAsset asset)
        {
            if (asset != null && asset.codexId > 0)
                MarkRelicSeen(asset.codexId);
        }

        public static void MarkRelicSeen(int codexId)
        {
            if (!CodexIds.IsRelicId(codexId)) return;
            if (Instance.seenRelics.Add(codexId))
                GameLogger.Log($"[图鉴] 解锁遗物 No.{codexId}");
        }

        public static void MarkRelicSeenByAssetId(string relicId)
        {
            MarkRelicSeen(CodexIdRegistry.FindRelicByAssetId(relicId));
        }

        public static void MarkPotionSeen(PotionDataAsset asset)
        {
            if (asset != null && asset.codexId > 0)
                MarkPotionSeen(asset.codexId);
        }

        public static void MarkPotionSeen(int codexId)
        {
            if (!CodexIds.IsPotionId(codexId)) return;
            if (Instance.seenPotions.Add(codexId))
                GameLogger.Log($"[图鉴] 解锁药水 No.{codexId}");
        }

        public static void MarkPotionSeenByName(string potionName)
        {
            MarkPotionSeen(CodexIdRegistry.FindPotionByName(potionName));
        }

        /// <summary>按命令解锁单个图鉴条目（只解锁不发放）。</summary>
        public static bool UnlockOne(int codexId)
        {
            CodexCategory? cat = CodexIds.CategoryOf(codexId);
            if (cat == null)
            {
                GameLogger.LogWarning($"[图鉴] 无效图鉴 ID：{codexId}");
                return false;
            }
            switch (cat.Value)
            {
                case CodexCategory.Card:
                    if (CodexIdRegistry.GetCard(codexId) == null) return UnlockFail(cat.Value, codexId);
                    MarkCardSeen(codexId);
                    return true;
                case CodexCategory.Relic:
                    if (CodexIdRegistry.GetRelic(codexId) == null) return UnlockFail(cat.Value, codexId);
                    MarkRelicSeen(codexId);
                    return true;
                case CodexCategory.Potion:
                    if (CodexIdRegistry.GetPotion(codexId) == null) return UnlockFail(cat.Value, codexId);
                    MarkPotionSeen(codexId);
                    return true;
            }
            return false;
        }

        private static bool UnlockFail(CodexCategory cat, int id)
        {
            GameLogger.LogWarning($"[图鉴] ID {id} 在{CodexIds.CategoryName(cat)}段内无对应资产");
            return false;
        }

        /// <summary>全部解锁（调试命令 seeall）。</summary>
        public void UnlockAll()
        {
            int added = 0;
            foreach (var c in CodexIdRegistry.GetCardsByIdOrdered())
                if (seenCards.Add(c.codexId)) added++;
            foreach (var r in CodexIdRegistry.GetRelicsByIdOrdered())
                if (seenRelics.Add(r.codexId)) added++;
            foreach (var p in CodexIdRegistry.GetPotionsByIdOrdered())
                if (seenPotions.Add(p.codexId)) added++;
            GameLogger.Log($"[图鉴] 全部解锁完成（新增 {added} 条）：卡牌 {seenCards.Count} · 遗物 {seenRelics.Count} · 药水 {seenPotions.Count}");
        }

        // ================= 查询 =================

        public bool IsCardSeen(int codexId) => seenCards.Contains(codexId);
        public bool IsRelicSeen(int codexId) => seenRelics.Contains(codexId);
        public bool IsPotionSeen(int codexId) => seenPotions.Contains(codexId);

        public int SeenCount(CodexCategory cat)
        {
            switch (cat)
            {
                case CodexCategory.Card: return seenCards.Count;
                case CodexCategory.Relic: return seenRelics.Count;
                case CodexCategory.Potion: return seenPotions.Count;
                default: return 0;
            }
        }

        public int TotalCount(CodexCategory cat)
        {
            switch (cat)
            {
                case CodexCategory.Card: return CodexIdRegistry.GetCardsByIdOrdered().Count;
                case CodexCategory.Relic: return CodexIdRegistry.GetRelicsByIdOrdered().Count;
                case CodexCategory.Potion: return CodexIdRegistry.GetPotionsByIdOrdered().Count;
                default: return 0;
            }
        }

        // ================= 存档接口 =================

        public string SaveKey => "codex";

        public string SerializeState()
        {
            var d = new CodexSaveData();
            d.cards.AddRange(seenCards);
            d.relics.AddRange(seenRelics);
            d.potions.AddRange(seenPotions);
            d.cards.Sort();
            d.relics.Sort();
            d.potions.Sort();
            return JsonUtility.ToJson(d);
        }

        public void DeserializeState(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            try
            {
                CodexSaveData d = JsonUtility.FromJson<CodexSaveData>(json);
                if (d == null) return;
                seenCards.Clear();
                seenRelics.Clear();
                seenPotions.Clear();
                if (d.cards != null) foreach (int id in d.cards) if (CodexIds.IsCardId(id)) seenCards.Add(id);
                if (d.relics != null) foreach (int id in d.relics) if (CodexIds.IsRelicId(id)) seenRelics.Add(id);
                if (d.potions != null) foreach (int id in d.potions) if (CodexIds.IsPotionId(id)) seenPotions.Add(id);
                GameLogger.Log($"[图鉴] 恢复图鉴进度：卡牌 {seenCards.Count} · 遗物 {seenRelics.Count} · 药水 {seenPotions.Count}");
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[图鉴] codex 反序列化失败：{e.Message}");
            }
        }
    }
}
