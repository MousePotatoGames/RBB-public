using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// CAN-001 (B1~B5, B15): drives every attached projectile weapon.
    ///
    /// This is the first weapon that attacks without the player colliding with
    /// anything. It fires on the cooldown alone, straight down the barrel — it never
    /// looks for a target. Because the barrel turns with the ball (WPN-002), rolling
    /// is what decides where the shots go, and positioning replaces aiming.
    ///
    /// Deliberately reads each weapon's own transform rather than assuming it is a
    /// child of the ball, so orbit and follow mounts (F12) need no changes here
    /// (Decision 0002 constraint).
    /// </summary>
    public sealed class ProjectileWeaponController : MonoBehaviour
    {
        private const int MaxWeapons = 3; // WPN-003

        [SerializeField] private WeaponSlots weapons;
        [SerializeField] private ProjectilePool pool;
        [SerializeField] private GameSession session;

        [Tooltip("F10 임시: 발사를 콘솔로 확인한다 (F13에서 HUD로 대체)")]
        [SerializeField] private bool logToConsole;

        private readonly float[] _lastFireTime = new float[MaxWeapons];
        private readonly Float3[] _shots = new Float3[ProjectileLogic.MaxShotsPerBurst];

        /// <summary>Bursts fired this session — PlayMode tests assert on this.</summary>
        public int FireCount { get; private set; }

        /// <summary>Individual projectiles launched (a burst of 5 counts as 5).</summary>
        public int ShotCount { get; private set; }

        public WeaponSlots Weapons { get => weapons; set => weapons = value; }
        public ProjectilePool Pool { get => pool; set => pool = value; }
        public GameSession Session { get => session; set => session = value; }

        private void Awake()
        {
            if (weapons == null)
            {
                weapons = GetComponent<WeaponSlots>();
            }

            ResetCooldowns();
        }

        /// <summary>Lets the first shot fire immediately instead of waiting one interval.</summary>
        public void ResetCooldowns()
        {
            for (int i = 0; i < _lastFireTime.Length; i++)
            {
                _lastFireTime[i] = ProjectileLogic.NeverFired;
            }
        }

        private void FixedUpdate()
        {
            if (weapons == null || pool == null || weapons.AttachedCount == 0)
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
                if (definition == null || definition.attack != WeaponAttack.Projectile)
                {
                    continue; // B15
                }

                GameObject weapon = weapons.AttachedAt(i);
                if (weapon == null)
                {
                    continue;
                }

                ProjectileConfig config = definition.ToProjectileConfig();
                if (!ProjectileLogic.CanFire(_lastFireTime[i], now, config.FireInterval))
                {
                    continue; // B2 — the only gate
                }

                Transform muzzle = weapon.transform;

                // WeaponSlots aligns a surface weapon's local +Y with the outward
                // normal, so `up` is the barrel direction in world space (B4).
                Fire(muzzle.position, muzzle.up, config, definition);
                _lastFireTime[i] = now;
            }
        }

        private void Fire(Vector3 origin, Vector3 barrel, in ProjectileConfig config, WeaponDefinition definition)
        {
            // B5: straight down the barrel. No target, no assist, no correction.
            int shots = ProjectileLogic.SpreadDirections(
                ToFloat3(barrel), Float3.Up, config.ShotsPerBurst, config.SpreadDegrees, _shots); // B6/B7

            int launched = 0;
            for (int s = 0; s < shots; s++)
            {
                if (!pool.TryLaunch(origin, ToVector3(_shots[s]), config, definition.projectileRadius))
                {
                    break; // B9 — the cap was reached; the rest of this burst is dropped
                }

                launched++;
            }

            FireCount++;
            ShotCount += launched;

            if (logToConsole)
            {
                Debug.Log($"[PRJ] {definition.displayName} fired {launched}/{shots} (live {pool.LiveCount})", this);
            }
        }

        private static Float3 ToFloat3(Vector3 v) => new Float3(v.x, v.y, v.z);

        private static Vector3 ToVector3(Float3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
