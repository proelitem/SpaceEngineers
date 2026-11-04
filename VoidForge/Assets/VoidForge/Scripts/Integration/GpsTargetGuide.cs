using UnityEngine;
using UnityEngine.Rendering;

namespace VoidForge.Integration
{
    // World-depth brackets preserve stereo depth. Only the peripheral arrow follows the HMD.
    public sealed class GpsTargetGuide : MonoBehaviour
    {
        public Camera viewer;
        public Transform target;
        public Vector3 targetPosition;
        public bool hasTarget;
        readonly LineRenderer[] corners = new LineRenderer[4];
        LineRenderer arrow;
        Material material;

        void Awake()
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(.15f, .9f, 1));
            material.SetFloat("_ZTest", (float)CompareFunction.Always);
            for (int i = 0; i < corners.Length; i++) corners[i] = MakeLine("GPS target corner " + i);
            arrow = MakeLine("GPS offscreen chevron");
        }

        LineRenderer MakeLine(string label)
        {
            var line = new GameObject(label).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = material;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.useWorldSpace = true;
            line.positionCount = 3;
            line.enabled = false;
            return line;
        }

        public static Vector2 EdgeDirection(Vector3 cameraLocalPosition)
        {
            var direction = new Vector2(cameraLocalPosition.x, cameraLocalPosition.y);
            // A point directly behind has no unique left/right bearing; keep a stable cue.
            return direction.sqrMagnitude < .000001f ? Vector2.down : direction.normalized;
        }

        void LateUpdate()
        {
            if (viewer == null) viewer = Camera.main;
            if (viewer == null || !hasTarget || !Application.isFocused) { SetVisible(false, false); return; }
            Vector3 point = target != null ? target.position : targetPosition;
            Vector3 view = viewer.WorldToViewportPoint(point);
            bool onscreen = view.z > viewer.nearClipPlane && view.x > .04f && view.x < .96f && view.y > .06f && view.y < .94f;
            SetVisible(onscreen, !onscreen);
            if (!onscreen)
            {
                Vector2 direction = EdgeDirection(viewer.transform.InverseTransformPoint(point));
                float halfY = Mathf.Tan(viewer.fieldOfView * Mathf.Deg2Rad * .5f);
                float halfX = halfY * viewer.aspect;
                Vector2 extent = new Vector2(halfX * .78f, halfY * .72f);
                float reach = 1 / Mathf.Max(Mathf.Abs(direction.x) / extent.x, Mathf.Abs(direction.y) / extent.y);
                Vector2 p = direction * reach;
                Vector2 cross = new Vector2(-direction.y, direction.x) * .012f;
                Vector2 back = p - direction * .022f;
                arrow.startWidth = arrow.endWidth = .003f;
                arrow.SetPositions(new[] { At(back + cross), At(p), At(back - cross) });
                return;
            }
            Bounds bounds = new Bounds(point, Vector3.one);
            if (target != null)
            {
                bool first = true;
                foreach (var renderer in target.GetComponentsInChildren<Renderer>())
                {
                    if (!renderer.enabled || renderer is LineRenderer || renderer is TrailRenderer) continue;
                    if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds);
                }
                var volume=target.GetComponent<GpsTargetBounds>();
                if(volume!=null)bounds=volume.WorldBounds;
            }
            Vector3 right = viewer.transform.right, up = viewer.transform.up;
            var e = bounds.extents;
            float width = Vector3.Dot(new Vector3(Mathf.Abs(right.x), Mathf.Abs(right.y), Mathf.Abs(right.z)), e) + .08f;
            float height = Vector3.Dot(new Vector3(Mathf.Abs(up.x), Mathf.Abs(up.y), Mathf.Abs(up.z)), e) + .08f;
            float distance = Vector3.Distance(viewer.transform.position, bounds.center);
            float arm = Mathf.Min(width, height) * .24f;
            for (int i = 0; i < 4; i++)
            {
                float x = (i & 1) == 0 ? -1 : 1, y = (i & 2) == 0 ? -1 : 1;
                Vector3 p = bounds.center + right * (x * width) + up * (y * height);
                corners[i].startWidth = corners[i].endWidth = Mathf.Max(.003f, distance * .0008f);
                corners[i].SetPositions(new[] { p - right * (x * arm), p, p - up * (y * arm) });
            }
        }
        Vector3 At(Vector2 p) => viewer.transform.TransformPoint(new Vector3(p.x, p.y, 1));
        void SetVisible(bool frame, bool chevron)
        {
            foreach (var corner in corners) if (corner != null) corner.enabled = frame;
            if (arrow != null) arrow.enabled = chevron;
        }
        void OnDisable() => SetVisible(false, false);
        void OnDestroy() { if (material != null) Destroy(material); }
    }
}
