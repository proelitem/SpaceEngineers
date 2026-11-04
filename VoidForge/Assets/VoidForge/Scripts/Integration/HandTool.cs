using UnityEngine;

namespace VoidForge.Integration
{
    public enum HandToolKind { MiningDrill, Grinder, Welder }

    public sealed class HandTool : MonoBehaviour
    {
        public HandToolKind kind;
        public Transform primaryGrip, tip, movingPart;
        // Axis in the moving part's rest frame, calibrated from the imported geometry.
        public Vector3 spinAxisLocal = Vector3.forward;
        public bool IsHeld { get; private set; }
        public bool IsOperating { get; private set; }
        Transform home;
        Vector3 homePosition;
        Quaternion homeRotation;
        Quaternion movingRest;
        float phase;

        public void RememberHome()
        {
            home = transform.parent;
            homePosition = transform.localPosition;
            homeRotation = transform.localRotation;
            if (movingPart != null) movingRest = movingPart.localRotation;
        }

        // Align the delivered primary handle frame, rather than the model origin.
        public void Hold(Transform socket)
        {
            IsHeld = true;
            transform.SetParent(socket, true);
            transform.rotation = socket.rotation * Quaternion.Inverse(Quaternion.Inverse(transform.rotation) * primaryGrip.rotation);
            transform.position += socket.position - primaryGrip.position;
        }

        public void ReturnHome()
        {
            IsHeld = IsOperating = false;
            transform.SetParent(home, false);
            transform.SetLocalPositionAndRotation(homePosition, homeRotation);
            if (movingPart != null) movingPart.localRotation = movingRest;
        }

        public void Operate(float trigger)
        {
            IsOperating = IsHeld && trigger > .55f;
            if (!IsOperating || movingPart == null || kind == HandToolKind.Welder) return;
            phase = (phase + Time.deltaTime * 1440f) % 360f;
            SetRotationPhase(phase);
        }

        public void SetRotationPhase(float degrees)
        {
            if (movingPart != null && kind != HandToolKind.Welder)
                movingPart.localRotation = movingRest * Quaternion.AngleAxis(degrees, spinAxisLocal);
        }
    }
}
