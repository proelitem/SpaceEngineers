using UnityEngine;

namespace VoidForge.World
{
    /// <summary>
    /// Astéroïdes décoratifs issus de la bibliothèque locale, placés à partir d'une graine.
    /// Repli sur les primitives J1 si les ressources sous licence ne sont pas installées.
    /// Les maillages décoratifs ne sont pas des volumes voxel creusables.
    /// </summary>
    public sealed class AsteroidField : MonoBehaviour
    {
        [SerializeField] private int count = 40;
        [SerializeField] private float fieldRadius = 80f;
        [Tooltip("Rayon libre autour de l'origine (zone d'apparition du joueur).")]
        [SerializeField] private float clearRadius = 10f;
        [SerializeField] private float minSize = 2f;
        [SerializeField] private float maxSize = 14f;
        [SerializeField] private int seed = 42;

        private void Awake()
        {
            if (VoidForge.Integration.SurvivalSettings.Supported && Resources.Load<VoidForge.Integration.SurvivalSettings>(VoidForge.Integration.SurvivalSettings.Resource) != null) return;
            if (transform.childCount > 0) return; // déjà généré dans la scène
            Generate();
        }

        public void Generate()
        {
            var rng = new System.Random(seed);
            var mat = MaterialLibrary.Rock;
            var assets = Resources.Load<AsteroidAssetSet>(AsteroidAssetSet.ResourcePath);
            bool useAssets = assets != null && assets.IsUsable;
            if (!useAssets)
                Debug.LogWarning("[VoidForge] Astéroïdes locaux absents : utiliser VoidForge > 4. Importer les astéroïdes UnityAssets. Repli sur les primitives J1.");

            for (int i = 0; i < count; i++)
            {
                Vector3 pos;
                int guard = 0;
                do
                {
                    pos = new Vector3(Range(rng, -1f, 1f), Range(rng, -1f, 1f), Range(rng, -1f, 1f)) * fieldRadius;
                    guard++;
                } while (pos.magnitude < clearRadius && guard < 20);

                var go = useAssets ? new GameObject() : GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"Asteroid_{i:00}";
                go.transform.SetParent(transform, false);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(Range(rng, 0f, 360f), Range(rng, 0f, 360f), Range(rng, 0f, 360f));
                float s = Range(rng, minSize, maxSize);
                go.transform.localScale = new Vector3(s * Range(rng, 0.7f, 1.3f), s * Range(rng, 0.6f, 1.2f), s * Range(rng, 0.7f, 1.3f));
                go.isStatic = true;

                if (useAssets) assets.AddVisual(go, i);
                else go.GetComponent<Renderer>().sharedMaterial = mat;
            }
            Debug.Log($"[VoidForge] Astéroïdes : {count}, source {(useAssets ? "Asteroid Pack 01, 3 LOD" : "primitives J1")}.");
        }

        private static float Range(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
