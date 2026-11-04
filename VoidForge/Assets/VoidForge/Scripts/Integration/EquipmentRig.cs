using UnityEngine;
using UnityEngine.XR;
using VoidForge.Player;

namespace VoidForge.Integration
{
    // Equipment stays local. The existing network avatar still carries only head/hands.
    [DefaultExecutionOrder(-30)]
    public sealed class EquipmentRig : MonoBehaviour
    {
        public HandTool[] tools;
        public LocalAstronaut astronaut;
        public TextMesh instructions;
        public HandTool Held { get; private set; }
        public HandTool Selected { get; private set; }
        public bool Ready { get; private set; }
        public Transform RightController { get; private set; }
        PlayerInputProvider input;
        Transform head, left, socket;
        bool gripLatched;
        bool waitingForRelease;
        bool awaitingGrip;
        HandTool lastInstruction;
        bool messageSet;
        public bool HasTrackedRightHand => Tracked(XRNode.RightHand);
        public static bool Tracked(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            return device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked;
        }

        public void Bind(PlayerInputProvider provider)
        {
            input = provider;
            head = provider.GetComponentInChildren<Camera>().transform;
            var tracking = head.parent;
            left = tracking.Find("LeftHand");
            RightController = tracking.Find("RightHand");
            socket = new GameObject("RightToolGrip").transform;
            socket.SetParent(RightController, false);
            socket.localRotation=astronaut.PalmCorrection;
            foreach (var tool in tools) tool.RememberHome();
            astronaut.Bind(head, left, RightController);
            Ready = true;
            SelectTool(tools[0]);
            Debug.Log("[VoidForge Integration] XR equipment bound: astronaut, left wrist, 3 hand tools.");
        }

        void Update()
        {
            if (!Ready || input.Actions == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude > .01f)
                transform.SetPositionAndRotation(head.position, Quaternion.LookRotation(forward, Vector3.up));
            ProcessInput(Tracked(XRNode.Head) && HasTrackedRightHand, Application.isFocused,
                input.Actions.RightGrip.ReadValue<float>(), input.Actions.RightTrigger.ReadValue<float>(),
                input.Actions.LeftGrip.ReadValue<float>());
        }

        public void ProcessInput(bool tracked, bool focused, float grip, float trigger, float leftGrip)
        {
            if (!Ready) return;
            if (!tracked || !focused)
            {
                Release();
                waitingForRelease = true;
                return;
            }
            if (grip < .25f)
            {
                gripLatched = false; waitingForRelease = false;
                if (!awaitingGrip) Release();
            }
            if (grip > .7f && !gripLatched && !waitingForRelease)
            {
                gripLatched = true;
                HandTool nearest = null;
                float distance = .18f;
                foreach (var tool in tools)
                {
                    if (tool != Selected || !tool.gameObject.activeInHierarchy) continue;
                    float d = Vector3.Distance(tool.primaryGrip.position, socket.position);
                    if (d < distance) { distance = d; nearest = tool; }
                }
                if (nearest != null) Grab(nearest);
            }
            if (grip > .7f && Held != null) awaitingGrip = false;
            if (Held != null) Held.Operate(awaitingGrip ? 0 : trigger);
            astronaut.SetGrip(Held != null, trigger, leftGrip);
            if (instructions != null && (!messageSet || lastInstruction != Held))
            {
                messageSet = true; lastInstruction = Held;
                string action = Held == null ? "" : Held.kind == HandToolKind.MiningDrill
                    ? "FOREUSE — gâchette : creuser (pointe à moins de 1 m)"
                    : Held.kind == HandToolKind.Grinder ? "MEULEUSE — gâchette : rotation / démontage continu"
                    : "SOUDEUSE — gâchette : soudage continu";
                instructions.text = Held == null
                    ? "CEINTURE — approcher la main droite\nMaintenir grip : prendre / relâcher : ranger"
                    : action + "\nGrip gauche : freiner / relâcher grip droit : ranger";
            }
        }

        // Compatibility with the Cyberdeck adapter: selection stocks the thigh holster.
        // Only a proximity grab can move that tool into the tracked hand.
        public void Take(HandTool tool) => SelectTool(tool);

        public bool SelectTool(HandTool tool)
        {
            if (!Ready || tool == null || !System.Array.Exists(tools, t => t == tool)) return false;
            if (Selected == tool) return true;
            Release();
            Selected = tool;
            foreach (var candidate in tools) candidate.gameObject.SetActive(candidate == Selected);
            return true;
        }

        public void Grab(HandTool tool)
        {
            if (!Ready || tool == null || tool != Selected) return;
            Release();
            Held = tool;
            awaitingGrip = true;
            input.RightGripConsumed = true;
            Held.Hold(socket);
        }
        public void Release()
        {
            if (Held != null) Held.ReturnHome();
            Held = null;
            awaitingGrip = false;
            if (input != null) input.RightGripConsumed = false;
            if (astronaut != null) astronaut.SetGrip(false, 0, 0);
        }
        void OnDisable() => Release();
        void OnApplicationFocus(bool focused) { if (!focused) { Release(); waitingForRelease = true; } }
    }
}
