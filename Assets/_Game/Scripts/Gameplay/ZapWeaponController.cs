using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// TES-001 (B1~B13): drives every attached zap weapon.
    ///
    /// Unlike the cannon it never misses — it picks the closest eligible enemy inside
    /// its short reach and damages it on the spot. Closing the distance is what pays:
    /// damage falls off across the range, so diving into a crowd is the correct play.
    ///
    /// Reads each weapon's own transform rather than assuming it is a child of the
    /// ball, so orbit and follow mounts (F12) need no changes here
    /// (Decision 0002 constraint).
    /// </summary>
    public sealed class ZapWeaponController : MonoBehaviour
    {
        private const int MaxWeapons = 3; // WPN-003

        [SerializeField] private WeaponSlots weapons;

        [Tooltip("살아있는 적의 출처. 스포너가 이미 가진 목록을 쓰므로 물리 쿼리를 아예 하지 않는다 (TES-001)")]
        [SerializeField] private DroneSpawner enemies;

        [SerializeField] private GameSession session;

        [Tooltip("F11 임시: 방전을 콘솔로 확인한다 (F13에서 HUD로 대체)")]
        [SerializeField] private bool logToConsole;

        private readonly float[] _lastZapTime = new float[MaxWeapons];
        private Float3[] _positions = new Float3[64];
        private EnemyHealth[] _candidates = new EnemyHealth[64];

        /// <summary>Zapped(from, to) — raised for every discharge so Presentation can draw it.</summary>
        public event Action<Vector3, Vector3> Zapped;

        /// <summary>Discharges this session — PlayMode tests assert on this.</summary>
        public int ZapCount { get; private set; }

        /// <summary>Damage dealt by the most recent discharge, for tests and later HUD work.</summary>
        public float LastZapDamage { get; private set; }

        public WeaponSlots Weapons { get => weapons; set => weapons = value; }
        public DroneSpawner Enemies { get => enemies; set => enemies = value; }
        public GameSession Session { get => session; set => session = value; }

        private void Awake()
        {
            if (weapons == null)
            {
                weapons = GetComponent<WeaponSlots>();
            }

            ResetCooldowns();
        }

        /// <summary>Lets the first discharge happen immediately instead of waiting one interval.</summary>
        public void ResetCooldowns()
        {
            for (int i = 0; i < _lastZapTime.Length; i++)
            {
                _lastZapTime[i] = ZapLogic.NeverZapped;
            }
        }

        private void FixedUpdate()
        {
            if (weapons == null || weapons.AttachedCount == 0)
            {
                return;
            }

            if (session != null && !session.IsRunning)
            {
                return; // B13
            }

            float now = Time.time;
            int slots = Mathf.Min(weapons.AttachedCount, MaxWeapons);

            for (int i = 0; i < slots; i++)
            {
                WeaponDefinition definition = weapons.DefinitionAt(i);
                if (definition == null || definition.attack != WeaponAttack.Zap)
                {
                    continue; // B11
                }

                if (!ZapLogic.CanZap(_lastZapTime[i], now, definition.zapInterval))
                {
                    continue; // B1
                }

                GameObject weapon = weapons.AttachedAt(i);
                if (weapon == null)
                {
                    continue;
                }

                // B5: reach is measured from the ring, not the ball's centre.
                Vector3 origin = weapon.transform.position;

                int count = GatherEligibleEnemies(now);
                if (count == 0)
                {
                    continue; // B3 — the interval keeps running
                }

                ZapConfig config = definition.ToZapConfig();
                int target = ZapLogic.NearestInRange(
                    ToFloat3(origin), _positions, count, config.Range, out float distance);

                if (target == ZapLogic.NoTarget)
                {
                    continue; // B3
                }

                Discharge(origin, _candidates[target], distance, config, now, definition);
                _lastZapTime[i] = now;
            }
        }

        private void Discharge(
            Vector3 origin, EnemyHealth enemy, float distance, in ZapConfig config, float now, WeaponDefinition definition)
        {
            float damage = ZapLogic.Damage(distance, config); // B4

            // B6/B7: immediate, and through the same cooldown gate as every other source.
            enemy.TryTakeDamage(damage, now);

            ZapCount++;
            LastZapDamage = damage;

            Zapped?.Invoke(origin, enemy.transform.position); // B14

            if (logToConsole)
            {
                Debug.Log($"[ZAP] {definition.displayName} → {damage:0.#} dmg at {distance:0.0}m", this);
            }
        }

        /// <summary>
        /// B2/B8/B9: live enemies that can actually be hurt right now.
        ///
        /// Filtering out enemies inside their DMG-002 cooldown matters more than it
        /// looks: without it the nearest enemy is picked, absorbs nothing, and the
        /// whole discharge interval is wasted — which is exactly the situation the
        /// tesla is supposed to be good at.
        /// </summary>
        private int GatherEligibleEnemies(float now)
        {
            if (enemies == null)
            {
                return 0;
            }

            var active = enemies.Active;
            if (_positions.Length < active.Count)
            {
                _positions = new Float3[Mathf.NextPowerOfTwo(active.Count)];
                _candidates = new EnemyHealth[_positions.Length];
            }

            int count = 0;
            for (int i = 0; i < active.Count; i++)
            {
                ScrapDrone drone = active[i];
                if (drone == null || drone.IsDead)
                {
                    continue; // B9 — corpses linger in the list until despawn
                }

                EnemyHealth health = drone.Health;
                if (health == null || !health.CanBeHit(now))
                {
                    continue; // B8
                }

                Vector3 p = drone.transform.position;
                _positions[count] = new Float3(p.x, p.y, p.z);
                _candidates[count] = health;
                count++;
            }

            return count;
        }

        private static Float3 ToFloat3(Vector3 v) => new Float3(v.x, v.y, v.z);
    }
}
