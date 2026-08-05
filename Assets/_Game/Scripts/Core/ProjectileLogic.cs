namespace Game.Core
{
    /// <summary>
    /// WPN-009: how a projectile travels. Only <see cref="Straight"/> is implemented
    /// in F10 — Homing and Hitscan arrive with the weapons that need them (F11/F12),
    /// so no unused code path ships ahead of its use.
    /// </summary>
    public enum ProjectileTravel
    {
        Straight = 0,
        Homing = 1,
        Hitscan = 2,
    }

    /// <summary>WPN-009 / CAN-001 tuning for a projectile weapon (all TEMPORARY).</summary>
    public readonly struct ProjectileConfig
    {
        public readonly ProjectileTravel Travel;

        /// <summary>Seconds between bursts (CAN-001 cooldown).</summary>
        public readonly float FireInterval;

        /// <summary>Shots per burst. Raising this alone turns the cannon into a shotgun.</summary>
        public readonly int ShotsPerBurst;

        /// <summary>Total fan width in degrees. The outermost shots sit at ±half of this.</summary>
        public readonly float SpreadDegrees;

        public readonly float Speed;

        /// <summary>Metres a projectile travels before expiring (B10).</summary>
        public readonly float Range;

        /// <summary>Extra enemies a projectile passes through after the first (B11).</summary>
        public readonly int Pierce;

        public readonly float Damage;

        public ProjectileConfig(
            ProjectileTravel travel,
            float fireInterval,
            int shotsPerBurst,
            float spreadDegrees,
            float speed,
            float range,
            int pierce,
            float damage)
        {
            Travel = travel;
            FireInterval = fireInterval;
            ShotsPerBurst = shotsPerBurst;
            SpreadDegrees = spreadDegrees;
            Speed = speed;
            Range = range;
            Pierce = pierce;
            Damage = damage;
        }
    }

    /// <summary>
    /// CAN-001 / WPN-009: when a projectile weapon fires, where the shots go, and
    /// how they move.
    ///
    /// Two things this deliberately does <b>not</b> do:
    ///
    /// Buckshot is not a code path — it is <see cref="ProjectileConfig.ShotsPerBurst"/>
    /// and <see cref="ProjectileConfig.SpreadDegrees"/> set to something other than 1 and 0.
    ///
    /// Aiming is not a code path either. CAN-001 fires on the cooldown alone, along
    /// whatever direction the barrel already points. The first version gated firing on
    /// the nearest enemy sitting inside a 25° cone; measured in play that produced
    /// 3 shots in 8.8s (and 0 in another run), so the weapon barely existed. Rotation
    /// still drives the cannon — it decides *where* shots go, not *whether* they go.
    /// </summary>
    public static class ProjectileLogic
    {
        /// <summary>Sentinel for "has not fired yet", so the first shot is never gated.</summary>
        public const float NeverFired = float.NegativeInfinity;

        /// <summary>
        /// WPN-009 Exception: a hard ceiling on one burst, so buckshot × fire rate can
        /// never drain the pool no matter what an asset asks for.
        /// </summary>
        public const int MaxShotsPerBurst = 16;

        /// <summary>
        /// B2/B3: the whole firing condition. No target check, no range check, no
        /// angle check — the cannon fires into empty air just as happily.
        /// </summary>
        public static bool CanFire(float lastFireTime, float now, float interval)
        {
            if (interval <= 0f)
            {
                return true;
            }

            return now - lastFireTime >= interval;
        }

        /// <summary>
        /// B6/B7: fills <paramref name="into"/> with the burst's directions and returns
        /// how many were written. One shot goes exactly where aimed regardless of
        /// spread; more than one fans evenly, with the outermost pair at ±half spread.
        /// </summary>
        public static int SpreadDirections(Float3 aim, Float3 upAxis, int count, float spreadDegrees, Float3[] into)
        {
            if (into == null || count <= 0)
            {
                return 0;
            }

            if (count > into.Length)
            {
                count = into.Length;
            }

            if (count > MaxShotsPerBurst)
            {
                count = MaxShotsPerBurst;
            }

            Float3 direction = aim.Normalized();
            if (direction.Equals(Float3.Zero))
            {
                return 0;
            }

            // B7: a single shot ignores spread entirely. Without this, "1 shot with a
            // spread value left over from tuning" would silently drift off-aim.
            if (count == 1)
            {
                into[0] = direction;
                return 1;
            }

            Float3 axis = upAxis.Normalized();
            if (axis.Equals(Float3.Zero))
            {
                axis = Float3.Up;
            }

            float step = spreadDegrees / (count - 1);
            float start = -spreadDegrees * 0.5f;

            for (int i = 0; i < count; i++)
            {
                into[i] = Rotation.AroundAxis(direction, axis, start + step * i);
            }

            return count;
        }

        /// <summary>B10: one step of straight travel.</summary>
        public static Float3 Advance(Float3 position, Float3 direction, float speed, float deltaTime) =>
            position + direction.Normalized() * (speed * deltaTime);
    }
}
