using UnityEngine;
using UnityEngine.UI;

namespace VoidForge.UI.Cyberdeck
{
    // Native UI mesh: clipped corners, gradient fill and a constant-width border.
    // No mockup screenshot is used by the runtime HUD.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DeckPanel : MaskableGraphic
    {
        public Color bottom = new Color(.005f, .055f, .07f, 1);
        public Color border = new Color(.29f, .77f, .83f, 1);
        public float bevel = 14, thickness = 2.5f;
        public float innerStroke;
        public DeckPanel() { useLegacyMeshGeneration = false; }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            float b = Mathf.Min(bevel, Mathf.Min(r.width, r.height) * .25f);
            var outer = Corners(r, b);
            Rect inside = new Rect(r.xMin + thickness, r.yMin + thickness,
                Mathf.Max(0, r.width - thickness * 2), Mathf.Max(0, r.height - thickness * 2));
            var inner = Corners(inside, Mathf.Max(0, b - thickness * .4f));
            vh.AddVert(r.center, Color.Lerp(bottom, color, .5f), Vector2.zero);
            for (int i = 0; i < 8; i++)
            {
                Color fill = Color.Lerp(bottom, color, Mathf.InverseLerp(r.yMin, r.yMax, inner[i].y));
                vh.AddVert(inner[i], fill, Vector2.zero);
            }
            for (int i = 0; i < 8; i++) vh.AddTriangle(0, (i + 1) % 8 + 1, i + 1);
            for (int i = 0; i < 8; i++)
            {
                int n = vh.currentVertCount, next = (i + 1) % 8;
                vh.AddVert(outer[i], border, Vector2.zero); vh.AddVert(outer[next], border, Vector2.zero);
                vh.AddVert(inner[next], border, Vector2.zero); vh.AddVert(inner[i], border, Vector2.zero);
                vh.AddTriangle(n, n + 2, n + 1); vh.AddTriangle(n, n + 3, n + 2);
            }
            if (innerStroke > 0)
            {
                float inset = thickness + 2;
                var a = Corners(new Rect(r.x + inset, r.y + inset, r.width - inset * 2, r.height - inset * 2), Mathf.Max(0, b - inset * .5f));
                inset += innerStroke;
                var c = Corners(new Rect(r.x + inset, r.y + inset, r.width - inset * 2, r.height - inset * 2), Mathf.Max(0, b - inset * .5f));
                Color ink = new Color(.015f, .05f, .06f);
                for (int i = 0; i < 8; i++)
                {
                    int n = vh.currentVertCount, next = (i + 1) % 8;
                    vh.AddVert(a[i], ink, Vector2.zero); vh.AddVert(a[next], ink, Vector2.zero);
                    vh.AddVert(c[next], ink, Vector2.zero); vh.AddVert(c[i], ink, Vector2.zero);
                    vh.AddTriangle(n, n + 2, n + 1); vh.AddTriangle(n, n + 3, n + 2);
                }
            }
        }
        static Vector2[] Corners(Rect r, float b) => new[] {
            new Vector2(r.xMin + b, r.yMin), new Vector2(r.xMax - b, r.yMin),
            new Vector2(r.xMax, r.yMin + b), new Vector2(r.xMax, r.yMax - b),
            new Vector2(r.xMax - b, r.yMax), new Vector2(r.xMin + b, r.yMax),
            new Vector2(r.xMin, r.yMax - b), new Vector2(r.xMin, r.yMin + b) };
    }
}
