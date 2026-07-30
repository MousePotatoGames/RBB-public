using System.Collections;
using System.Collections.Generic;
using Game.Gameplay;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// PlayMode coverage for F02 camera (Docs/Features/F02-camera.md).
    /// Builds a minimal Cinemachine rig at runtime; damping is zeroed for
    /// determinism, so these verify rig behavior, not feel (feel = manual).
    /// </summary>
    public sealed class CameraRigPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private Camera _camera;
        private CinemachineOrbitalFollow _orbital;
        private Rigidbody _playerBody;
        private BallMotor _motor;
        private const float Radius = 11f;

        private void BuildRig()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            _cleanup.Add(ground);

            var config = ScriptableObject.CreateInstance<BallMovementConfig>();
            config.maxSpeed = 8f;
            config.timeToMaxSpeed = 0.5f;
            _cleanup.Add(config);

            var player = new GameObject("player", typeof(SphereCollider), typeof(Rigidbody), typeof(BallMotor));
            player.transform.position = new Vector3(0f, 0.5f, 0f);
            _playerBody = player.GetComponent<Rigidbody>();
            _motor = player.GetComponent<BallMotor>();
            _motor.Config = config;
            _cleanup.Add(player);

            var camGo = new GameObject("camera_with_brain", typeof(Camera), typeof(CinemachineBrain));
            var brain = camGo.GetComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate; // CAM-003
            _camera = camGo.GetComponent<Camera>();
            _cleanup.Add(camGo);

            var rig = new GameObject("cm_rig", typeof(CinemachineCamera));
            var vcam = rig.GetComponent<CinemachineCamera>();
            vcam.Target.TrackingTarget = player.transform;

            _orbital = rig.AddComponent<CinemachineOrbitalFollow>();
            _orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
            _orbital.Radius = Radius;
            _orbital.HorizontalAxis.Range = new Vector2(-180f, 180f);
            _orbital.HorizontalAxis.Wrap = true;
            _orbital.VerticalAxis.Value = 38f;
            _orbital.VerticalAxis.Range = new Vector2(38f, 38f);
            _orbital.TrackerSettings.PositionDamping = Vector3.zero; // determinism

            var composer = rig.AddComponent<CinemachineRotationComposer>();
            var composition = composer.Composition;
            composition.ScreenPosition = new Vector2(0f, 0.08f); // CM3: +y = 타깃을 화면 아래쪽에 배치
            composer.Composition = composition;
            composer.Damping = Vector2.zero;

            _motor.CameraTransform = _camera.transform;
            _cleanup.Add(rig);
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

        private static IEnumerator WaitFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        private Vector3 PlanarCameraOffset()
        {
            Vector3 offset = _camera.transform.position - _playerBody.position;
            offset.y = 0f;
            return offset;
        }

        // B1 — CAM-001
        [UnityTest]
        public IEnumerator Cam001_HorizontalAxis_OrbitsCameraAroundPlayer()
        {
            BuildRig();
            yield return WaitFrames(10);

            Vector3 before = PlanarCameraOffset();
            float distBefore = Vector3.Distance(_camera.transform.position, _playerBody.position);

            _orbital.HorizontalAxis.Value = 90f;
            yield return WaitFrames(10);

            Vector3 after = PlanarCameraOffset();
            float distAfter = Vector3.Distance(_camera.transform.position, _playerBody.position);

            Assert.That(distAfter, Is.EqualTo(distBefore).Within(distBefore * 0.1f),
                "CAM-001: orbit must keep camera distance");
            float sweep = Vector3.Angle(before, after);
            Assert.That(sweep, Is.EqualTo(90f).Within(5f),
                $"CAM-001: horizontal axis 90 must orbit ~90° (was {sweep:0.#}°)");
        }

        // B2 — CAM-001
        [UnityTest]
        public IEnumerator Cam001_Pitch_RemainsFixed_WhenOrbiting()
        {
            BuildRig();
            yield return WaitFrames(10);

            float pitchBefore = NormalizePitch(_camera.transform.eulerAngles.x);
            _orbital.HorizontalAxis.Value = 137f;
            yield return WaitFrames(10);
            float pitchAfter = NormalizePitch(_camera.transform.eulerAngles.x);

            Assert.That(pitchAfter, Is.EqualTo(pitchBefore).Within(1f),
                "CAM-001: pitch must stay fixed while orbiting (quarter view)");
        }

        // B3 — CAM-002
        [UnityTest]
        public IEnumerator Cam002_Camera_FollowsMovingPlayer()
        {
            BuildRig();
            yield return WaitFrames(10);

            _playerBody.position = new Vector3(15f, 0.5f, 7f);
            yield return WaitFrames(15);

            float dist = Vector3.Distance(_camera.transform.position, _playerBody.position);
            Assert.That(dist, Is.EqualTo(Radius).Within(Radius * 0.25f),
                $"CAM-002: camera must follow the player (distance {dist:0.#} vs radius {Radius})");
        }

        // B4 — CAM-002
        [UnityTest]
        public IEnumerator Cam002_Player_FramedInLowerScreenBand()
        {
            BuildRig();
            yield return WaitFrames(10);

            Vector3 viewport = _camera.WorldToViewportPoint(_playerBody.position);
            Assert.That(viewport.z, Is.GreaterThan(0f), "player must be in front of the camera");
            Assert.That(viewport.y, Is.InRange(0.35f, 0.55f),
                $"CAM-002: player must sit in the lower screen band (viewport y = {viewport.y:0.###})");
        }

        // B5 — MOVE-001 (F01 연동)
        [UnityTest]
        public IEnumerator Move001_BallFollowsRotatedCameraForward()
        {
            BuildRig();
            yield return WaitFrames(10);

            _orbital.HorizontalAxis.Value = 90f;
            yield return WaitFrames(10);

            _motor.SetMoveInput(Vector2.up);
            float elapsed = 0f;
            while (elapsed < 0.8f)
            {
                yield return new WaitForFixedUpdate();
                elapsed += Time.fixedDeltaTime;
            }

            Vector3 planarVel = _playerBody.linearVelocity;
            planarVel.y = 0f;
            Vector3 camForward = _camera.transform.forward;
            camForward.y = 0f;

            Assert.Greater(planarVel.magnitude, 0.5f, "ball did not start moving");
            float angle = Vector3.Angle(planarVel, camForward);
            Assert.Less(angle, 20f,
                $"MOVE-001: after orbiting, W must follow the new camera forward (angle {angle:0.#}°)");
        }

        private static float NormalizePitch(float eulerX)
        {
            return eulerX > 180f ? eulerX - 360f : eulerX;
        }
    }
}
