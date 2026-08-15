using TMPro;
using UnityEngine;

namespace MutationChess.UI
{
    /// <summary>
    /// 项目统一字体加载：霞鹜文楷（LXGW WenKai）TMP SDF 资产。
    /// 集中字体路径与缓存，替换原宋体（SIMSUN SDF）硬编码加载——
    /// 宋体西文字形生硬，霞鹜文楷西文为 Klee One 圆润手写体，中英皆宜。
    /// </summary>
    public static class UiFonts
    {
        /// <summary>霞鹜文楷 TMP 资产运行时路径（相对 Assets/_Project/Resources）。</summary>
        public const string DefaultFontPath = "Fonts & Materials/LXGW WenKai SDF";

        private static TMP_FontAsset cached;

        /// <summary>加载霞鹜文楷字体资产（缓存）。SDF 资产由编辑器脚本 LXGWFontSetup 自动生成。</summary>
        public static TMP_FontAsset Load()
        {
            if (cached == null)
                cached = Resources.Load<TMP_FontAsset>(DefaultFontPath);
            return cached;
        }
    }
}
