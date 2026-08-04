using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// DMG-001~004: the player's only attack. Movement into an enemy is the hit,
    /// so this reads the ball's own velocity and dash state and applies the Core
    /// formula. Raises Hit/Killed so Presentation can react (hit stop) without
    /// Gameplay depending on Presentation.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerDamageDealer : MonoBehaviour
    {
        [SerializeField] private DroneConfig config;
        [SerializeField] private BallMovementConfig movementConfig;
        [SerializeField] private BallMotor motor;

        [Tooltip("F09 (SPK-001): 접촉 무기의 추가 피해를 이 충돌에 더한다")]
        [SerializeField] private WeaponSlots weapons;

        [Tooltip("F06 임시: 타격 결과를 콘솔로 확인한다 (F13에서 HUD로 대체)")]
        [SerializeField] private bool logToConsole;

        private Rigidbody _body;

        /// <summary>Hit(damage, enemyPosition) — raised for every landed hit.</summary>
        public event Action<float, Vector3> Hit;

        /// <summary>Killed(enemyPosition).</summary>
        public event Action<Vector3> Killed;

        public int HitCount { get; private set; }
        public int KillCount { get; private set; }

        public DroneConfig Config { get => config; set => config = value; }
        public BallMovementConfig MovementConfig { get => movementConfig; set => movementConfig = value; }
        public BallMotor Motor { get => motor; set => motor = value; }
        public WeaponSlots Weapons { get => weapons; set => weapons = value; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            if (motor == null)
            {
                motor = GetComponent<BallMotor>();
            }

            if (weapons == null)
            {
                weapons = GetComponent<WeaponSlots>();
            }

            if (movementConfig == null && motor != null)
            {
                movementConfig = motor.Config;
            }
        }

        private void OnTriggerEnter(Collider other) => TryHit(other.gameObject);

        private void OnTriggerStay(Collider other) => TryHit(other.gameObject);

        private void OnCollisionEnter(Collision collision) => TryHit(collision.gameObject);

        private void OnCollisionStay(Collision collision) => TryHit(collision.gameObject);

        private void TryHit(GameObject other)
        {
            if (config == null || movementConfig == null)
            {
                return;
            }

            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy == null)
            {
                return;
            }

            float now = Time.time;
            if (!enemy.CanBeHit(now))
            {
                return; // DMG-002
            }

            Vector3 velocity = _body.linearVelocity;
            Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 toEnemy = enemy.transform.position - transform.position;

            bool dashing = motor != null && motor.IsDashActive;

            float damage = DamageLogic.CollisionDamage(
                planar.magnitude,
                movementConfig.maxSpeed,
                ToFloat3(planar),
                ToFloat3(toEnemy),
                dashing,
                velocity.y,
                passiveMultiplier: 1f, // F12 will feed this
                config.ToDamageConfig());

            // SPK-001 (B7): the weapon bonus rides on this collision rather than
            // being its own hit, so it shares the DMG-002 cooldown for free.
            float knockbackMultiplier = 1f;
            if (weapons != null && weapons.AttachedCount > 0)
            {
                float frontality = DamageLogic.Frontality(
                    ToFloat3(planar), ToFloat3(toEnemy), config.ToDamageConfig());

                damage += weapons.BonusDamageAgainst(
                    toEnemy, planar.magnitude, movementConfig.maxSpeed, frontality, dashing,
                    config.minSpeedMultiplier);

                knockbackMultiplier = weapons.KnockbackMultiplierAgainst(
                    toEnemy, dashing, config.minSpeedMultiplier);
            }

            if (!enemy.TryTakeDamage(damage, now))
            {
                return;
            }

            HitCount++;
            bool killed = !enemy.IsAlive;

            var drone = enemy.GetComponent<ScrapDrone>();
            if (drone != null)
            {
                Float3 impulse = KnockbackLogic.Impulse(
                    ToFloat3(toEnemy),
                    damage,
                    config.droneMaxHealth,
                    config.knockbackForce,
                    config.knockbackResistance);
                // SPK-001 (B6): a dashing spike hit throws the enemy harder.
                drone.TakeKnockback(new Vector3(impulse.X, impulse.Y, impulse.Z) * knockbackMultiplier);
            }

            Hit?.Invoke(damage, enemy.transform.position);
            if (killed)
            {
                KillCount++;
                Killed?.Invoke(enemy.transform.position);
            }

            if (logToConsole)
            {
                Debug.Log($"[HIT] {damage:0.#} dmg{(killed ? " (KILL)" : string.Empty)} — speed {planar.magnitude:0.#}", this);
            }
        }

        private static Float3 ToFloat3(Vector3 v) => new Float3(v.x, v.y, v.z);
    }
}
