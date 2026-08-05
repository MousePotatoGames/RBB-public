using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// PlayMode coverage for F10 (Docs/Features/F10-projectile-weapon.md).
    ///
    /// CAN-001 was revised mid-feature: the cannon now fires on its cooldown alone,
    /// down whatever direction the barrel points. Several assertions here are the
    /// exact inverse of the first version's.
    /// </summary>
    public sealed class ProjectileWeaponPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private DroneConfig _droneConfig;
        private WeaponConfig _weaponConfig;
        private DroneSpawner _spawner;
        private GameObject _ball;
        private WeaponSlots _slots;
        private ProjectilePool _pool;
        private ProjectileWeaponController _controller;
        private PlayerHealth _playerHealth;

        // ---- rig -----------------------------------------------------------------

        private WeaponDefinition Cannon(
            float interval = 0.1f,
            int shots = 1,
            float spread = 0f,
            float speed = 18f,
            float range = 14f,
            int pierce = 0,
            float damage = 6f)
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.kind = WeaponKind.Cannon;
            def.displayName = "test cannon";
            def.mount = WeaponMount.Surface;
            def.attack = WeaponAttack.Projectile;
            def.travel = ProjectileTravel.Straight;
            def.fireInterval = interval;
            def.shotsPerBurst = shots;
            def.spreadDegrees = spread;
            def.projectileSpeed = speed;
            def.projectileRange = range;
            def.pierce = pierce;
            def.projectileDamage = damage;
            def.projectileRadius = 0.15f;
            def.shape = PrimitiveType.Cylinder;
            def.scale = 0.28f;
            _cleanup.Add(def);
            return def;
        }

        private WeaponDefinition Spike()
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.kind = WeaponKind.Spike;
            def.displayName = "test spike";
            def.mount = WeaponMount.Surface;
            def.attack = WeaponAttack.Contact;
            def.arcHalfAngleDegrees = 60f;
            def.bonusDamage = 8f;
            def.shape = PrimitiveType.Capsule;
            def.scale = 0.28f;
            _cleanup.Add(def);
            return def;
        }

        private void BuildRig(float hitCooldown = 0.35f)
        {
            // Ground is included on purpose: the sweep must ignore anything without
            // health, or every shot would die on the floor (B11 filtering).
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.transform.position = new Vector3(0f, -1f, 0f);
            _cleanup.Add(ground);

            _droneConfig = ScriptableObject.CreateInstance<DroneConfig>();
            _droneConfig.moveSpeed = 0f; // targets stay where the test puts them
            _droneConfig.contactDamage = 0f;
            _droneConfig.droneMaxHealth = 1000f; // never dies mid-test
            _droneConfig.hitCooldown = hitCooldown;
            _droneConfig.startCount = 1;
            _droneConfig.endCount = 1;
            _droneConfig.rampSeconds = 90f;
            _droneConfig.hardCap = 4;
            _droneConfig.spawnInterval = 999f; // only the test spawns
            _droneConfig.ringRadiusMin = 6f;
            _droneConfig.ringRadiusMax = 6f;
            _droneConfig.despawnDistance = 60f;
            _droneConfig.playerMaxHealth = 100f;
            _droneConfig.invulnerabilityDuration = 0.45f;
            _cleanup.Add(_droneConfig);

            _weaponConfig = ScriptableObject.CreateInstance<WeaponConfig>();
            _weaponConfig.maxWeapons = 3;
            _weaponConfig.attachRadius = 0.55f;
            _weaponConfig.pickupRadius = 1.1f;
            _weaponConfig.minSeparationDegrees = 50f;
            _weaponConfig.spawnTimes = new[] { 999f };
            _weaponConfig.spawnDistanceMin = 3f;
            _weaponConfig.spawnDistanceMax = 3f;
            _cleanup.Add(_weaponConfig);

            _ball = new GameObject("test_ball", typeof(SphereCollider), typeof(Rigidbody), typeof(PlayerHealth));
            _ball.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _ball.GetComponent<Rigidbody>().isKinematic = true;
            _playerHealth = _ball.GetComponent<PlayerHealth>();
            _playerHealth.Config = _droneConfig;
            _slots = _ball.AddComponent<WeaponSlots>();
            _slots.Config = _weaponConfig;
            _cleanup.Add(_ball);

            var prefabGo = new GameObject("drone_prefab", typeof(SphereCollider), typeof(Rigidbody), typeof(ScrapDrone));
            prefabGo.GetComponent<SphereCollider>().isTrigger = true;
            prefabGo.SetActive(false);
            _cleanup.Add(prefabGo);

            var spawnerGo = new GameObject("spawner", typeof(DroneSpawner));
            _spawner = spawnerGo.GetComponent<DroneSpawner>();
            _spawner.Config = _droneConfig;
            _spawner.DronePrefab = prefabGo.GetComponent<ScrapDrone>();
            _spawner.Player = _ball.transform;
            _spawner.PlayerHealth = _playerHealth;
            _spawner.CameraTransform = _ball.transform;
            _cleanup.Add(spawnerGo);

            var poolGo = new GameObject("projectile_pool", typeof(ProjectilePool));
            _pool = poolGo.GetComponent<ProjectilePool>();
            _cleanup.Add(poolGo);

            _controller = _ball.AddComponent<ProjectileWeaponController>();
            _controller.Weapons = _slots;
            _controller.Pool = _pool;
            _controller.ResetCooldowns();
        }

        /// <summary>Attaches a weapon on the ball's local +Z, so the barrel points at world +Z.</summary>
        private void AttachForward(WeaponDefinition definition) =>
            _slots.TryAttach(definition, _ball.transform.TransformPoint(Vector3.forward * 0.5f));

        /// <summary>
        /// Puts one live enemy at an exact position — the spawner's own ring angle is
        /// random, and these tests need a known one.
        ///
        /// The move goes through the Rigidbody with interpolation off. A plain
        /// transform write on an interpolated kinematic body is silently undone when
        /// it rebuilds its Transform from the buffered poses (the B19 trap), which
        /// leaves the drone back on the random ring and makes these tests flaky
        /// rather than failing.
        /// </summary>
        private EnemyHealth PlaceEnemy(Vector3 position)
        {
            _spawner.Spawn();
            ScrapDrone drone = _spawner.Active[_spawner.Active.Count - 1];

            var body = drone.GetComponent<Rigidbody>();
            RigidbodyInterpolation previous = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            body.position = position;
            drone.transform.position = position;
            body.interpolation = previous;

            return drone.Health;
        }

        /// <summary>Fails loudly if a placed enemy drifted — otherwise these tests go quietly flaky.</summary>
        private void AssertEnemyStillAt(Vector3 expected)
        {
            Vector3 actual = _spawner.Active[_spawner.Active.Count - 1].transform.position;
            Assert.Less((actual - expected).magnitude, 0.5f,
                $"rig precondition: the target moved from {expected} to {actual} — the result would be meaningless");
        }

        private static ProjectileConfig Config(float speed = 18f, float range = 14f, float damage = 6f) =>
            new ProjectileConfig(ProjectileTravel.Straight, 0.1f, 1, 0f, speed, range, 0, damage);

        private static IEnumerator Steps(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [TearDown]
        public void TearDown()
        {
            GameSession.RestoreTime(); // a test that ends the session leaves timeScale at 0

            foreach (Object o in _cleanup)
            {
                if (o != null)
                {
                    Object.Destroy(o);
                }
            }

            _cleanup.Clear();
        }

        // ---- B1: attacks without the player colliding ----------------------------

        [UnityTest]
        public IEnumerator Can001_CannonFiresAtEnemy_WithoutPlayerCollision()
        {
            BuildRig();
            AttachForward(Cannon());
            EnemyHealth enemy = PlaceEnemy(new Vector3(0f, 0f, 5f));
            float before = enemy.Current;

            yield return Steps(30);

            AssertEnemyStillAt(new Vector3(0f, 0f, 5f));
            Assert.Greater(_controller.FireCount, 0, "CAN-001: the cannon must fire on its own");
            Assert.Less(enemy.Current, before,
                "CAN-001 B1: damage must land without the ball ever touching the enemy — this is the first self-acting weapon");
            Assert.AreEqual(100f, _playerHealth.Current, 0.001f,
                "B13: the ball must not be able to shoot itself");
        }

        // ---- B3: the revision. No target is not a reason to hold fire -------------

        [UnityTest]
        public IEnumerator Can001_NoEnemy_StillFires()
        {
            BuildRig();
            AttachForward(Cannon(interval: 0.1f));

            yield return Steps(30); // 0.6s of empty arena

            Assert.Greater(_controller.ShotCount, 0,
                "CAN-001 (revised): the cannon fires into empty air. Gating on a target is what made it fire " +
                "3 times in 8.8s and 0 times in another run");
        }

        // ---- B4/B5: the barrel decides direction, nothing else -------------------

        [UnityTest]
        public IEnumerator Can001_FiresAlongBarrel_NotAtEnemy()
        {
            BuildRig();
            AttachForward(Cannon(interval: 999f)); // exactly one burst
            EnemyHealth enemy = PlaceEnemy(new Vector3(5f, 0f, 0f)); // 90° off the barrel
            float before = enemy.Current;

            yield return Steps(30);

            AssertEnemyStillAt(new Vector3(5f, 0f, 0f));
            Assert.AreEqual(1, _controller.FireCount, "it still fires — the enemy's position is irrelevant");
            Assert.AreEqual(before, enemy.Current, 0.001f,
                "CAN-001 B5: with no aim assist, a target square off the barrel is simply missed");
        }

        [UnityTest]
        public IEnumerator Can001_BallRotation_ChangesFireDirection()
        {
            BuildRig();
            AttachForward(Cannon(interval: 0.1f));
            EnemyHealth enemy = PlaceEnemy(new Vector3(5f, 0f, 0f));
            float before = enemy.Current;

            yield return Steps(15);
            AssertEnemyStillAt(new Vector3(5f, 0f, 0f));
            Assert.AreEqual(before, enemy.Current, 0.001f, "precondition: the barrel points away from the enemy");
            Assert.Greater(_controller.ShotCount, 0, "precondition: it was firing the whole time");

            // Roll the ball so the barrel swings onto the enemy (CAN-001 Result).
            _ball.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            yield return Steps(30);

            Assert.Less(enemy.Current, before,
                "CAN-001: rolling the ball is what aims the cannon — direction, not timing, is what rotation controls");
        }

        [UnityTest]
        public IEnumerator Can001_FireRate_MatchesInterval()
        {
            BuildRig();
            AttachForward(Cannon(interval: 0.2f));

            yield return Steps(50); // 50 × 0.02s = 1.0s

            // 1.0s at one burst per 0.2s — allow ±1 for where the window lands.
            Assert.That(_controller.FireCount, Is.InRange(4, 6),
                $"CAN-001 B2: the interval is now the only thing controlling fire rate (got {_controller.FireCount})");
        }

        // ---- B11/B12: damage and the shared cooldown -----------------------------

        [UnityTest]
        public IEnumerator Wpn009_ProjectileSharesHitCooldown()
        {
            BuildRig(hitCooldown: 5f); // nothing may land twice inside this test
            AttachForward(Cannon(interval: 0.05f, damage: 6f));
            EnemyHealth enemy = PlaceEnemy(new Vector3(0f, 0f, 4f));
            float before = enemy.Current;

            yield return Steps(60); // many bursts, many projectiles reaching the enemy

            AssertEnemyStillAt(new Vector3(0f, 0f, 4f));
            Assert.Greater(_controller.ShotCount, 3, "precondition: several projectiles were actually fired");
            Assert.AreEqual(6f, before - enemy.Current, 0.001f,
                "DMG-002: projectile damage shares the per-enemy cooldown, so repeated hits inside it must add nothing");
        }

        // ---- B13 -----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn009_ProjectileDoesNotHitPlayer()
        {
            BuildRig();
            AttachForward(Cannon());

            yield return Steps(40);

            Assert.Greater(_controller.ShotCount, 0,
                $"precondition: shots were fired past the ball (fires={_controller.FireCount})");
            Assert.AreEqual(100f, _playerHealth.Current, 0.001f, "B13: our own projectiles must never damage us");
        }

        // ---- B10 -----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn009_ProjectileExpiresAtRange()
        {
            BuildRig();

            // Launched straight from the pool: no target, nothing to hit, so the only
            // thing that can retire it is the range.
            Assert.IsTrue(_pool.TryLaunch(Vector3.zero, Vector3.forward, Config(speed: 10f, range: 2f), 0.15f));
            Assert.AreEqual(1, _pool.LiveCount);

            yield return Steps(5); // 5 × 0.02s × 10m/s = 1.0m — still short of 2m
            Assert.AreEqual(1, _pool.LiveCount, "must not expire early");

            yield return Steps(10); // now well past 2m

            Assert.AreEqual(0, _pool.LiveCount, "B10: a projectile must retire once it has flown its range");
        }

        // ---- B8: pooling ---------------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn009_ProjectilesComeFromPool()
        {
            BuildRig();
            AttachForward(Cannon(interval: 0.05f, range: 3f));

            yield return Steps(90);

            Assert.Greater(_controller.ShotCount, 6, "precondition: enough shots to force reuse");
            Assert.Less(_pool.TotalCreated, _controller.ShotCount,
                "WPN-009 B8: projectiles must be recycled, not instantiated per shot (기획서 15장)");
        }

        // ---- B9: live cap --------------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn009_LiveCap_SkipsFire()
        {
            BuildRig();
            _pool.LiveCap = 2;

            Assert.IsTrue(_pool.TryLaunch(Vector3.zero, Vector3.forward, Config(range: 50f), 0.15f));
            Assert.IsTrue(_pool.TryLaunch(Vector3.zero, Vector3.forward, Config(range: 50f), 0.15f));
            Assert.IsFalse(_pool.TryLaunch(Vector3.zero, Vector3.forward, Config(range: 50f), 0.15f),
                "WPN-009 Exception: past the cap the shot is skipped, never queued or grown into");

            Assert.AreEqual(2, _pool.LiveCount);
            yield return null;
        }

        // ---- B14: the reason there is no collider --------------------------------

        [UnityTest]
        public IEnumerator Wpn009_FastProjectile_DoesNotTunnel()
        {
            BuildRig();

            // 200 m/s crosses 4m per physics step — far past the enemy's 0.5m radius.
            // A trigger-collider projectile would sail straight through.
            AttachForward(Cannon(speed: 200f, range: 20f));
            EnemyHealth enemy = PlaceEnemy(new Vector3(0f, 0f, 6f));
            float before = enemy.Current;

            yield return Steps(30);

            AssertEnemyStillAt(new Vector3(0f, 0f, 6f));
            Assert.Less(enemy.Current, before,
                "B14: the sweep must cover the whole step, or raising projectile speed silently stops hits landing");
        }

        // ---- B15 -----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn008_ContactWeapon_FiresNothing()
        {
            BuildRig();
            AttachForward(Spike());

            yield return Steps(30);

            Assert.AreEqual(0, _controller.FireCount, "WPN-008: a contact weapon has no projectiles");
            Assert.AreEqual(0, _pool.LiveCount);
        }

        // ---- B6: buckshot is data ------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn009_BurstCount_ComesFromTheDefinitionAlone()
        {
            BuildRig();

            // Same code path as the single shot — only the numbers differ (WPN-009).
            AttachForward(Cannon(interval: 999f, shots: 5, spread: 30f, range: 20f));

            yield return Steps(20);

            Assert.AreEqual(1, _controller.FireCount, "precondition: exactly one burst inside the test window");
            Assert.AreEqual(5, _controller.ShotCount,
                "WPN-009: raising shots per burst alone must produce buckshot — no code path of its own");
        }

        // ---- B16 -----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn009_SessionEnd_ClearsProjectiles()
        {
            BuildRig();

            var sessionGo = new GameObject("session", typeof(GameSession));
            var session = sessionGo.GetComponent<GameSession>();
            session.PlayerHealth = _playerHealth;
            _cleanup.Add(sessionGo);

            _pool.Session = session;
            _pool.enabled = false;
            _pool.enabled = true; // re-run OnEnable so the pool subscribes to Ended

            Assert.IsTrue(_pool.TryLaunch(Vector3.zero, Vector3.forward, Config(range: 50f), 0.15f));
            Assert.AreEqual(1, _pool.LiveCount);

            session.End(SessionOutcome.Defeat);
            yield return null; // Ended is raised in LateUpdate

            Assert.AreEqual(0, _pool.LiveCount,
                "B16: projectiles must not outlive the session — they would hang in the air over the result screen");
        }
    }
}
