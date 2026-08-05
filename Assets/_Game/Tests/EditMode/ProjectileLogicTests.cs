using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// CAN-001 / WPN-009 coverage (Docs/Features/F10-projectile-weapon.md).
    /// </summary>
    public sealed class ProjectileLogicTests
    {
        private static readonly Float3 Forward = new Float3(0f, 0f, 1f);
        private static readonly Float3 Up = new Float3(0f, 1f, 0f);

        private static ProjectileConfig Config(
            float interval = 0.7f,
            int shots = 1,
            float spread = 0f) =>
            new ProjectileConfig(
                travel: ProjectileTravel.Straight,
                fireInterval: interval,
                shotsPerBurst: shots,
                spreadDegrees: spread,
                speed: 18f,
                range: 14f,
                pierce: 0,
                damage: 6f);

        // ---- B2/B3 firing condition — the cooldown is the whole gate --------------

        [Test]
        public void Can001_FirstShot_IsNotGated()
        {
            Assert.IsTrue(ProjectileLogic.CanFire(ProjectileLogic.NeverFired, now: 0f, interval: 0.7f),
                "CAN-001: the very first shot must not wait out a cooldown that never started");
        }

        [Test]
        public void Can001_OffCooldown_CanFire()
        {
            Assert.IsTrue(ProjectileLogic.CanFire(lastFireTime: 1f, now: 1.7f, interval: 0.7f),
                "CAN-001: exactly one interval later counts as off cooldown");
        }

        [Test]
        public void Can001_OnCooldown_CannotFire()
        {
            Assert.IsFalse(ProjectileLogic.CanFire(lastFireTime: 1f, now: 1.3f, interval: 0.7f));
        }

        [Test]
        public void Can001_ZeroInterval_AlwaysFires()
        {
            Assert.IsTrue(ProjectileLogic.CanFire(lastFireTime: 1f, now: 1f, interval: 0f));
        }

        // ---- B5/B7: no aiming. The barrel direction is the shot direction --------

        [Test]
        public void Wpn009_SingleShot_UsesExactBarrelDirection()
        {
            var into = new Float3[8];
            int count = ProjectileLogic.SpreadDirections(Forward, Up, count: 1, spreadDegrees: 30f, into);

            Assert.AreEqual(1, count);
            Assert.AreEqual(0f, Rotation.AngleDegrees(Forward, into[0]), 0.01f,
                "CAN-001 B5/B7: one shot goes exactly down the barrel — no spread, no aim correction");
        }

        [Test]
        public void Wpn009_BurstIsCentredOnTheBarrel()
        {
            var into = new Float3[8];
            int count = ProjectileLogic.SpreadDirections(Forward, Up, count: 5, spreadDegrees: 40f, into);

            // The middle shot of an odd burst is the barrel direction itself.
            Assert.AreEqual(0f, Rotation.AngleDegrees(Forward, into[count / 2]), 0.01f,
                "a burst must straddle the barrel, not lead or lag it");
        }

        // ---- B6: buckshot is a number, not a code path ---------------------------

        [Test]
        public void Wpn009_Shotgun_SpreadsEvenlyAcrossAngle()
        {
            var into = new Float3[8];
            int count = ProjectileLogic.SpreadDirections(Forward, Up, count: 5, spreadDegrees: 40f, into);

            Assert.AreEqual(5, count);

            // 5 shots across 40° sit 10° apart.
            for (int i = 1; i < count; i++)
            {
                Assert.AreEqual(10f, Rotation.AngleDegrees(into[i - 1], into[i]), 0.01f,
                    $"WPN-009: shot {i} should be one even step from its neighbour");
            }
        }

        [Test]
        public void Wpn009_Shotgun_OutermostShots_MatchHalfSpread()
        {
            var into = new Float3[8];
            int count = ProjectileLogic.SpreadDirections(Forward, Up, count: 5, spreadDegrees: 40f, into);

            Assert.AreEqual(20f, Rotation.AngleDegrees(Forward, into[0]), 0.01f);
            Assert.AreEqual(20f, Rotation.AngleDegrees(Forward, into[count - 1]), 0.01f);
            Assert.AreEqual(40f, Rotation.AngleDegrees(into[0], into[count - 1]), 0.01f,
                "WPN-009: the fan's total width must equal the configured spread");
        }

        [Test]
        public void Wpn009_ShotCount_ClampedToBurstCeiling()
        {
            var into = new Float3[64];
            int count = ProjectileLogic.SpreadDirections(Forward, Up, count: 100, spreadDegrees: 40f, into);

            Assert.AreEqual(ProjectileLogic.MaxShotsPerBurst, count,
                "WPN-009 Exception: one burst must never exceed the ceiling, or buckshot × fire rate drains the pool");
        }

        [Test]
        public void Wpn009_ShotCount_ClampedToBufferLength()
        {
            var into = new Float3[3];
            int count = ProjectileLogic.SpreadDirections(Forward, Up, count: 8, spreadDegrees: 40f, into);

            Assert.AreEqual(3, count, "must never write past the caller's buffer");
        }

        [Test]
        public void Wpn009_ZeroShots_WritesNothing()
        {
            var into = new Float3[4];
            Assert.AreEqual(0, ProjectileLogic.SpreadDirections(Forward, Up, count: 0, spreadDegrees: 40f, into));
            Assert.AreEqual(0, ProjectileLogic.SpreadDirections(Float3.Zero, Up, count: 3, spreadDegrees: 40f, into),
                "a zero barrel direction carries no angle — fire nothing rather than a random direction");
        }

        // ---- B10 travel ----------------------------------------------------------

        [Test]
        public void Wpn009_Advance_MovesBySpeedTimesDelta()
        {
            Float3 moved = ProjectileLogic.Advance(Float3.Zero, Forward, speed: 18f, deltaTime: 0.02f);

            Assert.AreEqual(0.36f, moved.Magnitude(), 1e-4f);
            Assert.AreEqual(0f, Rotation.AngleDegrees(Forward, moved), 0.01f);
        }

        [Test]
        public void Wpn009_Advance_NormalisesDirection()
        {
            Float3 moved = ProjectileLogic.Advance(Float3.Zero, Forward * 7f, speed: 18f, deltaTime: 0.02f);

            Assert.AreEqual(0.36f, moved.Magnitude(), 1e-4f,
                "an unnormalised direction must not multiply the speed");
        }

        // ---- config plumbing -----------------------------------------------------

        [Test]
        public void Wpn009_Config_CarriesTheNumbersThatDefineTheWeapon()
        {
            ProjectileConfig c = Config(interval: 0.6f, shots: 5, spread: 30f);

            Assert.AreEqual(0.6f, c.FireInterval, 1e-4f);
            Assert.AreEqual(5, c.ShotsPerBurst);
            Assert.AreEqual(30f, c.SpreadDegrees, 1e-4f);
            Assert.AreEqual(ProjectileTravel.Straight, c.Travel);
        }
    }
}
