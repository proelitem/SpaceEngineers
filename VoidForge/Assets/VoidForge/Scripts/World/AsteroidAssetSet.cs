using System;
using UnityEngine;

namespace VoidForge.World
{
    /// <summary>Références locales générées depuis la bibliothèque UnityAssets, sans embarquer ses sources dans Git.</summary>
    public sealed class AsteroidAssetSet : ScriptableObject
    {
        public const string ResourcePath = "VoidForge/LocalAsteroids";

        [Serializable]
        public sealed class Variant
        {
            public Mesh[] lodMeshes;
            public Material material;
        }

        public Variant[] variants;

        public bool IsUsable => variants != null && variants.Length > 0 &&
            Array.TrueForAll(variants, v => v != null && v.material != null && v.lodMeshes != null &&
                v.lodMeshes.Length == 3 && Array.TrueForAll(v.lodMeshes, mesh => mesh != null));

        public void AddVisual(GameObject asteroid, int index)
        {
            var variant = variants[index % variants.Length];
            var levels = new LOD[3];
            float[] thresholds = { 0.15f, 0.05f, 0.005f };
            for (int i = 0; i < levels.Length; i++)
            {
                var child = new GameObject("LOD" + i);
                child.transform.SetParent(asteroid.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = variant.lodMeshes[i];
                var renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = variant.material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                levels[i] = new LOD(thresholds[i], new Renderer[] { renderer });
            }
            var group = asteroid.AddComponent<LODGroup>();
            group.SetLODs(levels);
            group.RecalculateBounds();
            // Même obstacle physique que J1 ; aucun recooking de MeshCollider sur le casque.
            asteroid.AddComponent<SphereCollider>().radius = 0.5f;
        }
    }
}
