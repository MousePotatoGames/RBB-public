using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for F06 collision combat
    /// (Docs/Features/F06-collision-combat.md). Names carry the GAME_RULES id.
    /// </summary>
    public sealed class CombatLogicTests
    {
        private const float MaxSpeed = 12f;

        private static DamageConfig Config => new DamageConfig(
            baseDamage: 10f,
            minSpeedMultiplier: 0.25f,
            maxSpeedMultiplier: 1.5f,
            minFrontality: 0.3f,
            dashMultiplier: 1.6f,
            fallMultiplier: 1.5f,
            fallSpeedThreshold: 6f);

        private static HitStopConfig StopConfig =>
            new HitStopConfig(damageThreshold: 5f, referenceDamage: 15f, minDuration: 0.05f, maxDuration: 0.09f);

        private static readonly Float3 Forward = new Float3(0f, 0f, 1f);

        private static float Damage(float speed, Float3 moveDir, Float3 toEnemy, bool dashing = false, float vertical = 0f, float passive = 1f)
        {
            return DamageLogic.CollisionDamage(speed, MaxSpeed, moveDir, toEnemy, dashing, vertical, passive, Config);
        }

        // B1, B2 — DMG-001
        [Test]
        public void Dmg001_Damage_ScalesWithSpeed()
        {
            float slow = Damage(3f, Forward, Forward);
            float fast = Damage(12f, Forward, Forward);

            Assert.Greater(fast, slow, "DMG-001: faster contact must hurt more — this is the core of the game");
        }

        // B2 — DMG-001
        [Test]
        public void Dmg001_LowSpeed_StillDealsMinimumDamage()
        {
            float crawling = Damage(0.01f, Forward, Forward);

            Assert.Greater(crawling, 0f, "DMG-001: slow contact must not be worthless");
            Assert.That(crawling, Is.EqualTo(Config.BaseDamage * Config.MinSpeedMultiplier).Within(1e-3f));
        }

        // B3 — DMG-001
        [Test]
        public void Dmg001_Frontality_HeadOnBeatsGlancing()
        {
            float headOn = Damage(12f, Forward, Forward);
            float glancing = Damage(12f, Forward, new Float3(1f, 0f, 0f)); // 90°

            Assert.Greater(headOn, glancing, "DMG-001: head-on collision must beat a glancing one");
        }

        // B3 — DMG-001
        [Test]
        public void Dmg001_Frontality_RearContact_ClampedNotNegative()
        {
            float rear = Damage(12f, Forward, new Float3(0f, 0f, -1f)); // enemy behind

            Assert.Greater(rear, 0f, "DMG-001: rear contact must clamp, never go negative");
            Assert.That(rear, Is.EqualTo(Config.BaseDamage * 1f * Config.MinFrontality).Within(1e-3f));
        }

        // B4 — DMG-001 / DASH-001
        [Test]
        public void Dmg001_Dashing_MultipliesDamage()
        {
            float normal = Damage(12f, Forward, Forward);
            float dashing = Damage(12f, Forward, Forward, dashing: true);

            Assert.That(dashing, Is.EqualTo(normal * Config.DashMultiplier).Within(1e-3f));
        }

        // B5 — DMG-004
        [Test]
        public void Dmg004_FallSpeedAboveThreshold_MultipliesDamage()
        {
            float flat = Damage(6f, Forward, Forward, vertical: 0f);
            float falling = Damage(6f, Forward, Forward, vertical: -8f);

            Assert.That(falling, Is.EqualTo(flat * Config.FallMultiplier).Within(1e-3f),
                "DMG-004: a fast descent must add the fall bonus");
        }

        // B5 — DMG-004
        [Test]
        public void Dmg004_FallSpeedBelowThreshold_NoBonus()
        {
            float flat = Damage(6f, Forward, Forward, vertical: 0f);
            float gentle = Damage(6f, Forward, Forward, vertical: -2f);

            Assert.That(gentle, Is.EqualTo(flat).Within(1e-3f), "a gentle drop must not count as a fall attack");
        }

        // B1 — DMG-001 (F12 hook)
        [Test]
        public void Dmg001_PassiveMultiplier_ScalesDamage()
        {
            float baseline = Damage(12f, Forward, Forward, passive: 1f);
            float boosted = Damage(12f, Forward, Forward, passive: 1.5f);

            Assert.That(boosted, Is.EqualTo(baseline * 1.5f).Within(1e-3f));
        }

        // B6 — DMG-002
        [Test]
        public void Dmg002_SameEnemy_WithinCooldown_CannotBeHit()
        {
            const float cooldown = 0.25f;
            float lastHit = 10f;

            Assert.IsFalse(HitCooldownLogic.CanHit(lastHit, 10.1f, cooldown),
                "DMG-002: one collision must not damage every physics frame");
        }

        // B7 — DMG-002
        [Test]
        public void Dmg002_SameEnemy_AfterCooldown_CanBeHitAgain()
        {
            const float cooldown = 0.25f;

            Assert.IsTrue(HitCooldownLogic.CanHit(10f, 10.3f, cooldown));
            Assert.IsTrue(HitCooldownLogic.CanHit(HitCooldownLogic.NeverHit, 0f, cooldown), "a fresh enemy is hittable");
        }

        // B8 — ENM-004
        [Test]
        public void Enm004_Knockback_PointsAwayFromPlayer()
        {
            var playerToEnemy = new Float3(0f, 0f, 1f);

            var impulse = KnockbackLogic.Impulse(playerToEnemy, damage: 10f, referenceDamage: 10f, baseForce: 9f, resistance: 0f);

            Assert.Greater(impulse.Z, 0f, "ENM-004: knockback must push the enemy away from the player");
            Assert.That(impulse.Magnitude(), Is.EqualTo(9f).Within(1e-3f));
            Assert.That(impulse.Y, Is.EqualTo(0f).Within(1e-6f), "knockback stays planar");
        }

        // B9 — DMG-003
        [Test]
        public void Dmg003_Knockback_ReducedByResistance()
        {
            var dir = new Float3(0f, 0f, 1f);

            var light = KnockbackLogic.Impulse(dir, 10f, 10f, 9f, resistance: 0f);
            var heavy = KnockbackLogic.Impulse(dir, 10f, 10f, 9f, resistance: 0.7f);

            Assert.Less(heavy.Magnitude(), light.Magnitude(),
                "DMG-003: heavy enemies are pushed, not launched");
            Assert.That(heavy.Magnitude(), Is.EqualTo(9f * 0.3f).Within(1e-3f));
        }

        // B9 — DMG-003
        [Test]
        public void Dmg003_FullResistance_ProducesNoKnockback()
        {
            var impulse = KnockbackLogic.Impulse(new Float3(0f, 0f, 1f), 10f, 10f, 9f, resistance: 1f);

            Assert.That(impulse.Magnitude(), Is.LessThan(1e-6f), "an immovable target must not be knocked back");
        }

        // B15 — hit stop
        [Test]
        public void HitStop_BelowThreshold_ProducesNoFreeze()
        {
            Assert.That(HitStopLogic.DurationFor(4.9f, StopConfig), Is.EqualTo(0f).Within(1e-6f),
                "weak contact must not freeze the screen");
        }

        // B15, B16 — hit stop
        [Test]
        public void HitStop_Duration_ScalesWithDamage_AndIsCapped()
        {
            float small = HitStopLogic.DurationFor(5f, StopConfig);
            float big = HitStopLogic.DurationFor(15f, StopConfig);
            float huge = HitStopLogic.DurationFor(500f, StopConfig);

            Assert.That(small, Is.EqualTo(StopConfig.MinDuration).Within(1e-4f));
            Assert.That(big, Is.EqualTo(StopConfig.MaxDuration).Within(1e-4f));
            Assert.That(huge, Is.EqualTo(StopConfig.MaxDuration).Within(1e-4f), "duration must be capped");
        }

        // B16 — hit stop
        [Test]
        public void HitStop_Merge_TakesLongerNotSum()
        {
            float merged = HitStopLogic.Merge(0.05f, 0.09f);
            Assert.That(merged, Is.EqualTo(0.09f).Within(1e-6f));

            float mergedSmaller = HitStopLogic.Merge(0.09f, 0.05f);
            Assert.That(mergedSmaller, Is.EqualTo(0.09f).Within(1e-6f),
                "simultaneous kills must not add up into a long freeze");
        }

        // DMG-005 / B20 — refractory period
        [Test]
        public void Dmg005_FirstFreeze_IsAlwaysAllowed()
        {
            Assert.IsTrue(HitStopLogic.CanFreeze(HitStopLogic.NeverFroze, 0f, 0.5f),
                "DMG-005: the very first hit must never be gated");
        }

        // DMG-005 / B20
        [Test]
        public void Dmg005_WithinRefractory_CannotFreezeAgain()
        {
            Assert.IsFalse(HitStopLogic.CanFreeze(lastFreezeTime: 10f, now: 10.2f, refractory: 0.5f),
                "DMG-005: sequential swarm hits must not chain into stutter");
        }

        // DMG-005 / B20
        [Test]
        public void Dmg005_AfterRefractory_CanFreezeAgain()
        {
            Assert.IsTrue(HitStopLogic.CanFreeze(lastFreezeTime: 10f, now: 10.5f, refractory: 0.5f),
                "DMG-005: the gate must open again once the interval has passed");
        }

        // DMG-005 / B20 — a zero interval keeps the old behaviour for callers that opt out
        [Test]
        public void Dmg005_ZeroRefractory_NeverGates()
        {
            Assert.IsTrue(HitStopLogic.CanFreeze(lastFreezeTime: 10f, now: 10.0001f, refractory: 0f));
        }
    }
}
