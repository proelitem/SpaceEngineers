using UnityEngine;
using VoidForge.World;

namespace VoidForge.Player
{
    /// <summary>
    /// Représentation minimale d'une manette : une sphère et un petit cylindre indiquant l'avant.
    /// Remplacée par un vrai modèle de main aux jalons suivants.
    /// </summary>
    public sealed class HandVisual : MonoBehaviour
    {
        [SerializeField] private float radius = 0.03f;

        private void Start()
        {
            if (transform.childCount > 0) return; // déjà construit (scène sauvegardée)
            Build();
        }

        public void Build()
        {
            var mat = MaterialLibrary.Hand;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Palm";
            SafeDestroy(sphere.GetComponent<Collider>());
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * (radius * 2f);
            sphere.GetComponent<Renderer>().sharedMaterial = mat;

            var pointer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pointer.name = "Pointer";
            SafeDestroy(pointer.GetComponent<Collider>());
            pointer.transform.SetParent(transform, false);
            pointer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pointer.transform.localPosition = new Vector3(0f, 0f, radius + 0.04f);
            pointer.transform.localScale = new Vector3(0.01f, 0.04f, 0.01f);
            pointer.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void SafeDestroy(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }
    }
}
