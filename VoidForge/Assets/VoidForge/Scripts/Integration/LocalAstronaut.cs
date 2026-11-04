using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;

namespace VoidForge.Integration
{
    // No compile-time dependency on the concurrently produced avatar or licensed FinalIK.
    [DefaultExecutionOrder(-20)]
    public sealed class LocalAstronaut : MonoBehaviour
    {
        public Transform avatar, headTarget, leftTarget, rightTarget;
        public Vector3 restHead;
        public Vector3 headFromView;
        public Quaternion headRotation, leftRotation, rightRotation;
        // Quest controller grip is pitched relative to the anatomical palm. XR aim stays unchanged.
        public Vector3 controllerPalmEuler = new Vector3(-45,0,0);
        public Quaternion PalmCorrection => Quaternion.Euler(controllerPalmEuler);
        public Vector3 wristFromController = new Vector3(.025f, -.015f, -.07f);
        Transform head, left, right;
        Behaviour solver;
        Renderer[] renderers;
        float[] leftCurl, rightCurl;
        bool bound;
        public bool CanBind => avatar != null && headTarget != null && leftTarget != null && rightTarget != null &&
            Array.Exists(avatar.GetComponents<MonoBehaviour>(), c => c != null && c.GetType().FullName == "VoidForge.Avatars.AstronautFinalIK") &&
            Array.Exists(avatar.GetComponents<MonoBehaviour>(), c => c != null && c.GetType().FullName == "RootMotion.FinalIK.VRIK") &&
            Array.TrueForAll(avatar.GetComponentsInChildren<SkinnedMeshRenderer>(), r => r.sharedMesh != null);

        public void Bind(Transform hmd, Transform leftController, Transform rightController)
        {
            head = hmd; left = leftController; right = rightController;
            var bridge = Array.Find(avatar.GetComponents<MonoBehaviour>(), c => c != null && c.GetType().FullName == "VoidForge.Avatars.AstronautFinalIK");
            var fingers = Array.Find(avatar.GetComponents<MonoBehaviour>(), c => c != null && c.GetType().FullName == "VoidForge.Avatars.AstronautFingerPose");
            if (bridge == null || fingers == null) throw new InvalidOperationException("Astronaut producer prefab is incomplete.");
            var type = bridge.GetType();
            type.GetField("headTarget").SetValue(bridge, headTarget);
            type.GetField("leftHandTarget").SetValue(bridge, leftTarget);
            type.GetField("rightHandTarget").SetValue(bridge, rightTarget);
            UpdatePose(false);
            type.GetMethod("Configure").Invoke(bridge, null);
            solver = (Behaviour)type.GetProperty("SolverComponent").GetValue(bridge);
            fingers.GetType().GetField("readControllers").SetValue(fingers, false);
            leftCurl = (float[])fingers.GetType().GetField("leftCurl").GetValue(fingers);
            rightCurl = (float[])fingers.GetType().GetField("rightCurl").GetValue(fingers);
            renderers = Array.FindAll(avatar.GetComponentsInChildren<Renderer>(), r => r.GetComponentInParent<HandTool>() == null);
            bound = true;
            UpdatePose(false);
            foreach (var controller in new[] { left, right })
            {
                var visual = controller.GetComponent<VoidForge.Player.HandVisual>();
                if (visual != null) visual.enabled = false;
                foreach (var renderer in controller.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            }
        }

        void Update()
        {
            if (!bound) return;
            UpdatePose(EquipmentRig.Tracked(XRNode.Head) && EquipmentRig.Tracked(XRNode.LeftHand) && EquipmentRig.Tracked(XRNode.RightHand));
        }

        public void UpdatePose(bool visible)
        {
            if (head == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude > .01f) avatar.rotation = Quaternion.LookRotation(forward, Vector3.up);
            avatar.position = head.position - avatar.rotation * restHead;
            headTarget.SetPositionAndRotation(head.TransformPoint(headFromView), head.rotation * headRotation);
            var leftOffset = wristFromController; leftOffset.x = -leftOffset.x;
            leftTarget.SetPositionAndRotation(left.position+left.rotation*PalmCorrection*leftOffset, left.rotation * PalmCorrection * leftRotation);
            rightTarget.SetPositionAndRotation(right.position+right.rotation*PalmCorrection*wristFromController, right.rotation * PalmCorrection * rightRotation);
            if (solver != null) solver.enabled = visible;
            if (renderers != null) foreach (var renderer in renderers) renderer.enabled = visible;
        }

        public void SetGrip(bool holding, float trigger, float leftGrip)
        {
            if (rightCurl == null) return;
            for (int i = 0; i < 5; i++)
            {
                leftCurl[i] = Mathf.Clamp01(leftGrip);
                // Initial industrial-handle pose, exposed for per-tool ergonomic refinement.
                rightCurl[i] = holding ? (i == 1 ? Mathf.Lerp(.25f, .65f, trigger) : .65f) : 0;
            }
        }
    }
}
