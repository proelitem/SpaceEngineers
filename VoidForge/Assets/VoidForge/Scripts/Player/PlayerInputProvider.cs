using UnityEngine;

namespace VoidForge.Player
{
    /// <summary>
    /// Détient les actions d'entrée du joueur local et les partage avec les autres composants du rig.
    /// </summary>
    public sealed class PlayerInputProvider : MonoBehaviour
    {
        public XRInputActions Actions { get; private set; }
        public bool RightGripConsumed { get; set; }

        private void Awake()
        {
            Actions = new XRInputActions();
        }

        private void OnEnable()
        {
            Actions?.Enable();
        }

        private void OnDisable()
        {
            Actions?.Disable();
        }

        private void OnDestroy()
        {
            Actions?.Dispose();
            Actions = null;
        }
    }
}
