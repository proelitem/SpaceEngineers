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
        public bool Ready { get; private set; }
        public Transform RightController { get; private set; }
        PlayerInputProvider input;
        Transform head, left, socket;
        bool gripLatched;
        bool waitingForRelease;
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
            foreach (var tool in tools) tool.RememberHome();
            astronaut.Bind(head, left, RightController);
            Ready = true;
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
            if (grip < .25f) { gripLatched = false; waitingForRelease = false; Release(); }
            if (grip > .7f && !gripLatched && !waitingForRelease)
            {
                gripLatched = true;
                HandTool nearest = null;
                float distance = .18f;
                foreach (var tool in tools)
                {
                    float d = Vector3.Distance(tool.primaryGrip.position, socket.position);
                    if (d < distance) { distance = d; nearest = tool; }
                }
                if (nearest != null) Take(nearest);
            }
            if (Held != null) Held.Operate(trigger);
            astronaut.SetGrip(Held != null, trigger, leftGrip);
            if (instructions != null && (!messageSet || lastInstruction != Held))
            {
                messageSet = true; lastInstruction = Held;
                string action = Held == null ? "" : Held.kind == HandToolKind.MiningDrill
                    ? "FOREUSE — gâchette : creuser (pointe à moins de 1 m)"
                    : Held.kind == HandToolKind.Grinder ? "MEULEUSE — gâchette : rotation / démontage à venir"
                    : "SOUDEUSE — prise prête / soudage à venir";
                instructions.text = Held == null
                    ? "OUTILS — approcher la main droite\nMaintenir grip : prendre / relâcher : ranger"
                    : action + "\nGrip gauche : freiner / relâcher grip droit : ranger";
            }
        }

        public void Take(HandTool tool)
        {
            Release();
            Held = tool;
            input.RightGripConsumed = true;
            Held.Hold(socket);
        }
        public void Release()
        {
            if (Held != null) Held.ReturnHome();
            Held = null;
            if (input != null) input.RightGripConsumed = false;
            if (astronaut != null) astronaut.SetGrip(false, 0, 0);
        }
        void OnDisable() => Release();
        void OnApplicationFocus(bool focused) { if (!focused) { Release(); waitingForRelease = true; } }
    }
}
