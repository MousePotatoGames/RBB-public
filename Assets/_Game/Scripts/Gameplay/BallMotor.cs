using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// Humble adapter for the engine-free movement rules (F01) plus jump and
    /// dash (F03). Captures Rigidbody state, runs the Core logic and applies
    /// the results as forces. Input arrives via SetMoveInput / QueueJump /
    /// QueueDash so tests and PlayerInputReader drive the motor the same way.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class BallMotor : MonoBehaviour
    {
        [SerializeField] private BallMovementConfig config;
        [SerializeField] private Transform cameraTransform;

        [Tooltip("PAS-002 (B22): 최대 속도 배율의 출처. 없으면 배율 1")]
        [SerializeField] private PlayerProgress progress;

        private Rigidbody _body;
        private SphereCollider _sphere;
        private Vector2 _moveInput;
        private bool _grounded;
        private Vector3 _groundNormal = Vector3.up;

        private JumpState _jumpState = JumpState.Initial;
        private DashState _dashState = DashState.Initial;
        private bool _jumpQueued;
        private bool _dashQueued;
        private float _dashActiveRemaining;

        public PlayerProgress Progress { get => progress; set => progress = value; }

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

        /// <summary>Remaining dash cooldown in seconds (0 = ready). For HUD and tests.</summary>
        public float DashCooldownRemaining => _dashState.CooldownRemaining;

        /// <summary>
        /// True during the short window after a dash. SPD-001 treats this as the
        /// Rumble tier regardless of actual speed.
        /// </summary>
        public bool IsDashActive => _dashActiveRemaining > 0f;

        public void SetMoveInput(Vector2 input) => _moveInput = input;

        /// <summary>Queues a jump press; consumed by the next FixedUpdate (JUMP-001).</summary>
        public void QueueJump() => _jumpQueued = true;

        /// <summary>Queues a dash press; consumed by the next FixedUpdate (DASH-001).</summary>
        public void QueueDash() => _dashQueued = true;

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

            float dt = Time.fixedDeltaTime;
            UpdateGroundState();

            // PAS-002 (B22): the passive raises the ceiling here, not in the asset.
            // SpeedTierTracker deliberately keeps normalising against the base value,
            // so a faster ball reaches 럼블 sooner instead of feeling identical
            // (설계 판단 3).
            MoveConfig cfg = config.ToMoveConfig(
                progress != null ? progress.Effects.SpeedMultiplier : 1f);

            Float3 direction = BallMovementLogic.CameraRelativeDirection(
                ToFloat3(cameraTransform.forward),
                ToFloat3(cameraTransform.right),
                _moveInput.x,
                _moveInput.y);

            Float3 normal = _grounded ? ToFloat3(_groundNormal) : Float3.Up;
            Float3 planarDirection = direction;
            if (_grounded)
            {
                direction = BallMovementLogic.ProjectOnSlope(direction, normal);
            }

            Float3 velocityChange = BallMovementLogic.ComputeVelocityChange(
                ToFloat3(_body.linearVelocity), direction, normal, cfg, dt, _grounded);
            _body.AddForce(ToVector3(velocityChange), ForceMode.VelocityChange);

            // MOVE-004: counter the along-slope gravity drain while driving on a slope.
            if (_grounded && direction.Magnitude() > 1e-4f)
            {
                Float3 assist = BallMovementLogic.SlopeAssistForce(ToFloat3(Physics.gravity), normal, cfg.SlopeAssist);
                _body.AddForce(ToVector3(assist), ForceMode.Acceleration);
            }

            ApplyJump(dt);
            ApplyDash(planarDirection, dt);
        }

        // JUMP-001
        private void ApplyJump(float deltaTime)
        {
            _jumpState = JumpLogic.Step(_jumpState, _grounded, _jumpQueued, deltaTime);
            _jumpQueued = false;

            if (!JumpLogic.ShouldJump(_jumpState, config.ToJumpConfig()))
            {
                return;
            }

            Vector3 velocity = _body.linearVelocity;
            velocity.y = config.JumpSpeed;
            _body.linearVelocity = velocity;
            _jumpState = JumpLogic.ConsumeJump(_jumpState);
        }

        // DASH-001
        private void ApplyDash(Float3 planarDirection, float deltaTime)
        {
            _dashState = DashLogic.Step(_dashState, deltaTime);
            if (_dashActiveRemaining > 0f)
            {
                _dashActiveRemaining -= deltaTime;
            }

            bool requested = _dashQueued;
            _dashQueued = false;
            if (!requested || !DashLogic.IsReady(_dashState))
            {
                return;
            }

            DashConfig dashConfig = config.ToDashConfig();
            Float3 change = DashLogic.DashVelocityChange(ToFloat3(_body.linearVelocity), planarDirection, dashConfig);
            if (change.Magnitude() < 1e-4f)
            {
                return; // no direction to dash in — keep the dash available
            }

            _body.AddForce(ToVector3(change), ForceMode.VelocityChange);
            _dashState = DashLogic.StartCooldown(_dashState, dashConfig);
            _dashActiveRemaining = config.dashActiveWindow; // SPD-001: rumble tier window
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
