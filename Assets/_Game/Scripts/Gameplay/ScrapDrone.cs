using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// ENM-001 scrap drone: kinematic pursuit driven by Core logic
    /// (Decision 0001 — the physics solver never decides its position while
    /// alive). Contact damages the player (F05, B13). Being hit switches it to a
    /// dynamic body so it physically flies (F06, B10/B11); if it survives it
    /// returns to kinematic chasing, if it dies the corpse lingers briefly and
    /// then goes back to the pool.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class ScrapDrone : MonoBehaviour
    {
        [SerializeField] private DroneConfig config;

        private Rigidbody _body;
        private SphereCollider _collider;
        private EnemyHealth _health;
        private Transform _target;
        private PlayerHealth _targetHealth;
        private DroneSpawner _owner;

        private float _dynamicRemaining;
        private float _corpseRemaining;

        /// <summary>
        /// Height the drone chases at. Kinematic movement keeps its own Y, so a
        /// knockback that lofts the drone would otherwise leave it hovering
        /// forever once it recovers.
        /// </summary>
        private float _hoverY;

        public DroneConfig Config
        {
            get => config;
            set => config = value;
        }

        public Transform Target => _target;
        public EnemyHealth Health => _health;

        /// <summary>True while the drone is flying from a hit (dynamic body).</summary>
        public bool IsRagdolling => !_body.isKinematic;

        /// <summary>True once killed — no longer chases or damages the player (B12).</summary>
        public bool IsDead => _health != null && !_health.IsAlive;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();
            _health = GetComponent<EnemyHealth>();
            SetKinematic(true);
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Died -= OnDied;
            }
        }

        private void SetKinematic(bool kinematic)
        {
            _body.isKinematic = kinematic;
            _body.useGravity = !kinematic;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            if (_collider != null)
            {
                // Trigger while chasing (B18); solid while flying so it bounces off the floor.
                _collider.isTrigger = kinematic;
            }
        }

        /// <summary>B12/B14: the player reference is injected once at spawn, and state is reset for reuse.</summary>
        public void Initialise(Transform target, PlayerHealth targetHealth, DroneConfig droneConfig, DroneSpawner owner)
        {
            _target = target;
            _targetHealth = targetHealth;
            config = droneConfig;
            _owner = owner;

            if (_health == null)
            {
                _health = GetComponent<EnemyHealth>();
            }

            _health.Config = droneConfig;
            _health.ResetHealth();

            _dynamicRemaining = 0f;
            _corpseRemaining = 0f;
            _hoverY = transform.position.y;

            // A pooled drone is usually already kinematic, and writing velocity on a
            // kinematic body is unsupported (it warns every spawn).
            if (!_body.isKinematic)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
            }

            SetKinematic(true);
        }

        /// <summary>
        /// B10/B11: knockback recovery and corpse expiry are owned by the drone
        /// itself, not by whoever drives its movement — a knocked-back drone must
        /// recover even if the spawner never ticks it again.
        /// </summary>
        private void FixedUpdate()
        {
            if (config == null)
            {
                return;
            }

            float dt = Time.fixedDeltaTime;

            if (IsDead)
            {
                _corpseRemaining -= dt;
                if (_corpseRemaining <= 0f && _owner != null)
                {
                    _owner.Despawn(this); // B11
                }

                return;
            }

            if (_dynamicRemaining <= 0f)
            {
                return;
            }

            _dynamicRemaining -= dt;
            if (_dynamicRemaining <= 0f)
            {
                // B10: recovered — back to scripted pursuit at the chase height,
                // otherwise a lofted drone would hover there forever (B19).
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                SetKinematic(true);
                SnapToChaseHeight();
            }
        }

        /// <summary>
        /// B19: drops the drone back to its chase height. Interpolation is turned
        /// off across the write — an interpolated body reconstructs its Transform
        /// from the buffered poses and silently undoes the teleport.
        /// </summary>
        private void SnapToChaseHeight()
        {
            Vector3 p = _body.position;
            if (Mathf.Approximately(p.y, _hoverY))
            {
                return;
            }

            var snapped = new Vector3(p.x, _hoverY, p.z);
            RigidbodyInterpolation previous = _body.interpolation;
            _body.interpolation = RigidbodyInterpolation.None;
            _body.position = snapped;
            transform.position = snapped;
            _body.interpolation = previous;
        }

        /// <summary>
        /// Movement only. Driven by the spawner so neighbour queries happen once
        /// per step for the whole swarm instead of each drone searching on its
        /// own (기획서 14.5). State timers live in FixedUpdate.
        /// </summary>
        public void Tick(float deltaTime, Float3[] neighbours, int neighbourCount)
        {
            if (config == null || IsDead || _dynamicRemaining > 0f)
            {
                return; // dead or flying: no scripted movement
            }

            if (_target == null)
            {
                return;
            }

            Float3 position = ToFloat3(_body.position);
            Float3 target = ToFloat3(_target.position);

            Float3 chased = ChaseLogic.Step(position, target, config.moveSpeed, deltaTime);
            Float3 push = SeparationLogic.Push(position, neighbours, neighbourCount, config.minSeparation);
            Float3 separation = SeparationLogic.SeparationVelocity(push, config.moveSpeed, config.separationStrength);
            Float3 next = chased + separation * deltaTime;

            _body.MovePosition(new Vector3(next.X, _hoverY, next.Z));
        }

        /// <summary>
        /// B10 / Decision 0001: switches to a dynamic body and applies the impulse.
        /// A surviving drone returns to kinematic after the recovery window.
        /// </summary>
        public void TakeKnockback(Vector3 impulse)
        {
            if (config == null)
            {
                return;
            }

            SetKinematic(false);
            _body.linearVelocity = Vector3.zero;
            _body.AddForce(impulse, ForceMode.VelocityChange);
            _dynamicRemaining = IsDead ? float.PositiveInfinity : config.knockbackRecovery;
        }

        private void OnDied(EnemyHealth _)
        {
            // B11: corpse flies and lingers before returning to the pool.
            _corpseRemaining = config != null ? config.corpseTime : 1f;
            _dynamicRemaining = float.PositiveInfinity;
        }

        // Drone colliders are triggers while chasing (B18): a solid kinematic swarm
        // would wall the player in, contradicting ENM-004.
        private void OnTriggerEnter(Collider other) => TryDamage(other.gameObject);

        private void OnTriggerStay(Collider other) => TryDamage(other.gameObject);

        private void OnCollisionEnter(Collision collision) => TryDamage(collision.gameObject);

        private void OnCollisionStay(Collision collision) => TryDamage(collision.gameObject);

        // B13/B14 (F05) + B12 (F06): a dead drone stops hurting the player.
        private void TryDamage(GameObject other)
        {
            if (_targetHealth == null || config == null || IsDead)
            {
                return;
            }

            if (other.transform != _target)
            {
                return;
            }

            _targetHealth.TakeDamage(config.contactDamage);
        }

        private static Float3 ToFloat3(Vector3 v) => new Float3(v.x, v.y, v.z);
    }
}
