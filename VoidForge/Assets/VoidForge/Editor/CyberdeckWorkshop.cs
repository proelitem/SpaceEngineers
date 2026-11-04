using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using VoidForge.UI.Cyberdeck;
using Object = UnityEngine.Object;

namespace VoidForge.Editor
{
    public static class CyberdeckWorkshop
    {
        const string Art = "Assets/VoidForge/Art/UI/Cyberdeck";
        const string Output = "Assets/Resources/VoidForge/Cyberdeck";
        [Serializable] class Trim { public string id; public float x, y, w, h; }
        [Serializable] class Trims { public Trim[] icons; }
        [Serializable] class Report
        {
            public string unityVersion, renderPipeline, projectPath, referenceResolution = "1914x822";
            public bool success, xrVerified, measured98Percent;
            public int icons, shaderErrors;
            public string[] checks, captures;
        }
        static readonly List<string> checks = new List<string>();
        static double readyAt;
        static string Evidence
        {
            get
            {
                var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-vfHudOutput");
                return index >= 0 ? args[index + 1] : Path.GetFullPath("docs/validation/cyberdeck-2026-11-03");
            }
        }
        [MenuItem("VoidForge/Cyberdeck/1. Générer le HUD 2D")]
        public static void Build()
        {
            Directory.CreateDirectory(Output); AssetDatabase.Refresh();
            var trims = JsonUtility.FromJson<Trims>(File.ReadAllText(Art + "/icon-trim.json"));
            var icons = new List<CyberdeckAssets.Icon>();
            foreach (var trim in trims.icons)
            {
                string path = Art + "/" + trim.id + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new FileNotFoundException(path);
                bool mask = trim.id.StartsWith("ui-", StringComparison.Ordinal);
                importer.textureType = TextureImporterType.Default;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.sRGBTexture = !mask; importer.alphaIsTransparency = !mask;
                importer.mipmapEnabled = false; importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = mask ? 512 : 1024;
                importer.textureCompression = mask ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true,
                    maxTextureSize = mask ? 512 : 1024, format = mask ? TextureImporterFormat.RGB24 : TextureImporterFormat.ASTC_6x6 });
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) throw new InvalidOperationException("Expected Texture2D: " + path + ", imported as " + AssetDatabase.LoadMainAssetAtPath(path)?.GetType().Name);
                icons.Add(new CyberdeckAssets.Icon { id = trim.id, texture = texture,
                    uv = new Rect(trim.x, trim.y, trim.w, trim.h) });
            }
            var graphics = AssetDatabase.LoadAssetAtPath<CyberdeckAssets>(Output + "/Graphics.asset");
            if (graphics == null) { graphics = ScriptableObject.CreateInstance<CyberdeckAssets>(); AssetDatabase.CreateAsset(graphics, Output + "/Graphics.asset"); }
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Art + "/VoidForgeGlyphMask.shader");
            if (shader == null) throw new Exception("HUD mask shader missing");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Output + "/GlyphMask.mat");
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, Output + "/GlyphMask.mat"); }
            graphics.icons = icons.ToArray(); graphics.font = AssetDatabase.LoadAssetAtPath<Font>(Art + "/DejaVuSans-Bold.ttf");
            graphics.textStyle = FontStyle.Normal;
            if (graphics.font == null) { graphics.font = AssetDatabase.LoadAssetAtPath<Font>(Art + "/LiberationSans.ttf"); graphics.textStyle = FontStyle.Bold; }
            graphics.glyphMaterial = material; EditorUtility.SetDirty(graphics);
            var pointerMaterial = AssetDatabase.LoadAssetAtPath<Material>(Output + "/Pointer.mat");
            if (pointerMaterial == null)
            {
                pointerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                pointerMaterial.SetColor("_BaseColor", new Color(.3f, .85f, 1));
                AssetDatabase.CreateAsset(pointerMaterial, Output + "/Pointer.mat");
            }
            graphics.pointerMaterial = pointerMaterial;
            var root = new GameObject("Cyberdeck HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CyberdeckSession));
            try
            {
                var rect = root.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(CyberdeckHud.Width, CyberdeckHud.Height);
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 50;
                root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 1;
                var hud = root.AddComponent<CyberdeckHud>(); hud.assets = graphics;
                root.transform.localScale = Vector3.one * (.200f / CyberdeckHud.Width);
                PrefabUtility.SaveAsPrefabAsset(root, Output + "/Hud.prefab");
            }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Debug.Log("[VoidForge Cyberdeck] Native world-space HUD prefab generated with " + icons.Count + " original graphics.");
        }
        [MenuItem("VoidForge/Cyberdeck/2. Ouvrir la scène de recette")]
        public static void Review()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/Hud.prefab") == null) Build();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var hud = CreateReview(out var camera); hud.State.LoadPreview(); hud.Show(DeckPage.Inventory);
            Directory.CreateDirectory("Assets/VoidForge/Scenes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/VoidForge/Scenes/CyberdeckReview.unity");
            Selection.activeGameObject = hud.gameObject;
        }
        public static void RunBatch()
        {
            readyAt = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += Begin;
        }
        static void Begin()
        {
            if (EditorApplication.timeSinceStartup < readyAt || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= Begin;
            try
            {
                Build();
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
                if (GraphicsSettings.defaultRenderPipeline == null)
                {
                    var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    var pipeline = UniversalRenderPipelineAsset.Create(renderer); pipeline.msaaSampleCount = 1; pipeline.renderScale = 1;
                    GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
                }
                readyAt = EditorApplication.timeSinceStartup + 2;
                EditorApplication.update += Finish;
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        static CyberdeckHud CreateReview(out Camera camera)
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Output + "/Hud.prefab"));
            go.transform.localScale = Vector3.one;
            var hud = go.GetComponent<CyberdeckHud>(); hud.Initialize();
            var cameraGo = new GameObject("Cyberdeck Review Camera", typeof(Camera)); cameraGo.tag = "MainCamera";
            camera = cameraGo.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = CyberdeckHud.Height * .5f;
            camera.transform.position = new Vector3(0, 0, -1000); camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.015f, .05f, .07f);
            camera.allowHDR = false; camera.allowMSAA = false;
            go.GetComponent<Canvas>().worldCamera = camera;
            var pointer = go.AddComponent<CyberdeckPointer>(); pointer.hud = hud;
            return hud;
        }
        static void Finish()
        {
            if (EditorApplication.timeSinceStartup < readyAt) return;
            EditorApplication.update -= Finish;
            var report = new Report { unityVersion = Application.unityVersion, renderPipeline = "Universal RP 17.4",
                projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/'), xrVerified = false, measured98Percent = false };
            try
            {
                Directory.CreateDirectory(Evidence); checks.Clear();
                var hud = CreateReview(out Camera camera);
                hud.State.LoadPreview();
                Check(hud.assets.icons.Length == JsonUtility.FromJson<Trims>(File.ReadAllText(Art + "/icon-trim.json")).icons.Length && hud.assets.icons.All(i => i.texture != null), "all catalogue graphics resolved");
                Check(hud.GetComponentsInChildren<DeckPanel>().All(p => p.canvasRenderer != null), "every custom panel has a CanvasRenderer");
                report.icons = hud.assets.icons.Length;
                TestInteractions(hud);
                var captures = new List<string>();
                foreach (DeckPage page in Enum.GetValues(typeof(DeckPage)))
                {
                    hud.ResetView(); hud.State.LoadPreview(); hud.Show(page);
                    string name = page.ToString().ToLowerInvariant() + ".png";
                    Capture(hud, camera, name); captures.Add(name);
                }
                hud.State.preview = false; hud.State.DisconnectCockpit(); hud.Show(DeckPage.Refinery);
                Capture(hud, camera, "ship-disconnected.png"); captures.Add("ship-disconnected.png");
                hud.State.cargoConnected = false; hud.State.items.Clear(); hud.Show(DeckPage.Inventory);
                Capture(hud, camera, "inventory-disconnected.png"); captures.Add("inventory-disconnected.png");
                hud.ResetView(); hud.State.LoadPreview(); hud.Show(DeckPage.Assembly); hud.State.items.Find(i => i.id == "iron").quantity = 0; hud.State.Publish();
                Check(!hud.Controls["assembly-add"].interactable, "insufficient resources disable enqueue");
                Capture(hud, camera, "assembly-insufficient.png"); captures.Add("assembly-insufficient.png");
                var shader = hud.assets.glyphMaterial.shader;
                report.shaderErrors = ShaderUtil.GetShaderMessages(shader).Count(m => m.severity.ToString() == "Error");
                Check(report.shaderErrors == 0 && !ShaderUtil.ShaderHasError(shader), "URP glyph shader renders without compiler errors");
                report.captures = captures.ToArray(); report.checks = checks.ToArray(); report.success = true;
                File.WriteAllText(Path.Combine(Evidence, "unity-validation.json"), JsonUtility.ToJson(report, true));
                // Save a completely interactive, clearly labelled review scene, not a game scene override.
                hud.ResetView(); hud.State.LoadPreview(); hud.Show(DeckPage.Inventory);
                Directory.CreateDirectory("Assets/VoidForge/Scenes");
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/VoidForge/Scenes/CyberdeckReview.unity");
                AssetDatabase.SaveAssets(); Debug.Log("VF_CYBERDECK_UI_OK"); EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                report.success = false; report.checks = checks.ToArray(); Directory.CreateDirectory(Evidence);
                File.WriteAllText(Path.Combine(Evidence, "unity-validation.json"), JsonUtility.ToJson(report, true));
                Debug.LogException(e); EditorApplication.Exit(1);
            }
        }
        static void TestInteractions(CyberdeckHud hud)
        {
            Check(hud.Controls.Keys.Count(k => k.StartsWith("nav-")) == 4, "four persistent navigation tabs");
            Check(hud.Invoke("tool-1") && hud.State.equippedTool == 1, "welder selection");
            hud.Invoke("filter-materials"); Check(hud.Materials && hud.Controls.ContainsKey("item-plates") && !hud.Controls.ContainsKey("item-iron-ore"), "inventory filters use actual entries");
            hud.Invoke("cargo-source"); Check(hud.Controls.ContainsKey("cargo-source-current"), "cargo source dropdown opens");
            hud.Invoke("cargo-source-current");
            hud.Invoke("nav-construction"); Check(hud.page == DeckPage.Construction, "construction navigation");
            int before = hud.BlockIndex; hud.Invoke("carousel-right"); Check(hud.BlockIndex != before, "block carousel advances");
            hud.Invoke("select-block"); Check(hud.State.selectedBlock == "Cockpit", "construction selection command");
            hud.Show(DeckPage.Assembly); hud.Invoke("quantity-minus"); Check(hud.Quantity == 19, "quantity minus");
            hud.Invoke("quantity-plus"); Check(hud.Quantity == 20, "quantity plus");
            float iron = hud.State.items.Find(i => i.id == "iron").quantity;
            hud.Invoke("assembly-add"); Check(hud.State.assembly.Count == 3 && hud.State.items.Find(i => i.id == "iron").quantity == iron - 40, "preview enqueue reserves input once");
            var recipe = hud.State.recipes[0]; Check(!hud.State.Enqueue(recipe, 999) && !hud.State.Enqueue(recipe, 0), "reject over-budget and zero batches");
            hud.Invoke("recipe-circuits"); Check(!hud.Controls["assembly-add"].interactable, "undefined recipes cannot be fabricated");
            hud.Invoke("assembly-pause"); Check(hud.State.assemblyPaused, "assembly pause");
            hud.Show(DeckPage.Refinery); hud.Invoke("refinery-power"); Check(!hud.State.refineryPowered, "refinery power toggle");
            hud.Invoke("refinery-pause"); Check(hud.State.refineryPaused, "refinery pause toggle");
            hud.Show(DeckPage.Map); float range = hud.MapRange; hud.Invoke("map-zoom-in"); Check(hud.MapRange < range, "map zoom");
            hud.Invoke("gps-iron-a07"); hud.Invoke("gps-target"); Check(hud.State.targetedWaypoint == "iron-a07", "GPS selection and targeting");
            float yaw = hud.MapYaw; hud.Orbit(new Vector2(20, 10)); Check(hud.MapYaw == yaw + 20, "Vector3 map orbit");
            hud.Invoke("map-recenter"); Check(hud.MapYaw == -20 && hud.MapPitch == 22, "map recenter");
            hud.Invoke("gps-filter"); hud.Invoke("gps-filter-BASES"); Check(hud.Category == "BASES" && !hud.Controls.ContainsKey("gps-iron-a07"), "GPS category filter");
            hud.State.DisconnectCockpit(); hud.Show(DeckPage.Assembly);
            Check(!hud.Controls.ContainsKey("assembly-add") && !hud.State.Enqueue(recipe, 1), "cockpit exit immediately revokes ship actions");
            hud.State.LoadPreview(); hud.Show(DeckPage.Inventory);
            var pointer = hud.GetComponent<CyberdeckPointer>();
            var button = hud.Controls["nav-map"];
            Vector3 center = button.transform.TransformPoint(((RectTransform)button.transform).rect.center);
            var front = new Ray(center - Vector3.forward, Vector3.forward);
            pointer.Point(front, false, true); pointer.Point(front, true, false); pointer.Point(front, false, true);
            Check(hud.page == DeckPage.Map, "front-facing ray activates button on release");
            hud.Show(DeckPage.Inventory);
            pointer.Point(new Ray(center + Vector3.forward, Vector3.back), false, true);
            pointer.Point(new Ray(center + Vector3.forward, Vector3.back), true, false);
            pointer.Point(new Ray(center + Vector3.forward, Vector3.back), false, true);
            Check(hud.page == DeckPage.Inventory, "back-facing ray cannot activate button");
            Check(hud.GetComponentsInChildren<Text>().Length > 15 && hud.GetComponentsInChildren<RawImage>().All(i => !i.name.EndsWith(".png")), "native live text and separate graphics");
        }
        static void Capture(CyberdeckHud hud, Camera camera, string name)
        {
            Canvas.ForceUpdateCanvases();
            var rt = new RenderTexture(1914, 822, 24, RenderTextureFormat.ARGB32);
            rt.Create(); camera.targetTexture = rt;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(1914, 822, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1914, 822), 0, 0); texture.Apply();
            int activeX = hud.page == DeckPage.Inventory ? 250 : hud.page == DeckPage.Construction ? 720 : hud.page == DeckPage.Map ? 1680 : 1180;
            Color amber = texture.GetPixel(activeX, 822 - 695);
            Check(amber.r > .7f && amber.g > .35f && amber.b < .4f, "visible native amber panel: " + name + " (" + amber + ")");
            File.WriteAllBytes(Path.Combine(Evidence, name), texture.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            Object.DestroyImmediate(texture); rt.Release(); Object.DestroyImmediate(rt);
        }
        static void Check(bool condition, string name)
        { if (!condition) throw new InvalidOperationException("Cyberdeck check failed: " + name); checks.Add(name); }
    }
    public sealed class CyberdeckTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/VoidForge/Art/UI/Cyberdeck/", StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            bool mask = Path.GetFileName(assetPath).StartsWith("ui-", StringComparison.Ordinal);
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.sRGBTexture = !mask; importer.alphaIsTransparency = !mask;
            importer.mipmapEnabled = false; importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = mask ? 512 : 1024;
            importer.textureCompression = mask ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings { name = "Android", overridden = true,
                maxTextureSize = mask ? 512 : 1024, format = mask ? TextureImporterFormat.RGB24 : TextureImporterFormat.ASTC_6x6 });
        }
    }
}
