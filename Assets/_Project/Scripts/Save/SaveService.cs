using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 存档服务（懒加载单例，运行时自动创建，无需场景接线）：
    /// 收集已注册的 ISaveable 状态，序列化为 JSON 写入
    /// Application.persistentDataPath/saves/slot{N}.json（1~3 号槽位）。
    /// 三个存档位 + 以撒式存档页面（SaveSlotPanel）；ActiveSlot 记录当前活动槽位；
    /// 退出游戏（Application.quitting）时若对局进行中则自动写入活动槽位，
    /// 实现"保留上次退出时游戏内的时刻"（地图/战斗快照由各 ISaveable 提供）。
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        public const int MaxSlots = 3;
        private const string ActiveSlotKey = "ActiveSlot";

        [Serializable]
        public class SaveSlotMeta
        {
            public int slot;
            public string difficulty;
            public int floor;
            public string savedAt;
            public int hp;
            public int maxHp;
            public int gold;
            public long playtimeSeconds;
            public int codexCardsSeen;
            public int codexRelicsSeen;
            public int codexPotionsSeen;
        }

        [Serializable] public class SaveEntry { public string key; public string json; }
        [Serializable] public class SaveFileData { public int version = 1; public int slot; public SaveSlotMeta meta; public List<SaveEntry> entries; }

        private static SaveService _instance;
        public static SaveService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SaveService>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SaveService");
                        _instance = go.AddComponent<SaveService>();
                    }
                }
                return _instance;
            }
        }

        private readonly List<ISaveable> savables = new List<ISaveable>();

        /// <summary>待读档槽位（跨场景静态标记：首页"继续游戏"→ 进入主场景后由 GameManager 消费）。0 表示无。</summary>
        private static int pendingLoadSlot = 0;

        /// <summary>当前活动槽位（静态，跨场景存活；持久化到 PlayerPrefs 供首页刷新显示）。</summary>
        private static int activeSlot = -1;

        /// <summary>是否有一局进行中的游戏（GameManager 开新局/读档后置 true，失败/胜利回首页前置 false）。
        /// 退出游戏时据此决定是否自动存档——保留"上次退出游戏内的时刻"。</summary>
        private static bool runActive = false;

        public static string SaveDir => Path.Combine(Application.persistentDataPath, "saves");
        public static string SlotPath(int slot) => Path.Combine(SaveDir, $"slot{slot}.json");

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景保留注册表（场景存档对象随场景重建后重新注册）
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void OnApplicationQuit()
        {
            // 对局进行中退出：把游戏内时刻写入活动槽位，下次"继续游戏"原样接续
            if (!runActive) return;
            if (savables.Count == 0) return;
            try
            {
                SaveGame(GetActiveSlot());
                GameLogger.Log($"[存档] 退出时已自动保存活动槽位 {GetActiveSlot()}");
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[存档] 退出自动存档失败：{e.Message}");
            }
        }

        // ================= 活动槽位 =================

        /// <summary>获取活动槽位（1~MaxSlots；从未设置时读 PlayerPrefs，默认 1）。</summary>
        public static int GetActiveSlot()
        {
            if (activeSlot < 1)
                activeSlot = Mathf.Clamp(PlayerPrefs.GetInt(ActiveSlotKey, 1), 1, MaxSlots);
            return activeSlot;
        }

        /// <summary>设置活动槽位并持久化（存档页面选择"新游戏/继续游戏"槽位时调用）。</summary>
        public static void SetActiveSlot(int slot)
        {
            slot = Mathf.Clamp(slot, 1, MaxSlots);
            activeSlot = slot;
            PlayerPrefs.SetInt(ActiveSlotKey, slot);
            PlayerPrefs.Save();
            GameLogger.Log($"[存档] 活动槽位 → {slot}");
        }

        /// <summary>标记一局进行中/结束（GameManager 在新局开始与对局结束回首页时调用）。</summary>
        public static void MarkRunActive(bool active)
        {
            runActive = active;
        }

        public static bool IsRunActive() => runActive;

        // ================= 注册 =================

        public void Register(ISaveable s)
        {
            if (s == null || savables.Contains(s)) return;
            savables.Add(s);
            GameLogger.Log($"[存档] 已注册存档对象：{s.SaveKey}");
        }

        public void Unregister(ISaveable s)
        {
            savables.Remove(s);
        }

        public int RegisteredCount => savables.Count;

        // ================= 存/读/删 =================

        public bool SaveGame(int slot)
        {
            if (!IsValidSlot(slot)) return false;

            PruneDeadSavables();

            SaveFileData data = new SaveFileData
            {
                slot = slot,
                meta = BuildMeta(slot),
                entries = new List<SaveEntry>()
            };

            foreach (var s in savables)
            {
                try
                {
                    data.entries.Add(new SaveEntry { key = s.SaveKey, json = s.SerializeState() });
                }
                catch (Exception e)
                {
                    GameLogger.LogError($"[存档] {s.SaveKey} 序列化失败：{e.Message}");
                }
            }

            try
            {
                Directory.CreateDirectory(SaveDir);
                File.WriteAllText(SlotPath(slot), JsonUtility.ToJson(data, true));
                GameLogger.Log($"[存档] 已保存槽位 {slot}（{data.meta.difficulty} · 第 {data.meta.floor} 层 · HP {data.meta.hp}/{data.meta.maxHp}）→ {SlotPath(slot)}");
                return true;
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[存档] 写入失败：{e.Message}");
                return false;
            }
        }

        public bool LoadGame(int slot)
        {
            if (!HasSave(slot)) return false;

            PruneDeadSavables();

            try
            {
                SaveFileData data = JsonUtility.FromJson<SaveFileData>(File.ReadAllText(SlotPath(slot)));
                if (data == null || data.entries == null)
                {
                    GameLogger.LogError($"[存档] 槽位 {slot} 数据为空或损坏");
                    return false;
                }

                foreach (var entry in data.entries)
                {
                    if (string.IsNullOrEmpty(entry?.key)) continue;
                    ISaveable s = savables.Find(x => x.SaveKey == entry.key);
                    if (s == null)
                    {
                        GameLogger.LogWarning($"[存档] 未注册的存档条目，跳过：{entry.key}");
                        continue;
                    }
                    try
                    {
                        s.DeserializeState(entry.json);
                    }
                    catch (Exception e)
                    {
                        GameLogger.LogError($"[存档] {entry.key} 反序列化失败：{e.Message}");
                    }
                }

                GameLogger.Log($"[存档] 已读取槽位 {slot}（难度 {data.meta?.difficulty} · 第 {data.meta?.floor} 层）");
                return true;
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[存档] 读取失败：{e.Message}");
                return false;
            }
        }

        public bool DeleteSave(int slot)
        {
            if (!IsValidSlot(slot)) return false;
            try
            {
                if (!File.Exists(SlotPath(slot))) return false;
                File.Delete(SlotPath(slot));
                GameLogger.Log($"[存档] 已删除槽位 {slot}");
                return true;
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[存档] 删除失败：{e.Message}");
                return false;
            }
        }

        public bool HasSave(int slot)
        {
            return IsValidSlot(slot) && File.Exists(SlotPath(slot));
        }

        public List<SaveSlotMeta> ListSaveSlots()
        {
            var list = new List<SaveSlotMeta>();
            for (int i = 1; i <= MaxSlots; i++)
            {
                if (!HasSave(i)) continue;
                try
                {
                    SaveFileData d = JsonUtility.FromJson<SaveFileData>(File.ReadAllText(SlotPath(i)));
                    if (d?.meta != null) list.Add(d.meta);
                }
                catch { /* 损坏文件跳过 */ }
            }
            return list;
        }

        /// <summary>读取单槽元数据（存档页面展示用；无存档返回 null）。</summary>
        public SaveSlotMeta GetMeta(int slot)
        {
            if (!HasSave(slot)) return null;
            try
            {
                SaveFileData d = JsonUtility.FromJson<SaveFileData>(File.ReadAllText(SlotPath(slot)));
                return d?.meta;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>自动存档：写入活动槽位（难度选定/楼层推进/战斗结束等节点调用）。
        /// 传入 1~MaxSlots 可显式指定槽位，0 或省略 = 活动槽位。</summary>
        public void AutoSave(int slot = 0)
        {
            if (savables.Count == 0)
            {
                GameLogger.LogWarning("[存档] 尚无注册的存档对象，跳过自动存档");
                return;
            }
            if (slot < 1) slot = GetActiveSlot();
            SaveGame(slot);
        }

        // ================= 待读档标记（首页"继续游戏" → 主场景消费） =================

        /// <summary>标记待读档槽位（首页"继续游戏"点击后调用，随后加载主场景）。</summary>
        public static void SetPendingLoad(int slot)
        {
            pendingLoadSlot = slot >= 1 && slot <= MaxSlots ? slot : 0;
            GameLogger.Log($"[存档] 已标记待读档槽位 {pendingLoadSlot}");
        }

        public static bool HasPendingLoad() => pendingLoadSlot >= 1;

        /// <summary>消费待读档标记（GameManager 读档流程开始时调用一次）。</summary>
        public static int ConsumePendingLoad()
        {
            int slot = pendingLoadSlot;
            pendingLoadSlot = 0;
            return slot;
        }

        // ================= 内部 =================

        /// <summary>清理随场景销毁的存档对象引用（SaveService 跨场景常驻，场景对象重建后会重新注册）。</summary>
        private void PruneDeadSavables()
        {
            savables.RemoveAll(s => s == null || (s is UnityEngine.Object uo && uo == null));
        }

        private bool IsValidSlot(int slot)
        {
            if (slot >= 1 && slot <= MaxSlots) return true;
            GameLogger.LogWarning($"[存档] 槽位越界：{slot}（合法范围 1~{MaxSlots}）");
            return false;
        }

        private SaveSlotMeta BuildMeta(int slot)
        {
            string difficulty = "未选择";
            DifficultyManager dm = DifficultyManager.Instance;
            if (dm != null && dm.HasChosen)
                difficulty = DifficultyManager.GetDisplayName(dm.CurrentDifficulty);

            int floor = 1;
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null) floor = gm.GetCurrentFloor();

            var meta = new SaveSlotMeta
            {
                slot = slot,
                difficulty = difficulty,
                floor = floor,
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 对局内数值快照（存档页面卡片展示）
            PlayerDataManager pdm = PlayerDataManager.Instance;
            if (pdm != null && pdm.PlayerData != null)
            {
                meta.hp = Mathf.Max(0, pdm.PlayerData.currentHealth);
                meta.maxHp = pdm.PlayerData.maxHealth;
                meta.gold = pdm.PlayerData.gold;
            }

            if (gm != null)
                meta.playtimeSeconds = gm.GetPlaytimeSeconds();

            // 图鉴收集进度（以撒式全成就页面的基础数据）
            CodexProgress codex = CodexProgress.Instance;
            if (codex != null)
            {
                meta.codexCardsSeen = codex.SeenCount(CodexCategory.Card);
                meta.codexRelicsSeen = codex.SeenCount(CodexCategory.Relic);
                meta.codexPotionsSeen = codex.SeenCount(CodexCategory.Potion);
            }

            return meta;
        }
    }
}
