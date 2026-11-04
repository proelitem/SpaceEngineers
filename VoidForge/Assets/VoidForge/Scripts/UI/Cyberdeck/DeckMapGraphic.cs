using UnityEngine;
using UnityEngine.UI;

namespace VoidForge.UI.Cyberdeck
{
    // Vector3 entity projection and orbit/recenter follow the supplied ELANCE-MAP
    // architecture. This renderer is original uGUI code, with no legacy mouse camera.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DeckMapGraphic : MaskableGraphic
    {
        public CyberdeckHud hud;
        public DeckMapGraphic() { useLegacyMeshGeneration = false; }
        readonly Color grid = new Color(.06f, .40f, .48f, .29f);
        readonly Color ring = new Color(.14f, .68f, .77f, .75f);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (hud == null || hud.State == null) return;
            Vector3 center = hud.State.playerPosition;
            float radius = hud.MapRange;
            for (int i = -8; i <= 8; i++)
            {
                float step = radius / 6;
                Segment(vh, hud.ProjectMap(center + new Vector3(i * step, 0, -radius * 1.5f)),
                    hud.ProjectMap(center + new Vector3(i * step, 0, radius * 1.5f)), 1, grid);
                Segment(vh, hud.ProjectMap(center + new Vector3(-radius * 1.5f, 0, i * step)),
                    hud.ProjectMap(center + new Vector3(radius * 1.5f, 0, i * step)), 1, grid);
            }
            for (int k = 1; k <= 2; k++)
            for (int i = 0; i < 96; i++)
            {
                float a = i * Mathf.PI * 2 / 96, b = (i + 1) * Mathf.PI * 2 / 96, r = radius * (k == 1 ? .45f : 1);
                Segment(vh, hud.ProjectMap(center + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r)),
                    hud.ProjectMap(center + new Vector3(Mathf.Cos(b) * r, 0, Mathf.Sin(b) * r)), k == 1 ? 2 : 3, ring);
            }
            var chosen = hud.State.waypoints.Find(w => w.id == hud.SelectedWaypoint);
            if (chosen != null)
            {
                Vector2 a = hud.ProjectMap(center), b = hud.ProjectMap(chosen.Position);
                float length = Vector2.Distance(a, b);
                for (float d = 0; d < length; d += 27)
                    Segment(vh, Vector2.Lerp(a, b, d / length), Vector2.Lerp(a, b, Mathf.Min(1, (d + 16) / length)), 3.5f, new Color(1, .69f, .15f));
            }
        }
        void Segment(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            Rect r = rectTransform.rect;
            // Liang–Barsky clipping keeps the spatial grid inside its physical panel.
            Vector2 delta = b - a; float t0 = 0, t1 = 1;
            if (!Clip(-delta.x, a.x - 3, ref t0, ref t1) || !Clip(delta.x, 1216 - a.x, ref t0, ref t1) ||
                !Clip(-delta.y, a.y - 3, ref t0, ref t1) || !Clip(delta.y, 544 - a.y, ref t0, ref t1)) return;
            b = a + delta * t1; a += delta * t0;
            a = new Vector2(r.xMin + a.x, r.yMax - a.y); b = new Vector2(r.xMin + b.x, r.yMax - b.y);
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            int n = vh.currentVertCount;
            vh.AddVert(a - normal, tint, Vector2.zero); vh.AddVert(a + normal, tint, Vector2.zero);
            vh.AddVert(b + normal, tint, Vector2.zero); vh.AddVert(b - normal, tint, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
        static bool Clip(float p, float q, ref float a, ref float b)
        {
            if (Mathf.Abs(p) < .00001f) return q >= 0;
            float t = q / p;
            if (p < 0) { if (t > b) return false; if (t > a) a = t; }
            else { if (t < a) return false; if (t < b) b = t; }
            return true;
        }
    }
}
