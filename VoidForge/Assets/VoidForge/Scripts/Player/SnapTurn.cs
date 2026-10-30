using UnityEngine;

namespace VoidForge.Player
{
    /// <summary>
    /// Rotation par crans (confort VR) : stick droit vers la gauche/droite = rotation de
    /// <see cref="angleDegrees"/> autour de la tête, sur l'axe « haut » du rig.
    /// </summary>
    public sealed class SnapTurn : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private float angleDegrees = 30f;
        [Tooltip("Seuil d'activation du stick.")]
        [SerializeField] private float threshold = 0.7f;
        [Tooltip("Seuil de réarmement (le stick doit revenir sous cette valeur).")]
        [SerializeField] private float rearmThreshold = 0.3f;

        private PlayerInputProvider _input;
        private Rigidbody _body;
        private bool _armed = true;

        public void Configure(Transform headTransform)
        {
            head = headTransform;
        }

        private void Awake()
        {
            _input = GetComponentInParent<PlayerInputProvider>();
            _body = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (_input == null || _input.Actions == null || head == null) return;

            float x = _input.Actions.RightStick.ReadValue<Vector2>().x;

            if (_armed && Mathf.Abs(x) >= threshold)
            {
                Turn(Mathf.Sign(x) * angleDegrees);
                _armed = false;
            }
            else if (!_armed && Mathf.Abs(x) < rearmThreshold)
            {
                _armed = true;
            }
        }

        private void Turn(float degrees)
        {
            Vector3 pivot = head.position;
            Quaternion delta = Quaternion.AngleAxis(degrees, transform.up);

            if (_body != null)
            {
                Vector3 offset = _body.position - pivot;
                _body.position = pivot + delta * offset;
                _body.rotation = delta * _body.rotation;
                // Garde la vitesse dans le repère monde : tourner la tête ne change pas la trajectoire.
            }
            else
            {
                transform.RotateAround(pivot, transform.up, degrees);
            }
        }
    }
}
