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
    /// PlayMode coverage for F05 (Docs/Features/F05-drone-spawner.md).
    /// </summary>
    public sealed class DroneSpawnerPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private DroneConfig _config;
        private DroneSpawner _spawner;
        private PlayerHealth _health;
        private GameObject _player;
        private Transform _camera;

        private void BuildRig(bool fastRamp = true)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            _cleanup.Add(ground);

            var cameraGo = new GameObject("test_camera");
            cameraGo.transform.SetPositionAndRotation(new Vector3(0f, 8f, -10f), Quaternion.Euler(35f, 0f, 0f));
            _camera = cameraGo.transform;
            _cleanup.Add(cameraGo);

            _config = ScriptableObject.CreateInstance<DroneConfig>();
            _config.moveSpeed = 6f;
            _config.contactDamage = 8f;
            _config.startCount = fastRamp ? 3 : 5;
            _config.endCount = fastRamp ? 3 : 25;
            _config.rampSeconds = 90f;
            _config.hardCap = 30;
            _config.spawnInterval = 0.05f;
            _config.ringRadiusMin = 8f;
            _config.ringRadiusMax = 10f;
            _config.despawnDistance = 45f;
            _config.playerMaxHealth = 100f;
            _config.invulnerabilityDuration = 0.45f;
            _cleanup.Add(_config);

            // Mirror the real Player: dynamic Rigidbody (kinematic↔kinematic pairs
            // raise no physics events, so a kinematic test player would never be hit).
            _player = new GameObject("test_player", typeof(SphereCollider), typeof(Rigidbody), typeof(PlayerHealth));
            _player.transform.position = new Vector3(0f, 0.5f, 0f);
            var playerBody = _player.GetComponent<Rigidbody>();
            playerBody.isKinematic = false;
            playerBody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            _health = _player.GetComponent<PlayerHealth>();
            _health.Config = _config;
            _cleanup.Add(_player);

            // Mirror Drone.prefab: trigger collider (B18) so the swarm never walls the player in.
            var prefabGo = new GameObject("drone_prefab", typeof(SphereCollider), typeof(Rigidbody), typeof(ScrapDrone));
            prefabGo.GetComponent<SphereCollider>().isTrigger = true;
            prefabGo.SetActive(false);
            _cleanup.Add(prefabGo);

            var spawnerGo = new GameObject("spawner", typeof(DroneSpawner));
            _spawner = spawnerGo.GetComponent<DroneSpawner>();
            _spawner.Config = _config;
            _spawner.DronePrefab = prefabGo.GetComponent<ScrapDrone>();
            _spawner.Player = _player.transform;
            _spawner.PlayerHealth = _health;
            _spawner.CameraTransform = _camera;
            _cleanup.Add(spawnerGo);
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

        // B3, B4 — WAVE-001
        [UnityTest]
        public IEnumerator Wave001_Spawner_ReachesTargetCount()
        {
            BuildRig();
            yield return WaitPhysics(1f);

            Assert.AreEqual(3, _spawner.ActiveCount, "WAVE-001: spawner must fill up to the target count");
        }

        // B5 — WAVE-001
        [UnityTest]
        public IEnumerator Wave001_ActiveCount_NeverExceedsHardCap()
        {
            BuildRig(fastRamp: false);
            _config.startCount = 100;   // demand far beyond the cap
            _config.endCount = 100;
            _config.hardCap = 6;

            yield return WaitPhysics(1.5f);

            Assert.LessOrEqual(_spawner.ActiveCount, 6, "WAVE-001: hard cap must hold");
        }

        // B6 — WAVE-002
        [UnityTest]
        public IEnumerator Wave002_SpawnedDrones_AppearOutsideCameraCone()
        {
            BuildRig();
            yield return WaitPhysics(1f);

            var drones = Object.FindObjectsByType<ScrapDrone>(FindObjectsInactive.Exclude);
            int checkedCount = 0;
            foreach (ScrapDrone drone in drones)
            {
                if (!drone.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 offset = drone.transform.position - _player.transform.position;
                var dir = new Float3(offset.x, 0f, offset.z);
                var camForward = new Float3(_camera.forward.x, 0f, _camera.forward.z);

                Assert.IsFalse(SpawnRingLogic.IsInsideCameraCone(dir, camForward, _config.cameraConeHalfAngle),
                    "WAVE-002: drones must not spawn inside the camera front cone");
                checkedCount++;
            }

            Assert.Greater(checkedCount, 0, "sanity: at least one drone should exist");
        }

        // B1 — ENM-001
        [UnityTest]
        public IEnumerator Enm001_Drone_ClosesDistanceToPlayer()
        {
            BuildRig();
            yield return WaitPhysics(0.3f);

            var drone = Object.FindAnyObjectByType<ScrapDrone>();
            Assert.IsNotNull(drone, "sanity: a drone must exist");
            float before = Vector3.Distance(drone.transform.position, _player.transform.position);

            yield return WaitPhysics(0.8f);
            float after = Vector3.Distance(drone.transform.position, _player.transform.position);

            Assert.Less(after, before - 0.5f, $"ENM-001: drone must close in ({before:0.##} → {after:0.##})");
        }

        // B2 — Decision 0001
        [UnityTest]
        public IEnumerator Enm001_Drone_IsKinematic()
        {
            BuildRig();
            yield return WaitPhysics(0.3f);

            var drone = Object.FindAnyObjectByType<ScrapDrone>();
            Assert.IsTrue(drone.GetComponent<Rigidbody>().isKinematic,
                "Decision 0001: drones stay kinematic while alive");
        }

        // B11 — pooling
        [UnityTest]
        public IEnumerator Wave001_Pool_ReusesInstances()
        {
            BuildRig();
            yield return WaitPhysics(1f);
            int createdAfterFirstFill = _spawner.TotalCreated;
            Assert.Greater(createdAfterFirstFill, 0, "sanity: drones were created");

            for (int cycle = 0; cycle < 3; cycle++)
            {
                _spawner.ResetSession();
                yield return WaitPhysics(1f);
            }

            Assert.AreEqual(createdAfterFirstFill, _spawner.TotalCreated,
                "B11: refilling must reuse pooled drones instead of instantiating new ones");
        }

        // B13 — HP-001
        [UnityTest]
        public IEnumerator Hp001_DroneContact_DamagesPlayer()
        {
            BuildRig();
            yield return WaitPhysics(0.3f);

            float before = _health.Current;
            var drone = Object.FindAnyObjectByType<ScrapDrone>();
            drone.transform.position = _player.transform.position + new Vector3(0.6f, 0f, 0f);

            yield return WaitPhysics(0.6f);

            Assert.Less(_health.Current, before, "HP-001: drone contact must damage the player");
        }

        // B14 — HP-001
        [UnityTest]
        public IEnumerator Hp001_ContinuousContact_LimitedByInvulnerability()
        {
            BuildRig();
            yield return WaitPhysics(0.3f);

            var drone = Object.FindAnyObjectByType<ScrapDrone>();
            drone.transform.position = _player.transform.position + new Vector3(0.6f, 0f, 0f);

            yield return WaitPhysics(1f); // ~2 invulnerability windows at 0.45s
            float lost = _config.playerMaxHealth - _health.Current;

            Assert.Greater(lost, 0f, "sanity: contact happened");
            Assert.LessOrEqual(lost, _config.contactDamage * 3f,
                $"HP-001: invulnerability must throttle continuous contact (lost {lost})");
        }
    }
}
