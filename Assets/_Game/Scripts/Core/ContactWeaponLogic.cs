using System;

namespace Game.Core
{
    /// <summary>How a weapon is held (WPN-007).</summary>
    public enum WeaponMount
    {
        Surface = 0,
        Orbit = 1,
        Follow = 2,
    }

    /// <summary>How a weapon deals damage (WPN-008).</summary>
    public enum WeaponAttack
    {
        Contact = 0,
        Projectile = 1,
        Zap = 2,
    }

    /// <summary>SPK-001 tuning for a contact weapon (all TEMPORARY).</summary>
    public readonly struct ContactWeaponConfig
    {
        /// <summary>Half-angle of the cone in front of the weapon that counts as a hit.</summary>
        public readonly float ArcHalfAngleDegrees;

        public readonly float BonusDamage;
        public readonly float DashDamageMultiplier;
        public readonly float DashKnockbackMultiplier;

        /// <summary>Floor for the speed factor, mirroring DMG-001 so slow contact is not worthless.</summary>
        public readonly float MinSpeedMultiplier;

        public ContactWeaponConfig(
            float arcHalfAngleDegrees,
            float bonusDamage,
            float dashDamageMultiplier,
            float dashKnockbackMultiplier,
            float minSpeedMultiplier)
        {
            ArcHalfAngleDegrees = arcHalfAngleDegrees;
            BonusDamage = bonusDamage;
            DashDamageMultiplier = dashDamageMultiplier;
            DashKnockbackMultiplier = dashKnockbackMultiplier;
            MinSpeedMultiplier = minSpeedMultiplier;
        }
    }

    /// <summary>
    /// SPK-001: the extra damage a surface weapon adds to a body collision.
    ///
    /// The weapon has no collider — a child trigger's events are attributed to the
    /// ball's Rigidbody, which would make a graze read as a body hit. Instead the
    /// enemy must lie inside a cone around the weapon's own direction, which is
    /// also what makes *where* the weapon attached matter (HYP-005).
    /// </summary>
    public static class ContactWeaponLogic
    {
        /// <summary>B3/B4: is the enemy within the weapon's cone? Boundary counts as inside.</summary>
        public static bool InArc(Float3 weaponDirection, Float3 toEnemyDirection, float halfAngleDegrees)
        {
            Float3 weapon = weaponDirection.Normalized();
            Float3 toEnemy = toEnemyDirection.Normalized();
            if (weapon.Equals(Float3.Zero) || toEnemy.Equals(Float3.Zero))
            {
                return false;
            }

            if (halfAngleDegrees >= 180f)
            {
                return true;
            }

            float dot = Float3.Dot(weapon, toEnemy);
            float threshold = (float)Math.Cos(halfAngleDegrees * Math.PI / 180.0);

            // Small epsilon so a contact exactly on the boundary counts (B3).
            return dot >= threshold - 1e-4f;
        }

        /// <summary>
        /// B5/B6: bonus damage for one contact. Returns 0 when the enemy is outside
        /// the cone. Scales with speed and frontality like DMG-001, so a weapon
        /// never rewards standing still.
        /// </summary>
        public static float BonusDamage(
            Float3 weaponDirection,
            Float3 toEnemyDirection,
            float speed,
            float maxSpeed,
            float frontality,
            bool isDashing,
            in ContactWeaponConfig config)
        {
            if (!InArc(weaponDirection, toEnemyDirection, config.ArcHalfAngleDegrees))
            {
                return 0f; // B4
            }

            float speedFactor = maxSpeed <= 1e-6f ? config.MinSpeedMultiplier : speed / maxSpeed;
            if (speedFactor < config.MinSpeedMultiplier)
            {
                speedFactor = config.MinSpeedMultiplier;
            }

            float clampedFrontality = frontality < 0f ? 0f : frontality;

            float damage = config.BonusDamage * speedFactor * clampedFrontality;
            return isDashing ? damage * config.DashDamageMultiplier : damage;
        }

        /// <summary>B6: knockback multiplier this weapon contributes to a landed hit.</summary>
        public static float KnockbackMultiplier(
            Float3 weaponDirection,
            Float3 toEnemyDirection,
            bool isDashing,
            in ContactWeaponConfig config)
        {
            if (!InArc(weaponDirection, toEnemyDirection, config.ArcHalfAngleDegrees))
            {
                return 1f;
            }

            return isDashing ? config.DashKnockbackMultiplier : 1f;
        }
    }
}
