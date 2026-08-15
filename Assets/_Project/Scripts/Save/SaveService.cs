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
    /// 当前阶段提供完整接口 + 自动存档（难度选定/楼层推进时写入槽位 1），
    /// 读档入口 UI 待后续接入。
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        public const int MaxSlots = 3;

        [Serializable] public class SaveSlotMeta { public int slot; public string difficulty; public int floor; public string savedAt; }
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
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

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
                GameLogger.Log($"[存档] 已保存槽位 {slot}（{data.meta.difficulty} · 第 {data.meta.floor} 层）→ {SlotPath(slot)}");
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

        /// <summary>自动存档：写入默认槽位 1（难度选定/楼层推进等节点调用）。</summary>
        public void AutoSave(int slot = 1)
        {
            if (savables.Count == 0)
            {
                GameLogger.LogWarning("[存档] 尚无注册的存档对象，跳过自动存档");
                return;
            }
            SaveGame(slot);
        }

        // ================= 内部 =================

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

            return new SaveSlotMeta
            {
                slot = slot,
                difficulty = difficulty,
                floor = floor,
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
    }
}
