using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MutationChess.EditorTools
{
    /// <summary>
    /// 霞鹜文楷（LXGW WenKai）字体装配（域重载后执行一次，幂等）：
    /// 1. 从 TTF 生成 TMP SDF 字体资产到 Resources/Fonts & Materials（运行时路径 "Fonts & Materials/LXGW WenKai SDF"）；
    /// 2. 切换 TMP Settings 默认字体为霞鹜文楷（替换宋体——宋体西文字形生硬，霞鹜文楷中英皆宜）。
    /// 旧 SIMSUN SDF 资产暂时保留作回退，验证字体渲染无误后下一批次可删除。
    /// </summary>
    [InitializeOnLoad]
    public static class LXGWFontSetup
    {
        private const string TtfPath = "Assets/_Project/Fonts/LXGWWenKai-Regular.ttf";
        private const string FontAssetPath = "Assets/_Project/Resources/Fonts & Materials/LXGW WenKai SDF.asset";
        private const string TmpSettingsPath = "Assets/Plugins/TextMesh Pro/Resources/TMP Settings.asset";

        static LXGWFontSetup()
        {
            EditorApplication.delayCall += EnsureFontAssetAndDefaultFont;
        }

        /// <summary>手动入口：自动执行失败时可用菜单补生成。</summary>
        [MenuItem("工具/生成霞鹜文楷字体资产并设为默认")]
        public static void EnsureFontAssetAndDefaultFontMenu()
        {
            EnsureFontAssetAndDefaultFont();
        }

        private static void EnsureFontAssetAndDefaultFont()
        {
            if (!File.Exists(TtfPath))
            {
                UnityEngine.Debug.LogWarning($"[LXGWFontSetup] 未找到字体文件：{TtfPath}");
                return;
            }

            // 1. TMP SDF 字体资产（不存在才生成，避免覆盖后续调整）
            TMP_FontAsset fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fa == null)
            {
                Font font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
                if (font == null)
                {
                    UnityEngine.Debug.LogError($"[LXGWFontSetup] TTF 尚未导入或导入失败：{TtfPath}");
                    return;
                }

                // 动态图集 + 多图集支持：中文按需填充字形，4096×4096 容量充足
                fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA,
                    4096, 4096, AtlasPopulationMode.Dynamic, true);
                if (fa == null)
                {
                    UnityEngine.Debug.LogError("[LXGWFontSetup] 字体资产生成失败（FontEngine 加载字形失败，请检查 TTF 导入设置 Include Font Data）");
                    return;
                }

                AssetDatabase.CreateAsset(fa, FontAssetPath);
                if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null)
                    AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa); // 图集纹理作为子资产随字体资产保存
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("[LXGWFontSetup] 已生成霞鹜文楷 TMP 字体资产：" + FontAssetPath);
            }

            // 2. TMP 默认字体切换（未生成过或默认字体不是霞鹜文楷时执行）
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                UnityEngine.Debug.LogWarning($"[LXGWFontSetup] 未找到 TMP Settings：{TmpSettingsPath}");
                return;
            }

            if (TMP_Settings.defaultFontAsset != fa) // 静态成员，用类型名访问
            {
                SerializedObject so = new SerializedObject(settings);
                so.FindProperty("m_defaultFontAsset").objectReferenceValue = fa;
                so.FindProperty("m_defaultFontAssetPath").stringValue = "Fonts & Materials/";
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("[LXGWFontSetup] TMP 默认字体已切换为霞鹜文楷");
            }
        }
    }
}
