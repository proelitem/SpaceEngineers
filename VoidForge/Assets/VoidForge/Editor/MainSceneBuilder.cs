using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidForge.Core;
using VoidForge.Net;
using VoidForge.Player;
using VoidForge.World;

namespace VoidForge.Editor
{
    /// <summary>
    /// Construit la scène principale en code et l'enregistre dans Assets/Scenes/Main.unity.
    /// Menu : VoidForge > 2. Construire la scène principale.
    /// Ligne de commande : -executeMethod VoidForge.Editor.MainSceneBuilder.BuildBatch
    /// </summary>
    public static class MainSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        public const string AvatarPrefabPath = "Assets/VoidForge/Prefabs/NetworkPlayerAvatar.prefab";

        [MenuItem("VoidForge/2. Construire la scène principale", priority = 2)]
        public static void Build()
        {
            Quest3ProjectConfigurator.EnsureFolder("Assets/Scenes");
            Quest3ProjectConfigurator.EnsureFolder("Assets/VoidForge/Prefabs");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Éclairage : pas de skybox, ambiance sombre, un « soleil » lointain.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.08f, 0.09f, 0.12f);
            RenderSettings.fog = false;

            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            RenderSettings.sun=light;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.6f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(35f, -60f, 0f);

            var bootstrap = new GameObject("GameBootstrap");
            bootstrap.AddComponent<GameBootstrap>();

            PlayerRigFactory.Create(Vector3.zero);

            var starfield = new GameObject("Starfield");
            starfield.AddComponent<MeshFilter>();
            starfield.AddComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Star;
            starfield.AddComponent<Starfield>();

            if (!VoidForge.Integration.SurvivalSettings.Supported || Resources.Load<VoidForge.Integration.SurvivalSettings>(VoidForge.Integration.SurvivalSettings.Resource) == null)
            {
                var asteroids = new GameObject("AsteroidField");
                asteroids.AddComponent<AsteroidField>();
            }

            BuildNetwork();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[VoidForge] Scène construite : " + ScenePath);
        }

        public static void BuildBatch()
        {
            Build();
        }

        private static void BuildNetwork()
        {
            var prefab = CreateAvatarPrefab();

            var netGo = new GameObject("NetworkManager");
            var manager = netGo.AddComponent<NetworkManager>();
            var transport = netGo.AddComponent<UnityTransport>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                PlayerPrefab = prefab,
                TickRate = 30,
                ConnectionApproval = false,
                EnableSceneManagement = false,
            };
            manager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = prefab });

            netGo.AddComponent<NetworkLauncher>();
        }

        private static GameObject CreateAvatarPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefabPath);
            if (existing != null) return existing;

            var go = new GameObject("NetworkPlayerAvatar");
            go.AddComponent<NetworkObject>();
            var avatar = go.AddComponent<NetworkPlayerAvatar>();
            avatar.BuildVisuals();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, AvatarPrefabPath);
            Object.DestroyImmediate(go);
            AssetDatabase.ImportAsset(AvatarPrefabPath, ImportAssetOptions.ForceUpdate);
            return prefab;
        }
    }
}
