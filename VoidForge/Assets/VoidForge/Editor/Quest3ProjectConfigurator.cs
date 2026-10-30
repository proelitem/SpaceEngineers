using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace VoidForge.Editor
{
    /// <summary>
    /// Configure le projet pour un build Meta Quest 3 (Android ARM64, IL2CPP, Vulkan, OpenXR, URP)
    /// de façon reproductible : tout passe par le code, aucune case à cocher à la main.
    /// Menu : VoidForge > 1. Configurer le projet pour Quest 3.
    /// Ligne de commande : -executeMethod VoidForge.Editor.Quest3ProjectConfigurator.ConfigureBatch
    /// </summary>
    public static class Quest3ProjectConfigurator
    {
        public const string ApplicationId = "com.proelitem.voidforge";
        public const string ProductName = "VoidForge VR";
        public const string CompanyName = "proelitem";
        public const string SettingsFolder = "Assets/VoidForge/Settings";
        public const string ResourcesFolder = "Assets/VoidForge/Resources/VoidForge";

        [MenuItem("VoidForge/1. Configurer le projet pour Quest 3", priority = 1)]
        public static void Configure()
        {
            EnsureFolders();
            ConfigurePlayerSettings();
            ConfigureInputSystem();
            ConfigureUrp();
            ConfigureXr();
            CreateMaterials();
            AssetDatabase.SaveAssets();
            Debug.Log("[VoidForge] Projet configuré pour Quest 3. Étape suivante : VoidForge > 2. Construire la scène principale.");
        }

        public static void ConfigureBatch()
        {
            Configure();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/VoidForge");
            EnsureFolder(SettingsFolder);
            EnsureFolder("Assets/VoidForge/Resources");
            EnsureFolder(ResourcesFolder);
            EnsureFolder("Assets/VoidForge/Prefabs");
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/XR");
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void ConfigurePlayerSettings()
        {
            var android = NamedBuildTarget.Android;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            PlayerSettings.productName = ProductName;
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(android, ApplicationId);
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1")
            {
                PlayerSettings.bundleVersion = "0.1.0";
            }
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;

            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.gcIncremental = true;
            PlayerSettings.stripEngineCode = true;

            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(android, ApiCompatibilityLevel.NET_Standard);
            PlayerSettings.SetIl2CppCompilerConfiguration(android, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(android, ManagedStrippingLevel.Low);
            PlayerSettings.SetMobileMTRendering(android, true);

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32; // exigé par Meta pour le Quest 3
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.androidTVCompatibility = false;
            PlayerSettings.Android.optimizedFramePacing = false; // géré par le runtime OpenXR

            // Vulkan uniquement : recommandé par Meta pour le Quest 3 avec Unity 6.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });

            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            Debug.Log("[VoidForge] PlayerSettings Android configurés (IL2CPP, ARM64, Vulkan, minSdk 32).");
        }

        /// <summary>Active le nouveau système d'entrée (« Both » pour garder les API XR héritées disponibles).</summary>
        private static void ConfigureInputSystem()
        {
            var playerSettings = new SerializedObject(Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings"));
            var prop = playerSettings.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue != 2)
            {
                prop.intValue = 2; // 0 = ancien, 1 = Input System, 2 = les deux
                playerSettings.ApplyModifiedProperties();
                Debug.Log("[VoidForge] Active Input Handling = Both (redémarrage de l'éditeur demandé par Unity).");
            }
        }

        private static void ConfigureUrp()
        {
            string rendererPath = SettingsFolder + "/URP_Quest3_Renderer.asset";
            string assetPath = SettingsFolder + "/URP_Quest3.asset";

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "URP_Quest3_Renderer";
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            if (urp == null)
            {
                urp = UniversalRenderPipelineAsset.Create(rendererData);
                urp.name = "URP_Quest3";
                AssetDatabase.CreateAsset(urp, assetPath);
            }

            // Réglages adaptés au Quest 3 : MSAA 4x, pas de HDR, ombres courtes.
            urp.msaaSampleCount = 4;
            urp.supportsHDR = false;
            urp.renderScale = 1.0f;
            urp.shadowDistance = 40f;
            urp.supportsCameraDepthTexture = false;
            urp.supportsCameraOpaqueTexture = false;
            EditorUtility.SetDirty(urp);

            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
                QualitySettings.vSyncCount = 0;
            }
            QualitySettings.SetQualityLevel(current, false);

            Debug.Log("[VoidForge] URP assigné (" + assetPath + ").");
        }

        private static void ConfigureXr()
        {
            // 1) XR Plug-in Management : chargeur OpenXR pour Android, init automatique.
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
            {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                var settings = perTarget.SettingsForBuildTarget(group);
                if (settings == null)
                {
                    perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
                    settings = perTarget.SettingsForBuildTarget(group);
                }
                if (settings == null)
                {
                    Debug.LogWarning($"[VoidForge] Impossible de créer les réglages XR pour {group}. Ouvre Project Settings > XR Plug-in Management une fois, puis relance la configuration.");
                    continue;
                }
                settings.InitManagerOnStart = true;
                XRPackageMetadataStore.AssignLoader(settings.Manager, typeof(OpenXRLoader).FullName, group);
                EditorUtility.SetDirty(settings);
            }
            EditorUtility.SetDirty(perTarget);

            // 2) OpenXR : fonctionnalité Meta Quest + profil Oculus Touch, rendu single-pass instancié.
            var openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXr == null)
            {
                // Les réglages OpenXR sont créés quand la page de réglages est ouverte une première fois.
                SettingsService.OpenProjectSettings("Project/XR Plug-in Management/OpenXR");
                openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            }
            if (openXr == null)
            {
                Debug.LogWarning("[VoidForge] Réglages OpenXR Android introuvables. Ouvre Project Settings > XR Plug-in Management > OpenXR, puis relance VoidForge > 1.");
                return;
            }

            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);

            var quest = openXr.GetFeature<UnityEditor.XR.OpenXR.Features.MetaQuestSupport.MetaQuestFeature>();
            if (quest != null) { quest.enabled = true; EditorUtility.SetDirty(quest); }
            else Debug.LogWarning("[VoidForge] Fonctionnalité « Meta Quest Support » introuvable : vérifie la version du package OpenXR.");

            var touch = openXr.GetFeature<OculusTouchControllerProfile>();
            if (touch != null) { touch.enabled = true; EditorUtility.SetDirty(touch); }
            else Debug.LogWarning("[VoidForge] Profil « Oculus Touch Controller » introuvable.");

            openXr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            openXr.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;
            EditorUtility.SetDirty(openXr);

            Debug.Log("[VoidForge] OpenXR configuré (Meta Quest Support, Oculus Touch, single-pass instancié).");
        }

        /// <summary>Crée les matériaux dans Resources pour qu'ils soient toujours inclus dans l'APK.</summary>
        private static void CreateMaterials()
        {
            CreateMaterial("Mat_Hand", new Color(0.85f, 0.85f, 0.9f), unlit: false);
            CreateMaterial("Mat_Rock", new Color(0.35f, 0.33f, 0.30f), unlit: false);
            CreateMaterial("Mat_Star", Color.white, unlit: true);
            CreateMaterial("Mat_Avatar", new Color(0.2f, 0.6f, 1.0f), unlit: false);
        }

        private static void CreateMaterial(string name, Color color, bool unlit)
        {
            string path = ResourcesFolder + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            var mat = World.MaterialLibrary.CreateFallback(name, color, unlit);
            AssetDatabase.CreateAsset(mat, path);
        }
    }
}
