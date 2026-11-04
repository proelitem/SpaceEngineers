using System;
using UnityEngine;

namespace VoidForge.UI.Cyberdeck
{
    [CreateAssetMenu(menuName = "VoidForge/Cyberdeck graphics")]
    public sealed class CyberdeckAssets : ScriptableObject
    {
        [Serializable] public struct Icon { public string id; public Texture2D texture; public Rect uv; }
        public Icon[] icons;
        public Font font;
        public FontStyle textStyle = FontStyle.Bold;
        public Material glyphMaterial;
        public Material pointerMaterial;
        public Texture2D Find(string id)
        {
            if (icons != null) foreach (var icon in icons) if (icon.id == id) return icon.texture;
            return null;
        }
        public Rect Bounds(string id)
        {
            if (icons != null) foreach (var icon in icons) if (icon.id == id && icon.uv.width > 0) return icon.uv;
            return new Rect(0, 0, 1, 1);
        }
    }
}
