using System;
using System.IO;
using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 调试/开发者配置（persistentDataPath/debug_config.json）：
    ///   devMode        开发者模式——图鉴显示全部条目（不隐藏未见过内容）
    ///   consoleEnabled 控制台开关——上架包可在文件中置 true 开启控制台继续调试
    /// 规则：文件存在则按文件值；文件不存在时编辑器/开发构建默认开启，
    /// 正式包默认关闭（兼容旧 debug_enable 标记文件）。控制台 devmode 命令可写回文件。
    /// </summary>
    public static class DevConfig
    {
        [Serializable]
        private class ConfigData
        {
            public bool devMode = true;
            public bool consoleEnabled = true;
        }

        private static bool _loaded;
        private static bool _fileExists;
        private static bool _legacyMarker;
        private static ConfigData _data = new ConfigData();

        public static string FilePath => Path.Combine(Application.persistentDataPath, "debug_config.json");

        public static bool DevMode
        {
            get
            {
                EnsureLoaded();
                if (Application.isEditor || UnityEngine.Debug.isDebugBuild) return true; // 编辑器/开发构建始终开发者模式（Debug 会被 MutationChess.Debug 命名空间遮蔽，必须全限定）
                return _fileExists ? _data.devMode : false;
            }
        }

        public static bool ConsoleEnabled
        {
            get
            {
                EnsureLoaded();
                if (Application.isEditor || UnityEngine.Debug.isDebugBuild) return true; // 编辑器/开发构建始终可用
                return _fileExists ? _data.consoleEnabled : _legacyMarker;
            }
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                if (File.Exists(FilePath))
                {
                    ConfigData d = JsonUtility.FromJson<ConfigData>(File.ReadAllText(FilePath));
                    if (d != null) _data = d;
                    _fileExists = true;
                }
            }
            catch (Exception e)
            {
                GameLogger.LogWarning($"[DevConfig] 读取配置失败：{e.Message}");
            }
            if (!_fileExists) _legacyMarker = CheckLegacyMarker();
            GameLogger.Log($"[DevConfig] {FilePath} → devMode={DevMode} consoleEnabled={ConsoleEnabled}" +
                           (_fileExists ? "（来自文件）" : "（默认值，正式包可写此文件开启）"));
        }

        /// <summary>开发者模式开关（控制台 devmode 命令用，写回文件立即生效）。</summary>
        public static void SetDevMode(bool on)
        {
            EnsureLoaded();
            _data.devMode = on;
            Save();
        }

        /// <summary>控制台开关（写回文件；正式包下次启动按文件值生效）。</summary>
        public static void SetConsoleEnabled(bool on)
        {
            EnsureLoaded();
            _data.consoleEnabled = on;
            Save();
        }

        private static void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(_data, true));
                _fileExists = true;
                GameLogger.Log($"[DevConfig] 已写入 {FilePath}");
            }
            catch (Exception e)
            {
                GameLogger.LogError($"[DevConfig] 写入失败：{e.Message}");
            }
        }

        /// <summary>旧机制兼容：exe 同级或 StreamingAssets 下的 debug_enable 标记文件。</summary>
        private static bool CheckLegacyMarker()
        {
            string[] candidates =
            {
                Application.dataPath + "/../debug_enable",
                Application.dataPath + "/../debug_enable.txt",
                Application.streamingAssetsPath + "/debug_enable",
                Application.streamingAssetsPath + "/debug_enable.txt"
            };
            foreach (string path in candidates)
            {
                try
                {
                    if (File.Exists(path)) return true;
                }
                catch (Exception) { /* 路径不可访问时忽略，继续检查下一个 */ }
            }
            return false;
        }
    }
}
