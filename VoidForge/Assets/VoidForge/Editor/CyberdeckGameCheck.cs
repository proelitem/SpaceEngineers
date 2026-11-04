using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VoidForge.UI.Cyberdeck;
using VoidForge.Economy;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace VoidForge.Editor
{
    // Run without -quit, on the main project only, after native flat-view validation.
    // Play changes and the added camera are discarded; Android XR configuration is untouched.
    public static class CyberdeckGameCheck
    {
        const string Key = "VoidForge.CyberdeckCheck.";
        static double deadline;
        static Task excavation;
        static decimal stoneBefore;
        static int editsBefore;
        [Serializable] sealed class Report
        {
            public bool success, headsetTested;
            public string unityVersion, error;
            public float widthMetres, heightMetres, normalDot;
            public int mapEntities, tools;
            public string[] checks;
        }
        static string Evidence => Path.GetFullPath("docs/validation/economy-2026-11-03");
        public static void RunBatch()
        {
            excavation = null;
            EconomyRuntime.ValidationSaveOverride = Path.GetFullPath("Library/EconomyGameValidation/world-" + Guid.NewGuid().ToString("N") + ".json");
            SessionState.SetBool(Key + "Run", true);
            SessionState.SetBool(Key + "OptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(Key + "Options", (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            var standalone = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (standalone != null)
            {
                SessionState.SetBool(Key + "RestoreXR", true);
                SessionState.SetBool(Key + "XR", standalone.InitManagerOnStart);
                standalone.InitManagerOnStart = false;
            }
            EditorSceneManager.OpenScene(MainSceneBuilder.ScenePath);
            deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (EditorApplication.timeSinceStartup > deadline) { Finish(new Report { error = "Timed out waiting for runtime cyberdeck binding." }); return; }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            var hud = Object.FindAnyObjectByType<CyberdeckHud>();
            if (hud == null || hud.State == null) return;
            var economy = Object.FindAnyObjectByType<EconomyRuntime>();
            if (economy == null || !economy.Ready) return;
            var mining = Object.FindAnyObjectByType<EconomyMiningAdapter>();
            if (mining != null && !mining.Ready) return;
            var report = new Report { unityVersion = Application.unityVersion, headsetTested = false };
            try
            {
#if VF_LOCAL_DIGGER
                if (excavation == null && mining != null)
                {
                    var prototype = Object.FindAnyObjectByType<VoidForge.World.MiningPrototype>();
                    Physics.SyncTransforms();
                    if (!Physics.Raycast(prototype.RockCenter + Vector3.left * 6, Vector3.right, out var rock, 12) || !rock.collider.transform.IsChildOf(prototype.transform))
                        throw new Exception("Economy mining check cannot reach the actual Digger rock.");
                    stoneBefore = economy.World.Backpack.Get("ore-stone"); editsBefore = economy.World.Excavations.Count;
                    excavation = mining.Dig(rock.point + Vector3.right * .15f); return;
                }
                if (excavation != null && !excavation.IsCompleted) return;
                if (excavation != null && (excavation.IsFaulted || economy.World.Backpack.Get("ore-stone") <= stoneBefore || economy.World.Excavations.Count != editsBefore + 1))
                    throw new Exception("Actual Digger callback did not credit and persist one excavation.");
#endif
                var anchor = hud.transform.parent;
                if (anchor == null || !anchor.name.EndsWith("HUDAnchor", StringComparison.Ordinal)) throw new Exception("HUD is not attached to delivered wrist anchor.");
                Vector3[] corners = new Vector3[4]; ((RectTransform)hud.transform).GetWorldCorners(corners);
                report.widthMetres = Vector3.Distance(corners[1], corners[2]);
                report.heightMetres = Vector3.Distance(corners[0], corners[1]);
                report.normalDot = Vector3.Dot(hud.transform.forward, anchor.forward);
                if (report.widthMetres < .198f || report.widthMetres > .2001f || report.heightMetres > .0858f || report.heightMetres < .085f)
                    throw new Exception("HUD does not fit the measured safe area.");
                if (report.normalDot > -.99f) throw new Exception("HUD front must face out of the device.");
                if (hud.State.preview || hud.State.cockpitConnected || !hud.State.cargoConnected) throw new Exception("Game is not displaying actual backpack state.");
                hud.Show(DeckPage.Refinery);
                if (hud.Controls.ContainsKey("refinery-power")) throw new Exception("Cockpit-less ship controls exposed.");
                hud.Show(DeckPage.Construction);
                if (!hud.Controls["select-block"].interactable) throw new Exception("Delivered armor missing from live catalogue.");
                hud.MoveBlock(3); // Armor -> Refinery.
                if (!hud.Controls["select-block"].interactable) throw new Exception("Functional refinery missing from live construction catalogue.");
                if (hud.State.recipes.Count != 24 || hud.assets.icons.Length != 75 || economy.World.Count("Cargo") < 1) throw new Exception("Full economy catalogue or physical cargo is missing.");
                report.mapEntities = hud.State.waypoints.Count;
                var equipment = Object.FindObjectsByType<MonoBehaviour>().FirstOrDefault(m => m != null && m.GetType().FullName == "VoidForge.Integration.EquipmentRig");
                report.tools = equipment == null ? 0 : ((Array)equipment.GetType().GetField("tools").GetValue(equipment)).Length;
                if (report.tools != 3) throw new Exception("Three delivered hand tools were not found.");
                if (report.mapEntities < 1) throw new Exception("Actual asteroid field not registered on the map.");
                // Freeze the wrist in its actual runtime transform for a native device inspection.
                Object.FindAnyObjectByType<CyberdeckRuntime>().enabled = false;
                var pointer = hud.GetComponent<CyberdeckPointer>(); if (pointer != null) pointer.enabled = false;
                hud.GetComponent<Canvas>().enabled = true;
                hud.ResetView(); economy.PublishSession(); hud.Show(DeckPage.Inventory);
                // Only reveal the cyberdeck's own delivered meshes, not the untracked avatar.
                var deck = anchor.parent;
                while (deck.parent != null && !deck.GetComponents<MonoBehaviour>().Any(b => b != null && b.GetType().FullName == "VoidForge.Integration.CyberdeckSurface")) deck = deck.parent;
                foreach (var renderer in deck.GetComponentsInChildren<Renderer>()) renderer.enabled = true;
                CaptureDevice(hud, anchor);
                report.checks = new[] { "runtime binding to delivered CyberdeckSurface", "safe area in actual FBX hierarchy", "outward readable Canvas orientation", "real backpack and initial cockpit lock", "functional refinery catalogue", "24 assembler recipes and75 icons", "three actual tools", "actual asteroid and deposit positions", "actual Digger callback credits exactly one persisted excavation", "native device preview captured with live state" };
                report.success = true;
            }
            catch (Exception e) { report.error = e.ToString(); Debug.LogException(e); }
            Finish(report);
        }
        static void CaptureDevice(CyberdeckHud hud, Transform anchor)
        {
            var go = new GameObject("Cyberdeck validation camera", typeof(Camera));
            var camera = go.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = .074f;
            camera.transform.position = anchor.position + anchor.forward * .35f;
            camera.transform.rotation = Quaternion.LookRotation(-anchor.forward, anchor.up);
            camera.nearClipPlane = .01f; camera.farClipPlane = .7f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .035f, .05f);
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32); target.Create(); camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); texture.Apply();
            Directory.CreateDirectory(Evidence); File.WriteAllBytes(Path.Combine(Evidence, "device-preview.png"), texture.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null;
            Object.Destroy(texture); target.Release(); Object.Destroy(target); Object.Destroy(go);
        }
        static void Finish(Report report)
        {
            EditorApplication.update -= Tick;
            Directory.CreateDirectory(Evidence); File.WriteAllText(Path.Combine(Evidence, "game-validation.json"), JsonUtility.ToJson(report, true));
            SessionState.SetBool(Key + "Success", report.success);
            EditorApplication.playModeStateChanged += Stopped;
            if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
            else Stopped(PlayModeStateChange.EnteredEditMode);
        }
        static void Stopped(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= Stopped;
            var standalone = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (standalone != null && SessionState.GetBool(Key + "RestoreXR", false)) standalone.InitManagerOnStart = SessionState.GetBool(Key + "XR", true);
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + "OptionsEnabled", false);
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Key + "Options", 0);
            SessionState.SetBool(Key + "Run", false);
            EconomyRuntime.ValidationSaveOverride = null;
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(SessionState.GetBool(Key + "Success", false) ? 0 : 1);
        }
    }
}
