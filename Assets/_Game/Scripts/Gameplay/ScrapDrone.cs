using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// ENM-001 scrap drone: kinematic pursuit driven by Core logic
    /// (Decision 0001 — the physics solver never decides its position).
    /// Contact damages the player (B13); the drone itself takes no damage
    /// in F05 — TakeKnockback is the entry point F06 will implement.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class ScrapDrone : MonoBehaviour
    {
        [SerializeField] private DroneConfig config;

        private Rigidbody _body;
        private Transform _target;
        private PlayerHealth _targetHealth;
        private DroneSpawner _owner;

        public DroneConfig Config
        {
            get => config;
            set => config = value;
        }

        public Transform Target => _target;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            ConfigureKinematic();
        }

        private void ConfigureKinematic()
        {
            // B2 / Decision 0001: kinematic while alive.
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        /// <summary>B12: the player reference is injected once at spawn, never searched per frame.</summary>
        public void Initialise(Transform target, PlayerHealth targetHealth, DroneConfig droneConfig, DroneSpawner owner)
        {
            _target = target;
            _targetHealth = targetHealth;
            config = droneConfig;
            _owner = owner;
            ConfigureKinematic();
        }

        /// <summary>
        /// Driven by the spawner so neighbour queries happen once per step for the
        /// whole swarm instead of each drone searching on its own (기획서 14.5).
        /// </summary>
        public void Tick(float deltaTime, Float3[] neighbours, int neighbourCount)
        {
            if (_target == null || config == null)
            {
                return;
            }

            Float3 position = ToFloat3(_body.position);
            Float3 target = ToFloat3(_target.position);

            // B1: straight-line pursuit.
            Float3 chased = ChaseLogic.Step(position, target, config.moveSpeed, deltaTime);

            // B8/B9: separation, capped below the chase speed.
            Float3 push = SeparationLogic.Push(position, neighbours, neighbourCount, config.minSeparation);
            Float3 separation = SeparationLogic.SeparationVelocity(push, config.moveSpeed, config.separationStrength);
            Float3 next = chased + separation * deltaTime;

            _body.MovePosition(new Vector3(next.X, _body.position.y, next.Z));
        }

        /// <summary>F06 entry point for knockback (Decision 0001 dynamic switch). Not implemented in F05.</summary>
        public void TakeKnockback(Vector3 impulse)
        {
            // Intentionally empty in F05 — enemy damage, knockback and death are F06.
        }

        // Drone colliders are triggers in F05: a solid kinematic swarm would wall
        // the player in, contradicting ENM-004 ("둘러싸되 완전히 가두지 않는다").
        // Collision callbacks are kept so a non-trigger setup still deals damage.
        private void OnTriggerEnter(Collider other) => TryDamage(other.gameObject);

        private void OnTriggerStay(Collider other) => TryDamage(other.gameObject);

        private void OnCollisionEnter(Collision collision) => TryDamage(collision.gameObject);

        private void OnCollisionStay(Collision collision) => TryDamage(collision.gameObject);

        // B13/B14: contact damage; the invulnerability window in PlayerHealth
        // keeps continuous contact from draining HP every frame.
        private void TryDamage(GameObject other)
        {
            if (_targetHealth == null || config == null)
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
