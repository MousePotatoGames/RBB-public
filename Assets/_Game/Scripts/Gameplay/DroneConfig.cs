using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// F05 TEMPORARY tuning for drones, spawning and player health.
    /// Single home for the values — no magic numbers in code.
    /// </summary>
    [CreateAssetMenu(menuName = "RumbleBall/Drone Config", fileName = "DroneConfig")]
    public sealed class DroneConfig : ScriptableObject
    {
        [Header("ENM-001 — drone movement (TEMPORARY)")]
        [Min(0.1f)] public float moveSpeed = 4f;

        [Tooltip("Damage dealt to the player on contact (HP-002)")]
        [Min(0f)] public float contactDamage = 8f;

        [Header("ENM-004 — separation (TEMPORARY)")]
        [Min(0f)] public float minSeparation = 1.2f;

        [Tooltip("Separation speed as a fraction of chase speed — must stay below 1")]
        [Range(0f, 1f)] public float separationStrength = 0.5f;

        [Header("WAVE-001 — spawn budget (TEMPORARY, 90s FP curve)")]
        [Min(0)] public int startCount = 5;
        [Min(0)] public int endCount = 25;
        [Min(1f)] public float rampSeconds = 90f;
        [Min(1)] public int hardCap = 30;
        [Min(0.02f)] public float spawnInterval = 0.35f;

        [Header("WAVE-002 — spawn placement (TEMPORARY)")]
        [Min(1f)] public float ringRadiusMin = 18f;
        [Min(1f)] public float ringRadiusMax = 26f;

        [Tooltip("Half-angle of the camera front cone where spawns are forbidden")]
        [Range(0f, 90f)] public float cameraConeHalfAngle = 55f;

        [Tooltip("Angle step between successive spawns (golden angle spreads them)")]
        public float angleStep = 137f;

        [Tooltip("Drones beyond this distance from the player return to the pool")]
        [Min(5f)] public float despawnDistance = 45f;

        [Header("HP-001 / HP-002 — player health (TEMPORARY)")]
        [Min(1f)] public float playerMaxHealth = 100f;

        [Tooltip("Invulnerability window after a hit (range 0.35~0.55)")]
        [Min(0f)] public float invulnerabilityDuration = 0.45f;

        [Header("F06 — enemy health & collision damage (TEMPORARY)")]
        [Min(1f)] public float droneMaxHealth = 10f;
        [Min(0f)] public float baseCollisionDamage = 10f;

        [Tooltip("Floor for the speed factor — slow contact must not be worthless (DMG-001)")]
        [Range(0f, 1f)] public float minSpeedMultiplier = 0.25f;

        [Tooltip("Ceiling for the speed factor — rewards dash overspeed")]
        [Min(1f)] public float maxSpeedMultiplier = 1.5f;

        [Tooltip("Floor for frontality so side/rear contact never zeroes damage")]
        [Range(0f, 1f)] public float minFrontality = 0.3f;

        [Min(1f)] public float dashDamageMultiplier = 1.6f;
        [Min(1f)] public float fallDamageMultiplier = 1.5f;
        [Min(0f)] public float fallSpeedThreshold = 6f;

        [Tooltip("Per-enemy hit cooldown (DMG-002)")]
        [Min(0.01f)] public float hitCooldown = 0.25f;

        [Header("F06 — knockback & death (TEMPORARY)")]
        [Min(0f)] public float knockbackForce = 9f;

        [Tooltip("0 = flies freely, 1 = immovable (DMG-003)")]
        [Range(0f, 1f)] public float knockbackResistance;

        [Tooltip("Seconds spent dynamic after a non-lethal hit before resuming the chase")]
        [Min(0f)] public float knockbackRecovery = 0.35f;

        [Tooltip("Seconds a corpse stays before returning to the pool (ENM-004)")]
        [Min(0f)] public float corpseTime = 1f;

        [Header("F06 — hit stop (TEMPORARY, 기획서 11.2)")]
        [Min(0f)] public float hitStopDamageThreshold = 5f;
        [Min(0.1f)] public float hitStopReferenceDamage = 15f;
        [Min(0f)] public float hitStopMinDuration = 0.05f;
        [Min(0f)] public float hitStopMaxDuration = 0.09f;

        public DamageConfig ToDamageConfig() => new DamageConfig(
            baseCollisionDamage,
            minSpeedMultiplier,
            maxSpeedMultiplier,
            minFrontality,
            dashDamageMultiplier,
            fallDamageMultiplier,
            fallSpeedThreshold);

        public HitStopConfig ToHitStopConfig() =>
            new HitStopConfig(hitStopDamageThreshold, hitStopReferenceDamage, hitStopMinDuration, hitStopMaxDuration);

        public SpawnBudgetConfig ToSpawnBudgetConfig() =>
            new SpawnBudgetConfig(startCount, endCount, rampSeconds, hardCap, spawnInterval);

        public HealthConfig ToHealthConfig() =>
            new HealthConfig(playerMaxHealth, invulnerabilityDuration);
    }
}
