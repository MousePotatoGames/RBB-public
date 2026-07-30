using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Humble adapter for BallMovementLogic (F01). Captures Rigidbody state,
    /// runs the engine-free rules and applies the results as forces.
    /// Input arrives via SetMoveInput so tests and PlayerInputReader both
    /// drive the motor the same way.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class BallMotor : MonoBehaviour
    {
        [SerializeField] private BallMovementConfig config;
        [SerializeField] private Transform cameraTransform;

        private Rigidbody _body;
        private SphereCollider _sphere;
        private Vector2 _moveInput;
        private bool _grounded;
        private Vector3 _groundNormal = Vector3.up;

        public BallMovementConfig Config
        {
            get => config;
            set => config = value;
        }

        public Transform CameraTransform
        {
            get => cameraTransform;
            set => cameraTransform = value;
        }

        public bool IsGrounded => _grounded;
        public Vector3 GroundNormal => _groundNormal;

        public void SetMoveInput(Vector2 input) => _moveInput = input;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _sphere = GetComponent<SphereCollider>();
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void FixedUpdate()
        {
            if (config == null || cameraTransform == null)
            {
                return;
            }

            UpdateGroundState();

            MoveConfig cfg = config.ToMoveConfig();
            Float3 direction = BallMovementLogic.CameraRelativeDirection(
                ToFloat3(cameraTransform.forward),
                ToFloat3(cameraTransform.right),
                _moveInput.x,
                _moveInput.y);

            Float3 normal = _grounded ? ToFloat3(_groundNormal) : Float3.Up;
            if (_grounded)
            {
                direction = BallMovementLogic.ProjectOnSlope(direction, normal);
            }

            Float3 velocityChange = BallMovementLogic.ComputeVelocityChange(
                ToFloat3(_body.linearVelocity), direction, normal, cfg, Time.fixedDeltaTime);
            _body.AddForce(ToVector3(velocityChange), ForceMode.VelocityChange);

            // MOVE-004: counter the along-slope gravity drain while driving on a slope.
            if (_grounded && direction.Magnitude() > 1e-4f)
            {
                Float3 assist = BallMovementLogic.SlopeAssistForce(ToFloat3(Physics.gravity), normal, cfg.SlopeAssist);
                _body.AddForce(ToVector3(assist), ForceMode.Acceleration);
            }
        }

        private void UpdateGroundState()
        {
            float radius = _sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            Vector3 origin = _body.position;
            float castRadius = radius * 0.95f;
            float castDistance = Mathf.Max(0.01f, config.groundCheckDistance - castRadius);

            if (Physics.SphereCast(origin, castRadius, Vector3.down, out RaycastHit hit, castDistance,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                _grounded = true;
                _groundNormal = hit.normal;
            }
            else
            {
                _grounded = false;
                _groundNormal = Vector3.up;
            }
        }

        private static Float3 ToFloat3(Vector3 v) => new Float3(v.x, v.y, v.z);
        private static Vector3 ToVector3(Float3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
