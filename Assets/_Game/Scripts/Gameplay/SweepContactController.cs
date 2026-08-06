using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// WPN-008a (B9/B10): contact damage for weapons that are <b>not</b> on the ball's
    /// surface.
    ///
    /// This exists because surface contact (SPK-001) cannot be reused here. That path
    /// adds bonus damage to a collision the ball already had — it needs the ball to
    /// touch the enemy. An orbiting axe has to hit enemies the ball never reaches, so
    /// it is its own damage source with its own timer, and it hits everything inside
    /// its radius rather than one collided enemy.
    ///
    /// This is the half of Decision 0002's "new weapons need zero attack code" that
    /// turned out to be wrong.
    /// </summary>
    public sealed class SweepContactController : MonoBehaviour
    {
        private const int MaxWeapons = 3; // WPN-003

        [SerializeField] private WeaponSlots weapons;

        [Tooltip("살아있는 적의 출처. 스포너가 이미 가진 목록을 쓴다 (기획서 15장)")]
        [SerializeField] private DroneSpawner enemies;

        [SerializeField] private GameSession session;

        [Tooltip("F12 임시: 판정을 콘솔로 확인한다 (F13에서 HUD로 대체)")]
        [SerializeField] private bool logToConsole;

        private readonly float[] _lastSweepTime = new float[MaxWeapons];
        private Float3[] _positions = new Float3[64];
        private EnemyHealth[] _candidates = new EnemyHealth[64];
        private int[] _found = new int[64];

        /// <summary>Sweeps performed — PlayMode tests assert on this.</summary>
        public int SweepCount { get; private set; }

        /// <summary>Enemies actually damaged (a sweep hitting three counts as three).</summary>
        public int HitCount { get; private set; }

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

        public void ResetCooldowns()
        {
            for (int i = 0; i < _lastSweepTime.Length; i++)
            {
                _lastSweepTime[i] = ProjectileLogic.NeverFired;
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
                return;
            }

            float now = Time.time;
            int slots = Mathf.Min(weapons.AttachedCount, MaxWeapons);

            for (int i = 0; i < slots; i++)
            {
                WeaponDefinition definition = weapons.DefinitionAt(i);
                if (definition == null ||
                    definition.attack != WeaponAttack.Contact ||
                    definition.mount == WeaponMount.Surface)
                {
                    continue; // B11 — surface contact stays on the SPK-001 path
                }

                if (!ProjectileLogic.CanFire(_lastSweepTime[i], now, definition.sweepInterval))
                {
                    continue;
                }

                GameObject weapon = weapons.AttachedAt(i);
                if (weapon == null)
                {
                    continue;
                }

                Sweep(weapon.transform.position, definition, now);
                _lastSweepTime[i] = now;
            }
        }

        private void Sweep(Vector3 origin, WeaponDefinition definition, float now)
        {
            int count = GatherEnemies();
            SweepCount++;

            if (count == 0)
            {
                return;
            }

            var from = new Float3(origin.x, origin.y, origin.z);
            int hits = SweepLogic.AllInRange(from, _positions, count, definition.sweepRadius, _found); // B9

            int landed = 0;
            for (int i = 0; i < hits; i++)
            {
                // B10: an enemy inside its DMG-002 cooldown simply absorbs nothing.
                // No need to filter it out first — unlike the tesla, a sweep that hits
                // several enemies wastes nothing by including one that is immune.
                if (_candidates[_found[i]].TryTakeDamage(definition.sweepDamage, now))
                {
                    landed++;
                }
            }

            HitCount += landed;

            if (logToConsole && landed > 0)
            {
                Debug.Log($"[SWEEP] {definition.displayName} hit {landed} for {definition.sweepDamage:0.#} each", this);
            }
        }

        private int GatherEnemies()
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
                _found = new int[_positions.Length];
            }

            int count = 0;
            for (int i = 0; i < active.Count; i++)
            {
                ScrapDrone drone = active[i];
                if (drone == null || drone.IsDead || drone.Health == null)
                {
                    continue;
                }

                Vector3 p = drone.transform.position;
                _positions[count] = new Float3(p.x, p.y, p.z);
                _candidates[count] = drone.Health;
                count++;
            }

            return count;
        }
    }
}
