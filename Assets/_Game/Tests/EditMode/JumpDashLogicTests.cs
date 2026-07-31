using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// EditMode coverage for F03 jump / dash / steering damping
    /// (Docs/Features/F03-jump-dash.md). Test names carry the GAME_RULES id.
    /// </summary>
    public sealed class JumpDashLogicTests
    {
        private const float Dt = 0.02f;

        private static JumpConfig Jump => new JumpConfig(coyoteTime: 0.1f, bufferTime: 0.12f);
        private static DashConfig Dash => new DashConfig(dashSpeed: 8f, cooldown: 1.8f);

        private static JumpState Advance(JumpState state, float seconds, bool grounded, bool pressed = false)
        {
            for (float t = 0f; t < seconds; t += Dt)
            {
                state = JumpLogic.Step(state, grounded, pressed, Dt);
                pressed = false; // a press is a single-step edge
            }

            return state;
        }

        // B1 — JUMP-001
        [Test]
        public void Jump001_Grounded_AllowsJump()
        {
            var state = JumpLogic.Step(JumpState.Initial, isGrounded: true, pressedThisStep: true, Dt);

            Assert.IsTrue(JumpLogic.ShouldJump(state, Jump), "grounded press must be allowed to jump");
        }

        // B2 — JUMP-001
        [Test]
        public void Jump001_CoyoteTime_AllowsJumpShortlyAfterLeavingGround()
        {
            var state = JumpLogic.Step(JumpState.Initial, isGrounded: true, pressedThisStep: false, Dt);
            state = Advance(state, 0.06f, grounded: false);          // inside the 0.1s window
            state = JumpLogic.Step(state, isGrounded: false, pressedThisStep: true, Dt);

            Assert.IsTrue(JumpLogic.ShouldJump(state, Jump), "JUMP-001: coyote time must still allow the jump");
        }

        // B2 — JUMP-001
        [Test]
        public void Jump001_AfterCoyoteWindow_DeniesJump()
        {
            var state = JumpLogic.Step(JumpState.Initial, isGrounded: true, pressedThisStep: false, Dt);
            state = Advance(state, 0.3f, grounded: false);           // well past 0.1s
            state = JumpLogic.Step(state, isGrounded: false, pressedThisStep: true, Dt);

            Assert.IsFalse(JumpLogic.ShouldJump(state, Jump), "JUMP-001: coyote window must expire");
        }

        // B3 — JUMP-001
        [Test]
        public void Jump001_InAir_DeniesSecondJump()
        {
            var state = JumpLogic.Step(JumpState.Initial, isGrounded: true, pressedThisStep: true, Dt);
            Assert.IsTrue(JumpLogic.ShouldJump(state, Jump));
            state = JumpLogic.ConsumeJump(state);

            state = Advance(state, 0.05f, grounded: false);          // still inside coyote window
            state = JumpLogic.Step(state, isGrounded: false, pressedThisStep: true, Dt);

            Assert.IsFalse(JumpLogic.ShouldJump(state, Jump), "JUMP-001: no double jump before landing");
        }

        // B3 — JUMP-001
        [Test]
        public void Jump001_Landing_RestoresJump()
        {
            var state = JumpLogic.Step(JumpState.Initial, isGrounded: true, pressedThisStep: true, Dt);
            state = JumpLogic.ConsumeJump(state);
            state = Advance(state, 0.5f, grounded: false);

            state = JumpLogic.Step(state, isGrounded: true, pressedThisStep: true, Dt); // landed and pressed

            Assert.IsTrue(JumpLogic.ShouldJump(state, Jump), "JUMP-001: landing must clear the once-per-airtime latch");
        }

        // B4 — JUMP-001
        [Test]
        public void Jump001_BufferedInput_FiresOnLanding()
        {
            var state = Advance(JumpState.Initial, 0.4f, grounded: false);
            state = JumpLogic.Step(state, isGrounded: false, pressedThisStep: true, Dt); // pressed just before landing
            Assert.IsFalse(JumpLogic.ShouldJump(state, Jump), "airborne press must not fire immediately");

            state = JumpLogic.Step(state, isGrounded: true, pressedThisStep: false, Dt); // lands 1 step later

            Assert.IsTrue(JumpLogic.ShouldJump(state, Jump), "JUMP-001: buffered press must fire on landing");
        }

        // B4 — JUMP-001
        [Test]
        public void Jump001_StaleBufferedInput_DoesNotFireOnLanding()
        {
            var state = Advance(JumpState.Initial, 0.2f, grounded: false);
            state = JumpLogic.Step(state, isGrounded: false, pressedThisStep: true, Dt);
            state = Advance(state, 0.4f, grounded: false);                              // buffer expires
            state = JumpLogic.Step(state, isGrounded: true, pressedThisStep: false, Dt);

            Assert.IsFalse(JumpLogic.ShouldJump(state, Jump), "JUMP-001: expired buffer must not fire");
        }

        // B1 — JUMP-001 (relationship, not an exact tuning value)
        [Test]
        public void Jump001_JumpSpeed_ScalesWithAirTime()
        {
            float shortJump = JumpLogic.JumpSpeedForAirTime(0.55f, 9.81f);
            float longJump = JumpLogic.JumpSpeedForAirTime(0.8f, 9.81f);

            Assert.Greater(longJump, shortJump, "longer air time must need a higher launch speed");
            Assert.That(JumpLogic.JumpSpeedForAirTime(0.65f, 10f), Is.EqualTo(3.25f).Within(1e-3f));
        }

        // B5 — DASH-001
        [Test]
        public void Dash001_Ready_ProducesVelocityAlongInput()
        {
            var change = DashLogic.DashVelocityChange(Float3.Zero, new Float3(1f, 0f, 0f), Dash);

            Assert.That(change.Magnitude(), Is.EqualTo(Dash.DashSpeed).Within(1e-3f));
            Assert.That(change.X, Is.EqualTo(Dash.DashSpeed).Within(1e-3f));
        }

        // B5 — DASH-001
        [Test]
        public void Dash001_NoInput_UsesCurrentHeading()
        {
            var velocity = new Float3(0f, -2f, 6f); // falling while rolling forward

            var change = DashLogic.DashVelocityChange(velocity, Float3.Zero, Dash);

            Assert.That(change.Z, Is.EqualTo(Dash.DashSpeed).Within(1e-3f), "DASH-001: dash follows the heading");
            Assert.That(change.Y, Is.EqualTo(0f).Within(1e-4f), "dash must stay planar");
        }

        // B5 — DASH-001
        [Test]
        public void Dash001_NoInputAndNoHeading_ProducesNothing()
        {
            var change = DashLogic.DashVelocityChange(Float3.Zero, Float3.Zero, Dash);

            Assert.That(change.Magnitude(), Is.LessThan(1e-6f), "a standing still dash has no direction");
        }

        // B6 — DASH-001
        [Test]
        public void Dash001_DuringCooldown_Denied()
        {
            var state = DashLogic.StartCooldown(DashState.Initial, Dash);
            state = DashLogic.Step(state, 0.5f);

            Assert.IsFalse(DashLogic.IsReady(state), "DASH-001: dash must stay locked during cooldown");
        }

        // B6 — DASH-001
        [Test]
        public void Dash001_AfterCooldown_ReadyAgain()
        {
            var state = DashLogic.StartCooldown(DashState.Initial, Dash);
            for (float t = 0f; t < Dash.Cooldown + 0.1f; t += Dt)
            {
                state = DashLogic.Step(state, Dt);
            }

            Assert.IsTrue(DashLogic.IsReady(state), "DASH-001: dash must recover after the cooldown");
        }

        // B8 — MOVE-002
        [Test]
        public void Move002_AirInput_AcceleratesLessThanGround()
        {
            var config = new MoveConfig(maxSpeed: 12f, acceleration: 10f, slopeAssist: 1f, airControl: 0.35f);
            var input = new Float3(0f, 0f, 1f);

            var ground = BallMovementLogic.ComputeVelocityChange(Float3.Zero, input, Float3.Up, config, Dt, isGrounded: true);
            var air = BallMovementLogic.ComputeVelocityChange(Float3.Zero, input, Float3.Up, config, Dt, isGrounded: false);

            Assert.Greater(ground.Magnitude(), air.Magnitude(), "MOVE-002: air control must be weaker");
            Assert.That(air.Magnitude(), Is.EqualTo(ground.Magnitude() * config.AirControl).Within(1e-4f));
        }

        // B9 — MOVE-003
        [Test]
        public void Move003_TurnComponent_DampedAtHighSpeed()
        {
            var config = new MoveConfig(maxSpeed: 12f, acceleration: 10f, slopeAssist: 1f, steeringAtMaxSpeed: 0.4f);
            var sideways = new Float3(1f, 0f, 0f);

            var slow = BallMovementLogic.ComputeVelocityChange(new Float3(0f, 0f, 1f), sideways, Float3.Up, config, Dt);
            var fast = BallMovementLogic.ComputeVelocityChange(new Float3(0f, 0f, 12f), sideways, Float3.Up, config, Dt);

            Assert.Greater(slow.X, fast.X, "MOVE-003: turning must get heavier as speed approaches max");
        }

        // B10 — MOVE-003
        [Test]
        public void Move003_ForwardAcceleration_NotDampedAtHighSpeed()
        {
            var config = new MoveConfig(maxSpeed: 12f, acceleration: 10f, slopeAssist: 1f, steeringAtMaxSpeed: 0.4f);
            var forward = new Float3(0f, 0f, 1f);
            var velocity = new Float3(0f, 0f, 8f); // fast, but below max so there is room to accelerate

            var undamped = BallMovementLogic.ComputeVelocityChange(velocity, forward, Float3.Up, new MoveConfig(12f, 10f, 1f), Dt);
            var damped = BallMovementLogic.ComputeVelocityChange(velocity, forward, Float3.Up, config, Dt);

            Assert.That(damped.Z, Is.EqualTo(undamped.Z).Within(1e-4f),
                "MOVE-003: damping must not touch acceleration along the heading");
        }

        // B7 — DASH-001 / MOVE-005 regression
        [Test]
        public void Move005_DashOverspeed_NotBrakedByInput()
        {
            var config = new MoveConfig(maxSpeed: 12f, acceleration: 10f, slopeAssist: 1f, airControl: 0.35f, steeringAtMaxSpeed: 0.4f);
            var dashVelocity = new Float3(0f, 0f, 20f);

            var change = BallMovementLogic.ComputeVelocityChange(dashVelocity, new Float3(0f, 0f, 1f), Float3.Up, config, Dt);
            var after = dashVelocity + change;

            Assert.That(after.Magnitude(), Is.EqualTo(20f).Within(1e-3f),
                "DASH-001: overspeed from a dash must not be braked by movement input");
        }
    }
}
