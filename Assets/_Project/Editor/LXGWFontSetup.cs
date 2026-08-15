using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace MutationChess.EditorTools
{
    /// <summary>
    /// 霞鹜文楷（LXGW WenKai）字体装配与项目级字体迁移（域重载后执行一次，幂等）：
    /// 1. 修复字体资产（首版生成时漏存材质子资产 material={fileID:0}、图集 0×0 空纹理 → 文字不显示）：
    ///    重建 4096×4096 图集纹理并随资产保存、补建 TMP SDF 材质子资产并绑定图集；
    /// 2. 字符预填充：扫描工程内全部文本（.cs/.asset/.json/.unity/.prefab，含 \uXXXX 转义），
    ///    用 TryAddCharacters 预先栅格化到图集——静态字形渲染不依赖运行时 FontEngine，构建版同样可靠；
    /// 3. TMP Settings：默认字体切为霞鹜文楷，SIMSUN 保留为回退字体（冷僻字兜底）；
    /// 4. 预制体字体迁移：TMP_Text 显式引用 SIMSUN SDF 的全部改为霞鹜文楷并规范化共享材质
    ///    （场景内文本引用已由批量 guid 替换完成；Anton SDF 卡牌标题为设计保留，不动）。
    /// </summary>
    [InitializeOnLoad]
    public static class LXGWFontSetup
    {
        private const string TtfPath = "Assets/_Project/Fonts/LXGWWenKai-Regular.ttf";
        private const string FontAssetPath = "Assets/_Project/Resources/Fonts & Materials/LXGW WenKai SDF.asset";
        private const string SimsunFontAssetPath = "Assets/_Project/Resources/Fonts & Materials/SIMSUN SDF.asset";
        private const string TmpSettingsPath = "Assets/Plugins/TextMesh Pro/Resources/TMP Settings.asset";

        /// <summary>首次预填充阈值：字形表小于该数量视为需要填充（修复后一次填充，后续跳过）。</summary>
        private const int PrefillThreshold = 500;

        static LXGWFontSetup()
        {
            EditorApplication.delayCall += RepairAndMigrate;
        }

        /// <summary>手动入口：自动执行失败（如彼时存在编译错误）时可用菜单补执行。</summary>
        [MenuItem("工具/修复霞鹜文楷字体资产并迁移全项目字体")]
        public static void RepairAndMigrateMenu()
        {
            RepairAndMigrate();
        }

        private static void RepairAndMigrate()
        {
            if (!File.Exists(TtfPath))
            {
                UnityEngine.Debug.LogWarning($"[LXGWFontSetup] 未找到字体文件：{TtfPath}");
                return;
            }

            TMP_FontAsset fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fa == null)
            {
                fa = CreateFontAsset();
                if (fa == null) return;
            }

            bool repaired = RepairFontAsset(fa);
            if (repaired)
            {
                EditorUtility.SetDirty(fa);
                if (fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null)
                    EditorUtility.SetDirty(fa.atlasTextures[0]); // 图集字形数据随主资产一起落盘
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                UnityEngine.Debug.Log("[LXGWFontSetup] 字体资产修复完成：" + FontAssetPath);
            }

            UpdateTmpSettings(fa);
            MigratePrefabFonts(fa);
        }

        /// <summary>从 TTF 新建字体资产（资产不存在时的兜底路径）。</summary>
        private static TMP_FontAsset CreateFontAsset()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (font == null)
            {
                UnityEngine.Debug.LogError($"[LXGWFontSetup] TTF 尚未导入或导入失败：{TtfPath}");
                return null;
            }
            var fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA,
                4096, 4096, AtlasPopulationMode.Dynamic, true);
            if (fa == null)
            {
                UnityEngine.Debug.LogError("[LXGWFontSetup] 字体资产生成失败（FontEngine 加载字形失败，请检查 TTF 导入设置 Include Font Data）");
                return null;
            }
            AssetDatabase.CreateAsset(fa, FontAssetPath);
            UnityEngine.Debug.Log("[LXGWFontSetup] 已生成霞鹜文楷 TMP 字体资产：" + FontAssetPath);
            return fa;
        }

        /// <summary>
        /// 原地修复字体资产（保持 guid 不变，场景/预制体引用不受影响）：
        /// 图集纹理 0×0 → 重建；材质子资产缺失 → 补建并绑定图集；字形表空 → 预填充工程字符。
        /// </summary>
        private static bool RepairFontAsset(TMP_FontAsset fa)
        {
            bool dirty = false;

            // 1. 材质：material 是公开字段（TMP_Asset），序列化 {fileID: 0} 加载后为 null → 补建 TMP SDF 材质
            //    （ShaderUtilities.ShaderRef_MobileSDF 为 internal 不可跨程序集访问，直接用 Shader.Find）
            Material mat = fa.material;
            if (mat == null)
            {
                Shader shader = Shader.Find("TextMeshPro/Mobile/Distance Field");
                if (shader == null) shader = Shader.Find("TextMeshPro/Distance Field");
                if (shader == null)
                {
                    UnityEngine.Debug.LogError("[LXGWFontSetup] 找不到 TMP SDF 着色器，无法补建字体材质");
                    return dirty;
                }
                mat = new Material(shader);
                fa.material = mat;
            }
            if (mat.hideFlags == HideFlags.HideAndDontSave)
                mat.hideFlags = HideFlags.HideInHierarchy;
            if (string.IsNullOrEmpty(mat.name) || !mat.name.EndsWith(" Material"))
                mat.name = fa.name + " Material";

            // 2. 图集纹理：0×0 空纹理（初版生成时未填充）→ 销毁旧子资产换新
            Texture2D atlas = (fa.atlasTextures != null && fa.atlasTextures.Length > 0) ? fa.atlasTextures[0] : null;
            if (atlas == null || atlas.width == 0 || atlas.height == 0)
            {
                int w = fa.atlasWidth > 0 ? fa.atlasWidth : 4096;
                int h = fa.atlasHeight > 0 ? fa.atlasHeight : 4096;
                var newAtlas = new Texture2D(w, h, TextureFormat.Alpha8, false);
                newAtlas.name = fa.name + " Atlas";
                newAtlas.hideFlags = HideFlags.HideInHierarchy;
                if (atlas != null)
                    UnityEngine.Object.DestroyImmediate(atlas, true); // 移除旧的空图集子资产
                fa.atlasTextures = new Texture2D[] { newAtlas };
                if (!AssetDatabase.IsSubAsset(newAtlas))
                    AssetDatabase.AddObjectToAsset(newAtlas, fa);
                UnityEngine.Debug.Log($"[LXGWFontSetup] 已重建字体图集纹理 {w}×{h}（旧图集 0×0 空纹理已移除）");
                dirty = true;
                atlas = newAtlas;
            }

            // 3. 材质绑定图集 + 子资产保存
            if (mat.GetTexture(ShaderUtilities.ID_MainTex) != atlas)
            {
                mat.SetTexture(ShaderUtilities.ID_MainTex, atlas);
                dirty = true;
            }
            if (!AssetDatabase.IsSubAsset(mat))
            {
                AssetDatabase.AddObjectToAsset(mat, fa);
                UnityEngine.Debug.Log("[LXGWFontSetup] 已补存字体材质子资产（原 material 引用为空导致文字不显示）");
                dirty = true;
            }

            // 4. 字符预填充：扫描工程全部文本，一次性栅格化到图集（静态字形，运行时零 FontEngine 依赖）
            if (fa.characterTable == null || fa.characterTable.Count < PrefillThreshold)
            {
                string chars = CollectProjectCharacters();
                if (!string.IsNullOrEmpty(chars))
                {
                    fa.TryAddCharacters(chars); // 字体缺失的字形自动跳过，不影响其余
                    UnityEngine.Debug.Log($"[LXGWFontSetup] 字符预填充完成：扫描 {chars.Length} 个字符，字形表 {fa.characterTable?.Count ?? 0} 项");
                    dirty = true;
                }
            }

            return dirty;
        }

        /// <summary>扫描工程内全部文本文件，收集所需字符（汉字+ASCII+标点，含 YAML \uXXXX 转义）。</summary>
        private static string CollectProjectCharacters()
        {
            var set = new HashSet<int>();
            for (int i = 32; i < 127; i++) set.Add(i); // 可打印 ASCII

            string[] files = Directory.GetFiles("Assets/_Project", "*.*", SearchOption.AllDirectories);
            foreach (string path in files)
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext != ".cs" && ext != ".asset" && ext != ".json" && ext != ".unity" && ext != ".prefab" && ext != ".txt")
                    continue;
                if (path.Contains("Screenshots")) continue;
                if (path.Contains("SIMSUN SDF.asset") || path.Contains("LXGW WenKai SDF.asset")) continue;

                string text;
                try { text = File.ReadAllText(path, Encoding.UTF8); }
                catch { continue; }

                foreach (char c in text)
                {
                    if (c > 127) set.Add(c);
                }
                foreach (Match m in Regex.Matches(text, @"\\u([0-9a-fA-F]{4})"))
                {
                    set.Add(Convert.ToInt32(m.Groups[1].Value, 16));
                }
            }

            // 上限保护：超出则截断（当前工程约 1300 字符，远低于上限）
            const int cap = 4000;
            var sb = new StringBuilder(Math.Min(set.Count, cap));
            foreach (int cp in set)
            {
                if (sb.Length >= cap) break;
                sb.Append(char.ConvertFromUtf32(cp));
            }
            return sb.ToString();
        }

        /// <summary>TMP Settings：默认字体 + SIMSUN 回退字体（冷僻字兜底）。</summary>
        private static void UpdateTmpSettings(TMP_FontAsset fa)
        {
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                UnityEngine.Debug.LogWarning($"[LXGWFontSetup] 未找到 TMP Settings：{TmpSettingsPath}");
                return;
            }

            bool dirty = false;
            SerializedObject so = new SerializedObject(settings);

            if (TMP_Settings.defaultFontAsset != fa) // 静态成员，用类型名访问
            {
                so.FindProperty("m_defaultFontAsset").objectReferenceValue = fa;
                so.FindProperty("m_defaultFontAssetPath").stringValue = "Fonts & Materials/";
                dirty = true;
            }

            // SIMSUN 加入回退列表（只有显式缺失字形时才用得到，主字体始终是霞鹜文楷）
            TMP_FontAsset simsun = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SimsunFontAssetPath);
            var fallbackProp = so.FindProperty("m_fallbackFontAssets");
            bool hasSimsun = false;
            for (int i = 0; i < fallbackProp.arraySize; i++)
            {
                if (fallbackProp.GetArrayElementAtIndex(i).objectReferenceValue == simsun)
                {
                    hasSimsun = true;
                    break;
                }
            }
            if (!hasSimsun && simsun != null)
            {
                fallbackProp.arraySize++;
                fallbackProp.GetArrayElementAtIndex(fallbackProp.arraySize - 1).objectReferenceValue = simsun;
                dirty = true;
            }

            if (dirty)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("[LXGWFontSetup] TMP Settings 已更新：默认字体霞鹜文楷 + SIMSUN 冷僻字回退");
            }
        }

        /// <summary>预制体字体迁移：SIMSUN → 霞鹜文楷（Anton 卡牌标题保留），并规范化共享材质。</summary>
        private static void MigratePrefabFonts(TMP_FontAsset target)
        {
            TMP_FontAsset simsun = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SimsunFontAssetPath);
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" });
            int migratedPrefabs = 0, scannedTexts = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                foreach (var tmp in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    scannedTexts++;
                    if (simsun != null && tmp.font == simsun && tmp.font != target)
                    {
                        tmp.font = target;
                        changed = true;
                    }
                    // 材质规范化：字体已是霞鹜文楷但共享材质仍是宋体材质 → 改为字体自带材质
                    if (tmp.font == target && tmp.fontSharedMaterial != target.material)
                        tmp.fontSharedMaterial = target.material;
                }
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    migratedPrefabs++;
                }
                PrefabUtility.UnloadPrefabContents(root);
            }
            UnityEngine.Debug.Log($"[LXGWFontSetup] 预制体字体迁移：检查 {guids.Length} 个预制体 / {scannedTexts} 个文本组件，修改 {migratedPrefabs} 个预制体");
        }
    }
}
