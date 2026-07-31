using System.Collections;
using System.Collections.Generic;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// PlayMode coverage for F03 (Docs/Features/F03-jump-dash.md).
    /// Minimal runtime rig; asserts relationships, never exact tuning values.
    /// </summary>
    public sealed class JumpDashPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private BallMovementConfig _config;
        private BallMotor _motor;
        private Rigidbody _body;

        private void BuildRig()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "test_ground";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            _cleanup.Add(ground);

            var cameraGo = new GameObject("test_camera_rig");
            cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 8f, -10f), Quaternion.Euler(35f, 0f, 0f));
            _cleanup.Add(cameraGo);

            _config = ScriptableObject.CreateInstance<BallMovementConfig>();
            _config.maxSpeed = 8f;
            _config.timeToMaxSpeed = 0.5f;
            _config.slopeAssist = 1f;
            _config.groundCheckDistance = 0.6f;
            _config.jumpAirTime = 0.8f;
            _config.dashSpeed = 8f;
            _config.dashCooldown = 1.8f;
            _cleanup.Add(_config);

            var ball = new GameObject("test_ball", typeof(SphereCollider), typeof(Rigidbody), typeof(BallMotor));
            ball.transform.position = new Vector3(0f, 0.5f, 0f);
            _body = ball.GetComponent<Rigidbody>();
            _body.linearDamping = 0.05f;
            _body.angularDamping = 1.5f;
            _motor = ball.GetComponent<BallMotor>();
            _motor.Config = _config;
            _motor.CameraTransform = cameraGo.transform;
            _cleanup.Add(ball);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _cleanup)
            {
                if (o != null)
                {
                    Object.Destroy(o);
                }
            }

            _cleanup.Clear();
        }

        private static IEnumerator WaitPhysics(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
        }

        private static Vector3 Planar(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // B1 — JUMP-001
        [UnityTest]
        public IEnumerator Jump001_BallLeavesGround_WhenJumpPressed()
        {
            BuildRig();
            yield return WaitPhysics(0.3f);
            Assert.IsTrue(_motor.IsGrounded, "sanity: ball must start grounded");
            float startY = _body.position.y;

            _motor.QueueJump();
            yield return WaitPhysics(0.15f);

            Assert.Greater(_body.position.y, startY + 0.05f, "JUMP-001: jump must lift the ball off the ground");
        }

        // B3 — JUMP-001
        [UnityTest]
        public IEnumerator Jump001_NoDoubleJump_InAir()
        {
            BuildRig();
            yield return WaitPhysics(0.3f);

            _motor.QueueJump();
            yield return WaitPhysics(0.25f); // airborne, past the coyote window
            Assert.IsFalse(_motor.IsGrounded, "sanity: ball must be airborne");
            float risingSpeed = _body.linearVelocity.y;

            _motor.QueueJump();
            yield return WaitPhysics(0.06f);

            Assert.LessOrEqual(_body.linearVelocity.y, risingSpeed,
                "JUMP-001: a second press in mid-air must not add lift");
        }

        // B5 — DASH-001
        [UnityTest]
        public IEnumerator Dash001_PlanarSpeed_JumpsAfterDash()
        {
            BuildRig();
            yield return WaitPhysics(0.3f);

            _motor.SetMoveInput(Vector2.up);
            yield return WaitPhysics(0.3f);
            float before = Planar(_body.linearVelocity).magnitude;

            _motor.QueueDash();
            yield return WaitPhysics(0.06f);
            float after = Planar(_body.linearVelocity).magnitude;

            Assert.Greater(after, before + 2f, $"DASH-001: dash must add speed ({before:0.##} → {after:0.##})");
        }

        // B6 — DASH-001
        [UnityTest]
        public IEnumerator Dash001_SecondDash_BlockedByCooldown()
        {
            BuildRig();
            yield return WaitPhysics(0.3f);

            _motor.SetMoveInput(Vector2.up);
            yield return WaitPhysics(0.3f);

            _motor.QueueDash();
            yield return WaitPhysics(0.1f);
            Assert.Greater(_motor.DashCooldownRemaining, 0f, "sanity: first dash must start the cooldown");
            float afterFirst = Planar(_body.linearVelocity).magnitude;

            _motor.QueueDash();
            yield return WaitPhysics(0.06f);
            float afterSecond = Planar(_body.linearVelocity).magnitude;

            Assert.LessOrEqual(afterSecond, afterFirst + 1f,
                "DASH-001: a second dash during cooldown must not add another burst");
        }
    }
}
