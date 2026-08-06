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
    /// PlayMode coverage for F11 (Docs/Features/F11-tesla-zap.md).
    /// </summary>
    public sealed class ZapWeaponPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private DroneConfig _droneConfig;
        private WeaponConfig _weaponConfig;
        private DroneSpawner _spawner;
        private GameObject _ball;
        private WeaponSlots _slots;
        private ZapWeaponController _controller;
        private PlayerHealth _playerHealth;

        // ---- rig -----------------------------------------------------------------

        private WeaponDefinition Tesla(
            float interval = 0.1f,
            float range = 5f,
            float maxDamage = 12f,
            float minDamage = 4f)
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.kind = WeaponKind.Tesla;
            def.displayName = "test tesla";
            def.mount = WeaponMount.Surface;
            def.attack = WeaponAttack.Zap;
            def.zapInterval = interval;
            def.zapRange = range;
            def.zapMaxDamage = maxDamage;
            def.zapMinDamage = minDamage;
            def.shape = PrimitiveType.Cube;
            def.scale = 0.28f;
            _cleanup.Add(def);
            return def;
        }

        private WeaponDefinition Cannon()
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.kind = WeaponKind.Cannon;
            def.displayName = "test cannon";
            def.mount = WeaponMount.Surface;
            def.attack = WeaponAttack.Projectile;
            def.travel = ProjectileTravel.Straight;
            def.fireInterval = 0.1f;
            def.shotsPerBurst = 1;
            def.projectileSpeed = 18f;
            def.projectileRange = 14f;
            def.projectileDamage = 6f;
            def.shape = PrimitiveType.Cylinder;
            def.scale = 0.28f;
            _cleanup.Add(def);
            return def;
        }

        private void BuildRig(float hitCooldown = 0.35f)
        {
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

            _controller = _ball.AddComponent<ZapWeaponController>();
            _controller.Weapons = _slots;
            _controller.Enemies = _spawner;
            _controller.ResetCooldowns();
        }

        /// <summary>Attaches a weapon on the ball's local +Z, so the ring sits at world (0,0,0.55).</summary>
        private void AttachForward(WeaponDefinition definition) =>
            _slots.TryAttach(definition, _ball.transform.TransformPoint(Vector3.forward * 0.5f));

        /// <summary>
        /// Puts one live enemy at an exact position. The move goes through the
        /// Rigidbody with interpolation off — a plain transform write on an
        /// interpolated kinematic body is silently undone (the B19 trap), which would
        /// leave the drone on the spawner's random ring and make these tests flaky.
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

        private void AssertEnemyStillAt(EnemyHealth enemy, Vector3 expected)
        {
            Vector3 actual = enemy.transform.position;
            Assert.Less((actual - expected).magnitude, 0.5f,
                $"rig precondition: the target moved from {expected} to {actual} — a distance-based result would be meaningless");
        }

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

        // ---- B1/B2 ---------------------------------------------------------------

        [UnityTest]
        public IEnumerator Tes001_TeslaDamagesNearestEnemy()
        {
            BuildRig();
            AttachForward(Tesla());
            EnemyHealth enemy = PlaceEnemy(new Vector3(0f, 0f, 3f));
            float before = enemy.Current;

            yield return Steps(20);

            AssertEnemyStillAt(enemy, new Vector3(0f, 0f, 3f));
            Assert.Greater(_controller.ZapCount, 0, "TES-001: the tesla discharges on its own");
            Assert.Less(enemy.Current, before,
                "TES-001 B1/B2: damage must land with no collision and no aiming");
            Assert.AreEqual(100f, _playerHealth.Current, 0.001f, "the ball must not zap itself");
        }

        [UnityTest]
        public IEnumerator Tes001_ChoosesTheNearerOfTwo()
        {
            BuildRig(hitCooldown: 5f); // one hit each at most, so the counts are unambiguous
            AttachForward(Tesla(interval: 999f)); // exactly one discharge
            EnemyHealth near = PlaceEnemy(new Vector3(0f, 0f, 2f));
            EnemyHealth far = PlaceEnemy(new Vector3(0f, 0f, 4.5f));
            float nearBefore = near.Current;
            float farBefore = far.Current;

            yield return Steps(20);

            Assert.AreEqual(1, _controller.ZapCount, "precondition: exactly one discharge");
            Assert.Less(near.Current, nearBefore, "TES-001 B2: the nearer enemy is the target");
            Assert.AreEqual(farBefore, far.Current, 0.001f, "the farther enemy must be untouched");
        }

        // ---- B3 ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Tes001_NoEnemyInRange_DoesNothing()
        {
            BuildRig();
            AttachForward(Tesla(range: 3f));
            EnemyHealth enemy = PlaceEnemy(new Vector3(0f, 0f, 10f));
            float before = enemy.Current;

            yield return Steps(30);

            AssertEnemyStillAt(enemy, new Vector3(0f, 0f, 10f));
            Assert.AreEqual(0, _controller.ZapCount, "TES-001 B3: out of reach means no discharge");
            Assert.AreEqual(before, enemy.Current, 0.001f);
        }

        // ---- B4: the distance gradient — this feature's whole point ---------------

        [UnityTest]
        public IEnumerator Tes001_CloserEnemy_TakesMoreDamage()
        {
            // Two identical runs, only the distance differs.
            BuildRig(hitCooldown: 5f);
            AttachForward(Tesla(interval: 999f, range: 5f, maxDamage: 12f, minDamage: 4f));
            EnemyHealth close = PlaceEnemy(new Vector3(0f, 0f, 0.55f)); // right on the ring
            float closeBefore = close.Current;

            yield return Steps(20);
            float closeDamage = closeBefore - close.Current;

            TearDown();

            BuildRig(hitCooldown: 5f);
            AttachForward(Tesla(interval: 999f, range: 5f, maxDamage: 12f, minDamage: 4f));
            EnemyHealth farAway = PlaceEnemy(new Vector3(0f, 0f, 5.5f)); // ~4.95m from the ring
            float farBefore = farAway.Current;

            yield return Steps(20);
            float farDamage = farBefore - farAway.Current;

            Assert.Greater(closeDamage, 0f, "precondition: the close run actually landed");
            Assert.Greater(farDamage, 0f, "precondition: the far run actually landed");
            Assert.Greater(closeDamage, farDamage * 2f,
                $"TES-001 B4: contact must be worth far more than the edge (close={closeDamage:0.0}, far={farDamage:0.0}) — " +
                "without this the tesla is just a cannon that cannot miss");
        }

        // ---- B5 ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Tes001_RangeIsFromWeapon_NotBallCentre()
        {
            BuildRig();

            // The ring sits at +Z 0.55. An enemy at 5.4m from the centre is 4.85m from
            // the ring, so a 5m reach measured from the ring reaches it and one
            // measured from the centre does not.
            AttachForward(Tesla(range: 5f));
            EnemyHealth enemy = PlaceEnemy(new Vector3(0f, 0f, 5.4f));
            float before = enemy.Current;

            yield return Steps(20);

            AssertEnemyStillAt(enemy, new Vector3(0f, 0f, 5.4f));
            Assert.Greater(_controller.ZapCount, 0,
                "TES-001 B5: reach is measured from the ring's attachment point, not the ball's centre");
            Assert.Less(enemy.Current, before);
        }

        // ---- B7/B8 ---------------------------------------------------------------

        [UnityTest]
        public IEnumerator Tes001_ZapSharesHitCooldown()
        {
            BuildRig(hitCooldown: 5f);
            AttachForward(Tesla(interval: 0.05f, maxDamage: 12f, minDamage: 4f));
            EnemyHealth enemy = PlaceEnemy(new Vector3(0f, 0f, 2f));
            float before = enemy.Current;

            yield return Steps(60);

            AssertEnemyStillAt(enemy, new Vector3(0f, 0f, 2f));
            Assert.AreEqual(1, _controller.ZapCount,
                "DMG-002 + B8: with the only enemy on cooldown there is nothing eligible, so no further discharge happens");
            Assert.Greater(before - enemy.Current, 0f);
        }

        // B8 — the discharge must go to someone who can actually be hurt.
        [UnityTest]
        public IEnumerator Tes001_CooldownEnemy_IsNotChosen()
        {
            BuildRig(hitCooldown: 5f);
            AttachForward(Tesla(interval: 999f));

            EnemyHealth near = PlaceEnemy(new Vector3(0f, 0f, 1.5f));
            EnemyHealth far = PlaceEnemy(new Vector3(0f, 0f, 4f));

            // Put the nearer one inside its DMG-002 cooldown before the first discharge.
            near.TryTakeDamage(1f, Time.time);
            float nearAfterSetup = near.Current;
            float farBefore = far.Current;

            yield return Steps(20);

            Assert.AreEqual(1, _controller.ZapCount);
            Assert.AreEqual(nearAfterSetup, near.Current, 0.001f, "the cooling enemy must be skipped");
            Assert.Less(far.Current, farBefore,
                "TES-001 B8: the discharge goes to the next eligible enemy instead of being wasted on a cooling one");
        }

        // ---- B9 ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator Tes001_DeadEnemy_IsNotChosen()
        {
            BuildRig(hitCooldown: 0f);
            AttachForward(Tesla(interval: 999f));

            EnemyHealth near = PlaceEnemy(new Vector3(0f, 0f, 1.5f));
            EnemyHealth far = PlaceEnemy(new Vector3(0f, 0f, 4f));

            near.TryTakeDamage(near.Current, Time.time); // kill it outright
            Assert.IsFalse(near.IsAlive, "precondition");
            float farBefore = far.Current;

            yield return Steps(20);

            Assert.Less(far.Current, farBefore, "TES-001 B9: corpses are not targets");
        }

        // ---- B11 -----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn008_ProjectileWeapon_DoesNotZap()
        {
            BuildRig();
            AttachForward(Cannon());
            PlaceEnemy(new Vector3(0f, 0f, 2f));

            yield return Steps(30);

            Assert.AreEqual(0, _controller.ZapCount, "WPN-008: a projectile weapon does not discharge");
        }

        // ---- B12 -----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Tes001_AttachedWeapon_HasNoCollider()
        {
            BuildRig();
            AttachForward(Tesla());

            GameObject weapon = _slots.AttachedAt(0);

            // Checked immediately: Destroy is deferred, so "next frame" would hide a
            // collider that is alive for one physics step.
            Assert.AreEqual(0, weapon.GetComponentsInChildren<Collider>(true).Length,
                "B12: weapons stay collider-free so the ball's rolling is untouched");
            yield return null;
        }

        // ---- B13 -----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Tes001_SessionEnd_StopsZapping()
        {
            BuildRig(hitCooldown: 0f);

            var sessionGo = new GameObject("session", typeof(GameSession));
            var session = sessionGo.GetComponent<GameSession>();
            session.PlayerHealth = _playerHealth;
            _cleanup.Add(sessionGo);
            _controller.Session = session;

            AttachForward(Tesla(interval: 0.05f));
            PlaceEnemy(new Vector3(0f, 0f, 2f));

            yield return Steps(10);
            int before = _controller.ZapCount;
            Assert.Greater(before, 0, "precondition: it was discharging");

            session.End(SessionOutcome.Defeat);
            yield return null; // Ended is raised in LateUpdate

            // Plain frames, not WaitForFixedUpdate: the session sets timeScale to 0,
            // so FixedUpdate never runs again and Steps() would hang forever.
            for (int i = 0; i < 10; i++)
            {
                yield return null;
            }

            Assert.AreEqual(before, _controller.ZapCount,
                "B13: the field must not keep firing underneath the result screen");
        }

        // ---- B14: the line exists, and it is not vacuous --------------------------

        [UnityTest]
        public IEnumerator Tes001_ZappedEvent_CarriesBothEndpoints()
        {
            BuildRig();
            AttachForward(Tesla(interval: 999f));
            EnemyHealth enemy = PlaceEnemy(new Vector3(0f, 0f, 3f));

            Vector3 from = Vector3.positiveInfinity;
            Vector3 to = Vector3.positiveInfinity;
            int raised = 0;
            _controller.Zapped += (a, b) => { from = a; to = b; raised++; };

            yield return Steps(20);

            Assert.AreEqual(1, raised, "B14: exactly one event per discharge");
            Assert.Less((from - _slots.AttachedAt(0).transform.position).magnitude, 0.01f,
                "the line must start at the ring");
            Assert.Less((to - enemy.transform.position).magnitude, 0.01f,
                "the line must end at the enemy that was actually hit");
        }

        [UnityTest]
        public IEnumerator Tes001_ZapVisual_FlashesOnDischarge()
        {
            BuildRig();
            AttachForward(Tesla(interval: 999f));
            PlaceEnemy(new Vector3(0f, 0f, 3f));

            var visualGo = new GameObject("zap_visual", typeof(LineRenderer), typeof(ZapVisual));
            var visual = visualGo.GetComponent<ZapVisual>();
            visual.Controller = _controller;
            visual.FlashDuration = 10f; // outlast the test window, so "still lit" is meaningful
            visual.enabled = false;
            visual.enabled = true; // re-run OnEnable so it subscribes
            _cleanup.Add(visualGo);

            yield return Steps(20);

            Assert.AreEqual(1, visual.FlashCount,
                "B14: without a visible line, 'does the tesla work' is unanswerable by a human");

            var line = visualGo.GetComponent<LineRenderer>();
            Assert.IsTrue(line.enabled, "the line must actually be switched on, not just counted");
            Assert.Greater((line.GetPosition(0) - line.GetPosition(1)).magnitude, 0.1f,
                "the two endpoints must differ — a zero-length line renders nothing");
        }
    }
}
