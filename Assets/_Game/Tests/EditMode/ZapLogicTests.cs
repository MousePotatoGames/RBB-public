using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// TES-001 / WPN-008 coverage (Docs/Features/F11-tesla-zap.md).
    /// </summary>
    public sealed class ZapLogicTests
    {
        private static ZapConfig Config(float interval = 0.9f, float range = 5f, float max = 12f, float min = 4f) =>
            new ZapConfig(interval, range, max, min);

        private static Float3 At(float z) => new Float3(0f, 0f, z);

        // ---- B1 interval ---------------------------------------------------------

        [Test]
        public void Tes001_FirstZap_IsNotGated()
        {
            Assert.IsTrue(ZapLogic.CanZap(ZapLogic.NeverZapped, now: 0f, interval: 0.9f),
                "TES-001: the first discharge must not wait out an interval that never started");
        }

        [Test]
        public void Tes001_OffCooldown_CanZap()
        {
            Assert.IsTrue(ZapLogic.CanZap(lastZapTime: 1f, now: 1.9f, interval: 0.9f));
        }

        [Test]
        public void Tes001_OnCooldown_CannotZap()
        {
            Assert.IsFalse(ZapLogic.CanZap(lastZapTime: 1f, now: 1.5f, interval: 0.9f));
        }

        // ---- B2/B3 target selection ----------------------------------------------

        [Test]
        public void Tes001_NearestInRange_IsChosen()
        {
            var positions = new[] { At(4f), At(1.5f), At(3f) };

            int i = ZapLogic.NearestInRange(Float3.Zero, positions, positions.Length, range: 5f, out float d);

            Assert.AreEqual(1, i);
            Assert.AreEqual(1.5f, d, 1e-4f);
        }

        [Test]
        public void Tes001_NoEnemyInRange_ReturnsNone()
        {
            var positions = new[] { At(9f), At(12f) };

            int i = ZapLogic.NearestInRange(Float3.Zero, positions, positions.Length, range: 5f, out float d);

            Assert.AreEqual(ZapLogic.NoTarget, i,
                "TES-001 B3: nothing in reach means no discharge — the interval still runs");
            Assert.AreEqual(0f, d, 1e-4f, "no target carries no distance");
        }

        [Test]
        public void Tes001_ExactlyAtRange_CountsAsInRange()
        {
            var positions = new[] { At(5f) };

            int i = ZapLogic.NearestInRange(Float3.Zero, positions, 1, range: 5f, out float d);

            Assert.AreEqual(0, i, "the boundary must not flicker between in and out");
            Assert.AreEqual(5f, d, 1e-4f);
        }

        [Test]
        public void Tes001_ReturnsDistanceOfChosenTarget()
        {
            // The distance is the input to Damage(), so a wrong one is a silent
            // damage bug rather than a visible targeting bug.
            var positions = new[] { new Float3(3f, 0f, 4f) };

            ZapLogic.NearestInRange(Float3.Zero, positions, 1, range: 6f, out float d);

            Assert.AreEqual(5f, d, 1e-4f, "3-4-5 triangle");
        }

        [Test]
        public void Tes001_OriginIsRespected()
        {
            // B5: the ring's attachment point, not the ball centre.
            var positions = new[] { At(6f) };
            var weapon = At(2f);

            int i = ZapLogic.NearestInRange(weapon, positions, 1, range: 5f, out float d);

            Assert.AreEqual(0, i, "4m from the weapon is in reach even though it is 6m from the origin");
            Assert.AreEqual(4f, d, 1e-4f);
        }

        [Test]
        public void Tes001_EmptyList_ReturnsNone()
        {
            Assert.AreEqual(ZapLogic.NoTarget,
                ZapLogic.NearestInRange(Float3.Zero, new Float3[4], count: 0, range: 5f, out _));
            Assert.AreEqual(ZapLogic.NoTarget,
                ZapLogic.NearestInRange(Float3.Zero, null, count: 3, range: 5f, out _));
        }

        [Test]
        public void Tes001_OnlyConsidersTheLiveCount()
        {
            // A pooled list reuses its array: entries past `count` are recycled enemies.
            var positions = new[] { At(4f), At(0.5f) };

            int i = ZapLogic.NearestInRange(Float3.Zero, positions, count: 1, range: 5f, out float d);

            Assert.AreEqual(0, i, "reading past the live count would zap a recycled enemy");
            Assert.AreEqual(4f, d, 1e-4f);
        }

        [Test]
        public void Tes001_CountBeyondArray_IsClamped()
        {
            var positions = new[] { At(2f) };

            Assert.AreEqual(0, ZapLogic.NearestInRange(Float3.Zero, positions, count: 99, range: 5f, out _));
        }

        // ---- B4 distance falloff — the weapon's identity -------------------------

        [Test]
        public void Tes001_Damage_MaxAtZeroDistance()
        {
            Assert.AreEqual(12f, ZapLogic.Damage(0f, Config()), 1e-4f);
        }

        [Test]
        public void Tes001_Damage_MinAtMaxRange()
        {
            Assert.AreEqual(4f, ZapLogic.Damage(5f, Config()), 1e-4f);
        }

        [Test]
        public void Tes001_Damage_FallsOffWithDistance()
        {
            ZapConfig config = Config();
            float previous = float.MaxValue;

            for (float d = 0f; d <= 5f; d += 0.5f)
            {
                float damage = ZapLogic.Damage(d, config);
                Assert.Less(damage, previous,
                    $"TES-001 B4: damage must keep dropping as distance grows (at {d}m)");
                previous = damage;
            }
        }

        [Test]
        public void Tes001_Damage_HalfRange_IsHalfway()
        {
            Assert.AreEqual(8f, ZapLogic.Damage(2.5f, Config()), 1e-4f, "linear falloff between 12 and 4");
        }

        [Test]
        public void Tes001_Damage_BeyondRange_ClampsToMin()
        {
            Assert.AreEqual(4f, ZapLogic.Damage(50f, Config()), 1e-4f,
                "a caller that fired out of range must still never produce negative damage");
        }

        [Test]
        public void Tes001_Damage_NegativeDistance_ClampsToMax()
        {
            Assert.AreEqual(12f, ZapLogic.Damage(-1f, Config()), 1e-4f);
        }

        [Test]
        public void Tes001_Damage_ZeroRange_IsMax()
        {
            Assert.AreEqual(12f, ZapLogic.Damage(3f, Config(range: 0f)), 1e-4f,
                "a degenerate config must not divide by zero");
        }

        // The gradient is the whole reason this weapon differs from a short cannon.
        [Test]
        public void Tes001_ContactIsWorthSeveralTimesTheEdge()
        {
            ZapConfig config = Config();

            float contact = ZapLogic.Damage(0f, config);
            float edge = ZapLogic.Damage(config.Range, config);

            Assert.GreaterOrEqual(contact / edge, 2f,
                "TES-001: if closing the distance is not clearly worth it, the tesla is just a cannon that cannot miss");
        }
    }
}
