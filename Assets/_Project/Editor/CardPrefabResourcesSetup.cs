using System.IO;
using UnityEditor;
using UnityEngine;

namespace MutationChess.EditorTools
{
    /// <summary>
    /// 卡牌预制体 Resources 同步（域重载后执行一次，幂等）：
    /// 牌库档案等运行时界面用 Resources.Load 展示真实卡牌预制体（CardPrefab.prefab），
    /// 但源预制体位于 Assets/_Project/Prefabs/Card/ 不在 Resources 目录下。
    /// 这里把源预制体复制到 Resources/Prefabs/CardPrefab.prefab（源缺失或较新时重新复制），
    /// 保证运行时加载到的始终与编辑器内编辑的预制体一致。
    /// </summary>
    [InitializeOnLoad]
    public static class CardPrefabResourcesSetup
    {
        private const string SourcePath = "Assets/_Project/Prefabs/Card/CardPrefab.prefab";
        private const string DestPath = "Assets/_Project/Resources/Prefabs/CardPrefab.prefab";

        static CardPrefabResourcesSetup()
        {
            EditorTaskGuard.RunWhenSafe(Sync);
        }

        /// <summary>手动入口：自动执行失败时可用菜单补执行。</summary>
        [MenuItem("工具/同步卡牌预制体到 Resources")]
        public static void SyncMenu()
        {
            EditorTaskGuard.RunWhenSafe(Sync);
            UnityEngine.Debug.Log("[CardPrefabResourcesSetup] 已提交同步任务（若正在 Play 模式，退出后自动执行）");
        }

        private static void Sync()
        {
            if (!File.Exists(SourcePath))
            {
                UnityEngine.Debug.LogWarning("[CardPrefabResourcesSetup] 源卡牌预制体不存在：" + SourcePath);
                return;
            }

            string srcFull = Path.GetFullPath(SourcePath);
            string destFull = Path.GetFullPath(DestPath);

            // 目标存在且比源新 → 无需同步
            if (File.Exists(destFull) &&
                File.GetLastWriteTimeUtc(destFull) >= File.GetLastWriteTimeUtc(srcFull))
                return;

            string dir = Path.GetDirectoryName(DestPath).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            if (File.Exists(destFull))
            {
                // 覆盖前先读入内存：AssetDatabase 删除后再 CopyAsset，避免 .meta 漂移
                AssetDatabase.DeleteAsset(DestPath);
            }
            AssetDatabase.CopyAsset(SourcePath, DestPath);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log("[CardPrefabResourcesSetup] 已同步卡牌预制体到 Resources：" + DestPath);
        }
    }
}
