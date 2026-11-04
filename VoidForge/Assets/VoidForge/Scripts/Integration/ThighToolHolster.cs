using UnityEngine;

namespace VoidForge.Integration
{
    // Follow the solved upper leg without inheriting the imported skeleton's FBX scale.
    [DefaultExecutionOrder(10020)]
    public sealed class ThighToolHolster : MonoBehaviour
    {
        public Transform thigh, avatar;
        public Vector3 offset = new Vector3(.16f, .02f, .08f);
        void LateUpdate() => Refresh();
        public void Refresh()
        {
            if (thigh == null || avatar == null) return;
            transform.SetPositionAndRotation(thigh.position + avatar.rotation * offset,
                avatar.rotation * Quaternion.LookRotation(Vector3.down, Vector3.forward));
        }
    }
}
