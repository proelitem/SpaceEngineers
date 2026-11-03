using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using VoidForge.World;

namespace VoidForge.Editor
{
    /// <summary>Import ciblé ; les modèles et leurs dérivés restent sous Assets/ThirdParty, ignoré par Git.</summary>
    public static class LocalAsteroidImporter
    {
        public const string OutputRoot = "Assets/ThirdParty/UnityAssets";
        public const string SetPath = OutputRoot + "/Resources/VoidForge/LocalAsteroids.asset";
        public static string LibraryRoot => Environment.GetEnvironmentVariable("VF_UNITY_ASSETS_ROOT") ?? "G:/UnityAssets";

        [MenuItem("VoidForge/4. Importer les astéroïdes UnityAssets", priority = 5)]
        public static void ImportBatch()
        {
            string source = Path.Combine(LibraryRoot, "asset_manager/store/Asteroid_Pack_01/Files/Asteroid Pack");
            if (!Directory.Exists(source)) throw new DirectoryNotFoundException(source);
            Quest3ProjectConfigurator.EnsureFolder(OutputRoot + "/Models");
            Quest3ProjectConfigurator.EnsureFolder(OutputRoot + "/Textures");
            Quest3ProjectConfigurator.EnsureFolder(OutputRoot + "/Generated");
            Quest3ProjectConfigurator.EnsureFolder(OutputRoot + "/Resources/VoidForge");

            var materials = new Material[4];
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("Shader URP/Lit absent.");
            for (int i = 0; i < materials.Length; i++)
            {
                var diffuse = ImportTexture(source, $"Asteroid_Diffuse_{i + 1:00}.jpg", false);
                var normal = ImportTexture(source, $"Asteroid_Normal_{i + 1:00}.jpg", true);
                string path = $"{OutputRoot}/Generated/Rock_{i + 1:00}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.shader = shader;
                material.SetTexture("_BaseMap", diffuse);
                material.SetTexture("_BumpMap", normal);
                material.SetFloat("_BumpScale", 1f);
                material.SetFloat("_Smoothness", 0.15f);
                material.SetFloat("_Metallic", 0f);
                material.EnableKeyword("_NORMALMAP");
                material.enableInstancing = true;
                EditorUtility.SetDirty(material);
                materials[i] = material;
            }

            var set = AssetDatabase.LoadAssetAtPath<AsteroidAssetSet>(SetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<AsteroidAssetSet>();
                AssetDatabase.CreateAsset(set, SetPath);
            }
            set.variants = new AsteroidAssetSet.Variant[12];
            long largestLod0 = 0;
            for (int variant = 0; variant < 12; variant++)
            {
                var meshes = new Mesh[3];
                Bounds reference = default;
                for (int lod = 0; lod < 3; lod++)
                {
                    string file = $"Asteroid_{variant + 1:00}_L{lod}.FBX";
                    string path = OutputRoot + "/Models/" + file;
                    CopyIfChanged(Path.Combine(source, "Meshes", file), path);
                    var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                    importer.importAnimation = false;
                    importer.materialImportMode = ModelImporterMaterialImportMode.None;
                    // Les FBX de travail sont lus pour générer les meshes normalisés, seuls utilisés dans le jeu.
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var filter = model.GetComponentInChildren<MeshFilter>();
                    if (filter == null) throw new InvalidOperationException("Maillage absent : " + path);
                    var mesh = UnityEngine.Object.Instantiate(filter.sharedMesh);
                    mesh.name = $"Asteroid_{variant + 1:00}_L{lod}";
                    var matrix = model.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                    var vertices = mesh.vertices;
                    for (int v = 0; v < vertices.Length; v++) vertices[v] = matrix.MultiplyPoint3x4(vertices[v]);
                    if (lod == 0)
                    {
                        reference = new Bounds(vertices[0], Vector3.zero);
                        foreach (var vertex in vertices) reference.Encapsulate(vertex);
                    }
                    float diameter = Mathf.Max(reference.size.x, reference.size.y, reference.size.z);
                    if (diameter < 0.001f) throw new InvalidOperationException("Maillage dégénéré : " + path);
                    for (int v = 0; v < vertices.Length; v++) vertices[v] = (vertices[v] - reference.center) / diameter;
                    mesh.vertices = vertices;
                    var normals = mesh.normals;
                    var normalMatrix = matrix.inverse.transpose;
                    for (int v = 0; v < normals.Length; v++) normals[v] = normalMatrix.MultiplyVector(normals[v]).normalized;
                    mesh.normals = normals;
                    if (matrix.determinant < 0)
                    {
                        var indices = mesh.triangles;
                        for (int t = 0; t < indices.Length; t += 3)
                            (indices[t + 1], indices[t + 2]) = (indices[t + 2], indices[t + 1]);
                        mesh.triangles = indices;
                    }
                    mesh.RecalculateBounds();
                    mesh.RecalculateTangents();
                    string generated = $"{OutputRoot}/Generated/{mesh.name}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(generated);
                    if (existing != null)
                    {
                        EditorUtility.CopySerialized(mesh, existing);
                        UnityEngine.Object.DestroyImmediate(mesh);
                        mesh = existing;
                        EditorUtility.SetDirty(mesh);
                    }
                    else AssetDatabase.CreateAsset(mesh, generated);
                    meshes[lod] = mesh;
                }
                largestLod0 = Math.Max(largestLod0, meshes[0].triangles.Length / 3);
                // Les chemins de textures dans les FBX confirment les groupes de trois variantes.
                set.variants[variant] = new AsteroidAssetSet.Variant { lodMeshes = meshes, material = materials[variant / 3] };
            }
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            if (!set.IsUsable || largestLod0 * 40 > 500000)
                throw new InvalidOperationException("Catalogue incomplet ou budget géométrique dépassé.");
            var probe = new GameObject("AsteroidImportProbe");
            try
            {
                probe.AddComponent<AsteroidField>().Generate();
                if (probe.transform.childCount != 40 || probe.GetComponentsInChildren<LODGroup>().Length != 40 ||
                    probe.GetComponentsInChildren<Collider>().Length != 40)
                    throw new InvalidOperationException("Champ d'astéroïdes importés incomplet.");
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
            Debug.Log($"[VoidForge Assets] OK : 12 variantes, 36 maillages, 4 matériaux URP, textures 1024 ASTC6x6 ; 40 obstacles/LODGroup. Maximum LOD0 pour 40 : {largestLod0 * 40} triangles.");
        }

        private static Texture2D ImportTexture(string source, string file, bool normal)
        {
            string path = OutputRoot + "/Textures/" + file;
            CopyIfChanged(Path.Combine(source, "Textures", file), path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.mipmapEnabled = true;
            importer.isReadable = false;
            importer.maxTextureSize = 1024;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android", overridden = true, maxTextureSize = 1024,
                format = TextureImporterFormat.ASTC_6x6, compressionQuality = 50,
            });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void CopyIfChanged(string source, string target)
        {
            var from = new FileInfo(source);
            var to = new FileInfo(target);
            if (!to.Exists || from.Length != to.Length || from.LastWriteTimeUtc != to.LastWriteTimeUtc)
            {
                File.Copy(source, target, true);
                AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            }
        }
    }
}
