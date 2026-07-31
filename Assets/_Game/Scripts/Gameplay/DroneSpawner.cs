using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.Pool;

namespace Game.Gameplay
{
    /// <summary>
    /// WAVE-001/WAVE-002 director for the 90s First Playable curve: keeps the
    /// live drone count on the budget curve, spawns on a ring outside the camera
    /// cone, and recycles instances through an ObjectPool (B11, 기획서 14.5).
    /// Also drives every drone's Tick so neighbour data is gathered once per step.
    /// </summary>
    public sealed class DroneSpawner : MonoBehaviour
    {
        [SerializeField] private DroneConfig config;
        [SerializeField] private ScrapDrone dronePrefab;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Transform cameraTransform;

        private ObjectPool<ScrapDrone> _pool;
        private readonly List<ScrapDrone> _active = new List<ScrapDrone>();
        private Float3[] _positions = new Float3[64];

        private float _elapsed;
        private float _sinceLastSpawn;
        private float _nextAngle;

        public int ActiveCount => _active.Count;
        public int TotalCreated { get; private set; }
        public float Elapsed => _elapsed;

        public DroneConfig Config { get => config; set => config = value; }
        public ScrapDrone DronePrefab { get => dronePrefab; set => dronePrefab = value; }
        public Transform Player { get => player; set => player = value; }
        public PlayerHealth PlayerHealth { get => playerHealth; set => playerHealth = value; }
        public Transform CameraTransform { get => cameraTransform; set => cameraTransform = value; }

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            EnsurePool();
        }

        private void EnsurePool()
        {
            if (_pool != null || dronePrefab == null)
            {
                return;
            }

            _pool = new ObjectPool<ScrapDrone>(
                createFunc: () =>
                {
                    TotalCreated++;
                    ScrapDrone drone = Instantiate(dronePrefab, transform);
                    drone.gameObject.SetActive(false);
                    return drone;
                },
                actionOnGet: drone => drone.gameObject.SetActive(true),
                actionOnRelease: drone => drone.gameObject.SetActive(false),
                actionOnDestroy: drone => Destroy(drone.gameObject),
                collectionCheck: true,
                defaultCapacity: 16,
                maxSize: 128);
        }

        /// <summary>Clears the field and restarts the curve (used by tests and, later, retry).</summary>
        public void ResetSession()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                _pool?.Release(_active[i]);
            }

            _active.Clear();
            _elapsed = 0f;
            _sinceLastSpawn = 0f;
            _nextAngle = 0f;
        }

        private void FixedUpdate()
        {
            if (config == null || player == null || dronePrefab == null)
            {
                return;
            }

            EnsurePool();

            float dt = Time.fixedDeltaTime;
            _elapsed += dt;
            _sinceLastSpawn += dt;

            TrySpawn();
            TickDrones(dt);
            RecycleDistant();
        }

        private void TrySpawn()
        {
            SpawnBudgetConfig budget = config.ToSpawnBudgetConfig();
            int target = SpawnBudgetLogic.TargetCount(_elapsed, budget);
            if (!SpawnBudgetLogic.ShouldSpawn(_active.Count, target, _sinceLastSpawn, budget))
            {
                return;
            }

            _sinceLastSpawn = 0f;
            Spawn();
        }

        /// <summary>B6/B7: ring position outside the camera cone, angles spread by the golden step.</summary>
        public void Spawn()
        {
            EnsurePool();
            if (_pool == null)
            {
                return;
            }

            Float3 camForward = cameraTransform != null
                ? new Float3(cameraTransform.forward.x, 0f, cameraTransform.forward.z)
                : new Float3(0f, 0f, 1f);

            float angle = SpawnRingLogic.FirstAngleOutsideCone(_nextAngle, config.angleStep, camForward, config.cameraConeHalfAngle);
            _nextAngle = SpawnRingLogic.NextAngle(angle, config.angleStep);

            float radius = Random.Range(config.ringRadiusMin, config.ringRadiusMax);
            Float3 position = SpawnRingLogic.SpawnPosition(ToFloat3(player.position), angle, radius);

            ScrapDrone drone = _pool.Get();
            drone.transform.position = new Vector3(position.X, player.position.y, position.Z);
            drone.Initialise(player, playerHealth, config, this);
            _active.Add(drone);
        }

        private void TickDrones(float deltaTime)
        {
            int count = _active.Count;
            if (_positions.Length < count)
            {
                _positions = new Float3[Mathf.NextPowerOfTwo(count)];
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 p = _active[i].transform.position;
                _positions[i] = new Float3(p.x, p.y, p.z);
            }

            for (int i = 0; i < count; i++)
            {
                _active[i].Tick(deltaTime, _positions, count);
            }
        }

        /// <summary>B11: returns a drone to the pool (corpse expiry or cleanup).</summary>
        public void Despawn(ScrapDrone drone)
        {
            if (drone == null || _pool == null)
            {
                return;
            }

            if (!_active.Remove(drone))
            {
                return; // already released — never double-release into the pool
            }

            _pool.Release(drone);
        }

        // B10: drones that drift too far come back to the pool.
        private void RecycleDistant()
        {
            float sqrLimit = config.despawnDistance * config.despawnDistance;
            Vector3 playerPos = player.position;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ScrapDrone drone = _active[i];
                if ((drone.transform.position - playerPos).sqrMagnitude <= sqrLimit)
                {
                    continue;
                }

                _active.RemoveAt(i);
                _pool.Release(drone);
            }
        }

        private static Float3 ToFloat3(Vector3 v) => new Float3(v.x, v.y, v.z);
    }
}
