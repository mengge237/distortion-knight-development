using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MutationChess.UI;

namespace MutationChess.EditorTools
{
    /// <summary>
    /// 首页场景自动装配（域重载后执行一次，幂等）：
    /// 1. 程序化创建 HomeScene.unity（相机 + EventSystem + HomeScreen 启动器，纯代码生成避免手写 YAML 易错）；
    /// 2. 注册 BuildSettings：HomeScene(0) → MainScene(1)，打包/启动时先进入首页。
    /// 首页选择难度并确认后由 HomeScreen 运行时加载主场景，主场景缺失接线仍可独立运行（GameManager 兜底弹难度面板）。
    /// </summary>
    [InitializeOnLoad]
    public static class HomeSceneSetup
    {
        private const string HomeScenePath = "Assets/_Project/Scenes/HomeScene.unity";
        private const string MainScenePath = "Assets/_Project/Scenes/MainScene.unity";

        static HomeSceneSetup()
        {
            EditorApplication.delayCall += EnsureHomeSceneAndBuildSettings;
        }

        private static void EnsureHomeSceneAndBuildSettings()
        {
            // 1. 场景文件（不存在才创建，避免覆盖人工调整）
            if (!File.Exists(HomeScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

                // 相机：暗色清屏（首页 UI 覆盖其上）
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.045f, 0.045f, 0.07f, 1f);
                cam.transform.position = new Vector3(0f, 0f, -10f);
                camGo.AddComponent<AudioListener>(); // 首页音效需要监听器

                // EventSystem（UI 点击必需）
                new GameObject("EventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));

                // 首页启动器（运行时自建全部 UI）
                var homeGo = new GameObject("HomeScreen");
                homeGo.AddComponent<HomeScreen>();

                EditorSceneManager.SaveScene(scene, HomeScenePath);
                EditorSceneManager.CloseScene(scene, true);
                Debug.Log("[HomeSceneSetup] 已创建首页场景：" + HomeScenePath);
            }

            // 2. BuildSettings：首页在前、主场景在后
            var scenes = EditorBuildSettings.scenes;
            bool hasHome = false, hasMain = false;
            foreach (var s in scenes)
            {
                if (s.path == HomeScenePath) hasHome = true;
                if (s.path == MainScenePath) hasMain = true;
            }

            if (!hasHome || !hasMain)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
                if (!hasHome) list.Add(new EditorBuildSettingsScene(HomeScenePath, true));
                if (!hasMain) list.Add(new EditorBuildSettingsScene(MainScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
                Debug.Log("[HomeSceneSetup] 已注册 BuildSettings：HomeScene → MainScene");
            }
        }
    }
}
