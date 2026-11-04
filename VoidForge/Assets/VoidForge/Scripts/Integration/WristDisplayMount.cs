using UnityEngine;

namespace VoidForge.Integration
{
    // VRIK distributes arm twist independently of the wrist. Calibrate after its LateUpdate,
    // using the tracked hand's anatomical palm projected onto the actual forearm plane.
    // The locally installed VRIK importer assigns executionOrder 9998.
    [DefaultExecutionOrder(10010)]
    public sealed class WristDisplayMount : MonoBehaviour
    {
        public Transform wrist, forearm, middle, thumb;
        public CyberdeckSurface surface;
        public float alongArm = .10f, standOff;

        void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (wrist == null || forearm == null || middle == null || thumb == null || surface == null) return;
            Vector3 along = (forearm.position - wrist.position).normalized;
            Vector3 palm = -Vector3.Cross((middle.position - wrist.position).normalized, thumb.position - wrist.position).normalized;
            Vector3 facing = Vector3.ProjectOnPlane(palm, along);
            if (facing.sqrMagnitude < .01f) return;
            facing.Normalize();
            var desired = Quaternion.LookRotation(facing, Vector3.Cross(facing, along));
            transform.rotation = desired * Quaternion.Inverse(surface.hudAnchor.rotation) * transform.rotation;
            transform.position += wrist.position + along * alongArm + facing * standOff - surface.screen.bounds.center;
        }
    }
}
