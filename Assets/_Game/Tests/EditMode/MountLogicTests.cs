using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// WPN-007 / WPN-008a coverage (Docs/Features/F12-orbit-follow-mounts.md).
    /// </summary>
    public sealed class MountLogicTests
    {
        private static Float3 At(float x, float z) => new Float3(x, 0f, z);

        // ---- OrbitLogic ----------------------------------------------------------

        [Test]
        public void Wpn007_Orbit_AngleAdvancesWithTime()
        {
            Assert.AreEqual(90f, OrbitLogic.Advance(0f, angularSpeedDegrees: 180f, deltaTime: 0.5f), 1e-3f);
        }

        [Test]
        public void Wpn007_Orbit_AngleWraps()
        {
            // 350 + 180 × 0.1 = 368 -> 8
            Assert.AreEqual(8f, OrbitLogic.Advance(350f, 180f, 0.1f), 1e-3f);
        }

        [Test]
        public void Wpn007_Orbit_NegativeSpeed_WrapsPositive()
        {
            float a = OrbitLogic.Advance(5f, angularSpeedDegrees: -180f, deltaTime: 0.1f);

            Assert.GreaterOrEqual(a, 0f, "angle must stay in [0,360) so downstream trig is stable");
            Assert.Less(a, 360f);
        }

        // B3 — the whole point of driving the angle from elapsed time
        [Test]
        public void Wpn007_Orbit_SameElapsed_SameAngle_RegardlessOfStepCount()
        {
            float coarse = OrbitLogic.Advance(0f, 180f, 1f);

            float fine = 0f;
            for (int i = 0; i < 50; i++)
            {
                fine = OrbitLogic.Advance(fine, 180f, 0.02f);
            }

            Assert.AreEqual(coarse, fine, 1e-2f,
                "WPN-007 B3: one big step and fifty small ones must land on the same angle");
        }

        [Test]
        public void Wpn007_Orbit_PositionIsOnTheCircle()
        {
            var centre = new Float3(3f, 1f, -2f);

            for (float angle = 0f; angle < 360f; angle += 37f)
            {
                Float3 p = OrbitLogic.Position(centre, angle, radius: 2.2f, height: 0f);
                Float3 flat = new Float3(p.X - centre.X, 0f, p.Z - centre.Z);

                Assert.AreEqual(2.2f, flat.Magnitude(), 1e-3f, $"at {angle}°");
            }
        }

        [Test]
        public void Wpn007_Orbit_PositionRespectsHeight()
        {
            Float3 p = OrbitLogic.Position(new Float3(0f, 1f, 0f), 0f, radius: 2f, height: 0.3f);

            Assert.AreEqual(1.3f, p.Y, 1e-3f, "height is relative to the centre, not absolute");
        }

        [Test]
        public void Wpn007_Orbit_MovesCentreWithPlayer()
        {
            Float3 a = OrbitLogic.Position(At(0f, 0f), 90f, 2f, 0f);
            Float3 b = OrbitLogic.Position(At(10f, 0f), 90f, 2f, 0f);

            Assert.AreEqual(10f, b.X - a.X, 1e-3f,
                "the orbit tracks a moving player because the centre is an argument, not state");
        }

        [Test]
        public void Wpn007_Orbit_AngleZero_IsOnPositiveX()
        {
            Float3 p = OrbitLogic.Position(Float3.Zero, 0f, radius: 2f, height: 0f);

            Assert.AreEqual(2f, p.X, 1e-3f);
            Assert.AreEqual(0f, p.Z, 1e-3f);
        }

        // ---- FollowLogic ---------------------------------------------------------

        [Test]
        public void Wpn007_Follow_ApproachesButDoesNotSnap()
        {
            Float3 current = At(0f, -10f);
            Float3 desired = At(0f, -2f);

            Float3 next = FollowLogic.Step(current, desired, speed: 6f, deltaTime: 0.02f);

            Assert.Greater(next.Z, current.Z, "it must move toward the target");
            Assert.Less(next.Z, desired.Z, "WPN-007 B4: but never arrive in one step — the lag is the feature");
        }

        [Test]
        public void Wpn007_Follow_ConvergesToStandoffDistance()
        {
            Float3 player = Float3.Zero;
            Float3 pet = At(0f, -10f);

            for (int i = 0; i < 300; i++)
            {
                Float3 desired = FollowLogic.DesiredPosition(pet, player, standoff: 1.8f);
                pet = FollowLogic.Step(pet, desired, speed: 6f, deltaTime: 0.02f);
            }

            Assert.AreEqual(1.8f, (pet - player).Magnitude(), 0.05f,
                "WPN-007 B5: the pet settles at the standoff distance, not on top of the player");
        }

        // B6 — same reason as the orbit test
        [Test]
        public void Wpn007_Follow_SameElapsed_SameResult_RegardlessOfStepCount()
        {
            Float3 desired = At(0f, 0f);

            Float3 coarse = FollowLogic.Step(At(0f, -10f), desired, speed: 6f, deltaTime: 1f);

            Float3 fine = At(0f, -10f);
            for (int i = 0; i < 50; i++)
            {
                fine = FollowLogic.Step(fine, desired, speed: 6f, deltaTime: 0.02f);
            }

            Assert.AreEqual(coarse.Z, fine.Z, 1e-2f,
                "WPN-007 B6: a fixed-step lag would trail further at low frame rates");
        }

        [Test]
        public void Wpn007_Follow_AtTarget_DoesNotJitter()
        {
            Float3 desired = At(0f, -1.8f);
            Float3 next = FollowLogic.Step(desired, desired, speed: 6f, deltaTime: 0.02f);

            Assert.AreEqual(desired.Z, next.Z, 1e-5f);
        }

        [Test]
        public void Wpn007_Follow_ExactlyOnPlayer_PicksADirection()
        {
            // No direction to back off along — must still produce a finite point.
            Float3 desired = FollowLogic.DesiredPosition(Float3.Zero, Float3.Zero, standoff: 1.8f);

            Assert.AreEqual(1.8f, desired.Magnitude(), 1e-3f,
                "a degenerate overlap must not leave the pet stuck inside the ball");
        }

        [Test]
        public void Wpn007_Follow_ZeroSpeed_DoesNotMove()
        {
            Float3 current = At(0f, -5f);

            Assert.AreEqual(current.Z, FollowLogic.Step(current, Float3.Zero, speed: 0f, deltaTime: 0.02f).Z, 1e-5f);
        }

        // ---- SweepLogic (WPN-008a) -----------------------------------------------

        [Test]
        public void Sweep_AllInRange_ReturnsEveryoneInside()
        {
            var positions = new[] { At(0.5f, 0f), At(5f, 0f), At(0f, 0.6f) };
            var into = new int[8];

            int n = SweepLogic.AllInRange(Float3.Zero, positions, positions.Length, radius: 0.7f, into);

            Assert.AreEqual(2, n, "WPN-008a: a swinging weapon hits everyone in reach, not just the nearest");
            Assert.AreEqual(0, into[0]);
            Assert.AreEqual(2, into[1]);
        }

        [Test]
        public void Sweep_AllInRange_ExcludesOutside()
        {
            var positions = new[] { At(5f, 0f), At(9f, 0f) };
            var into = new int[8];

            Assert.AreEqual(0, SweepLogic.AllInRange(Float3.Zero, positions, positions.Length, 0.7f, into));
        }

        [Test]
        public void Sweep_AllInRange_ExactlyAtRadius_IsIncluded()
        {
            var positions = new[] { At(0.7f, 0f) };
            var into = new int[4];

            Assert.AreEqual(1, SweepLogic.AllInRange(Float3.Zero, positions, 1, radius: 0.7f, into),
                "the boundary must not flicker");
        }

        [Test]
        public void Sweep_AllInRange_ClampsToBuffer()
        {
            var positions = new[] { At(0.1f, 0f), At(0.2f, 0f), At(0.3f, 0f) };
            var into = new int[2];

            Assert.AreEqual(2, SweepLogic.AllInRange(Float3.Zero, positions, positions.Length, 1f, into),
                "must never write past the caller's buffer");
        }

        [Test]
        public void Sweep_AllInRange_OnlyConsidersLiveCount()
        {
            // A pooled list reuses its array: entries past `count` are recycled enemies.
            var positions = new[] { At(0.1f, 0f), At(0.2f, 0f) };
            var into = new int[4];

            Assert.AreEqual(1, SweepLogic.AllInRange(Float3.Zero, positions, count: 1, radius: 1f, into));
        }

        [Test]
        public void Sweep_AllInRange_DegenerateInputs_ReturnNothing()
        {
            var into = new int[4];

            Assert.AreEqual(0, SweepLogic.AllInRange(Float3.Zero, null, 3, 1f, into));
            Assert.AreEqual(0, SweepLogic.AllInRange(Float3.Zero, new[] { At(0f, 0f) }, 1, 1f, null));
            Assert.AreEqual(0, SweepLogic.AllInRange(Float3.Zero, new[] { At(0f, 0f) }, 0, 1f, into));
            Assert.AreEqual(0, SweepLogic.AllInRange(Float3.Zero, new[] { At(0f, 0f) }, 1, 0f, into));
        }
    }
}
