using UnityEngine;

namespace VoidForge.Player
{
    /// <summary>
    /// Locomotion en apesanteur (jalon J1).
    /// - Stick gauche : poussée avant/arrière et latérale, dans le repère de la main gauche
    ///   (ou de la tête si <see cref="thrustRelativeToHand"/> est faux).
    /// - Stick droit, axe vertical : poussée haut/bas dans le repère de la tête.
    /// - Gâchettes latérales (grip) : freinage proportionnel.
    /// Le rig est un corps rigide sans gravité ; la vitesse est plafonnée.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ZeroGLocomotion : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private Transform head;
        [SerializeField] private Transform leftHand;

        [Header("Réglages")]
        [Tooltip("Accélération de poussée en m/s².")]
        [SerializeField] private float thrustAcceleration = 4f;
        [Tooltip("Vitesse maximale en m/s.")]
        [SerializeField] private float maxSpeed = 12f;
        [Tooltip("Décélération de freinage (grip à fond) en m/s².")]
        [SerializeField] private float brakeDeceleration = 8f;
        [Tooltip("Zone morte des sticks.")]
        [SerializeField] private float deadZone = 0.15f;
        [Tooltip("Pousser dans la direction de la main gauche (vrai) ou du regard (faux).")]
        [SerializeField] private bool thrustRelativeToHand = true;

        private Rigidbody _body;
        private PlayerInputProvider _input;

        /// <summary>Vitesse actuelle en m/s (pour le HUD).</summary>
        public float Speed => _body != null ? _body.linearVelocity.magnitude : 0f;

        public void Configure(Transform headTransform, Transform leftHandTransform)
        {
            head = headTransform;
            leftHand = leftHandTransform;
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
            _body.linearDamping = 0f;
            _body.angularDamping = 0f;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.constraints = RigidbodyConstraints.FreezeRotation;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _input = GetComponentInParent<PlayerInputProvider>();
        }

        private void FixedUpdate()
        {
            if (_input == null || _input.Actions == null || head == null) return;
            var a = _input.Actions;

            Vector2 left = ApplyDeadZone(a.LeftStick.ReadValue<Vector2>());
            Vector2 right = ApplyDeadZone(a.RightStick.ReadValue<Vector2>());

            Transform frame = (thrustRelativeToHand && leftHand != null) ? leftHand : head;
            Vector3 thrust = frame.forward * left.y + frame.right * left.x + head.up * right.y;
            if (thrust.sqrMagnitude > 1f) thrust.Normalize();

            if (thrust.sqrMagnitude > 0f)
            {
                _body.AddForce(thrust * thrustAcceleration, ForceMode.Acceleration);
            }

            float brake = Mathf.Max(a.LeftGrip.ReadValue<float>(),
                _input.RightGripConsumed ? 0f : a.RightGrip.ReadValue<float>());
            if (brake > 0.1f)
            {
                _body.linearVelocity = Vector3.MoveTowards(
                    _body.linearVelocity, Vector3.zero, brakeDeceleration * brake * Time.fixedDeltaTime);
            }

            if (_body.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
            {
                _body.linearVelocity = _body.linearVelocity.normalized * maxSpeed;
            }
        }

        private Vector2 ApplyDeadZone(Vector2 v)
        {
            float m = v.magnitude;
            if (m < deadZone) return Vector2.zero;
            // Remappe [deadZone,1] vers [0,1] pour une réponse progressive.
            return v / m * Mathf.InverseLerp(deadZone, 1f, m);
        }
    }
}
