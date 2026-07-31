namespace Game.Core
{
    /// <summary>Coefficients for the DMG-001 collision damage formula (all TEMPORARY).</summary>
    public readonly struct DamageConfig
    {
        public readonly float BaseDamage;

        /// <summary>Floor for the speed factor so slow contact is never worthless (DMG-001).</summary>
        public readonly float MinSpeedMultiplier;

        /// <summary>Ceiling for the speed factor, rewarding dash overspeed.</summary>
        public readonly float MaxSpeedMultiplier;

        /// <summary>Floor for frontality so side/rear contact never zeroes or inverts damage.</summary>
        public readonly float MinFrontality;

        public readonly float DashMultiplier;
        public readonly float FallMultiplier;

        /// <summary>Downward speed at or above which the fall bonus applies (DMG-004).</summary>
        public readonly float FallSpeedThreshold;

        public DamageConfig(
            float baseDamage,
            float minSpeedMultiplier,
            float maxSpeedMultiplier,
            float minFrontality,
            float dashMultiplier,
            float fallMultiplier,
            float fallSpeedThreshold)
        {
            BaseDamage = baseDamage;
            MinSpeedMultiplier = minSpeedMultiplier;
            MaxSpeedMultiplier = maxSpeedMultiplier;
            MinFrontality = minFrontality;
            DashMultiplier = dashMultiplier;
            FallMultiplier = fallMultiplier;
            FallSpeedThreshold = fallSpeedThreshold;
        }
    }

    /// <summary>
    /// DMG-001 / DMG-004: the game's core attack formula. Movement is the attack,
    /// so this is where "faster hurts more" actually lives. Engine-free.
    /// </summary>
    public static class DamageLogic
    {
        /// <summary>B2: speed ratio clamped between the configured floor and ceiling.</summary>
        public static float SpeedMultiplier(float speed, float maxSpeed, in DamageConfig config)
        {
            if (maxSpeed <= 1e-6f)
            {
                return config.MinSpeedMultiplier;
            }

            float ratio = speed / maxSpeed;
            if (ratio < config.MinSpeedMultiplier) return config.MinSpeedMultiplier;
            if (ratio > config.MaxSpeedMultiplier) return config.MaxSpeedMultiplier;
            return ratio;
        }

        /// <summary>
        /// B3: dot of travel direction and direction to the enemy, clamped to the
        /// configured floor — a glancing or rear touch still counts for something
        /// and can never go negative.
        /// </summary>
        public static float Frontality(Float3 moveDirection, Float3 toEnemyDirection, in DamageConfig config)
        {
            Float3 move = new Float3(moveDirection.X, 0f, moveDirection.Z).Normalized();
            Float3 toEnemy = new Float3(toEnemyDirection.X, 0f, toEnemyDirection.Z).Normalized();
            if (move.Equals(Float3.Zero) || toEnemy.Equals(Float3.Zero))
            {
                return config.MinFrontality;
            }

            float dot = Float3.Dot(move, toEnemy);
            return dot < config.MinFrontality ? config.MinFrontality : dot;
        }

        /// <summary>B5: fall bonus once the descent is fast enough (DMG-004).</summary>
        public static float FallMultiplier(float verticalVelocity, in DamageConfig config)
        {
            float descent = -verticalVelocity; // downward is negative in world space
            return descent >= config.FallSpeedThreshold ? config.FallMultiplier : 1f;
        }

        /// <summary>
        /// B1: base × speed × frontality × dash × fall × passive.
        /// </summary>
        public static float CollisionDamage(
            float speed,
            float maxSpeed,
            Float3 moveDirection,
            Float3 toEnemyDirection,
            bool isDashing,
            float verticalVelocity,
            float passiveMultiplier,
            in DamageConfig config)
        {
            float damage = config.BaseDamage;
            damage *= SpeedMultiplier(speed, maxSpeed, config);
            damage *= Frontality(moveDirection, toEnemyDirection, config);
            damage *= isDashing ? config.DashMultiplier : 1f;
            damage *= FallMultiplier(verticalVelocity, config);
            damage *= passiveMultiplier <= 0f ? 1f : passiveMultiplier;
            return damage;
        }
    }
}
