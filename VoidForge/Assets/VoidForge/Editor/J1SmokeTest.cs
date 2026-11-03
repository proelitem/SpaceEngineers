using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEngine;
using VoidForge.Player;
using VoidForge.World;

namespace VoidForge.Editor
{
    /// <summary>Recette sans casque : exécuter sans -quit, avec -executeMethod ...RunBatch.</summary>
    public static class J1SmokeTest
    {
        private const string Running = "VoidForge.J1Smoke.Running";
        private const string Errors = "VoidForge.J1Smoke.Errors";
        private const string RestoreXr = "VoidForge.J1Smoke.RestoreXr";
        private const string OriginalXr = "VoidForge.J1Smoke.OriginalXr";
        private const string OriginalPlayEnabled = "VoidForge.J1Smoke.OriginalPlayEnabled";
        private const string OriginalPlayOptions = "VoidForge.J1Smoke.OriginalPlayOptions";
        private static double _playStart;
        private static double _deadline;
        private static double _readyAt;

        public static void RunBatch()
        {
            // Laisser finir les callbacks de démarrage de l'éditeur avant de mesurer Play.
            _readyAt = EditorApplication.timeSinceStartup + 5;
            EditorApplication.update += WaitForStartup;
        }

        private static void WaitForStartup()
        {
            if (EditorApplication.timeSinceStartup < _readyAt || EditorApplication.isCompiling) return;
            EditorApplication.update -= WaitForStartup;
            Debug.Log("[VoidForge J1] Début de la mesure Play après le démarrage de l'éditeur.");
            SessionState.SetBool(OriginalPlayEnabled, EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetInt(OriginalPlayOptions, (int)EditorSettings.enterPlayModeOptions);
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            // Ce test couvre la scène sans matériel XR. Conserver le réglage Android intact.
            var standalone = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (standalone != null)
            {
                SessionState.SetBool(OriginalXr, standalone.InitManagerOnStart);
                SessionState.SetBool(RestoreXr, true);
                standalone.InitManagerOnStart = false;
                EditorUtility.SetDirty(standalone);
                AssetDatabase.SaveAssets();
            }
            EditorSceneManager.OpenScene(MainSceneBuilder.ScenePath);
            SessionState.SetInt(Errors, 0);
            SessionState.SetBool(Running, true);
            ResumeAfterReload();
            EditorApplication.isPlaying = true;
        }

        [InitializeOnLoadMethod]
        private static void ResumeAfterReload()
        {
            if (!SessionState.GetBool(Running, false)) return;
            _playStart = 0;
            _deadline = EditorApplication.timeSinceStartup + 120;
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                SessionState.SetInt(Errors, SessionState.GetInt(Errors, 0) + 1);
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup > _deadline)
            {
                Finish(false, "Délai d'entrée en Play dépassé (120 s).");
                return;
            }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (_playStart == 0) _playStart = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - _playStart < 8) return;

            try
            {
                var camera = Camera.main;
                Require(camera != null, "Caméra principale absente.");
                Require(camera.GetComponentInParent<ZeroGLocomotion>() != null, "Rig physique absent.");
                var stars = UnityEngine.Object.FindFirstObjectByType<Starfield>();
                Require(stars != null && stars.GetComponent<MeshFilter>().sharedMesh.vertexCount == 7500,
                    "Maillage des 2500 étoiles absent.");
                var starMesh = stars.GetComponent<MeshFilter>().sharedMesh;
                var vertices = starMesh.vertices;
                var indices = starMesh.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var a = vertices[indices[i]];
                    var b = vertices[indices[i + 1]];
                    var c = vertices[indices[i + 2]];
                    Require(Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f) < 0,
                        "Une étoile présente sa face arrière à la caméra centrale.");
                }
                var rocks = UnityEngine.Object.FindFirstObjectByType<AsteroidField>();
                Require(rocks != null && rocks.transform.childCount == 40, "Les 40 astéroïdes ne sont pas générés.");
                Require(rocks.GetComponentsInChildren<Collider>().Length == 40, "Colliders d'astéroïdes absents.");
                var hud = camera.GetComponentInChildren<TextMesh>();
                Require(hud != null && hud.text.Contains("i/s") && hud.text.Contains("m/s"), "HUD non actualisé.");
                Require(SessionState.GetInt(Errors, 0) == 0, "Erreur ou exception pendant Play : voir le journal.");
                Finish(true, "8 s en Play sans casque : caméra, rig, 2500 étoiles, 40 astéroïdes avec colliders et HUD actualisé ; aucune erreur.");
            }
            catch (Exception e)
            {
                Finish(false, e.Message);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish(bool success, string message)
        {
            SessionState.SetBool(Running, false);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            try
            {
                EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(OriginalPlayOptions, 0);
                EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(OriginalPlayEnabled, false);
                if (SessionState.GetBool(RestoreXr, false))
                {
                    var standalone = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
                    standalone.InitManagerOnStart = SessionState.GetBool(OriginalXr, true);
                    EditorUtility.SetDirty(standalone);
                    AssetDatabase.SaveAssets();
                    SessionState.SetBool(RestoreXr, false);
                }
            }
            catch (Exception e)
            {
                success = false;
                message += " Restauration XR impossible : " + e.Message;
            }
            if (success) Debug.Log("[VoidForge J1] OK : " + message);
            else Debug.LogError("[VoidForge J1] KO : " + message);
            EditorApplication.Exit(success ? 0 : 1);
        }
    }
}
