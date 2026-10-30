using UnityEngine;

namespace VoidForge.World
{
    /// <summary>
    /// Champ d'étoiles procédural : un maillage de petits triangles répartis sur une sphère
    /// lointaine, qui suit la position de la caméra (jamais sa rotation). Donne un repère
    /// d'orientation en apesanteur sans texture ni skybox.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class Starfield : MonoBehaviour
    {
        [SerializeField] private int starCount = 2500;
        [SerializeField] private float radius = 1500f;
        [Tooltip("Taille apparente d'une étoile en mètres, à la distance « radius ».")]
        [SerializeField] private float starSize = 4f;
        [SerializeField] private int seed = 1337;

        private Transform _follow;

        private void Awake()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter.sharedMesh == null) filter.sharedMesh = BuildMesh();

            var renderer = GetComponent<MeshRenderer>();
            if (renderer.sharedMaterial == null) renderer.sharedMaterial = MaterialLibrary.Star;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void LateUpdate()
        {
            if (_follow == null)
            {
                var cam = Camera.main;
                if (cam != null) _follow = cam.transform;
            }
            if (_follow != null) transform.position = _follow.position;
        }

        private Mesh BuildMesh()
        {
            var rng = new System.Random(seed);
            var vertices = new Vector3[starCount * 3];
            var triangles = new int[starCount * 3];

            for (int i = 0; i < starCount; i++)
            {
                Vector3 dir = RandomDirection(rng);
                Vector3 center = dir * radius;

                // Deux vecteurs tangents pour dessiner un triangle face au centre.
                Vector3 tangent = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                Vector3 bitangent = Vector3.Cross(dir, tangent);
                float size = starSize * (0.4f + 0.6f * (float)rng.NextDouble());

                int v = i * 3;
                vertices[v]     = center + tangent * size;
                vertices[v + 1] = center + (-tangent * 0.5f + bitangent * 0.866f) * size;
                vertices[v + 2] = center + (-tangent * 0.5f - bitangent * 0.866f) * size;
                triangles[v] = v; triangles[v + 1] = v + 1; triangles[v + 2] = v + 2;
            }

            var mesh = new Mesh { name = "Starfield" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radius * 2.2f);
            return mesh;
        }

        private static Vector3 RandomDirection(System.Random rng)
        {
            // Distribution uniforme sur la sphère.
            double z = 2.0 * rng.NextDouble() - 1.0;
            double t = 2.0 * System.Math.PI * rng.NextDouble();
            double r = System.Math.Sqrt(1.0 - z * z);
            return new Vector3((float)(r * System.Math.Cos(t)), (float)(r * System.Math.Sin(t)), (float)z);
        }
    }
}
