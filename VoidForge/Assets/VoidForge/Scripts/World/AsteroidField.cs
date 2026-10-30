using UnityEngine;

namespace VoidForge.World
{
    /// <summary>
    /// Astéroïdes décoratifs (sphères déformées par l'échelle) générés à partir d'une graine,
    /// pour donner des repères de distance et des obstacles au jalon J1.
    /// Les vrais astéroïdes (maillage, voxels) sont hors périmètre du MVP.
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
            if (transform.childCount > 0) return; // déjà généré dans la scène
            Generate();
        }

        public void Generate()
        {
            var rng = new System.Random(seed);
            var mat = MaterialLibrary.Rock;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos;
                int guard = 0;
                do
                {
                    pos = new Vector3(Range(rng, -1f, 1f), Range(rng, -1f, 1f), Range(rng, -1f, 1f)) * fieldRadius;
                    guard++;
                } while (pos.magnitude < clearRadius && guard < 20);

                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"Asteroid_{i:00}";
                go.transform.SetParent(transform, false);
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(Range(rng, 0f, 360f), Range(rng, 0f, 360f), Range(rng, 0f, 360f));
                float s = Range(rng, minSize, maxSize);
                go.transform.localScale = new Vector3(s * Range(rng, 0.7f, 1.3f), s * Range(rng, 0.6f, 1.2f), s * Range(rng, 0.7f, 1.3f));
                go.isStatic = true;

                var renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = mat;
            }
        }

        private static float Range(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
