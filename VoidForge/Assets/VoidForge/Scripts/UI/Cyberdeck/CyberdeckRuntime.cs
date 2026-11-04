using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;
using VoidForge.Economy;

namespace VoidForge.UI.Cyberdeck
{
    // Optional integration adapter. No serialized reference to the local/ignored avatar,
    // FinalIK or third-party assets is required by the independently shippable HUD.
    public sealed class CyberdeckRuntime : MonoBehaviour
    {
        MonoBehaviour surface, equipment;
        CyberdeckHud hud;
        Transform wrist;
        Vector3 lastPosition;
        float nextRefresh;
        int pendingTool = -1;
        bool gripArmed;
        float wristScale;
        Action<string> viewAction;

        public void ReleaseInterface()
        {
            StopAllCoroutines();
            if(hud!=null)
            {
                hud.State.EquipRequested-=Equip;
                if(viewAction!=null)hud.ActionInvoked-=viewAction;
            }
            enabled=false;
        }
        static MonoBehaviour Find(string name) => FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
            .FirstOrDefault(b => b != null && b.GetType().FullName == name);
        static object Field(object o, string name) => o == null ? null : o.GetType().GetField(name)?.GetValue(o);
        static object Property(object o, string name) => o == null ? null : o.GetType().GetProperty(name)?.GetValue(o);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (FindFirstObjectByType<CyberdeckRuntime>() == null)
                new GameObject("Cyberdeck HUD binding").AddComponent<CyberdeckRuntime>();
        }
        IEnumerator Start()
        {
            var wait = new WaitForSeconds(.5f);
            while (true)
            {
                if (hud == null)
                {
                    surface = Find("VoidForge.Integration.CyberdeckSurface");
                    if (surface != null) BindSurface();
                }
                if (hud != null && EconomyRuntime.Active != null && EconomyRuntime.Active.Ready)
                { EconomyRuntime.Active.Attach(hud.State); EconomyRuntime.Active.AddWaypoints(hud.State); }
                yield return wait;
            }
        }
        void BindSurface()
        {
            wrist = Field(surface, "hudAnchor") as Transform;
            var prefab = Resources.Load<GameObject>("VoidForge/Cyberdeck/Hud");
            if (wrist == null || prefab == null) return;
            hud = Instantiate(prefab).GetComponent<CyberdeckHud>();
            hud.name = "Cyberdeck • HUD 2D";
            hud.transform.SetParent(wrist, false);
            hud.transform.position = wrist.position + wrist.forward * .0008f;
            // Canvas front is -Z; the supplied surface anchor points +Z outwards.
            hud.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Vector2 safe = Field(surface, "safeMeters") is Vector2 size ? size : new Vector2(.2f, .08571429f);
            wristScale = Mathf.Min(safe.x / CyberdeckHud.Width, safe.y / CyberdeckHud.Height);
            SetWristScale(wristScale);
            hud.GetComponent<Canvas>().worldCamera = Camera.main;
            equipment = Find("VoidForge.Integration.EquipmentRig");
            hud.State.EquipRequested += Equip;
            hud.BlockAvailable = id => EconomyRuntime.Active != null && EconomyRuntime.Active.Ready ? EconomyRuntime.Active.BlockSupported(id) : CatalogueEntry(id) is object entry && (bool)Property(entry, "Available");
            hud.BlockSize = id => EconomyRuntime.Active != null && EconomyRuntime.Active.Ready ? EconomyRuntime.Active.BlockSize(id) : CatalogueEntry(id) is object entry && Field(entry, "size") is Vector3Int size ? size : Vector3Int.one;
            viewAction = id => { if (id == "expand" || id == "collapse") Resize(); };
            hud.ActionInvoked += viewAction;
            var pointer = hud.gameObject.AddComponent<CyberdeckPointer>();
            pointer.hud = hud;
            pointer.rightController = Property(equipment, "RightController") as Transform;
            AddWorldEntities();
            hud.State.Publish();
            Debug.Log("[VoidForge Cyberdeck] Five native 2D views attached to left wrist, inside the 200 mm safe area.");
        }
        object CatalogueEntry(string id)
        {
            var grid = Find("VoidForge.Integration.BlockGrid");
            var catalogue = Field(grid, "catalogue");
            return catalogue?.GetType().GetMethod("Find")?.Invoke(catalogue, new object[] { id });
        }
        void Equip(int selection)
        {
            if (EconomyRuntime.Active != null && !EconomyRuntime.Active.ToolOwned(selection)) { hud.State.Notify("OUTIL ABSENT DU SAC"); return; }
            if (equipment == null || !(Property(equipment, "Ready") is bool ready) || !ready) { hud.State.Notify("ÉQUIPEMENT NON CONNECTÉ"); return; }
            pendingTool = selection;
            hud.State.selectedTool = selection;
            hud.State.Notify("OUTIL PRÊT • MAINTENIR LE GRIP DROIT POUR PRENDRE");
            TryTake();
        }
        void TryTake()
        {
            if (pendingTool < 0 || equipment == null) return;
            if (EconomyRuntime.Active != null && !EconomyRuntime.Active.ToolOwned(pendingTool)) return;
            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!Application.isFocused || !device.isValid || !device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked ||
                !device.TryGetFeatureValue(CommonUsages.grip, out float grip) || grip < .7f) return;
            int selection = pendingTool;
            // Presentation order is Drill, Welder, Grinder; delivered enum order differs.
            string[] kinds = { "MiningDrill", "Welder", "Grinder" };
            if (!(Field(equipment, "tools") is Array tools)) return;
            foreach (var tool in tools)
            {
                if (Field(tool, "kind")?.ToString() != kinds[selection]) continue;
                equipment.GetType().GetMethod("Take").Invoke(equipment, new[] { tool });
                hud.State.equippedTool = selection; hud.State.Publish(); return;
            }
            hud.State.Notify("OUTIL INDISPONIBLE");
        }
        void AddWorldEntities()
        {
            // Actual decorative asteroid transforms; no invented mineral contents.
            foreach (var t in FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.StartsWith("Asteroid_", StringComparison.Ordinal)).Take(64))
                hud.State.waypoints.Add(new DeckWaypoint { id = t.name, name = t.name.Replace("Asteroid_", "ASTÉROÏDE "),
                    category = "REPÈRES", icon = "map-asteroid", target = t, position = t.position });
            if (Camera.main != null) hud.State.playerPosition = Camera.main.transform.position;
            if (hud.State.waypoints.Count > 0)
            {
                float radius = hud.State.waypoints.Max(p => Vector3.Distance(hud.State.playerPosition, p.Position));
                hud.State.localMapRange = Mathf.Max(30, radius * 1.2f);
                hud.Zoom(hud.State.localMapRange / hud.MapRange);
            }
        }
        void Resize()
        {
            if (hud.Expanded && Camera.main != null)
            {
                hud.transform.SetParent(Camera.main.transform, false);
                hud.transform.localPosition = new Vector3(0, -.08f, .62f);
                hud.transform.localRotation = Quaternion.identity;
                hud.transform.localScale = Vector3.one * (.62f / CyberdeckHud.Width);
            }
            else
            {
                hud.transform.SetParent(wrist, false);
                hud.transform.position = wrist.position + wrist.forward * .0008f;
                hud.transform.localRotation = Quaternion.Euler(0, 180, 0);
                SetWristScale(wristScale);
            }
        }
        void SetWristScale(float metresPerPixel)
        {
            Vector3 parent = wrist.lossyScale;
            hud.transform.localScale = new Vector3(metresPerPixel / Mathf.Max(.00001f, Mathf.Abs(parent.x)),
                metresPerPixel / Mathf.Max(.00001f, Mathf.Abs(parent.y)), metresPerPixel / Mathf.Max(.00001f, Mathf.Abs(parent.z)));
        }
        void LateUpdate()
        {
            if (hud == null || equipment == null || pendingTool < 0) return;
            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!device.isValid || !device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked || !Application.isFocused)
            { gripArmed = false; return; }
            device.TryGetFeatureValue(CommonUsages.grip, out float grip);
            if (grip < .25f) gripArmed = true;
            if (grip > .7f && gripArmed) { gripArmed = false; TryTake(); }
        }
        void Update()
        {
            if (hud != null)
            {
                var device = InputDevices.GetDeviceAtXRNode(hud.Expanded ? XRNode.Head : XRNode.LeftHand);
                hud.GetComponent<Canvas>().enabled = Application.isEditor || (device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked);
            }
            if (hud == null || Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + .5f;
            var held = Property(equipment, "Held"); string kind = Field(held, "kind")?.ToString();
            int tool = kind == "MiningDrill" ? 0 : kind == "Welder" ? 1 : kind == "Grinder" ? 2 : -1;
            bool changed = hud.State.equippedTool != tool;
            hud.State.equippedTool = tool;
            if (Camera.main != null)
            {
                hud.State.playerPosition = Camera.main.transform.position;
                if (hud.page == DeckPage.Map && Vector3.Distance(lastPosition, hud.State.playerPosition) > 1)
                { lastPosition = hud.State.playerPosition; changed = true; }
            }
            if (changed) hud.State.Publish();
        }
        void OnDestroy()
        { if (hud != null) Destroy(hud.gameObject); }
    }
}
