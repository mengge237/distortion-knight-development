using System.IO;
using UnityEditor;
using UnityEngine;

namespace MutationChess.EditorTools
{
    /// <summary>
    /// 地图节点材质修复（域重载后执行一次，幂等）：
    /// 资产审计时误删 Art/Environment/Materials 全部材质，MapNode 圆柱体预制体引用的
    /// 材质（guid 31321ba15b8f8eb4c954353edc038b1d）自仓库初始化起就未入库 → 节点渲染灰块/品红。
    /// 重建 Node_Default.mat（URP Unlit，与连线材质同管线；节点颜色由 MapView 按类型运行时覆盖）
    /// 并回填 MapNode.prefab 的 MeshRenderer 引用。
    /// </summary>
    [InitializeOnLoad]
    public static class NodeMaterialSetup
    {
        private const string MaterialPath = "Assets/_Project/Art/Environment/Materials/Node_Default.mat";
        private const string PrefabPath = "Assets/_Project/Prefabs/Map/MapNode.prefab";

        static NodeMaterialSetup()
        {
            EditorApplication.delayCall += EnsureNodeMaterial;
        }

        /// <summary>手动入口：自动执行失败时可用菜单补执行。</summary>
        [MenuItem("工具/修复地图节点材质")]
        public static void EnsureNodeMaterialMenu()
        {
            EnsureNodeMaterial();
        }

        private static void EnsureNodeMaterial()
        {
            // 1. 材质不存在则创建
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null)
                {
                    UnityEngine.Debug.LogError("[NodeMaterialSetup] 找不到可用着色器（URP Unlit / Sprites / Standard 均缺失）");
                    return;
                }

                mat = new Material(shader);
                mat.name = "Node_Default";
                mat.color = new Color(0.72f, 0.72f, 0.78f, 1f); // 默认底色，运行时由 MapView 覆盖

                string dir = Path.GetDirectoryName(MaterialPath).Replace('\\', '/');
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    Directory.CreateDirectory(dir); // 目录可能随资产审计被删除
                    AssetDatabase.Refresh();
                }
                AssetDatabase.CreateAsset(mat, MaterialPath);
                UnityEngine.Debug.Log("[NodeMaterialSetup] 已重建地图节点材质：" + MaterialPath);
            }

            // 2. 预制体引用回填（当前引用 guid 从未入库，属 Missing）
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            var mr = root.GetComponent<MeshRenderer>();
            if (mr != null && (mr.sharedMaterial == null || mr.sharedMaterial != mat))
            {
                mr.sharedMaterial = mat;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                UnityEngine.Debug.Log("[NodeMaterialSetup] 已回填 MapNode 圆柱体预制体材质引用");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
