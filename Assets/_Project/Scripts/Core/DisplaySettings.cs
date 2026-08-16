using UnityEngine;

namespace MutationChess.Core
{
    /// <summary>
    /// 显示设置静态助手：目标帧率 / 窗口模式（全屏·窗口·无边框）/ 长宽比 / 全屏开关
    /// 的 PlayerPrefs 读写与即时应用。首页设置面板与战斗内 SettingsManager 共用
    /// 同一份键值，两处修改互相可见；启动时调用 ApplyAll() 一次恢复全部设置。
    /// </summary>
    public static class DisplaySettings
    {
        public const string KeyTargetFPS = "TargetFPS";      // 30/60/120/0（0=不限）
        public const string KeyWindowMode = "WindowMode";    // 0=全屏 1=窗口 2=无边框
        public const string KeyAspectRatio = "AspectRatio";  // 0=跟随分辨率 1=16:9 2=16:10 3=4:3 4=21:9

        public static readonly int[] TargetFpsOptions = { 30, 60, 120, 0 };
        public static readonly string[] WindowModeNames = { "全屏", "窗口", "无边框" };
        public static readonly string[] AspectRatioNames = { "跟随分辨率", "16:9 宽屏", "16:10", "4:3 传统", "21:9 影院" };
        public static readonly float[] AspectRatioValues = { 0f, 16f / 9f, 16f / 10f, 4f / 3f, 21f / 9f };

        // ================= 读取 =================

        public static int GetTargetFPS() => PlayerPrefs.GetInt(KeyTargetFPS, 0);

        public static int GetWindowMode() => Mathf.Clamp(PlayerPrefs.GetInt(KeyWindowMode, 0), 0, 2);

        public static int GetAspectRatioIndex() => Mathf.Clamp(PlayerPrefs.GetInt(KeyAspectRatio, 0), 0, AspectRatioNames.Length - 1);

        /// <summary>目标帧率显示名（0 → "不限"）。</summary>
        public static string GetTargetFpsLabel()
        {
            int fps = GetTargetFPS();
            return fps > 0 ? $"{fps} FPS" : "不限";
        }

        public static string GetWindowModeLabel() => WindowModeNames[GetWindowMode()];

        public static string GetAspectRatioLabel() => AspectRatioNames[GetAspectRatioIndex()];

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
                PlayerPrefs.Save();
                GameLogger.Log($"[DisplaySettings] 长宽比 {AspectRatioNames[idx]} → {best.width}x{best.height}@{(int)best.refreshRateRatio.value}Hz");
            }
        }

        /// <summary>启动时一次性恢复全部显示设置（首页/主场景入口各调用一次，幂等）。</summary>
        public static void ApplyAll()
        {
            ApplyTargetFPS();
            ApplyWindowMode();
            ApplyAspectRatio();
        }
    }
}
