using System.Collections;
using System.Collections.Generic;
using Game.Gameplay;
using Game.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    /// <summary>
    /// PlayMode coverage for F06 (Docs/Features/F06-collision-combat.md).
    /// </summary>
    public sealed class CollisionCombatPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private DroneConfig _config;
        private BallMovementConfig _moveConfig;
        private GameObject _player;
        private Rigidbody _playerBody;
        private PlayerDamageDealer _dealer;
        private BallMotor _motor;

        private void BuildRig()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            _cleanup.Add(ground);

            var cameraGo = new GameObject("test_camera");
            cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 8f, -10f), Quaternion.Euler(35f, 0f, 0f));
            _cleanup.Add(cameraGo);

            _moveConfig = ScriptableObject.CreateInstance<BallMovementConfig>();
            _moveConfig.maxSpeed = 12f;
            _moveConfig.timeToMaxSpeed = 1f;
            _cleanup.Add(_moveConfig);

            _config = ScriptableObject.CreateInstance<DroneConfig>();
            _config.droneMaxHealth = 10f;
            _config.baseCollisionDamage = 10f;
            _config.contactDamage = 8f;
            _config.hitCooldown = 0.25f;
            _config.hitStopDamageThreshold = 5f;
            _config.hitStopReferenceDamage = 15f;
            _config.hitStopMinDuration = 0.05f;
            _config.hitStopMaxDuration = 0.09f;
            _config.hitStopRefractory = 0f;   // gate tested separately; here we want every qualifying hit
            _config.knockbackForce = 9f;
            _config.knockbackRecovery = 0.35f;
            _config.corpseTime = 0.4f;
            _config.moveSpeed = 0f;              // drones hold still: isolate player-side combat
            _config.startCount = 0;
            _config.endCount = 0;
            _config.playerMaxHealth = 100f;
            _cleanup.Add(_config);

            _player = new GameObject("test_player",
                typeof(SphereCollider), typeof(Rigidbody), typeof(BallMotor), typeof(PlayerHealth), typeof(PlayerDamageDealer));
            _player.transform.position = new Vector3(0f, 0.5f, 0f);
            _playerBody = _player.GetComponent<Rigidbody>();
            _playerBody.useGravity = false;
            _playerBody.linearDamping = 0f;
            _motor = _player.GetComponent<BallMotor>();
            _motor.Config = _moveConfig;
            _motor.CameraTransform = cameraGo.transform;
            _motor.enabled = false;              // drive velocity directly for determinism
            _player.GetComponent<PlayerHealth>().Config = _config;
            _dealer = _player.GetComponent<PlayerDamageDealer>();
            _dealer.Config = _config;
            _dealer.MovementConfig = _moveConfig;
            _dealer.Motor = _motor;
            _cleanup.Add(_player);
        }

        private ScrapDrone SpawnDrone(Vector3 position)
        {
            var go = new GameObject("test_drone", typeof(SphereCollider), typeof(Rigidbody), typeof(EnemyHealth), typeof(ScrapDrone));
            go.transform.position = position;
            go.GetComponent<SphereCollider>().isTrigger = true;
            var drone = go.GetComponent<ScrapDrone>();
            drone.Initialise(null, null, _config, null);
            _cleanup.Add(go);
            return drone;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f; // never leak a hit-stop freeze into other tests
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

        // B1, B11 — DMG-001
        [UnityTest]
        public IEnumerator Dmg001_FastCollision_KillsDrone()
        {
            BuildRig();
            var drone = SpawnDrone(new Vector3(0f, 0.5f, 2f));
            yield return WaitPhysics(0.1f);

            _playerBody.linearVelocity = new Vector3(0f, 0f, 12f); // full speed, head-on
            yield return WaitPhysics(0.5f);

            Assert.IsTrue(drone.IsDead, "DMG-001: a full-speed head-on hit must kill a drone");
            Assert.AreEqual(1, _dealer.KillCount);
        }

        // B1, B2 — DMG-001
        [UnityTest]
        public IEnumerator Dmg001_SlowContact_DoesNotKillInstantly()
        {
            BuildRig();
            var drone = SpawnDrone(new Vector3(0f, 0.5f, 0.9f)); // already overlapping
            yield return WaitPhysics(0.1f);

            _playerBody.linearVelocity = new Vector3(0f, 0f, 0.5f); // crawling
            yield return WaitPhysics(0.8f);

            Assert.Greater(_dealer.HitCount, 0, "sanity: contact happened");
            Assert.IsFalse(drone.IsDead, "DMG-001: slow contact must not one-shot a drone");
        }

        // B10 — Decision 0001
        [UnityTest]
        public IEnumerator Enm004_HitDrone_BecomesDynamicThenRecovers()
        {
            BuildRig();
            _config.droneMaxHealth = 1000f; // survive the hit so recovery can be observed
            var drone = SpawnDrone(new Vector3(0f, 0.5f, 2f));
            yield return WaitPhysics(0.1f);

            _playerBody.linearVelocity = new Vector3(0f, 0f, 12f);
            yield return WaitPhysics(0.2f);

            Assert.IsTrue(drone.IsRagdolling, "B10: a hit drone must switch to a dynamic body");

            _playerBody.linearVelocity = Vector3.zero;
            yield return WaitPhysics(_config.knockbackRecovery + 0.2f);

            Assert.IsFalse(drone.IsRagdolling, "B10: a surviving drone must return to kinematic chasing");
        }

        // B19 — a knockback that lofts a drone must not leave it hovering
        [UnityTest]
        public IEnumerator Enm004_LoftedDrone_ReturnsToChaseHeight()
        {
            BuildRig();
            const float chaseHeight = 0.5f;
            var drone = SpawnDrone(new Vector3(0f, chaseHeight, 3f));
            yield return WaitPhysics(0.1f);

            drone.TakeKnockback(new Vector3(0f, 12f, 0f)); // straight up
            yield return WaitPhysics(0.15f);
            Assert.Greater(drone.transform.position.y, chaseHeight + 0.3f,
                "sanity: the impulse must actually loft the drone");

            yield return WaitPhysics(_config.knockbackRecovery + 0.2f);

            Assert.IsFalse(drone.IsRagdolling, "sanity: the drone must have recovered");
            Assert.That(drone.transform.position.y, Is.EqualTo(chaseHeight).Within(1e-2f),
                "B19: a lofted drone must drop back to its chase height, not hover");
        }

        // B11, B14 — ENM-004 (corpse lingers, returns to the pool, and is reset on reuse)
        [UnityTest]
        public IEnumerator Enm004_DeadDrone_ReturnsToPoolAfterCorpseTime()
        {
            BuildRig();

            // Pool template: inactive so it is never itself a live drone.
            var template = new GameObject("drone_template",
                typeof(SphereCollider), typeof(Rigidbody), typeof(EnemyHealth), typeof(ScrapDrone));
            template.GetComponent<SphereCollider>().isTrigger = true;
            template.SetActive(false);
            _cleanup.Add(template);

            var spawnerGo = new GameObject("spawner", typeof(DroneSpawner));
            var spawner = spawnerGo.GetComponent<DroneSpawner>();
            spawner.Config = _config;
            spawner.Player = _player.transform;
            spawner.DronePrefab = template.GetComponent<ScrapDrone>();
            _cleanup.Add(spawnerGo);

            // Spawn it right next to the player — the drone's own Tick would drag a
            // teleported drone back to its spawn ring.
            _config.ringRadiusMin = 2f;
            _config.ringRadiusMax = 2f;
            _config.cameraConeHalfAngle = 0f;

            spawner.Spawn();
            Assert.AreEqual(1, spawner.ActiveCount, "sanity: the spawner must own the drone");
            ScrapDrone drone = spawnerGo.GetComponentInChildren<ScrapDrone>();
            yield return WaitPhysics(0.1f);

            // Charge it head-on, whichever way the ring placed it.
            Vector3 toDrone = drone.transform.position - _player.transform.position;
            toDrone.y = 0f;
            _playerBody.linearVelocity = toDrone.normalized * 12f;
            yield return WaitPhysics(0.4f);
            Assert.IsTrue(drone.IsDead, "sanity: drone must die first");
            Assert.IsTrue(drone.gameObject.activeInHierarchy, "ENM-004: the corpse must not vanish instantly");

            _playerBody.linearVelocity = Vector3.zero;
            yield return WaitPhysics(_config.corpseTime + 0.3f);

            Assert.AreEqual(0, spawner.ActiveCount, "B11: the corpse must return to the pool after the corpse time");
            Assert.IsFalse(drone.gameObject.activeInHierarchy, "B11: a pooled drone must be deactivated");

            // B14 — reuse must reset the instance instead of creating a new one.
            spawner.Spawn();
            Assert.AreEqual(1, spawner.TotalCreated, "B14: the pooled instance must be reused, not re-created");
            Assert.IsFalse(drone.IsDead, "B14: a reused drone must have its health reset");
            Assert.IsFalse(drone.IsRagdolling, "B14: a reused drone must be kinematic again");
        }

        // B6 — DMG-002
        [UnityTest]
        public IEnumerator Dmg002_SingleCollision_DamagesOnce()
        {
            BuildRig();
            _config.droneMaxHealth = 1000f;   // survive so we can count hits
            _config.hitCooldown = 5f;         // long cooldown: only one hit may land
            var drone = SpawnDrone(new Vector3(0f, 0.5f, 1.2f));
            yield return WaitPhysics(0.1f);

            _playerBody.linearVelocity = new Vector3(0f, 0f, 2f);
            yield return WaitPhysics(0.8f);   // many physics frames of contact

            Assert.AreEqual(1, _dealer.HitCount,
                "DMG-002: one collision must deal damage once, not once per frame");
        }

        // B12 — dead drones stop damaging the player
        [UnityTest]
        public IEnumerator Enm004_DeadDrone_StopsDamagingPlayer()
        {
            BuildRig();
            var drone = SpawnDrone(new Vector3(0f, 0.5f, 2f));
            var playerHealth = _player.GetComponent<PlayerHealth>();
            yield return WaitPhysics(0.1f);

            _playerBody.linearVelocity = new Vector3(0f, 0f, 12f);
            yield return WaitPhysics(0.3f);
            Assert.IsTrue(drone.IsDead, "sanity: drone must be dead");

            _playerBody.linearVelocity = Vector3.zero;
            drone.transform.position = _player.transform.position; // sit inside the player
            float hpAfterDeath = playerHealth.Current;
            yield return WaitPhysics(1f);

            Assert.That(playerHealth.Current, Is.EqualTo(hpAfterDeath).Within(1e-3f),
                "B12: a dead drone must not keep damaging the player");
        }

        // B17 — hit stop restores time scale
        [UnityTest]
        public IEnumerator HitStop_StrongHit_FreezesThenRestoresTimeScale()
        {
            BuildRig();
            var stop = _player.AddComponent<HitStopController>();
            stop.Dealer = _dealer;
            stop.Config = _config;
            stop.enabled = false;
            stop.enabled = true; // re-subscribe after wiring

            var drone = SpawnDrone(new Vector3(0f, 0.5f, 2f));
            yield return WaitPhysics(0.1f);

            _playerBody.linearVelocity = new Vector3(0f, 0f, 12f);
            yield return WaitPhysics(0.3f);

            Assert.Greater(_dealer.HitCount, 0, "sanity: a hit must have landed");
            Assert.Greater(stop.FreezeCount, 0,
                "B15: a full-speed hit is above the damage threshold and must actually freeze time");

            float deadline = Time.realtimeSinceStartup + 1f;
            while (stop.IsFrozen && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsFalse(stop.IsFrozen, "hit stop must end");
            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(1e-3f), "B17: time scale must be restored");
        }

        // DMG-005 / B20 — the refractory gate holds in a real scene
        [UnityTest]
        public IEnumerator Dmg005_RepeatedHits_DoNotChainFreezes()
        {
            BuildRig();
            _config.droneMaxHealth = 1000f;   // survive so hits keep landing
            _config.hitCooldown = 0.05f;      // hit the same drone often
            _config.hitStopRefractory = 5f;   // far longer than the test window

            var stop = _player.AddComponent<HitStopController>();
            stop.Dealer = _dealer;
            stop.Config = _config;
            stop.enabled = false;
            stop.enabled = true;

            var drone = SpawnDrone(new Vector3(0f, 0.5f, 1.2f));
            yield return WaitPhysics(0.1f);

            _playerBody.linearVelocity = new Vector3(0f, 0f, 12f);
            yield return WaitPhysics(1f);

            Assert.Greater(_dealer.HitCount, 1, "sanity: several hits must have landed");
            Assert.AreEqual(1, stop.FreezeCount,
                "DMG-005: repeated hits inside the refractory window must produce exactly one freeze");
        }

        // B18 — disable restores time scale
        [UnityTest]
        public IEnumerator HitStop_OnDisable_RestoresTimeScale()
        {
            BuildRig();
            var stop = _player.AddComponent<HitStopController>();
            stop.Dealer = _dealer;
            stop.Config = _config;
            stop.enabled = false;
            stop.enabled = true;

            var drone = SpawnDrone(new Vector3(0f, 0.5f, 2f));
            yield return WaitPhysics(0.1f);

            _playerBody.linearVelocity = new Vector3(0f, 0f, 12f);
            yield return WaitPhysics(0.3f);

            stop.enabled = false;
            yield return null;

            Assert.That(Time.timeScale, Is.EqualTo(1f).Within(1e-3f),
                "B18: disabling the controller must never leave the game frozen");
        }
    }
}
