using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// PlayMode coverage for F07 (Docs/Features/F07-lose-restart.md).
    /// The scene reload itself is not exercised here — it would tear down the
    /// test runner's scene. Everything up to the reload is.
    /// </summary>
    public sealed class SessionPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private DroneConfig _droneConfig;
        private SessionConfig _sessionConfig;
        private GameObject _player;
        private PlayerHealth _health;
        private PlayerDamageDealer _dealer;
        private GameSession _session;

        private void BuildRig(float resultDelay = 0f)
        {
            _droneConfig = ScriptableObject.CreateInstance<DroneConfig>();
            _droneConfig.playerMaxHealth = 30f;
            _droneConfig.invulnerabilityDuration = 0f; // let the test kill quickly
            _cleanup.Add(_droneConfig);

            _sessionConfig = ScriptableObject.CreateInstance<SessionConfig>();
            _sessionConfig.resultDelaySeconds = resultDelay;
            _cleanup.Add(_sessionConfig);

            _player = new GameObject("test_player",
                typeof(SphereCollider), typeof(Rigidbody), typeof(BallMotor), typeof(PlayerHealth), typeof(PlayerDamageDealer));
            _player.GetComponent<Rigidbody>().isKinematic = true;
            _health = _player.GetComponent<PlayerHealth>();
            _health.Config = _droneConfig;
            _dealer = _player.GetComponent<PlayerDamageDealer>();
            _dealer.Config = _droneConfig;
            _cleanup.Add(_player);

            var sessionGo = new GameObject("test_session");
            _session = sessionGo.AddComponent<GameSession>();
            _session.Config = _sessionConfig;
            _session.PlayerHealth = _health;
            _session.DamageDealer = _dealer;
            // Re-run OnEnable so the serialized references are actually subscribed.
            _session.enabled = false;
            _session.enabled = true;
            _cleanup.Add(sessionGo);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f; // a leaked pause would break every later test
            foreach (Object o in _cleanup)
            {
                if (o != null)
                {
                    Object.Destroy(o);
                }
            }

            _cleanup.Clear();
        }

        private void KillPlayer() => _health.TakeDamage(_droneConfig.playerMaxHealth * 2f);

        // B1, B2
        [UnityTest]
        public IEnumerator Lose001_PlayerDeath_EndsSessionOnce()
        {
            BuildRig();
            int raised = 0;
            SessionState captured = default;
            _session.Ended += s => { raised++; captured = s; };

            KillPlayer();
            yield return null;
            yield return null;

            Assert.AreEqual(1, raised, "B2: the result must be processed exactly once");
            Assert.AreEqual(1, _session.EndReportCount);
            Assert.AreEqual(SessionOutcome.Defeat, captured.Outcome, "B1");
            Assert.IsFalse(_session.IsRunning);
        }

        // B2 — a second death report must not produce a second result
        [UnityTest]
        public IEnumerator Lose001_RepeatedDeathReports_StillEndOnce()
        {
            BuildRig();
            int raised = 0;
            _session.Ended += _ => raised++;

            KillPlayer();
            _session.End(SessionOutcome.Defeat);
            yield return null;
            _session.End(SessionOutcome.Defeat);
            yield return null;

            Assert.AreEqual(1, raised, "B2");
        }

        // B5
        [UnityTest]
        public IEnumerator Lose001_SessionEnd_StopsTime()
        {
            BuildRig();
            KillPlayer();
            yield return null;

            Assert.That(Time.timeScale, Is.EqualTo(0f).Within(1e-4f), "B5: the field must stop");
        }

        // B4 — survival time is frozen at death
        [UnityTest]
        public IEnumerator Lose001_ElapsedFreezes_AtDeath()
        {
            BuildRig();
            yield return null;
            yield return null;

            KillPlayer();
            yield return null;
            float atDeath = _session.State.Elapsed;

            yield return null;
            yield return null;

            Assert.That(_session.State.Elapsed, Is.EqualTo(atDeath).Within(1e-4f), "B4");
        }

        // B6 — hit stop must not undo the session pause
        [UnityTest]
        public IEnumerator Lose001_SessionEnd_NeutralisesHitStop()
        {
            BuildRig();
            var stop = _player.AddComponent<HitStopController>();
            stop.Dealer = _dealer;
            stop.Config = _droneConfig;
            stop.Session = _session;
            stop.enabled = false;
            stop.enabled = true;

            KillPlayer();
            yield return null;
            yield return null;

            Assert.IsFalse(stop.enabled, "B6: hit stop must stand down once the session owns time");
            Assert.That(Time.timeScale, Is.EqualTo(0f).Within(1e-4f),
                "B6: releasing the freeze must not restore time under the result screen");
        }

        // B7, B8, B13
        [UnityTest]
        public IEnumerator Lose001_ResultScreen_ShowsAfterDelay()
        {
            BuildRig(resultDelay: 0.3f);

            var canvasGo = new GameObject("test_canvas", typeof(Canvas));
            var screen = canvasGo.AddComponent<ResultScreen>();
            var panel = new GameObject("panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var statsGo = new GameObject("stats");
            statsGo.transform.SetParent(panel.transform, false);
            var stats = statsGo.AddComponent<TextMeshProUGUI>();

            screen.Config = _sessionConfig;
            screen.Session = _session;
            screen.Panel = panel;
            screen.StatsLabel = stats;
            screen.enabled = false;
            screen.enabled = true;
            panel.SetActive(false);
            _cleanup.Add(canvasGo);

            Assert.IsFalse(screen.IsShown, "B13: hidden while the session runs");

            _dealer.GetType(); // keep the dealer referenced for clarity
            KillPlayer();
            yield return null;

            Assert.IsFalse(screen.IsShown, "B7: must wait out the delay");

            float deadline = Time.realtimeSinceStartup + 2f;
            while (!screen.IsShown && Time.realtimeSinceStartup < deadline)
            {
                yield return null; // unscaled — game time is stopped
            }

            Assert.IsTrue(screen.IsShown, "B7: the screen must appear after the delay even with time stopped");
            Assert.IsTrue(stats.text.Contains("TIME") && stats.text.Contains("KILLS"),
                "B8: survival time and kill count must be shown");
            foreach (char c in stats.text)
            {
                Assert.Less((int)c, 128,
                    $"B8: the result text must stay ASCII — '{c}' has no glyph in the default TMP font");
            }
            Assert.AreEqual(CursorLockMode.None, Cursor.lockState, "B7: the cursor must be free to click Retry");
        }

        // B9 — a real kill through the dealer must reach the session
        [UnityTest]
        public IEnumerator Lose001_KillCount_MatchesDealer()
        {
            BuildRig();

            // Real collision rig (same shape as the F06 tests): dynamic player, trigger drone.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            _cleanup.Add(ground);

            var moveConfig = ScriptableObject.CreateInstance<BallMovementConfig>();
            moveConfig.maxSpeed = 12f;
            moveConfig.timeToMaxSpeed = 1f;
            _cleanup.Add(moveConfig);

            _droneConfig.droneMaxHealth = 1f;
            _droneConfig.baseCollisionDamage = 100f;
            _droneConfig.contactDamage = 0f;      // isolate: the drone must not kill the player here
            _droneConfig.moveSpeed = 0f;

            var motor = _player.GetComponent<BallMotor>();
            motor.Config = moveConfig;
            motor.enabled = false;
            _dealer.MovementConfig = moveConfig;
            _dealer.Motor = motor;

            var body = _player.GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = false;
            body.linearDamping = 0f;
            _player.transform.position = new Vector3(0f, 0.5f, 0f);

            var drone = new GameObject("drone",
                typeof(SphereCollider), typeof(Rigidbody), typeof(EnemyHealth), typeof(ScrapDrone));
            drone.transform.position = new Vector3(0f, 0.5f, 2f);
            drone.GetComponent<SphereCollider>().isTrigger = true;
            drone.GetComponent<ScrapDrone>().Initialise(null, null, _droneConfig, null);
            _cleanup.Add(drone);

            yield return null;
            Assert.AreEqual(0, _session.State.Kills, "sanity: no kills yet");

            body.linearVelocity = new Vector3(0f, 0f, 12f);
            float deadline = Time.realtimeSinceStartup + 2f;
            while (_dealer.KillCount == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            yield return null;

            Assert.AreEqual(1, _dealer.KillCount, "sanity: the drone must have died");
            Assert.AreEqual(_dealer.KillCount, _session.State.Kills, "B9: the session must count the dealer's kills");
        }

        // B11 — the reload path restores time first
        [UnityTest]
        public IEnumerator Lose001_RestoreTime_ResetsTimeScale()
        {
            BuildRig();
            KillPlayer();
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(0f).Within(1e-4f), "sanity: paused");

            GameSession.RestoreTime();

            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(1e-4f),
                "B11: LoadScene does not restore timeScale — we must");
        }
    }
}
