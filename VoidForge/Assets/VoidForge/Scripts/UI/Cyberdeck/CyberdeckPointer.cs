using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;
using CommonUsages = UnityEngine.XR.CommonUsages;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VoidForge.UI.Cyberdeck
{
    // Dedicated short-range wrist pointer. It cannot activate through a collider,
    // from behind the screen, while untracked, or on reacquiring an already held trigger.
    public sealed class CyberdeckPointer : MonoBehaviour
    {
        public CyberdeckHud hud;
        public Transform rightController;
        Button hover, pressed;
        bool latched, releaseRequired = true;
        int keyboardIndex = -1;
        Vector2 dragOrigin;
        bool dragging;
        bool wasTracked;
        bool expandLatched;
        bool keyboardMode;
        Vector2 lastMouse;
        EventSystem ownedEvents;
        LineRenderer laser;
        RectTransform reticle;
        public bool IsPointing { get; private set; }
        public bool IsInteracting => latched;
        void Start()
        {
            if (EventSystem.current == null) ownedEvents = new GameObject("Cyberdeck EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            var line = new GameObject("Cyberdeck pointer ray"); line.transform.SetParent(transform, false);
            laser = line.AddComponent<LineRenderer>(); laser.sharedMaterial = hud.assets.pointerMaterial;
            laser.positionCount = 2; laser.startWidth = .0008f; laser.endWidth = .0003f;
            laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; laser.receiveShadows = false;
            laser.enabled = false;
            var target = new GameObject("Cyberdeck reticle", typeof(RectTransform), typeof(RawImage));
            reticle = target.GetComponent<RectTransform>(); reticle.SetParent(transform, false); reticle.sizeDelta = new Vector2(28, 28);
            var graphic = target.GetComponent<RawImage>(); graphic.texture = hud.assets.Find("ui-recenter"); graphic.uvRect = hud.assets.Bounds("ui-recenter");
            graphic.material = hud.assets.glyphMaterial; graphic.color = new Color(.35f, .9f, 1); graphic.raycastTarget = false;
            target.SetActive(false);
        }
        void Update()
        {
            if (hud == null || !Application.isFocused || !hud.GetComponent<Canvas>().enabled) { ResetPointer(); return; }
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            bool expandDown = left.isValid && left.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary) && secondary;
            if (expandDown && !expandLatched) hud.SetExpanded(!hud.Expanded);
            expandLatched = expandDown;
            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool tracked = device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool value) && value;
            if (wasTracked && !tracked) ResetPointer();
            wasTracked = tracked;
            if (tracked && rightController != null)
            {
                device.TryGetFeatureValue(CommonUsages.trigger, out float trigger);
                Point(new Ray(rightController.position, rightController.forward), trigger > .65f, trigger < .25f);
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null && Camera.main != null)
                {
                    var mouse = Mouse.current;
                    Vector2 position = mouse.position.ReadValue();
                    if (Vector2.Distance(position, lastMouse) > .1f || mouse.leftButton.isPressed) keyboardMode = false;
                    lastMouse = position;
                    if (!keyboardMode) Point(Camera.main.ScreenPointToRay(position), mouse.leftButton.isPressed, !mouse.leftButton.isPressed);
                    if (hud.page == DeckPage.Map && Mathf.Abs(mouse.scroll.y.ReadValue()) > .01f) hud.Zoom(mouse.scroll.y.ReadValue() > 0 ? .8f : 1.25f);
                    if (hud.page == DeckPage.Inventory && Mathf.Abs(mouse.scroll.y.ReadValue()) > .01f) hud.ScrollInventory(mouse.scroll.y.ReadValue() > 0 ? -1 : 1);
                    if (hud.page == DeckPage.Assembly && Mathf.Abs(mouse.scroll.y.ReadValue()) > .01f) hud.ScrollRecipes(mouse.scroll.y.ReadValue() > 0 ? -1 : 1);
                }
                else ResetPointer();
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.tabKey.wasPressedThisFrame)
                    {
                        keyboardMode = true;
                        var buttons = hud.Controls.Values.Where(b => b.interactable).ToArray();
                        if (buttons.Length > 0) { keyboardIndex = (keyboardIndex + 1) % buttons.Length; SetHover(buttons[keyboardIndex]); }
                    }
                    if (keyboard.enterKey.wasPressedThisFrame && hover != null) hover.onClick.Invoke();
                    if (keyboard.escapeKey.wasPressedThisFrame) { hud.SetExpanded(false); hud.Rebuild(); }
                    if (keyboard.mKey.wasPressedThisFrame) { hud.SetExpanded(!hud.Expanded); hud.Rebuild(); }
                    if (hud.page == DeckPage.Construction)
                    { if (keyboard.leftArrowKey.wasPressedThisFrame) hud.MoveBlock(-1); if (keyboard.rightArrowKey.wasPressedThisFrame) hud.MoveBlock(1); }
                }
#else
                ResetPointer();
#endif
            }
        }
        public bool Point(Ray ray, bool down, bool released)
        {
            if (released) { latched = releaseRequired = false; }
            var plane = new Plane(transform.forward, transform.position);
            bool hit = Vector3.Dot(ray.direction, transform.forward) > 0 && plane.Raycast(ray, out float distance) && distance <= 2;
            Vector3 world = hit ? ray.GetPoint(plane.GetDistanceToPoint(ray.origin) / -Vector3.Dot(plane.normal, ray.direction)) : Vector3.zero;
            if (hit && !((RectTransform)transform).rect.Contains(transform.InverseTransformPoint(world))) hit = false;
            if (hit && Physics.Raycast(ray, out RaycastHit obstruction, Vector3.Distance(ray.origin, world) - .002f, ~0, QueryTriggerInteraction.Ignore)) hit = false;
            IsPointing = hit;
            Button button = null;
            Vector2 local = hit ? transform.InverseTransformPoint(world) : Vector3.zero;
            if (laser != null)
            {
                laser.enabled = hit && wasTracked;
                if (laser.enabled) { laser.SetPosition(0, ray.origin); laser.SetPosition(1, world); }
            }
            if (reticle != null)
            {
                reticle.gameObject.SetActive(hit); reticle.SetAsLastSibling();
                if (hit) reticle.localPosition = new Vector3(local.x, local.y, -.2f);
            }
            if (hit)
            {
                // Last sibling wins for dropdowns and overlapping interactive controls.
                foreach (var candidate in hud.Controls.Values)
                {
                    if (candidate == null || !candidate.interactable || !candidate.gameObject.activeInHierarchy) continue;
                    var rect = (RectTransform)candidate.transform;
                    if (rect.rect.Contains(rect.InverseTransformPoint(world))) button = candidate;
                }
            }
            SetHover(button);
            if (down && !latched && !releaseRequired)
            { latched = true; pressed = button; dragOrigin = local; dragging = false; }
            Vector2 onSheet = new Vector2(local.x + CyberdeckHud.Width * .5f, CyberdeckHud.Height * .5f - local.y);
            if (down && latched && hit && hud.page == DeckPage.Inventory && new Rect(646, 287, 1240, 276).Contains(onSheet) && Mathf.Abs(local.y - dragOrigin.y) > 60)
            { pressed = null; dragging = true; hud.ScrollInventory(local.y > dragOrigin.y ? 1 : -1); dragOrigin = local; }
            if (down && latched && hit && hud.page == DeckPage.Construction && new Rect(123, 175, 1670, 332).Contains(onSheet) && Mathf.Abs(local.x - dragOrigin.x) > 90)
            { pressed = null; dragging = true; hud.MoveBlock(local.x > dragOrigin.x ? -1 : 1); dragOrigin = local; }
            if (down && latched && hit && Vector2.Distance(local, dragOrigin) > 18 && pressed == null && hud.page == DeckPage.Map && new Rect(23, 97, 1225, 553).Contains(onSheet))
            { Vector2 delta = local - dragOrigin; dragOrigin = local; dragging = true; hud.Orbit(new Vector2(delta.x * .18f, delta.y * .12f)); }
            if (released && pressed != null)
            {
                var target = pressed; pressed = null;
                if (target == button && !dragging) target.onClick.Invoke();
            }
            return hit;
        }
        void SetHover(Button button)
        {
            if (button == hover) return;
            var data = new PointerEventData(EventSystem.current);
            if (hover != null) ExecuteEvents.Execute(hover.gameObject, data, ExecuteEvents.pointerExitHandler);
            hover = button;
            if (hover != null) ExecuteEvents.Execute(hover.gameObject, data, ExecuteEvents.pointerEnterHandler);
        }
        void ResetPointer()
        {
            SetHover(null); pressed = null; latched = false; releaseRequired = true; IsPointing = false;
            expandLatched = true;
            if (laser != null) laser.enabled = false;
            if (reticle != null) reticle.gameObject.SetActive(false);
        }
        void OnDisable() => ResetPointer();
        void OnApplicationFocus(bool focused) { if (!focused) ResetPointer(); }
        void OnDestroy() { if (ownedEvents != null) Destroy(ownedEvents.gameObject); }
    }
}
