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
    /// PlayMode coverage for F13 stage 1 (Docs/Features/F13-xp-levelup-passives.md):
    /// orbs, the curve, the level-up pause and card selection. Passive <i>effects</i>
    /// arrive in stage 2 with their own tests.
    ///
    /// No test here uses <c>WaitForFixedUpdate</c> while the cards are up —
    /// LVL-001 stops time, and a fixed-update wait at timeScale 0 never returns.
    /// </summary>
    public sealed class ProgressPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private DroneConfig _droneConfig;
        private BallMovementConfig _moveConfig;
        private ProgressConfig _progressConfig;

        private GameObject _player;
        private Rigidbody _playerBody;
        private PlayerDamageDealer _dealer;
        private PlayerHealth _health;
        private PlayerProgress _progress;
        private GameSession _session;
        private ExperienceOrbPool _orbs;
        private LevelUpDirector _director;

        // ---- rig -----------------------------------------------------------------

        private PassiveDefinition Passive(PassiveKind kind, string name)
        {
            var def = ScriptableObject.CreateInstance<PassiveDefinition>();
            def.kind = kind;
            def.displayName = name;
            def.maxHealthPerStage = kind == PassiveKind.ArmorPlating ? 20f : 0f;
            def.healPerStage = kind == PassiveKind.ArmorPlating ? 20f : 0f;
            def.speedMultiplierPerStage = kind == PassiveKind.DenseCore ? 1.08f : 1f;
            def.damageMultiplierPerStage = kind == PassiveKind.DenseCore ? 1.15f : 1f;
            def.magnetMultiplierPerStage = kind == PassiveKind.MagnetField ? 1.6f : 1f;
            def.experienceMultiplierPerStage = kind == PassiveKind.MagnetField ? 1.25f : 1f;
            _cleanup.Add(def);
            return def;
        }

        /// <summary>
        /// Objects are built inactive and switched on last: every component here
        /// subscribes in OnEnable, and wiring a reference after that point leaves a
        /// silently unsubscribed listener.
        /// </summary>
        private void BuildRig(float[] thresholds = null, float magnetRadius = 2.5f)
        {
            _moveConfig = ScriptableObject.CreateInstance<BallMovementConfig>();
            _moveConfig.maxSpeed = 12f;
            _cleanup.Add(_moveConfig);

            _droneConfig = ScriptableObject.CreateInstance<DroneConfig>();
            _droneConfig.moveSpeed = 0f;
            _droneConfig.contactDamage = 0f;
            _droneConfig.droneMaxHealth = 1f; // one touch kills — this is not a damage test
            _droneConfig.hitCooldown = 0.35f;
            _droneConfig.startCount = 0;
            _droneConfig.endCount = 0;
            _droneConfig.playerMaxHealth = 100f;
            _droneConfig.hitStopDamageThreshold = 9999f; // hit stop is opted into per test
            _cleanup.Add(_droneConfig);

            _progressConfig = ScriptableObject.CreateInstance<ProgressConfig>();
            _progressConfig.droneExperience = 1f;
            _progressConfig.levelThresholds = thresholds ?? new[] { 15f, 35f, 65f, 105f };
            _progressConfig.magnetRadius = magnetRadius;
            _progressConfig.orbSpeed = 9f;
            _progressConfig.absorbDistance = 0.6f;
            _progressConfig.orbLiveCap = 64;
            _cleanup.Add(_progressConfig);

            var cameraGo = new GameObject("test_camera");
            _cleanup.Add(cameraGo);

            _player = new GameObject("test_player");
            _player.SetActive(false);
            _player.AddComponent<SphereCollider>();
            _playerBody = _player.AddComponent<Rigidbody>();
            _playerBody.useGravity = false;
            _playerBody.linearDamping = 0f;
            _player.transform.position = new Vector3(0f, 0.5f, 0f);

            var motor = _player.AddComponent<BallMotor>();
            motor.Config = _moveConfig;
            motor.CameraTransform = cameraGo.transform;
            motor.enabled = false; // velocity is driven directly for determinism

            _health = _player.AddComponent<PlayerHealth>();
            _health.Config = _droneConfig;

            _dealer = _player.AddComponent<PlayerDamageDealer>();
            _dealer.Config = _droneConfig;
            _dealer.MovementConfig = _moveConfig;
            _dealer.Motor = motor;

            _progress = _player.AddComponent<PlayerProgress>();
            _progress.Config = _progressConfig;
            _progress.Health = _health;
            _progress.PassiveAssets = new List<PassiveDefinition>
            {
                Passive(PassiveKind.ArmorPlating, "ARMOR PLATING"),
                Passive(PassiveKind.DenseCore, "DENSE CORE"),
                Passive(PassiveKind.MagnetField, "MAGNET FIELD"),
            };

            _health.Progress = _progress;
            _dealer.Progress = _progress;
            motor.Progress = _progress;
            _cleanup.Add(_player);

            var sessionGo = new GameObject("test_session");
            sessionGo.SetActive(false);
            _session = sessionGo.AddComponent<GameSession>();
            _session.PlayerHealth = _health;
            _session.DamageDealer = _dealer;
            _cleanup.Add(sessionGo);

            var orbGo = new GameObject("test_orbs");
            orbGo.SetActive(false);
            _orbs = orbGo.AddComponent<ExperienceOrbPool>();
            _orbs.Config = _progressConfig;
            _orbs.KillSource = _dealer;
            _orbs.Player = _player.transform;
            _orbs.Session = _session;
            _orbs.Progress = _progress;
            _cleanup.Add(orbGo);

            _progress.Orbs = _orbs;

            var directorGo = new GameObject("test_director");
            directorGo.SetActive(false);
            _director = directorGo.AddComponent<LevelUpDirector>();
            _director.Config = _progressConfig;
            _director.Progress = _progress;
            _director.Session = _session;
            _director.Passives = new List<PassiveDefinition>
            {
                Passive(PassiveKind.ArmorPlating, "ARMOR PLATING"),
                Passive(PassiveKind.DenseCore, "DENSE CORE"),
                Passive(PassiveKind.MagnetField, "MAGNET FIELD"),
            };
            _cleanup.Add(directorGo);

            _player.SetActive(true);
            sessionGo.SetActive(true);
            orbGo.SetActive(true);
            directorGo.SetActive(true);
        }

        private ScrapDrone SpawnDrone(Vector3 position)
        {
            var go = new GameObject("test_drone",
                typeof(SphereCollider), typeof(Rigidbody), typeof(EnemyHealth), typeof(ScrapDrone));
            go.transform.position = position;
            go.GetComponent<SphereCollider>().isTrigger = true;
            go.GetComponent<Rigidbody>().useGravity = false;
            var drone = go.GetComponent<ScrapDrone>();
            drone.Initialise(null, null, _droneConfig, null);
            _cleanup.Add(go);
            return drone;
        }

        /// <summary>
        /// Frame count, for asserting that something does <b>not</b> happen.
        /// Never use this to wait for motion to finish — see <see cref="Travel"/>.
        /// </summary>
        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Waits in game time, not frames. The test runner renders nothing and runs
        /// uncapped — a measured frame here was 2.3ms, so "60 frames" is 0.14
        /// seconds, not the second you would assume at 60fps. Orbs move at metres
        /// per <i>second</i>, so anything waiting for them has to wait in seconds.
        /// </summary>
        private static IEnumerator Travel(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitPhysics(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [TearDown]
        public void TearDown()
        {
            // A test that fails mid-level-up would otherwise freeze every test after it.
            GameSession.RestoreTime();

            foreach (Object o in _cleanup)
            {
                if (o != null)
                {
                    Object.Destroy(o);
                }
            }

            _cleanup.Clear();
        }

        // ---- B1/B2: drops ---------------------------------------------------------

        [UnityTest]
        public IEnumerator Xp001_EnemyKill_DropsOrbAtDeathPosition()
        {
            // No magnet: the kill happens at the ball, so an orb that could be
            // absorbed would be gone before the assertion could look at it.
            BuildRig(magnetRadius: 0f);
            var deathPlace = new Vector3(0f, 0.5f, 2f);
            ScrapDrone drone = SpawnDrone(deathPlace);
            yield return WaitPhysics(0.1f);

            _playerBody.linearVelocity = new Vector3(0f, 0f, 12f);
            yield return WaitPhysics(0.4f);

            Assert.IsTrue(drone.IsDead, "sanity: the drone has to die for an orb to drop");
            Assert.AreEqual(1, _orbs.DropCount, "XP-001 B1: a kill drops an orb");

            ExperienceOrb orb = _orbs.LiveAt(0);
            Assert.IsNotNull(orb);
            Assert.Less(Vector3.Distance(orb.transform.position, deathPlace), 1.5f,
                "XP-001 B1: the orb appears where the enemy died, not at the origin");
        }

        // XP-001 Exception. The kill event fires once, so this holds by construction —
        // the test is what keeps it that way.
        [UnityTest]
        public IEnumerator Xp001_OneKill_DropsExactlyOneOrb()
        {
            BuildRig(magnetRadius: 0f); // no absorption, so the count is not confounded
            SpawnDrone(new Vector3(0f, 0.5f, 1f));
            yield return WaitPhysics(0.1f);

            _playerBody.linearVelocity = new Vector3(0f, 0f, 12f);
            yield return WaitPhysics(1f); // stay in contact well past the kill

            Assert.AreEqual(1, _dealer.KillCount, "sanity: one drone, one kill");
            Assert.AreEqual(1, _orbs.DropCount, "XP-001 Exception: 사망 후 경험치 중복 지급 금지");
        }

        // ---- B3~B5: absorption ----------------------------------------------------

        [UnityTest]
        public IEnumerator Xp001_OrbOutsideRadius_DoesNotMove()
        {
            BuildRig(magnetRadius: 2.5f);
            _orbs.Drop(new Vector3(0f, 0.5f, 8f), 1f);

            ExperienceOrb orb = _orbs.LiveAt(0);
            Vector3 before = orb.transform.position;

            yield return Frames(10);

            Assert.AreEqual(before, orb.transform.position,
                "XP-001 B3: an orb outside the magnet radius stays put");
            Assert.IsFalse(orb.IsChasing);
        }

        [UnityTest]
        public IEnumerator Xp001_OrbInsideRadius_IsAbsorbed()
        {
            BuildRig(magnetRadius: 2.5f);
            _orbs.Drop(new Vector3(0f, 0.5f, 2f), 5f);

            // 2m gap at 9 m/s is ~0.16s. Waiting in seconds, not frames.
            yield return Travel(0.6f);

            Assert.AreEqual(0, _orbs.LiveCount, "XP-001 B4: it gets swallowed");
            Assert.AreEqual(1, _orbs.CollectCount);
            Assert.AreEqual(5f, _progress.TotalExperience, 1e-3f,
                "XP-001 B4: the orb's value reaches PlayerProgress");
        }

        [UnityTest]
        public IEnumerator Xp001_Orb_HasNoCollider()
        {
            BuildRig();
            _orbs.Drop(new Vector3(0f, 0.5f, 8f), 1f);

            yield return null;

            ExperienceOrb orb = _orbs.LiveAt(0);
            Assert.IsNull(orb.GetComponent<Collider>(),
                "XP-001 B5: orbs judge by distance, like every other hit test in this project");
            Assert.IsNull(orb.GetComponent<Rigidbody>());
        }

        [UnityTest]
        public IEnumerator Xp001_PoolReusesOrbs()
        {
            BuildRig(magnetRadius: 2.5f);

            for (int i = 0; i < 5; i++)
            {
                _orbs.Drop(new Vector3(0f, 0.5f, 1f), 1f);
                yield return Travel(0.3f); // 1m at 9 m/s is ~0.11s
            }

            Assert.AreEqual(5, _orbs.CollectCount);
            Assert.Less(_orbs.TotalCreated, 5,
                "기획서 15장: five drops through a pool must not instantiate five objects");
        }

        [UnityTest]
        public IEnumerator Xp001_SessionEnd_RecallsAllOrbs()
        {
            BuildRig(magnetRadius: 0f);
            _orbs.Drop(new Vector3(0f, 0.5f, 6f), 1f);
            _orbs.Drop(new Vector3(0f, 0.5f, 7f), 1f);
            yield return null;

            Assert.AreEqual(2, _orbs.LiveCount, "sanity");

            _session.End(SessionOutcome.Defeat);
            yield return null;

            Assert.AreEqual(0, _orbs.LiveCount, "XP-001 B6: the field is cleared when the session ends");
        }

        // ---- B7/B8: the curve -----------------------------------------------------

        [UnityTest]
        public IEnumerator Xp002_CrossingAThreshold_RaisesLevel()
        {
            BuildRig(thresholds: new[] { 15f, 35f });

            _progress.AddExperience(15f);
            yield return null;

            Assert.AreEqual(2, _progress.Level);
        }

        [UnityTest]
        public IEnumerator Xp002_TwoThresholdsAtOnce_RaisesLevelTwice()
        {
            BuildRig(thresholds: new[] { 15f, 35f });
            int events = 0;
            _progress.LevelUp += _ => events++;

            _progress.AddExperience(40f);
            yield return null;

            Assert.AreEqual(3, _progress.Level, "XP-002 B8: one gain can cross two thresholds");
            Assert.AreEqual(2, events, "LVL-001 B13: one PlayerLevelUp per level, not per gain");
        }

        // ---- B10~B16: the pause ---------------------------------------------------

        [UnityTest]
        public IEnumerator Lvl001_LevelUp_StopsGameTime()
        {
            BuildRig(thresholds: new[] { 15f });

            _progress.AddExperience(15f);
            yield return null;

            Assert.IsTrue(_director.IsOpen, "LVL-001: the cards are up");
            Assert.AreEqual(0f, Time.timeScale,
                "LVL-001 Exception: 레벨업 중 게임 시간은 완전히 멈춘다");
        }

        [UnityTest]
        public IEnumerator Lvl001_Selection_ResumesTime()
        {
            BuildRig(thresholds: new[] { 15f });
            _progress.AddExperience(15f);
            yield return null;

            bool taken = _director.Choose(0);

            Assert.IsTrue(taken);
            Assert.IsFalse(_director.IsOpen);
            Assert.AreEqual(1f, Time.timeScale, "LVL-001 B12: the choice resumes the game");
            Assert.AreEqual(1, _progress.StageOf(PassiveKind.ArmorPlating),
                "the chosen card is the passive that was taken");

            yield return null;
        }

        [UnityTest]
        public IEnumerator Lvl001_TwoLevelsAtOnce_ShowsCardsTwice()
        {
            BuildRig(thresholds: new[] { 15f, 35f });

            _progress.AddExperience(40f);
            yield return null;

            Assert.AreEqual(1, _director.OpenCount, "one card screen at a time");
            Assert.AreEqual(2, _director.QueuedLevelUps, "B16: the second level is waiting");

            _director.Choose(0);
            yield return null;

            Assert.AreEqual(2, _director.OpenCount, "B16: the queued level-up opens straight away");
            Assert.IsTrue(_director.IsOpen);
            Assert.AreEqual(0f, Time.timeScale, "still frozen for the second card");

            _director.Choose(1);
            yield return null;

            Assert.IsFalse(_director.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
        }

        // B15 — the result screen owns the end of the session.
        [UnityTest]
        public IEnumerator Lvl001_AfterSessionEnd_DoesNotShowCards()
        {
            BuildRig(thresholds: new[] { 15f });

            _session.End(SessionOutcome.Defeat);
            yield return null;

            Assert.IsFalse(_session.IsRunning, "sanity");

            _progress.AddExperience(15f);
            yield return null;

            Assert.IsFalse(_director.IsOpen,
                "LVL-001 B15: a level-up must not open over the result screen");
            Assert.AreEqual(0, _director.OpenCount);
        }

        [UnityTest]
        public IEnumerator Lvl001_SessionEndWhileOpen_ClosesTheCards()
        {
            BuildRig(thresholds: new[] { 15f });
            _progress.AddExperience(15f);
            yield return null;

            Assert.IsTrue(_director.IsOpen, "sanity");

            _session.End(SessionOutcome.Defeat);
            yield return null;

            Assert.IsFalse(_director.IsOpen);
            Assert.AreEqual(0f, Time.timeScale,
                "F07 B5: the session stopped time on purpose — the director must not undo it");
        }

        // ---- B20: PAS-004's exception ---------------------------------------------

        [UnityTest]
        public IEnumerator Pas004_AllMaxed_DoesNotStopTime()
        {
            BuildRig(thresholds: new[] { 15f });

            for (int k = 0; k < PassiveLogic.KindCount; k++)
            {
                for (int s = 0; s < PassiveLogic.MaxStage; s++)
                {
                    _progress.TakePassive((PassiveKind)k);
                }
            }

            _progress.AddExperience(15f);
            yield return null;

            Assert.IsFalse(_director.IsOpen);
            Assert.AreEqual(0, _director.OpenCount,
                "PAS-004 Exception: freezing the game for an empty card screen would be a pause the player cannot end");
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Pas004_MaxedPassive_LeavesTheCardPool()
        {
            BuildRig(thresholds: new[] { 15f });

            for (int s = 0; s < PassiveLogic.MaxStage; s++)
            {
                _progress.TakePassive(PassiveKind.ArmorPlating);
            }

            _progress.AddExperience(15f);
            yield return null;

            Assert.AreEqual(2, _director.CandidateCount);
            for (int i = 0; i < _director.CandidateCount; i++)
            {
                Assert.AreNotEqual(PassiveKind.ArmorPlating, _director.CandidateAt(i).kind);
            }
        }

        // ---- B18: 설계 판단 1의 방어선 ---------------------------------------------

        [UnityTest]
        public IEnumerator Pas004_TakingEveryStage_DoesNotMutateScriptableObjects()
        {
            BuildRig();

            float baseHealth = _droneConfig.playerMaxHealth;
            float baseSpeed = _moveConfig.maxSpeed;
            float baseMagnet = _progressConfig.magnetRadius;
            float baseExperience = _progressConfig.droneExperience;

            for (int k = 0; k < PassiveLogic.KindCount; k++)
            {
                for (int s = 0; s < PassiveLogic.MaxStage; s++)
                {
                    _progress.TakePassive((PassiveKind)k);
                }
            }

            yield return null;

            Assert.Greater(_progress.Effects.MaxHealthBonus, 0f,
                "sanity: the passives really did take effect — otherwise this test proves nothing");

            Assert.AreEqual(baseHealth, _droneConfig.playerMaxHealth,
                "설계 판단 1: a passive that writes to a shared config survives play mode and shows up as a config change nobody made");
            Assert.AreEqual(baseSpeed, _moveConfig.maxSpeed);
            Assert.AreEqual(baseMagnet, _progressConfig.magnetRadius);
            Assert.AreEqual(baseExperience, _progressConfig.droneExperience);
        }

        // ---- B21~B23: passive effects ---------------------------------------------

        [UnityTest]
        public IEnumerator Pas001_Acquire_RaisesMaxHealthAndHeals()
        {
            BuildRig();
            yield return null;

            float baseMax = _health.Max;
            _health.TakeDamage(50f);
            float hurt = _health.Current;

            _progress.TakePassive(PassiveKind.ArmorPlating);

            Assert.AreEqual(baseMax + 20f, _health.Max, 1e-3f, "PAS-001 B21: 최대 HP 증가");
            Assert.AreEqual(hurt + 20f, _health.Current, 1e-3f, "PAS-001 B21: 일부 즉시 회복");
        }

        [UnityTest]
        public IEnumerator Pas001_Heal_NeverExceedsTheEffectiveMax()
        {
            BuildRig();
            yield return null;

            _progress.TakePassive(PassiveKind.ArmorPlating); // full health already

            Assert.AreEqual(_health.Max, _health.Current, 1e-3f,
                "healing at full health tops out at the new max, it does not overflow");
        }

        // 설계 판단 3: the passive raises the ball's ceiling but SPD-001's thresholds
        // stay pinned to the base — that is what makes 럼블 easier to reach and the
        // growth visible. If the config moved, the tiers would move with it and the
        // player would feel nothing.
        [UnityTest]
        public IEnumerator Pas002_Acquire_RaisesSpeedWithoutMovingTierThresholds()
        {
            BuildRig();
            float baseMaxSpeed = _moveConfig.maxSpeed;

            for (int i = 0; i < PassiveLogic.MaxStage; i++)
            {
                _progress.TakePassive(PassiveKind.DenseCore);
            }

            yield return null;

            Assert.Greater(_progress.Effects.SpeedMultiplier, 1f, "PAS-002 B22: the ceiling went up");
            Assert.AreEqual(baseMaxSpeed, _moveConfig.maxSpeed, 1e-4f,
                "설계 판단 3: SpeedTierTracker normalises against this value — it must not move");
        }

        [UnityTest]
        public IEnumerator Pas003_Acquire_WidensTheMagnetRadius()
        {
            BuildRig(magnetRadius: 2.5f);
            Assert.AreEqual(2.5f, _orbs.EffectiveMagnetRadius, 1e-3f, "sanity");

            _progress.TakePassive(PassiveKind.MagnetField);
            yield return null;

            Assert.AreEqual(2.5f * 1.6f, _orbs.EffectiveMagnetRadius, 1e-3f, "PAS-003 B23: 획득 범위 증가");
            Assert.AreEqual(2.5f, _progressConfig.magnetRadius, 1e-4f,
                "설계 판단 1: the widening is a modifier, not an edit to the asset");
        }

        // The magnet only matters if an orb that used to be ignored now comes in.
        [UnityTest]
        public IEnumerator Pas003_WiderMagnet_PullsAnOrbThatWasOutOfRange()
        {
            BuildRig(magnetRadius: 2.5f);
            _orbs.Drop(new Vector3(0f, 0.5f, 3.5f), 1f); // outside 2.5, inside 4.0

            yield return Frames(5);
            Assert.AreEqual(1, _orbs.LiveCount, "sanity: out of reach to begin with");

            _progress.TakePassive(PassiveKind.MagnetField);
            yield return Travel(0.8f);

            Assert.AreEqual(0, _orbs.LiveCount, "PAS-003 B23: the wider radius reaches it");
        }

        [UnityTest]
        public IEnumerator Pas003_Acquire_MultipliesExperienceGain()
        {
            BuildRig();
            _progress.TakePassive(PassiveKind.MagnetField);

            _progress.AddExperience(10f);
            yield return null;

            Assert.AreEqual(12.5f, _progress.TotalExperience, 1e-3f, "PAS-003 B23: 경험치 배율");
        }

        [UnityTest]
        public IEnumerator Pas004_PastTheCap_ChangesNothing()
        {
            BuildRig();
            for (int i = 0; i < PassiveLogic.MaxStage; i++)
            {
                _progress.TakePassive(PassiveKind.MagnetField);
            }

            yield return null;
            float capped = _progress.Effects.MagnetMultiplier;

            _progress.TakePassive(PassiveKind.MagnetField); // one too many

            Assert.AreEqual(capped, _progress.Effects.MagnetMultiplier, 1e-4f,
                "PAS-004: a fourth take must not sneak past the cap");
        }

        // ---- ownership of Time.timeScale ------------------------------------------

        // An orb already inside the absorb distance would otherwise be swallowed
        // during the card screen — XP awarded in a frame the rule calls frozen.
        [UnityTest]
        public IEnumerator Lvl001_OrbsDoNotAbsorbWhileTimeIsStopped()
        {
            BuildRig(thresholds: new[] { 15f }, magnetRadius: 3f);
            _progress.AddExperience(15f);
            yield return null;

            Assert.IsTrue(_director.IsOpen, "sanity: time is stopped");

            _orbs.Drop(new Vector3(0f, 0.5f, 0.2f), 1f); // already within absorb distance
            float before = _progress.TotalExperience;

            yield return Frames(10);

            Assert.AreEqual(1, _orbs.LiveCount, "LVL-001 B10: the orb waits");
            Assert.AreEqual(before, _progress.TotalExperience, 1e-4f);
        }

        // HitStopController owns Time.timeScale too. A 0.05s freeze expiring behind
        // the card screen used to restore timeScale to 1 and quietly un-pause it.
        [UnityTest]
        public IEnumerator Lvl001_ExpiringHitStop_DoesNotResumeTheCardScreen()
        {
            BuildRig(thresholds: new[] { 15f });

            _droneConfig.hitStopDamageThreshold = 0f;
            _droneConfig.hitStopRefractory = 0f;
            _droneConfig.droneMaxHealth = 10_000f; // survive the hit: no kill, no orb

            var hitStopGo = new GameObject("test_hitstop");
            hitStopGo.SetActive(false);
            var hitStop = hitStopGo.AddComponent<HitStopController>();
            hitStop.Dealer = _dealer;
            hitStop.Config = _droneConfig;
            hitStop.Session = _session;
            hitStop.LevelUp = _director;
            hitStopGo.SetActive(true);
            _cleanup.Add(hitStopGo);

            SpawnDrone(new Vector3(0f, 0.5f, 2f));
            yield return WaitPhysics(0.1f);
            _playerBody.linearVelocity = new Vector3(0f, 0f, 12f);

            // Freeze first, then level up on top of it — the order that used to break.
            for (int i = 0; i < 300 && !hitStop.IsFrozen; i++)
            {
                yield return null;
            }

            Assert.IsTrue(hitStop.IsFrozen, "sanity: the hit stop has to be running for this test to mean anything");

            _progress.AddExperience(15f);
            yield return null;

            Assert.IsTrue(_director.IsOpen, "sanity: the cards are up");

            // Real time, because scaled time is stopped — long enough for any hit stop to expire.
            float until = Time.unscaledTime + 0.3f;
            while (Time.unscaledTime < until)
            {
                yield return null;
            }

            Assert.AreEqual(0f, Time.timeScale,
                "LVL-001 B10: an expiring hit stop must not hand time back while the cards are up");
            Assert.IsTrue(_director.IsOpen);
        }

        // ---- B24: restart ---------------------------------------------------------

        // A retry is a scene reload, which is exactly "these components, built fresh".
        [UnityTest]
        public IEnumerator Lose001_FreshRig_StartsWithNoProgress()
        {
            BuildRig(thresholds: new[] { 15f });
            _progress.AddExperience(20f);
            _progress.TakePassive(PassiveKind.DenseCore);
            yield return null;

            Assert.Greater(_progress.TotalExperience, 0f, "sanity");

            TearDown();
            BuildRig(thresholds: new[] { 15f });
            yield return null;

            Assert.AreEqual(0f, _progress.TotalExperience);
            Assert.AreEqual(ExperienceLogic.FirstLevel, _progress.Level);
            Assert.AreEqual(0, _progress.Passives.TotalStages,
                "LOSE-001: a restart must not carry passives into the next run");
        }
    }
}
