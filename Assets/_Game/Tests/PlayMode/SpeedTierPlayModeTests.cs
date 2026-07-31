using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// PlayMode coverage for F04 (Docs/Features/F04-speed-tiers.md).
    /// </summary>
    public sealed class SpeedTierPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private BallMovementConfig _config;
        private BallMotor _motor;
        private SpeedTierTracker _tracker;
        private Rigidbody _body;

        private void BuildRig()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            _cleanup.Add(ground);

            var cameraGo = new GameObject("test_camera");
            cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 8f, -10f), Quaternion.Euler(35f, 0f, 0f));
            _cleanup.Add(cameraGo);

            _config = ScriptableObject.CreateInstance<BallMovementConfig>();
            _config.maxSpeed = 8f;
            _config.timeToMaxSpeed = 0.5f;
            _config.dashSpeed = 8f;
            _config.dashCooldown = 1.8f;
            _config.dashActiveWindow = 0.35f;
            _cleanup.Add(_config);

            var ball = new GameObject("test_ball",
                typeof(SphereCollider), typeof(Rigidbody), typeof(BallMotor), typeof(SpeedTierTracker));
            ball.transform.position = new Vector3(0f, 0.5f, 0f);
            _body = ball.GetComponent<Rigidbody>();
            _body.linearDamping = 0.05f;
            _motor = ball.GetComponent<BallMotor>();
            _motor.Config = _config;
            _motor.CameraTransform = cameraGo.transform;
            _tracker = ball.GetComponent<SpeedTierTracker>();
            _tracker.Config = _config;
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

        // B1 — SPD-001
        [UnityTest]
        public IEnumerator Spd001_TierRises_AsBallAccelerates()
        {
            BuildRig();
            yield return WaitPhysics(0.2f);
            Assert.AreEqual(SpeedTier.Low, _tracker.CurrentTier, "starts at rest in Low");

            _motor.SetMoveInput(Vector2.up);
            yield return WaitPhysics(1.5f);

            Assert.Greater((int)_tracker.CurrentTier, (int)SpeedTier.Low,
                $"SPD-001: tier must rise with speed (ratio was {_tracker.SpeedRatio:0.##})");
        }

        // B4 — SPD-001
        [UnityTest]
        public IEnumerator Spd001_EventFires_OnlyOnTierChange()
        {
            BuildRig();
            int events = 0;
            _tracker.TierChanged += _ => events++;

            _motor.SetMoveInput(Vector2.up);
            yield return WaitPhysics(1.5f);

            Assert.Greater(events, 0, "at least one tier change expected while accelerating");
            Assert.LessOrEqual(events, 4, $"SPD-001: events must fire per change, not per frame (got {events})");
        }

        // B2 — SPD-001 exception
        [UnityTest]
        public IEnumerator Spd001_Dash_ImmediatelyEntersRumble()
        {
            BuildRig();
            yield return WaitPhysics(0.2f);

            _motor.SetMoveInput(Vector2.up);
            _motor.QueueDash();
            yield return WaitPhysics(0.1f);

            Assert.IsTrue(_motor.IsDashActive, "sanity: dash window must be open");
            Assert.AreEqual(SpeedTier.Rumble, _tracker.CurrentTier,
                "SPD-001: dashing forces the Rumble tier");
        }

        // B7 — no per-frame material instancing
        [UnityTest]
        public IEnumerator Spd001_Visuals_UpdateWithoutLeakingMaterials()
        {
            BuildRig();
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(_motor.transform, false);
            var visuals = sphere.AddComponent<SpeedTierVisuals>();
            visuals.Tracker = _tracker;
            _cleanup.Add(sphere);

            var renderer = sphere.GetComponent<Renderer>();
            Material sharedBefore = renderer.sharedMaterial;

            for (int i = 0; i < 20; i++)
            {
                visuals.Apply(SpeedTier.Low);
                visuals.Apply(SpeedTier.Rumble);
            }

            yield return null;

            Assert.AreSame(sharedBefore, renderer.sharedMaterial,
                "SPD-001 feedback must go through a MaterialPropertyBlock, not new material instances");
        }
    }
}
