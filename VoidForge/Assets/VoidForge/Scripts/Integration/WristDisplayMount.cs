using System;
using UnityEngine;

namespace VoidForge.Integration
{
    // VRIK distributes arm twist independently of the wrist. Calibrate after its LateUpdate,
    // using the tracked hand's anatomical palm projected onto the actual forearm plane.
    // The locally installed VRIK importer assigns executionOrder 9998.
    [DefaultExecutionOrder(10010)]
    public sealed class WristDisplayMount : MonoBehaviour
    {
        [Serializable] public sealed class SleeveSample
        {
            public Transform a,b,c,d;
            public Vector3 pa,pb,pc,pd;
            public Vector4 weights;
            public Vector3 WorldPosition => a.TransformPoint(pa)*weights.x+b.TransformPoint(pb)*weights.y+
                c.TransformPoint(pc)*weights.z+d.TransformPoint(pd)*weights.w;
        }
        public Transform wrist, forearm, middle, thumb;
        public CyberdeckSurface surface;
        public float alongArm = .10f, standOff;
        public float housingDepth;
        public SleeveSample[] sleeveSamples;
        public float ContactGapMeters { get; private set; }

        void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (wrist == null || forearm == null || middle == null || thumb == null || surface == null) return;
            Vector3 along = (forearm.position - wrist.position).normalized;
            Vector3 palm = -Vector3.Cross((middle.position - wrist.position).normalized, thumb.position - wrist.position).normalized;
            Vector3 facing = Vector3.ProjectOnPlane(palm, along);
            if (facing.sqrMagnitude < .01f) return;
            facing.Normalize();
            Vector3 centre = wrist.position + along * alongArm;
            if (sleeveSamples != null && sleeveSamples.Length > 0)
            {
                float support = float.NegativeInfinity;
                foreach (var sample in sleeveSamples)
                {
                    Vector3 delta = sample.WorldPosition - centre;
                    if (Mathf.Abs(Vector3.Dot(delta,along)) < .07f && Mathf.Abs(Vector3.Dot(delta,Vector3.Cross(along,facing))) < .055f)
                        support = Mathf.Max(support,Vector3.Dot(delta,facing));
                }
                if (!float.IsNegativeInfinity(support))
                {
                    standOff = support + housingDepth - .001f;
                    ContactGapMeters = standOff - housingDepth - support;
                }
            }
            var desired = Quaternion.LookRotation(facing, Vector3.Cross(facing, along));
            transform.rotation = desired * Quaternion.Inverse(surface.hudAnchor.rotation) * transform.rotation;
            transform.position += centre + facing * standOff - surface.screen.bounds.center;
        }
    }
}
