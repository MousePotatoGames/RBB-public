using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for F01 ball movement (Docs/Features/F01-ball-movement.md).
    /// Test names carry the GAME_RULES id they verify (MOVE-001/004/005).
    /// </summary>
    public sealed class BallMovementLogicTests
    {
        private static MoveConfig DefaultConfig => new MoveConfig(maxSpeed: 12f, acceleration: 10f, slopeAssist: 1f);

        private static Float3 CamForward => new Float3(0f, 0f, 1f);
        private static Float3 CamRight => new Float3(1f, 0f, 0f);

        private static void AssertNear(float expected, float actual, float tolerance, string message = null)
        {
            Assert.That(actual, Is.EqualTo(expected).Within(tolerance), message);
        }

        // B1, B2 — MOVE-001
        [Test]
        public void Move001_ForwardInput_MatchesCameraForwardOnPlane()
        {
            // Camera tilted down 45° (quarter view): forward has -Y, must flatten to +Z.
            var tiltedForward = new Float3(0f, -0.7071f, 0.7071f);
            var dir = BallMovementLogic.CameraRelativeDirection(tiltedForward, CamRight, 0f, 1f);

            AssertNear(0f, dir.X, 1e-4f);
            AssertNear(0f, dir.Y, 1e-4f, "movement direction must stay on the ground plane");
            AssertNear(1f, dir.Z, 1e-4f);
        }

        // B2 — MOVE-001
        [Test]
        public void Move001_RotatedCamera_RemapsInputDirection()
        {
            // Camera yawed 90°: forward = +X, right = -Z. W must now move along +X.
            var forward = new Float3(1f, 0f, 0f);
            var right = new Float3(0f, 0f, -1f);

            var dir = BallMovementLogic.CameraRelativeDirection(forward, right, 0f, 1f);

            AssertNear(1f, dir.X, 1e-4f);
            AssertNear(0f, dir.Z, 1e-4f);
        }

        // B7 — MOVE-001
        [Test]
        public void Move001_DiagonalInput_IsNormalized()
        {
            var dir = BallMovementLogic.CameraRelativeDirection(CamForward, CamRight, 1f, 1f);

            AssertNear(1f, dir.Magnitude(), 1e-4f, "diagonal input must not be faster than cardinal input");
        }

        // B5 — MOVE-001 (inertia: logic must not brake)
        [Test]
        public void Move001_NoInput_ProducesZeroVelocityChange()
        {
            var rollingVelocity = new Float3(5f, 0f, 3f);

            var dv = BallMovementLogic.ComputeVelocityChange(rollingVelocity, Float3.Zero, Float3.Up, DefaultConfig, 0.02f);

            Assert.That(dv.Magnitude(), Is.LessThan(1e-6f), "no input must apply no artificial braking");
        }

        // B3 — MOVE-005 (relationship, not exact value: reaches max in roughly maxSpeed/acceleration)
        [Test]
        public void Move005_ReachesMaxSpeed_WithinConfiguredTime()
        {
            var config = DefaultConfig; // 12 m/s at 10 m/s^2 → nominal 1.2 s
            float nominalTime = config.MaxSpeed / config.Acceleration;
            var velocity = Float3.Zero;
            var input = new Float3(0f, 0f, 1f);
            const float dt = 0.02f;

            float elapsed = 0f;
            while (velocity.Magnitude() < config.MaxSpeed * 0.99f && elapsed < nominalTime * 2f)
            {
                velocity += BallMovementLogic.ComputeVelocityChange(velocity, input, Float3.Up, config, dt);
                elapsed += dt;
            }

            Assert.That(elapsed, Is.InRange(nominalTime * 0.8f, nominalTime * 1.2f),
                "time to max speed must track maxSpeed/acceleration (±20%)");
        }

        // B4 — MOVE-005
        [Test]
        public void Move005_VelocityChange_NeverPushesBeyondMaxSpeed()
        {
            var config = DefaultConfig;
            var velocity = Float3.Zero;
            var input = new Float3(0.3f, 0f, 0.9f);
            const float dt = 0.02f;

            for (int i = 0; i < 500; i++)
            {
                velocity += BallMovementLogic.ComputeVelocityChange(velocity, input, Float3.Up, config, dt);
                Assert.That(velocity.Magnitude(), Is.LessThanOrEqualTo(config.MaxSpeed + 1e-3f),
                    $"planar speed exceeded max at step {i}");
            }
        }

        // B4 — MOVE-005 (already-faster body is not braked: dash compatibility)
        [Test]
        public void Move005_FasterThanMaxBody_IsNotBraked()
        {
            var config = DefaultConfig;
            var dashVelocity = new Float3(0f, 0f, 20f); // beyond max (dash, F03)

            var dv = BallMovementLogic.ComputeVelocityChange(dashVelocity, new Float3(0f, 0f, 1f), Float3.Up, config, 0.02f);
            var after = dashVelocity + dv;

            Assert.That(after.Magnitude(), Is.EqualTo(20f).Within(1e-3f),
                "input must neither accelerate nor brake a body already above max speed");
        }

        // B6 — MOVE-004
        [Test]
        public void Move004_SlopeProjection_KeepsMagnitude_AndPerpendicularToNormal()
        {
            // 30° slope facing -Z: normal tilted from up toward +Z.
            var normal = new Float3(0f, 0.8660f, 0.5f).Normalized();
            var flatDir = new Float3(0f, 0f, -1f);

            var slopeDir = BallMovementLogic.ProjectOnSlope(flatDir, normal);

            AssertNear(1f, slopeDir.Magnitude(), 1e-3f, "projection must preserve input magnitude");
            AssertNear(0f, Float3.Dot(slopeDir, normal), 1e-3f, "projected direction must lie on the slope plane");
        }

        // B6 — MOVE-004
        [Test]
        public void Move004_SlopeAssist_ZeroOnFlatGround()
        {
            var gravity = new Float3(0f, -9.81f, 0f);

            var assist = BallMovementLogic.SlopeAssistForce(gravity, Float3.Up, slopeAssist: 1f);

            Assert.That(assist.Magnitude(), Is.LessThan(1e-6f), "no assist force on flat ground");
        }

        // B6 — MOVE-004 (assist opposes the downhill pull)
        [Test]
        public void Move004_SlopeAssist_OpposesDownhillGravity()
        {
            var gravity = new Float3(0f, -9.81f, 0f);
            var normal = new Float3(0f, 0.8660f, 0.5f).Normalized();

            var downhill = gravity.OnPlane(normal);
            var assist = BallMovementLogic.SlopeAssistForce(gravity, normal, slopeAssist: 1f);

            AssertNear(-1f, Float3.Dot(assist.Normalized(), downhill.Normalized()), 1e-3f,
                "assist must point exactly against the along-slope gravity component");
        }
    }
}
