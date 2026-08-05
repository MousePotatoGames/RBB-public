using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// The rotation helper shared by WPN-001a attachment push-away and WPN-009
    /// projectile spread (Docs/Features/F10-projectile-weapon.md).
    ///
    /// These are the degenerate cases that produce NaN in naive implementations, and
    /// attachment depends on them: two weapons claimed at the same point must still
    /// be separated.
    /// </summary>
    public sealed class RotationTests
    {
        private static readonly Float3 Forward = new Float3(0f, 0f, 1f);
        private static readonly Float3 Up = new Float3(0f, 1f, 0f);

        [Test]
        public void Rotation_Exactly_RotatesTheRequestedAmount()
        {
            Float3 target = Rotation.AroundAxis(Forward, Up, 50f);
            Float3 result = Rotation.Exactly(Forward, target, degrees: 30f);

            Assert.AreEqual(30f, Rotation.AngleDegrees(Forward, result), 0.01f);
            Assert.AreEqual(1f, result.Magnitude(), 1e-4f, "must stay a unit vector");
        }

        // Exactly() always rotates the full amount even when the two are already
        // closer than that — WPN-001a's push-away depends on exactly this.
        [Test]
        public void Rotation_Exactly_OvershootsWhenCloserThanRequested()
        {
            Float3 near = Rotation.AroundAxis(Forward, Up, 5f);
            Float3 result = Rotation.Exactly(Forward, near, degrees: 50f);

            Assert.AreEqual(50f, Rotation.AngleDegrees(Forward, result), 0.01f);
        }

        [Test]
        public void Rotation_Exactly_Antipodal_StillRotates()
        {
            var back = new Float3(0f, 0f, -1f);
            Float3 result = Rotation.Exactly(Forward, back, degrees: 50f);

            Assert.AreEqual(1f, result.Magnitude(), 1e-4f);
            Assert.AreEqual(50f, Rotation.AngleDegrees(Forward, result), 0.01f);
        }

        [Test]
        public void Rotation_Exactly_Coincident_StillRotates()
        {
            Float3 result = Rotation.Exactly(Forward, Forward, degrees: 50f);

            Assert.AreEqual(1f, result.Magnitude(), 1e-4f);
            Assert.AreEqual(50f, Rotation.AngleDegrees(Forward, result), 0.01f,
                "WPN-001a relies on this: two weapons claimed at the same point must still be pushed apart");
        }

        [Test]
        public void Rotation_Exactly_ZeroStart_FallsBackToTheTarget()
        {
            Float3 result = Rotation.Exactly(Float3.Zero, Forward, degrees: 50f);

            Assert.AreEqual(1f, result.Magnitude(), 1e-4f);
        }

        [Test]
        public void Rotation_AroundAxis_MatchesRequestedAngle()
        {
            Float3 result = Rotation.AroundAxis(Forward, Up, 90f);

            Assert.AreEqual(90f, Rotation.AngleDegrees(Forward, result), 0.01f);
            Assert.AreEqual(1f, result.X, 1e-4f, "+Z yawed 90° about +Y lands on +X");
            Assert.AreEqual(0f, result.Z, 1e-4f);
        }

        [Test]
        public void Rotation_AroundAxis_ZeroAxis_LeavesVectorAlone()
        {
            Float3 result = Rotation.AroundAxis(Forward, Float3.Zero, 90f);

            Assert.AreEqual(0f, Rotation.AngleDegrees(Forward, result), 0.01f);
        }

        [Test]
        public void Rotation_AngleDegrees_ClampsAcosDomain()
        {
            // Dot can drift a hair past ±1 with float error; Acos would return NaN.
            Assert.AreEqual(0f, Rotation.AngleDegrees(Forward, Forward), 0.01f);
            Assert.AreEqual(180f, Rotation.AngleDegrees(Forward, new Float3(0f, 0f, -1f)), 0.01f);
            Assert.AreEqual(0f, Rotation.AngleDegrees(Forward, Float3.Zero), 0.01f,
                "a zero vector has no angle — return 0 rather than NaN");
        }

        [Test]
        public void Rotation_Perpendicular_IsAlwaysPerpendicularAndUnit()
        {
            foreach (Float3 v in new[] { Forward, Up, new Float3(0f, -1f, 0f), new Float3(1f, 1f, 1f) })
            {
                Float3 p = Rotation.Perpendicular(v);
                Assert.AreEqual(1f, p.Magnitude(), 1e-4f);
                Assert.AreEqual(0f, Float3.Dot(p, v.Normalized()), 1e-4f);
            }
        }
    }
}
