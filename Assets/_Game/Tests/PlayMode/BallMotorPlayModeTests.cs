using System.Collections;
using System.Collections.Generic;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// PlayMode coverage for F01 (Docs/Features/F01-ball-movement.md).
    /// Builds minimal GameObjects instead of loading scenes; asserts ranges,
    /// never exact tuning values.
    /// </summary>
    public sealed class BallMotorPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private BallMovementConfig _config;
        private Transform _cameraRig;
        private BallMotor _motor;
        private Rigidbody _body;

        private void BuildRig(float cameraYawDegrees = 0f)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "test_ground";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            _cleanup.Add(ground);

            var cameraGo = new GameObject("test_camera_rig");
            cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 8f, -10f), Quaternion.Euler(35f, cameraYawDegrees, 0f));
            _cameraRig = cameraGo.transform;
            _cleanup.Add(cameraGo);

            _config = ScriptableObject.CreateInstance<BallMovementConfig>();
            _config.maxSpeed = 8f;
            _config.timeToMaxSpeed = 0.5f;
            _config.slopeAssist = 1f;
            _config.groundCheckDistance = 0.6f;
            _cleanup.Add(_config);

            var ball = new GameObject("test_ball", typeof(SphereCollider), typeof(Rigidbody), typeof(BallMotor));
            ball.transform.position = new Vector3(0f, 0.5f, 0f);
            _body = ball.GetComponent<Rigidbody>();
            _body.linearDamping = 0.05f;
            _body.angularDamping = 1.5f;
            _motor = ball.GetComponent<BallMotor>();
            _motor.Config = _config;
            _motor.CameraTransform = _cameraRig;
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

        private static Vector3 Planar(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private static IEnumerator WaitPhysics(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }
        }

        // B1 — MOVE-001
        [UnityTest]
        public IEnumerator Move001_BallAccelerates_WhenInputHeld()
        {
            BuildRig();
            yield return WaitPhysics(0.2f); // settle on ground

            _motor.SetMoveInput(Vector2.up);
            yield return WaitPhysics(0.5f);

            Assert.Greater(Planar(_body.linearVelocity).magnitude, 0.5f,
                "MOVE-001: held input must accelerate the ball");
        }

        // B2 — MOVE-001
        [UnityTest]
        public IEnumerator Move001_MovesAlongCameraDirection()
        {
            BuildRig(cameraYawDegrees: 90f); // camera looks along +X
            yield return WaitPhysics(0.2f);

            _motor.SetMoveInput(Vector2.up);
            yield return WaitPhysics(0.6f);

            Vector3 planarVel = Planar(_body.linearVelocity);
            Assert.Greater(planarVel.magnitude, 0.5f, "ball did not start moving");
            float angle = Vector3.Angle(planarVel, Vector3.right);
            Assert.Less(angle, 15f, $"MOVE-001: W must follow the camera (angle to +X was {angle:0.#}°)");
        }

        // B4 — MOVE-005
        [UnityTest]
        public IEnumerator Move005_PlanarSpeed_StaysUnderMaxSpeed()
        {
            BuildRig();
            yield return WaitPhysics(0.2f);

            _motor.SetMoveInput(Vector2.up);
            yield return WaitPhysics(2f);

            float speed = Planar(_body.linearVelocity).magnitude;
            Assert.LessOrEqual(speed, _config.maxSpeed * 1.05f,
                $"MOVE-005: planar speed {speed:0.##} exceeded max {_config.maxSpeed}");
            Assert.Greater(speed, _config.maxSpeed * 0.7f,
                "sanity: ball should be near max speed after 2s of input");
        }

        // B5 — MOVE-001 (inertia)
        [UnityTest]
        public IEnumerator Move001_KeepsRolling_AfterInputReleased()
        {
            BuildRig();
            yield return WaitPhysics(0.2f);

            _motor.SetMoveInput(Vector2.up);
            yield return WaitPhysics(1f);
            float speedBefore = Planar(_body.linearVelocity).magnitude;

            _motor.SetMoveInput(Vector2.zero);
            yield return WaitPhysics(0.3f);
            float speedAfter = Planar(_body.linearVelocity).magnitude;

            Assert.Greater(speedBefore, 1f, "sanity: ball must be rolling before release");
            Assert.Greater(speedAfter, speedBefore * 0.3f,
                "MOVE-001: releasing input must not stop the ball instantly (inertia)");
        }
    }
}
