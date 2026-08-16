using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MutationChess.UI;

namespace MutationChess.EditorTools
{
    /// <summary>
    /// 首页场景自动装配（域重载后执行一次，幂等）：
    /// 1. 程序化生成首页美术贴图（深渊夜色渐变背景 / 金边按钮面板 / 设置面板 / 顶部纹章 / 四角金饰），
    ///    输出到 Resources/UI/Home 并配置 9-slice 导入；
    /// 2. 重建 HomeScene.unity：场景内实体画布（1920×1080 ScaleWithScreenSize 全屏适配）+
    ///    标题/副标题/四入口按钮/设置面板/难度选择面板全部编辑器可见，无需运行时自建；
    ///    HomeScreen 组件负责绑定控件逻辑；场景缺接线时回退运行时自建（兼容旧场景）；
    ///    难度选择面板由 DifficultyManager 绑定（独立 900 层画布场景实体）；
    /// 3. 注册 BuildSettings：HomeScene(0) → MainScene(1)。
    /// </summary>
    [InitializeOnLoad]
    public static class HomeSceneSetup
    {
        private const string HomeScenePath = "Assets/_Project/Scenes/HomeScene.unity";
        private const string MainScenePath = "Assets/_Project/Scenes/MainScene.unity";
        private const string ArtDir = "Assets/_Project/Resources/UI/Home";
        private const string FontAssetPath = "Assets/_Project/Resources/Fonts & Materials/LXGW WenKai SDF.asset";
        private const string MarkerScriptPath = "Assets/_Project/Scripts/UI/HomeSceneMarker.cs";

        static HomeSceneSetup()
        {
            EditorTaskGuard.RunWhenSafe(EnsureAll);
        }

        /// <summary>手动入口：域重载自动执行失败时可用菜单补生成。</summary>
        [MenuItem("工具/重新生成首页场景（画布+面板+美术）")]
        public static void EnsureAllMenu()
        {
            EditorTaskGuard.RunWhenSafe(EnsureAll);
            UnityEngine.Debug.Log("[HomeSceneSetup] 已提交生成任务（若正在 Play 模式，退出后自动执行）");
        }

        private static void EnsureAll()
        {
            // 分阶段容错：任一阶段失败不阻断后续（否则字体/美术异常会导致场景永远重建不出来）
            TryPhase("字体修复", LXGWFontSetup.EnsureReady); // 先修复字体再建场景：场景内文字直接引用健康字体资产
            TryPhase("首页美术", EnsureArtAssets);
            TryPhase("首页场景", EnsureHomeScene);
            TryPhase("BuildSettings", EnsureBuildSettings);
        }

        private static void TryPhase(string phaseName, System.Action phase)
        {
            try
            {
                phase();
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[HomeSceneSetup] 「{phaseName}」阶段执行失败（已跳过，不影响后续阶段）：{e}");
            }
        }

        // ================= 美术贴图 =================

        private static void EnsureArtAssets()
        {
            if (!Directory.Exists(ArtDir))
            {
                Directory.CreateDirectory(ArtDir);
                AssetDatabase.Refresh();
            }

            PaintIfMissing("home_bg.png", PaintBackground);
            PaintIfMissing("btn_panel.png", () => PaintPanel(460, 160));
            PaintIfMissing("settings_panel.png", () => PaintPanel(820, 660));
            PaintIfMissing("ornament.png", PaintOrnament);
            PaintIfMissing("corner.png", PaintCorner);

            ConfigureTexture("home_bg.png", 2048, Vector4.zero);
            ConfigureTexture("btn_panel.png", 512, new Vector4(64f, 64f, 64f, 64f));
            ConfigureTexture("settings_panel.png", 1024, new Vector4(60f, 60f, 60f, 60f));
            ConfigureTexture("ornament.png", 512, Vector4.zero);
            ConfigureTexture("corner.png", 512, Vector4.zero);
        }

        private static void PaintIfMissing(string file, System.Func<Texture2D> painter)
        {
            string path = ArtDir + "/" + file;
            if (File.Exists(path)) return;
            Texture2D tex = painter();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            UnityEngine.Debug.Log("[HomeSceneSetup] 已生成首页美术：" + path);
        }

        private static void ConfigureTexture(string file, int maxSize, Vector4 border)
        {
            string path = ArtDir + "/" + file;
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            bool dirty = false;
            if (imp.textureType != TextureImporterType.Sprite) { imp.textureType = TextureImporterType.Sprite; dirty = true; }
            if (imp.spriteImportMode != SpriteImportMode.Single) { imp.spriteImportMode = SpriteImportMode.Single; dirty = true; }
            if (imp.mipmapEnabled) { imp.mipmapEnabled = false; dirty = true; }
            if (imp.maxTextureSize != maxSize) { imp.maxTextureSize = maxSize; dirty = true; }
            if (imp.spriteBorder != border) { imp.spriteBorder = border; dirty = true; }
            if (dirty) imp.SaveAndReimport();
        }

        /// <summary>首页背景 1920×1080：深渊夜色渐变 + 极淡棋盘格 + 暗角 + 金色微尘。</summary>
        private static Texture2D PaintBackground()
        {
            const int w = 1920, h = 1080;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color top = new Color(0.075f, 0.09f, 0.17f, 1f);
            Color bottom = new Color(0.022f, 0.026f, 0.055f, 1f);

            for (int y = 0; y < h; y++)
            {
                float t = (float)y / (h - 1);
                Color row = Color.Lerp(top, bottom, t * t);
                int cy = y / 240;
                for (int x = 0; x < w; x++)
                {
                    Color c = row;
                    if (((x / 240) + cy) % 2 == 0) c += new Color(0.012f, 0.012f, 0.02f, 0f);
                    tex.SetPixel(x, y, c);
                }
            }
            Color line = new Color(0.014f, 0.014f, 0.024f, 0f);
            for (int x = 0; x < w; x += 240)
                for (int y = 0; y < h; y++) tex.SetPixel(x, y, tex.GetPixel(x, y) + line);
            for (int y = 0; y < h; y += 240)
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, tex.GetPixel(x, y) + line);

            // 暗角
            float cx0 = w * 0.5f, cy0 = h * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float dy = (y - cy0) / (h * 0.5f);
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - cx0) / (w * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float vig = 1f - 0.55f * Mathf.Clamp01((d - 0.5f) / 0.65f);
                    tex.SetPixel(x, y, tex.GetPixel(x, y) * vig);
                }
            }

            // 金色微尘（固定种子，确定性）
            var rng = new System.Random(20260816);
            Color gold = new Color(0.92f, 0.78f, 0.5f, 1f);
            for (int i = 0; i < 220; i++)
            {
                int x = rng.Next(0, w), y = rng.Next(0, h);
                float r = 1f + (float)rng.NextDouble() * 2.5f;
                float a = 0.05f + (float)rng.NextDouble() * 0.14f;
                for (int oy = -3; oy <= 3; oy++)
                {
                    for (int ox = -3; ox <= 3; ox++)
                    {
                        int px = x + ox, py = y + oy;
                        if (px < 0 || py < 0 || px >= w || py >= h) continue;
                        float d = Mathf.Sqrt(ox * ox + oy * oy);
                        if (d > r) continue;
                        tex.SetPixel(px, py, Color.Lerp(tex.GetPixel(px, py), gold, a * (1f - d / (r + 1f))));
                    }
                }
            }
            tex.Apply(false);
            return tex;
        }

        /// <summary>金边面板（9-slice）：深色底 + 外暗金/内亮金双线 + 四角加粗金饰。</summary>
        private static Texture2D PaintPanel(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color fill = new Color(0.085f, 0.085f, 0.12f, 0.97f);
            Color inner = new Color(0.79f, 0.66f, 0.33f, 0.9f);
            Color outer = new Color(0.55f, 0.45f, 0.22f, 0.85f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, fill);

            for (int i = 0; i < 2; i++) // 外框 2px
                for (int k = 0; k < w; k++) { tex.SetPixel(k, i, outer); tex.SetPixel(k, h - 1 - i, outer); }
            for (int i = 0; i < 2; i++)
                for (int k = 0; k < h; k++) { tex.SetPixel(i, k, outer); tex.SetPixel(w - 1 - i, k, outer); }
            for (int k = 4; k < w - 4; k++) { tex.SetPixel(k, 4, inner); tex.SetPixel(k, h - 5, inner); } // 内金线
            for (int k = 4; k < h - 4; k++) { tex.SetPixel(4, k, inner); tex.SetPixel(w - 5, k, inner); }

            // 四角 L 形加粗角饰
            int l = 28, t = 3;
            for (int i = 0; i < l; i++)
                for (int j = 0; j < t; j++)
                {
                    tex.SetPixel(3 + i, 3 + j, inner);
                    tex.SetPixel(3 + j, 3 + i, inner);
                    tex.SetPixel(w - 4 - i, 3 + j, inner);
                    tex.SetPixel(w - 4 - j, 3 + i, inner);
                    tex.SetPixel(3 + i, h - 4 - j, inner);
                    tex.SetPixel(3 + j, h - 4 - i, inner);
                    tex.SetPixel(w - 4 - i, h - 4 - j, inner);
                    tex.SetPixel(w - 4 - j, h - 4 - i, inner);
                }
            tex.Apply(false);
            return tex;
        }

        /// <summary>顶部纹章 640×48：中央菱形 + 两侧横线 + 副菱形，金色。</summary>
        private static Texture2D PaintOrnament()
        {
            const int w = 640, h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color c0 = Color.clear;
            Color gold = new Color(0.79f, 0.66f, 0.33f, 0.9f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c0);

            int cy = h / 2;
            // 两侧横线
            for (int y = cy - 1; y <= cy; y++)
                for (int x = 8; x < w / 2 - 30; x++)
                { tex.SetPixel(x, y, gold); tex.SetPixel(w - 1 - x, y, gold); }
            // 中央菱形 18×18
            for (int y = -9; y <= 9; y++)
            {
                int half = 9 - Mathf.Abs(y);
                for (int x = -half; x <= half; x++)
                    tex.SetPixel(w / 2 + x, cy + y, gold);
            }
            // 两侧副菱形 9×9
            for (int i = 0; i < 2; i++)
            {
                int dx = i == 0 ? w / 4 : w * 3 / 4;
                for (int y = -4; y <= 4; y++)
                {
                    int half = 4 - Mathf.Abs(y);
                    for (int x = -half; x <= half; x++)
                        tex.SetPixel(dx + x, cy + y, gold);
                }
            }
            tex.Apply(false);
            return tex;
        }

        /// <summary>四角金饰 240×240：双 L 线 + 交点菱形。</summary>
        private static Texture2D PaintCorner()
        {
            const int s = 240;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            Color gold = new Color(0.79f, 0.66f, 0.33f, 0.85f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                    tex.SetPixel(x, y, Color.clear);

            // 双 L 线（外 8px / 内 18px，线宽 3，长 200）
            for (int pass = 0; pass < 2; pass++)
            {
                int o = pass == 0 ? 8 : 18;
                for (int t = 0; t < 3; t++)
                    for (int i = 0; i <= 200; i++)
                    {
                        tex.SetPixel(o + t, o + i, gold);
                        tex.SetPixel(o + i, o + t, gold);
                    }
            }
            // 交点菱形 12×12
            for (int y = -5; y <= 5; y++)
            {
                int half = 5 - Mathf.Abs(y);
                for (int x = -half; x <= half; x++)
                    tex.SetPixel(13 + x, 13 + y, gold);
            }
            tex.Apply(false);
            return tex;
        }

        // ================= 场景构建 =================

        private static void EnsureHomeScene()
        {
            bool exists = File.Exists(HomeScenePath);
            bool fileOk = exists && SceneFileHasMarker(HomeScenePath) && SceneFileHasTabbedSettings(HomeScenePath)
                && SceneFileHasDifficultyPanel(HomeScenePath);
            if (fileOk) return;

            // 旧版场景（无画布）可能正被用户打开：不能关闭"最后一个打开的场景"（CloseScene 会静默失败），
            // 也不能把新建场景保存到仍被旧场景占用的同路径 → 改为原地清空重建并保存回自身路径
            Scene openScene = EditorSceneManager.GetSceneByPath(HomeScenePath);
            if (openScene.isLoaded)
            {
                // 打开态以内存为准：已含画布或标记（含未保存的手动修改）→ 只升级设置子面板/难度面板，不动其余节点
                if (SceneHasCanvas(openScene) || SceneHasMarker(openScene))
                {
                    UpgradeSettingsPanelInScene(openScene);
                    EnsureDifficultyPanelInScene(openScene);
                    return;
                }

                UnityEngine.Debug.Log("[HomeSceneSetup] HomeScene 正打开且缺少标记，原地清空重建（场景保持打开，用户可立即看到）");
                foreach (GameObject root in openScene.GetRootGameObjects())
                    UnityEngine.Object.DestroyImmediate(root);
                BuildHomeScene(openScene);
                EditorSceneManager.SaveScene(openScene);
                UnityEngine.Debug.Log("[HomeSceneSetup] 已原地重建首页场景（画布+面板+美术）：" + HomeScenePath);
                return;
            }

            // 场景未打开：新建空场景构建后保存；此时编辑器至少有一个其他场景打开，新建场景可安全关闭
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            BuildHomeScene(scene);
            EditorSceneManager.SaveScene(scene, HomeScenePath);
            EditorSceneManager.CloseScene(scene, true);
            UnityEngine.Debug.Log("[HomeSceneSetup] 已重建首页场景（画布+面板+美术）：" + HomeScenePath);
        }

        /// <summary>
        /// 场景文件内是否含 HomeSceneMarker 组件（按脚本 GUID 精确匹配）。
        /// 手动编辑改名/重排节点不会误触发重建，只有删掉标记组件才会（场景缺标记＝旧版场景）。
        /// </summary>
        private static bool SceneFileHasMarker(string scenePath)
        {
            string markerGuid = AssetDatabase.AssetPathToGUID(MarkerScriptPath);
            string text = File.ReadAllText(scenePath);
            if (!string.IsNullOrEmpty(markerGuid)) return text.Contains("guid: " + markerGuid);
            return text.Contains("HomeCanvas"); // 标记脚本尚未导入等罕见情况：回退旧画布字符串判定
        }

        private static bool SceneHasMarker(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<HomeSceneMarker>(true) != null) return true;
            return false;
        }

        private static bool SceneHasCanvas(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponent<Canvas>() != null) return true;
            return false;
        }

        /// <summary>场景文件内是否含 TabBar 节点（SettingsPanelBuilder 生成的标签页式设置面板哨兵）。</summary>
        private static bool SceneFileHasTabbedSettings(string scenePath)
        {
            return File.ReadAllText(scenePath).Contains("TabBar");
        }

        /// <summary>场景文件内是否含难度选择面板节点（DifficultyPanelBuilder 生成的场景实体哨兵）。</summary>
        private static bool SceneFileHasDifficultyPanel(string scenePath)
        {
            return File.ReadAllText(scenePath).Contains("DifficultySelectPanel");
        }

        /// <summary>补建难度选择面板场景实体（独立 900 层画布，盖在首页 500 层之上；已存在则不动）。</summary>
        private static void EnsureDifficultyPanelInScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "DifficultySelectPanel") return;

            CreateDifficultyPanel(scene);
            EditorSceneManager.SaveScene(scene);
            UnityEngine.Debug.Log("[HomeSceneSetup] 已补建场景内难度选择面板（DifficultySelectPanel，可手动编辑）");
        }

        /// <summary>
        /// 难度选择面板场景实体：与运行时兜底自建共用 DifficultyPanelBuilder 同一构建器，
        /// 场景实体与运行时结构完全一致（滚轮几何参数存于 DifficultyWheel 组件可直接改）。
        /// 场景内保持激活供编辑器查看；运行时由 DifficultyManager 绑定控件并在加载时隐藏，弹出时复用。
        /// </summary>
        private static void CreateDifficultyPanel(Scene scene)
        {
            GameObject root = DifficultyPanelBuilder.CreateCanvasRoot("DifficultySelectPanel");
            // 面板根画布独立成根节点：创建时归入目标场景（HomeScene 打开时活动场景可能是别的场景）
            SceneManager.MoveGameObjectToScene(root, scene);
            DifficultyPanelBuilder.Build(root.transform);
        }

        /// <summary>
        /// 旧版平铺设置面板（无 TabBar 标记）→ 原地替换为标签页式（显示/音量/游戏 + 滚轮内容区）。
        /// 只动设置子面板这一个节点，保留用户对场景其余节点的手动修改；面板已删除或已升级则不动。
        /// </summary>
        private static void UpgradeSettingsPanelInScene(Scene scene)
        {
            Canvas canvas = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null) break;
            }
            if (canvas == null) return;

            Transform oldPanel = canvas.transform.Find("HomeSettings");
            if (oldPanel == null || oldPanel.Find("TabBar") != null) return;

            UnityEngine.Object.DestroyImmediate(oldPanel.gameObject);
            CreateSettingsPanel(canvas.transform);
            EditorSceneManager.SaveScene(scene);
            UnityEngine.Debug.Log("[HomeSceneSetup] 已升级首页设置子面板为标签页式（显示/音量/游戏 + 滚轮）");
        }

        private static void BuildHomeScene(Scene scene)
        {
            // 相机（暗色清屏，首页画布覆盖其上）
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.065f, 1f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            // EventSystem（UI 点击必需）
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // 首页启动器（绑定画布控件；缺接线时回退运行时自建）
            var homeGo = new GameObject("HomeScreen");
            homeGo.AddComponent<HomeScreen>();
            homeGo.AddComponent<HomeSceneMarker>(); // 生成哨兵：EnsureHomeScene 据此识别已生成场景，不再自动重建

            // 画布：1920×1080 参考分辨率，全屏适配
            var canvasGo = new GameObject("HomeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // 低于难度面板(900)/牌库档案(700)
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            // 背景美术 + 四角金饰 + 顶部纹章
            var bg = CreateImage(canvasGo.transform, "Bg", LoadSprite("UI/Home/home_bg"));
            StretchFull(bg.rectTransform);
            bg.raycastTarget = false;

            CreateCornerDecor(canvasGo.transform, "Corner_TL", new Vector2(0f, 1f), new Vector2(0f, 1f), 0f);
            CreateCornerDecor(canvasGo.transform, "Corner_TR", new Vector2(1f, 1f), new Vector2(1f, 1f), 270f);
            CreateCornerDecor(canvasGo.transform, "Corner_BR", new Vector2(1f, 0f), new Vector2(1f, 0f), 180f);
            CreateCornerDecor(canvasGo.transform, "Corner_BL", new Vector2(0f, 0f), new Vector2(0f, 0f), 90f);

            var orn = CreateImage(canvasGo.transform, "TopOrnament", LoadSprite("UI/Home/ornament"));
            SetRect(orn.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -300f), new Vector2(640f, 48f));
            orn.raycastTarget = false;

            // 标题 + 副标题
            CreateTmpText(canvasGo.transform, "Title", font, 84, "异 变 棋 局", TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1000f, 110f),
                new Color(0.92f, 0.8f, 0.42f), FontStyles.Bold);
            CreateTmpText(canvasGo.transform, "Subtitle", font, 24, "以牌局对抗深渊 · 在诅咒中抉择", TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -252f), new Vector2(900f, 40f),
                new Color(0.6f, 0.58f, 0.52f), FontStyles.Normal);

            // 按钮组（锚定顶部居中，四个入口）
            var btnPanel = new GameObject("BtnPanel", typeof(RectTransform));
            btnPanel.transform.SetParent(canvasGo.transform, false);
            SetRect(btnPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);

            CreateHomeButton(btnPanel.transform, font, "Btn_开始游戏", "开始游戏", "选择难度，踏入深渊", -400f);
            CreateHomeButton(btnPanel.transform, font, "Btn_继续游戏", "继续游戏", "", -540f);
            CreateHomeButton(btnPanel.transform, font, "Btn_牌库档案", "图鉴", "卡牌 · 遗物 · 药水", -680f);
            CreateHomeButton(btnPanel.transform, font, "Btn_设置", "设置", "显示 · 音量 · 游戏", -820f);

            // 底部提示
            CreateTmpText(canvasGo.transform, "Footer", font, 18, "分支 8.16.3 · F2 图鉴 · ESC 关闭面板", TextAlignmentOptions.Center,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(1200f, 34f),
                new Color(0.45f, 0.43f, 0.4f), FontStyles.Normal);

            // 设置子面板（编辑器内可见；运行时由 HomeScreen 绑定并隐藏）
            CreateSettingsPanel(canvasGo.transform);

            // 难度选择面板（独立 900 层画布场景实体；运行时由 DifficultyManager 绑定并隐藏）
            CreateDifficultyPanel(scene);
        }

        private static void CreateCornerDecor(Transform parent, string name, Vector2 anchor, Vector2 pivot, float rotation)
        {
            var img = CreateImage(parent, name, LoadSprite("UI/Home/corner"));
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = new Vector2(16f * (anchor.x < 0.5f ? 1f : -1f), 16f * (anchor.y < 0.5f ? 1f : -1f));
            rt.sizeDelta = new Vector2(240f, 240f);
            rt.localEulerAngles = new Vector3(0f, 0f, rotation);
        }

        private static void CreateHomeButton(Transform parent, TMP_FontAsset font, string name, string label, string hint, float y)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(460f, 108f));

            var img = go.GetComponent<Image>();
            img.sprite = LoadSprite("UI/Home/btn_panel");
            img.type = Image.Type.Sliced;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            CreateTmpText(go.transform, "Label", font, 32, label, TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(420f, 46f),
                new Color(0.93f, 0.9f, 0.82f), FontStyles.Bold);
            CreateTmpText(go.transform, "Hint", font, 16, hint, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(430f, 26f),
                new Color(0.55f, 0.53f, 0.5f), FontStyles.Normal);
        }

        /// <summary>
        /// 标签页式设置子面板（显示/音量/游戏 + 滚轮内容区）：与运行时共用 SettingsPanelBuilder 同一构建器，
        /// 场景实体与运行时结构完全一致，可直接在编辑器内手动调整；控件逻辑由 HomeScreen 运行时绑定
        /// （以 TabBar 标记识别新版面板；未激活的标签页内容组可在层级里手动激活后编辑）。
        /// </summary>
        private static void CreateSettingsPanel(Transform canvasT)
        {
            var handle = SettingsPanelBuilder.Build(canvasT, "HomeSettings");
            if (handle == null || handle.Panel == null) return;

            // 面板底板换用首页设置专用 9-slice 美术（builder 默认底图为通用奖励面板背景）
            Image img = handle.Panel.GetComponent<Image>();
            Sprite sp = LoadSprite("UI/Home/settings_panel");
            if (img != null && sp != null)
            {
                img.sprite = sp;
                img.type = Image.Type.Sliced;
            }
        }

        // ================= 工具 =================

        private static Sprite LoadSprite(string resourcePath)
        {
            // 必须带扩展名：LoadAssetAtPath 不带 .png 时找不到资产返回 null，场景里所有 Image 会存成空 Sprite
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + Path.GetFileName(resourcePath) + ".png");
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            return img;
        }

        private static TMP_Text CreateTmpText(Transform parent, string name, TMP_FontAsset font, float fontSize, string text,
            TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition,
            Vector2 sizeDelta, Color color, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.text = text;
            tmp.raycastTarget = false;
            SetRect(go.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            return tmp;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
        }

        private static void StretchFull(RectTransform rt)
        {
            SetRect(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        // ================= BuildSettings =================

        private static void EnsureBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            bool hasHome = false, hasMain = false;
            foreach (var s in scenes)
            {
                if (s.path == HomeScenePath) hasHome = true;
                if (s.path == MainScenePath) hasMain = true;
            }

            if (!hasHome || !hasMain)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes);
                if (!hasHome) list.Insert(0, new EditorBuildSettingsScene(HomeScenePath, true));
                if (!hasMain) list.Add(new EditorBuildSettingsScene(MainScenePath, true));
                EditorBuildSettings.scenes = list.ToArray();
                UnityEngine.Debug.Log("[HomeSceneSetup] 已注册 BuildSettings：HomeScene → MainScene");
            }
        }
    }
}
