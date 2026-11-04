using UnityEngine;

namespace VoidForge.Integration
{
    public sealed class CyberdeckSurface : MonoBehaviour
    {
        public Transform hudAnchor;
        public Renderer screen;
        public Vector2 activeMeters = new Vector2(.216f, .09257143f);
        public Vector2 safeMeters = new Vector2(.200f, .08571429f);

        // UI producer may bind a RenderTexture without changing the wearable or avatar.
        public void SetDisplay(Texture texture)
        {
            var properties = new MaterialPropertyBlock();
            screen.GetPropertyBlock(properties);
            properties.SetTexture("_BaseMap", texture);
            properties.SetColor("_BaseColor", Color.white);
            screen.SetPropertyBlock(properties);
        }
    }
}
