using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 显示设置静态助手：目标帧率 / 窗口模式（全屏·窗口·无边框）/ 分辨率 / 长宽比 / 画质 / 全屏开关
    /// 的 PlayerPrefs 读写与即时应用。首页设置面板与战斗内 SettingsManager 共用
    /// 同一份键值，两处修改互相可见；启动时调用 ApplyAll() 一次恢复全部设置。
    /// 语义：长宽比=比例约束（选比例后按比例挑最大分辨率并同步分辨率行）；分辨率=具体值
    /// （直接切换并把长宽比约束回退为"跟随分辨率"）；窗口模式只切模式不重设尺寸。
    /// </summary>
    public static class DisplaySettings
    {
        public const string KeyTargetFPS = "TargetFPS";      // 30/60/120/0（0=不限）
        public const string KeyWindowMode = "WindowMode";    // 0=全屏 1=窗口 2=无边框
        public const string KeyAspectRatio = "AspectRatio";  // 0=跟随分辨率 1=16:9 2=16:10 3=4:3 4=21:9
        public const string KeyResOption = "ResOptionIndex"; // 分辨率候选索引（固定常见列表）
        public const string KeyQuality = "QualityIndex";     // 画质等级索引（映射 QualitySettings.names）

        public static readonly int[] TargetFpsOptions = { 30, 60, 120, 0 };
        public static readonly string[] WindowModeNames = { "全屏", "窗口", "无边框" };
        public static readonly string[] AspectRatioNames = { "跟随分辨率", "16:9 宽屏", "16:10", "4:3 传统", "21:9 影院" };
        public static readonly float[] AspectRatioValues = { 0f, 16f / 9f, 16f / 10f, 4f / 3f, 21f / 9f };
        public static readonly int[] ResolutionWidths = { 1920, 1600, 1366, 1280, 1024, 2560, 3840 };
        public static readonly int[] ResolutionHeights = { 1080, 900, 768, 720, 768, 1440, 2160 };
        public static readonly string[] ResolutionNames = { "1920×1080", "1600×900", "1366×768", "1280×720", "1024×768", "2560×1440", "3840×2160" };
        public static readonly string[] QualityNames = { "低", "中", "高" };

        // ================= 读取 =================

        public static int GetTargetFPS() => PlayerPrefs.GetInt(KeyTargetFPS, 0);

        public static int GetWindowMode() => Mathf.Clamp(PlayerPrefs.GetInt(KeyWindowMode, 0), 0, 2);

        public static int GetAspectRatioIndex() => Mathf.Clamp(PlayerPrefs.GetInt(KeyAspectRatio, 0), 0, AspectRatioNames.Length - 1);

        /// <summary>画质索引（clamp 到项目实际等级数，默认最高画质）。</summary>
        public static int GetQualityIndex()
        {
            int max = Mathf.Max(0, QualitySettings.names.Length - 1);
            return Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, max), 0, max);
        }

        /// <summary>分辨率候选索引：有保存键用保存键，否则按当前窗口尺寸就近匹配。</summary>
        public static int GetResOptionIndex()
        {
            if (PlayerPrefs.HasKey(KeyResOption))
                return Mathf.Clamp(PlayerPrefs.GetInt(KeyResOption), 0, ResolutionNames.Length - 1);
            return GetNearestResOption(Screen.width, Screen.height);
        }

        /// <summary>把任意宽高映射到最近的候选分辨率索引（旧分辨率下拉改动后同步步进行用）。</summary>
        public static int GetNearestResOption(int width, int height)
        {
            int best = 0;
            long bestDiff = long.MaxValue;
            for (int i = 0; i < ResolutionNames.Length; i++)
            {
                long diff = Mathf.Abs(ResolutionWidths[i] - width) + Mathf.Abs(ResolutionHeights[i] - height);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>目标帧率显示名（0 → "不限"）。</summary>
        public static string GetTargetFpsLabel()
        {
            int fps = GetTargetFPS();
            return fps > 0 ? $"{fps} FPS" : "不限";
        }

        public static string GetWindowModeLabel() => WindowModeNames[GetWindowMode()];

        public static string GetAspectRatioLabel() => AspectRatioNames[GetAspectRatioIndex()];

        public static string GetResOptionLabel() => ResolutionNames[GetResOptionIndex()];

        public static string GetQualityLabel() => QualityNames[GetQualityIndex()];

        // ================= 设置 =================

        /// <summary>设置目标帧率：>0 时关闭垂直同步（否则 vsync 会锁住实际帧率），0 表示不限。</summary>
        public static void SetTargetFPS(int fps)
        {
            PlayerPrefs.SetInt(KeyTargetFPS, fps);
            PlayerPrefs.Save();
            ApplyTargetFPS();
        }

        public static void SetWindowMode(int mode)
        {
            mode = Mathf.Clamp(mode, 0, 2);
            PlayerPrefs.SetInt(KeyWindowMode, mode);
            // 旧布尔键同步（战斗内旧全屏开关/旧逻辑读它）：窗口=0，全屏与无边框=1
            PlayerPrefs.SetInt("Fullscreen", mode == 1 ? 0 : 1);
            PlayerPrefs.Save();
            ApplyWindowMode();
        }

        /// <summary>旧布尔全屏开关兼容入口：开 → 全屏，关 → 窗口。</summary>
        public static void SetFullscreen(bool full)
        {
            SetWindowMode(full ? 0 : 1);
            // 旧键同步保留（战斗内旧开关/旧逻辑读它）
            PlayerPrefs.SetInt("Fullscreen", full ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>设置长宽比：按比例匹配系统支持的最大分辨率并应用；0=跟随分辨率不做干预。</summary>
        public static void SetAspectRatioIndex(int index)
        {
            index = Mathf.Clamp(index, 0, AspectRatioNames.Length - 1);
            PlayerPrefs.SetInt(KeyAspectRatio, index);
            PlayerPrefs.Save();
            ApplyAspectRatio();
        }

        /// <summary>设置分辨率（固定候选列表）：立即切换，并把长宽比约束回退为"跟随分辨率"。</summary>
        public static void SetResOptionIndex(int index)
        {
            index = Mathf.Clamp(index, 0, ResolutionNames.Length - 1);
            PlayerPrefs.SetInt(KeyResOption, index);
            PlayerPrefs.SetInt(KeyAspectRatio, 0); // 具体分辨率优先于比例约束
            PlayerPrefs.Save();
            ApplyResolutionPreset();
        }

        /// <summary>旧分辨率下拉（战斗场景接线）改动后同步候选索引，供分辨率步进行标签刷新。</summary>
        public static void SyncResOptionFromCurrent(int width, int height)
        {
            PlayerPrefs.SetInt(KeyResOption, GetNearestResOption(width, height));
            PlayerPrefs.Save();
        }

        /// <summary>设置画质等级并立即应用（clamp 到项目实际等级数）。</summary>
        public static void SetQualityIndex(int index)
        {
            int max = Mathf.Max(0, QualitySettings.names.Length - 1);
            index = Mathf.Clamp(index, 0, max);
            QualitySettings.SetQualityLevel(index, true);
            PlayerPrefs.SetInt(KeyQuality, index);
            PlayerPrefs.Save();
        }

        // ================= 应用 =================

        public static void ApplyTargetFPS()
        {
            int fps = GetTargetFPS();
            Application.targetFrameRate = fps;
            QualitySettings.vSyncCount = fps > 0 ? 0 : 1;
        }

        public static void ApplyWindowMode()
        {
            switch (GetWindowMode())
            {
                case 1: // 窗口
                    Screen.fullScreenMode = FullScreenMode.Windowed;
                    Screen.fullScreen = false;
                    break;
                case 2: // 无边框（窗口化全屏）
                    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                    Screen.fullScreen = true;
                    break;
                default: // 全屏
                    Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                    Screen.fullScreen = true;
                    break;
            }
        }

        /// <summary>应用长宽比：在系统分辨率列表中寻找比例最接近的最大分辨率并切换。</summary>
        public static void ApplyAspectRatio()
        {
            int idx = GetAspectRatioIndex();
            if (idx == 0) return; // 跟随分辨率：不干预

            float targetRatio = AspectRatioValues[idx];
            Resolution best = default;
            float bestDiff = float.MaxValue;
            bool found = false;
            int bestIndex = -1;

            Resolution[] all = Screen.resolutions;
            for (int i = 0; i < all.Length; i++)
            {
                Resolution r = all[i];
                if (r.width <= 0 || r.height <= 0) continue;
                float ratio = (float)r.width / r.height;
                float diff = Mathf.Abs(ratio - targetRatio);
                // 比例更接近者优先；接近时选更高分辨率
                if (diff < bestDiff - 0.005f || (Mathf.Abs(diff - bestDiff) <= 0.005f && r.width * r.height > best.width * best.height))
                {
                    bestDiff = diff;
                    best = r;
                    bestIndex = i;
                    found = true;
                }
            }

            if (found)
            {
                Screen.SetResolution(best.width, best.height, Screen.fullScreenMode, best.refreshRateRatio);
                PlayerPrefs.SetInt("ResolutionIndex", bestIndex);
                PlayerPrefs.SetInt(KeyResOption, GetNearestResOption(best.width, best.height)); // 同步分辨率步进行
                PlayerPrefs.Save();
                GameLogger.Log($"[DisplaySettings] 长宽比 {AspectRatioNames[idx]} → {best.width}x{best.height}@{(int)best.refreshRateRatio.value}Hz");
            }
        }

        /// <summary>按候选索引应用分辨率（保持当前窗口模式）。</summary>
        public static void ApplyResolutionPreset()
        {
            int idx = GetResOptionIndex();
            Screen.SetResolution(ResolutionWidths[idx], ResolutionHeights[idx], Screen.fullScreenMode);
            GameLogger.Log($"[DisplaySettings] 分辨率 {ResolutionNames[idx]}");
        }

        public static void ApplyQuality()
        {
            QualitySettings.SetQualityLevel(GetQualityIndex(), true);
        }

        /// <summary>启动时一次性恢复全部显示设置（首页/主场景入口各调用一次，幂等）。</summary>
        public static void ApplyAll()
        {
            ApplyTargetFPS();
            ApplyQuality();
            if (GetAspectRatioIndex() != 0)
                ApplyAspectRatio();      // 比例约束优先：按比例重选分辨率并同步候选索引
            else
                ApplyResolutionPreset(); // 跟随分辨率：按候选索引恢复尺寸
            ApplyWindowMode();           // 最后只切窗口模式（不重设尺寸）
        }

        /// <summary>恢复显示与音量默认值（只删设置相关键，不动 ActiveSlot/图鉴等存档键）并立即应用。</summary>
        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(KeyTargetFPS);
            PlayerPrefs.DeleteKey(KeyWindowMode);
            PlayerPrefs.DeleteKey(KeyAspectRatio);
            PlayerPrefs.DeleteKey(KeyResOption);
            PlayerPrefs.DeleteKey(KeyQuality);
            PlayerPrefs.DeleteKey("Fullscreen");
            PlayerPrefs.DeleteKey("ShowFPS");
            PlayerPrefs.DeleteKey("MasterVolume");
            PlayerPrefs.DeleteKey("MusicVolume");
            PlayerPrefs.DeleteKey("SFXVolume");
            PlayerPrefs.DeleteKey("BossRelicPickSfx");
            PlayerPrefs.DeleteKey("ResolutionIndex");
            PlayerPrefs.Save();
            ApplyAll();
        }
    }
}
