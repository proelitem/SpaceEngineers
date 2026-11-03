#if VF_LOCAL_DIGGER
using System;
using Digger.Modules.Core.Sources;
using Digger.Modules.Core.Sources.Jobs;
using Digger.Modules.Runtime.Sources;
using Digger.Modules.Runtime.Sources.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidForge.World;

namespace VoidForge.Editor
{
    public static class MiningPrototypeBuilder
    {
        public const string PrefabPath = LocalAsteroidImporter.OutputRoot + "/Resources/VoidForge/LocalMiningPrototype.prefab";

        [MenuItem("VoidForge/5. Construire la zone de creusage", priority = 6)]
        public static void BuildBatch()
        {
            var assets = AssetDatabase.LoadAssetAtPath<AsteroidAssetSet>(LocalAsteroidImporter.SetPath);
            if (assets == null || !assets.IsUsable) LocalAsteroidImporter.ImportBatch();
            var previous = EnsureSavedScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var root = new GameObject("MiningPrototype");
                string dataPath = LocalAsteroidImporter.OutputRoot + "/Generated/MiningTerrain.asset";
                var data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
                if (data == null) { data = new TerrainData(); AssetDatabase.CreateAsset(data, dataPath); }
                data.heightmapResolution = 65;
                data.alphamapResolution = 64;
                data.size = new Vector3(16, 16, 16);
                var heights = new float[65, 65];
                for (int z = 0; z < 65; z++)
                    for (int x = 0; x < 65; x++)
                    {
                        heights[z, x] = 0.05f;
                    }
                data.SetHeights(0, 0, heights);
                var holes = new bool[64, 64];
                for (int z = 0; z < 64; z++) for (int x = 0; x < 64; x++) holes[z, x] = true;
                data.SetHoles(0, 0, holes);
                data.enableHolesTextureCompression = false;
                string layerPath = LocalAsteroidImporter.OutputRoot + "/Generated/MiningLayer.terrainlayer";
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
                if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, layerPath); }
                layer.diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(LocalAsteroidImporter.OutputRoot + "/Textures/Asteroid_Diffuse_01.jpg");
                layer.normalMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(LocalAsteroidImporter.OutputRoot + "/Textures/Asteroid_Normal_01.jpg");
                layer.tileSize = new Vector2(4, 4);
                layer.smoothness = 0.1f;
                data.terrainLayers = new[] { layer };
                var alpha = new float[64, 64, 1];
                for (int z = 0; z < 64; z++) for (int x = 0; x < 64; x++) alpha[z, x, 0] = 1;
                data.SetAlphamaps(0, 0, alpha);
                var terrainObject = Terrain.CreateTerrainGameObject(data);
                terrainObject.name = "VoxelTestTerrain";
                terrainObject.transform.SetParent(root.transform, false);
                terrainObject.transform.position = new Vector3(-8, -10, 0);
                var terrain = terrainObject.GetComponent<Terrain>();
                terrain.drawInstanced = true;
                terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var master = new GameObject("Digger Master").AddComponent<DiggerMaster>();
                master.transform.SetParent(root.transform, false);
                master.SceneDataFolder = "VoidForgeMiningPrototype";
                master.ChunkSize = 17;
                master.ResolutionMult = 1;
                master.AutoVoxelHeight = true;
                master.CreateLODs = false;
                master.EnableContributeGI = false;
                master.EnableOcclusionCulling = false;
                DiggerMasterRuntimeEditor.SetupRuntimeScripts();
                var runtime = UnityEngine.Object.FindFirstObjectByType<DiggerMasterRuntime>();
                runtime.enablePersistence = false;
                runtime.transform.SetParent(root.transform, false);
                var sign = new GameObject("MiningInstructions").AddComponent<TextMesh>();
                sign.transform.SetParent(root.transform, false);
                sign.transform.position = new Vector3(-2, 1, 6);
                sign.characterSize = 0.08f;
                sign.fontSize = 48;
                sign.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                sign.GetComponent<MeshRenderer>().sharedMaterial = sign.font.material;
                sign.color = new Color(1, 0.7f, 0.2f);
                sign.text = "ASTÉROÏDE CREUSABLE\nGâchette droite : creuser (8 m)";
                var prototype = root.AddComponent<MiningPrototype>();
                prototype.terrain = terrain;
                prototype.digger = runtime;
                prototype.label = sign;
                prototype.HideSupportTerrain();
                EditorUtility.SetDirty(data);
                EditorUtility.SetDirty(layer);
                AssetDatabase.SaveAssets();
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null || root.GetComponentsInChildren<DiggerSystem>().Length != 1)
                    throw new InvalidOperationException("Prototype Digger incomplet.");
                AssetDatabase.SaveAssets();
                Debug.Log("[VoidForge Mining] Préfab Digger prêt : terrain 16 m, voxels 0,25 m, chunks 16, persistance désactivée, test collider automatique en build développement.");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            ValidateBatch();
        }

        public static void ValidateBatch()
        {
            var previous = EnsureSavedScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            TerrainData clone = null;
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
                var prototype = instance.GetComponent<MiningPrototype>();
                clone = UnityEngine.Object.Instantiate(prototype.terrain.terrainData);
                prototype.terrain.terrainData = clone;
                prototype.terrain.GetComponent<TerrainCollider>().terrainData = clone;
                instance.GetComponentInChildren<DiggerMaster>().SceneDataFolder = "VoidForgeMiningSmoke";
                prototype.digger.SendMessage("Awake");
                prototype.digger.Modify(prototype.RockCenter, BrushType.Sphere, ActionType.Add, 0, 1f, MiningPrototype.RockRadius);
                prototype.HideSupportTerrain();
                Physics.SyncTransforms();
                Vector3 origin = prototype.ProbePoint + Vector3.up * 2;
                if (!Physics.Raycast(origin, Vector3.down, out var before, 12f))
                    throw new InvalidOperationException("Recette Digger : collider initial absent.");
                prototype.digger.Modify(before.point - Vector3.up * 0.15f, BrushType.Sphere, ActionType.Dig, 0, 1f, 0.75f);
                prototype.HideSupportTerrain();
                Physics.SyncTransforms();
                if (!Physics.Raycast(origin, Vector3.down, out var after, 12f) || after.distance <= before.distance + 0.05f)
                    throw new InvalidOperationException("Recette Digger : collision inchangée après excavation.");
                Debug.Log($"[VoidForge Mining] EDITOR SELFTEST OK : profondeur collider +{after.distance - before.distance:0.000} m ; données de terrain clonées, prefab intact.");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
                if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
                NativeCollectionsPool.Instance.Dispose();
            }
        }

        private static Scene EnsureSavedScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                if (!Application.isBatchMode)
                    throw new InvalidOperationException("Enregistrer la scène ouverte avant de construire le prototype.");
                if (!EditorSceneManager.SaveScene(scene, LocalAsteroidImporter.OutputRoot + "/Generated/ImportScratch.unity"))
                    throw new InvalidOperationException("Impossible de sauvegarder la scène temporaire d'import.");
            }
            return scene;
        }
    }
}
#endif
