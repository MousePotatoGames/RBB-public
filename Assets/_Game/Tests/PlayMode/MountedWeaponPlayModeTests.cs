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
    /// PlayMode coverage for F12 (Docs/Features/F12-orbit-follow-mounts.md).
    /// </summary>
    public sealed class MountedWeaponPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private DroneConfig _droneConfig;
        private WeaponConfig _weaponConfig;
        private DroneSpawner _spawner;
        private GameObject _ball;
        private WeaponSlots _slots;
        private WeaponContainer _container;
        private SweepContactController _sweep;
        private PlayerHealth _playerHealth;

        // ---- definitions ---------------------------------------------------------

        private WeaponDefinition Axe(
            float radius = 2.2f,
            float angularSpeed = 180f,
            float sweepRadius = 0.7f,
            float sweepInterval = 0.05f,
            float damage = 9f)
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.kind = WeaponKind.Axe;
            def.displayName = "test axe";
            def.mount = WeaponMount.Orbit;
            def.attack = WeaponAttack.Contact;
            def.orbitRadius = radius;
            def.orbitAngularSpeed = angularSpeed;
            def.orbitHeight = 0f;
            def.sweepRadius = sweepRadius;
            def.sweepInterval = sweepInterval;
            def.sweepDamage = damage;
            def.shape = PrimitiveType.Cube;
            def.scale = 0.35f;
            _cleanup.Add(def);
            return def;
        }

        private WeaponDefinition Pet(float standoff = 1.8f, float followSpeed = 6f)
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.kind = WeaponKind.Pet;
            def.displayName = "test pet";
            def.mount = WeaponMount.Follow;
            def.attack = WeaponAttack.Projectile;
            def.followStandoff = standoff;
            def.followSpeed = followSpeed;
            def.travel = ProjectileTravel.Straight;
            def.fireInterval = 0.1f;
            def.shotsPerBurst = 1;
            def.projectileSpeed = 14f;
            def.projectileRange = 10f;
            def.projectileDamage = 5f;
            def.projectileRadius = 0.15f;
            def.shape = PrimitiveType.Sphere;
            def.scale = 0.32f;
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

        // ---- rig -----------------------------------------------------------------

        private void BuildRig(float hitCooldown = 0.35f)
        {
            _droneConfig = ScriptableObject.CreateInstance<DroneConfig>();
            _droneConfig.moveSpeed = 0f;
            _droneConfig.contactDamage = 0f;
            _droneConfig.droneMaxHealth = 1000f;
            _droneConfig.hitCooldown = hitCooldown;
            _droneConfig.startCount = 1;
            _droneConfig.endCount = 1;
            _droneConfig.rampSeconds = 90f;
            _droneConfig.hardCap = 8;
            _droneConfig.spawnInterval = 999f;
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

            var containerGo = new GameObject("weapon_container", typeof(WeaponContainer));
            _container = containerGo.GetComponent<WeaponContainer>();
            _cleanup.Add(containerGo);

            _ball = new GameObject("test_ball", typeof(SphereCollider), typeof(Rigidbody), typeof(PlayerHealth));
            _ball.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            _ball.GetComponent<Rigidbody>().isKinematic = true;
            _playerHealth = _ball.GetComponent<PlayerHealth>();
            _playerHealth.Config = _droneConfig;
            _slots = _ball.AddComponent<WeaponSlots>();
            _slots.Config = _weaponConfig;
            _slots.Container = _container;
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

            _sweep = _ball.AddComponent<SweepContactController>();
            _sweep.Weapons = _slots;
            _sweep.Enemies = _spawner;
            _sweep.ResetCooldowns();
        }

        private GameObject Attach(WeaponDefinition definition) =>
            _slots.AttachedAt(_slots.TryAttach(definition, _ball.transform.TransformPoint(Vector3.forward * 0.5f)));

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

        // ---- B7/B8: where each mount lives ---------------------------------------

        [UnityTest]
        public IEnumerator Wpn007_OrbitWeapon_IsNotChildOfBall()
        {
            BuildRig();
            GameObject axe = Attach(Axe());

            Assert.AreNotEqual(_ball.transform, axe.transform.parent,
                "WPN-007 B7: an orbit weapon parented to the ball would inherit its rotation");
            Assert.AreEqual(_container.transform, axe.transform.parent);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Wpn007_SurfaceWeapon_IsStillChildOfBall()
        {
            BuildRig();
            GameObject spike = Attach(Spike());

            Assert.AreEqual(_ball.transform, spike.transform.parent,
                "WPN-002 B8 regression: surface weapons must keep riding the ball");
            yield return null;
        }

        // ---- B1/B2: orbit motion --------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn007_OrbitWeapon_CirclesThePlayer()
        {
            BuildRig();
            GameObject axe = Attach(Axe(radius: 2.2f, angularSpeed: 180f));

            Vector3 first = axe.transform.position;
            yield return Steps(25); // 0.5s -> 90°
            Vector3 second = axe.transform.position;

            float r1 = new Vector2(first.x, first.z).magnitude;
            float r2 = new Vector2(second.x, second.z).magnitude;

            Assert.AreEqual(2.2f, r1, 0.05f, "must sit on the orbit radius from the start");
            Assert.AreEqual(2.2f, r2, 0.05f, "and stay on it");
            Assert.Greater((second - first).magnitude, 1f, "WPN-007 B1: it must actually travel around");
        }

        // B2 — the reason orbit exists as a separate mount
        [UnityTest]
        public IEnumerator Wpn007_OrbitWeapon_IgnoresBallRotation()
        {
            BuildRig();
            GameObject axe = Attach(Axe(angularSpeed: 0f)); // hold the orbit still

            yield return Steps(5);
            Vector3 before = axe.transform.position;

            _ball.transform.rotation = Quaternion.Euler(0f, 137f, 0f);
            yield return Steps(5);

            Assert.Less((axe.transform.position - before).magnitude, 0.05f,
                "WPN-007 B2: rolling the ball must not move an orbit weapon");
            Assert.AreEqual(Quaternion.identity.eulerAngles.y, axe.transform.rotation.eulerAngles.y, 0.1f,
                "and it stays upright");
        }

        [UnityTest]
        public IEnumerator Wpn007_OrbitWeapon_FollowsMovingPlayer()
        {
            BuildRig();
            GameObject axe = Attach(Axe(angularSpeed: 0f));

            yield return Steps(5);
            _ball.transform.position = new Vector3(10f, 0f, 0f);
            yield return Steps(5);

            float radius = new Vector2(axe.transform.position.x - 10f, axe.transform.position.z).magnitude;
            Assert.AreEqual(2.2f, radius, 0.05f,
                "WPN-007 B1: the orbit centre tracks the player without anyone pushing updates at it");
        }

        // ---- B4/B5: follow motion -------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn007_FollowWeapon_LagsBehindThePlayer()
        {
            BuildRig();
            GameObject pet = Attach(Pet(standoff: 1.8f, followSpeed: 3f));

            yield return Steps(30);
            _ball.transform.position = new Vector3(0f, 0f, 20f); // teleport away

            yield return Steps(2); // only a couple of steps

            float gap = Vector3.Distance(pet.transform.position, _ball.transform.position);
            Assert.Greater(gap, 5f,
                "WPN-007 B4: the pet must not snap to the player — the lag is the mount's identity");
        }

        [UnityTest]
        public IEnumerator Wpn007_FollowWeapon_KeepsStandoffDistance()
        {
            BuildRig();
            GameObject pet = Attach(Pet(standoff: 1.8f, followSpeed: 6f));

            yield return Steps(200); // long enough to settle

            Assert.AreEqual(1.8f, Vector3.Distance(pet.transform.position, _ball.transform.position), 0.2f,
                "WPN-007 B5: it settles at the standoff distance, not inside the ball");
        }

        [UnityTest]
        public IEnumerator Wpn007_FollowWeapon_AppearsBesideThePlayer_NotAtWorldOrigin()
        {
            BuildRig();

            // Every other test attaches with the ball at the origin, which is exactly
            // why none of them caught this: the pet was built at world origin and the
            // player was standing on it. In play the capsule is picked up out in the
            // arena and the pet streaked in from the middle of the map.
            _ball.transform.position = new Vector3(12f, 0f, -9f);
            GameObject pet = Attach(Pet(standoff: 1.8f, followSpeed: 6f));

            yield return null; // no physics steps — this is the spawn pose, not settling

            Assert.AreEqual(1.8f, Vector3.Distance(pet.transform.position, _ball.transform.position), 0.05f,
                "WPN-007 B5: a follow weapon starts at its standoff distance from the player");
        }

        // ---- jitter: reported in play, so it gets a regression test ---------------

        [UnityTest]
        public IEnumerator Wpn007_SettledFollowWeapon_DoesNotSpin()
        {
            BuildRig();
            GameObject pet = Attach(Pet(standoff: 1.8f, followSpeed: 6f));

            yield return Steps(200); // let it settle next to a stationary player

            Quaternion before = pet.transform.rotation;
            float worst = 0f;

            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
                worst = Mathf.Max(worst, Quaternion.Angle(before, pet.transform.rotation));
            }

            Assert.Less(worst, 5f,
                $"a settled pet must hold its heading — turning to face floating-point noise is what reads as trembling (worst {worst:0.0}°)");
        }

        [UnityTest]
        public IEnumerator Wpn007_MountedWeapon_RendersBetweenPhysicsSteps()
        {
            BuildRig();
            GameObject axe = Attach(Axe(radius: 2.2f, angularSpeed: 180f));
            var mounted = axe.GetComponent<MountedWeapon>();

            yield return Steps(5);

            // Sample rendered positions across several frames: with no interpolation
            // they would sit on top of each other whenever two frames share a physics
            // step, which is the stutter against the interpolated ball.
            var rendered = new List<Vector3>();
            for (int i = 0; i < 12; i++)
            {
                yield return null;
                rendered.Add(axe.transform.position);
            }

            int distinct = 0;
            for (int i = 1; i < rendered.Count; i++)
            {
                if ((rendered[i] - rendered[i - 1]).sqrMagnitude > 1e-10f)
                {
                    distinct++;
                }
            }

            Assert.Greater(distinct, 0, "the weapon must actually move between rendered frames");
            Assert.Greater((mounted.LogicPosition - Vector3.zero).magnitude, 1f,
                "and the authoritative pose stays available for FixedUpdate judgement");
        }

        // ---- B9/B10: sweep contact — the new damage path -------------------------

        [UnityTest]
        public IEnumerator Wpn007_OrbitContact_DamagesEnemiesInRadius()
        {
            BuildRig();
            GameObject axe = Attach(Axe(radius: 2.2f, angularSpeed: 180f, sweepRadius: 0.9f));

            // Attaching on local +Z starts the orbit at 90°, so the window has to be
            // long enough to actually carry the axe onto the target. 100 steps at
            // 180°/s is a full revolution — placement cannot make this vacuous.
            EnemyHealth enemy = PlaceEnemy(new Vector3(2.2f, 0f, 0f));
            float before = enemy.Current;

            float closest = float.MaxValue;
            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForFixedUpdate();
                float d = Vector3.Distance(axe.transform.position, enemy.transform.position);
                if (d < closest)
                {
                    closest = d;
                }
            }

            Assert.Less(closest, 0.9f,
                $"rig precondition: the axe must actually pass within its sweep radius (closest was {closest:0.00}m)");
            Assert.Less(enemy.Current, before,
                "WPN-008a B9: the axe damages enemies the ball never touched");
            Assert.Greater(_sweep.HitCount, 0);
        }

        [UnityTest]
        public IEnumerator Wpn007_OrbitContact_DamagesMultipleAtOnce()
        {
            BuildRig(hitCooldown: 5f); // each enemy can only be hit once in this window
            Attach(Axe(radius: 2.2f, angularSpeed: 360f, sweepRadius: 1.2f));

            EnemyHealth a = PlaceEnemy(new Vector3(2.2f, 0f, 0.4f));
            EnemyHealth b = PlaceEnemy(new Vector3(2.2f, 0f, -0.4f));
            float aBefore = a.Current;
            float bBefore = b.Current;

            yield return Steps(60);

            Assert.Less(a.Current, aBefore);
            Assert.Less(b.Current, bBefore,
                "WPN-008a B9: a sweep hits everyone in reach — unlike the tesla, which picks one");
        }

        [UnityTest]
        public IEnumerator Wpn007_OrbitContact_SharesHitCooldown()
        {
            BuildRig(hitCooldown: 5f);
            Attach(Axe(radius: 0.1f, angularSpeed: 0f, sweepRadius: 2f, sweepInterval: 0.02f, damage: 9f));
            EnemyHealth enemy = PlaceEnemy(new Vector3(0.5f, 0f, 0f));
            float before = enemy.Current;

            yield return Steps(60); // dozens of sweeps, all overlapping the enemy

            Assert.Greater(_sweep.SweepCount, 5, "precondition: many sweeps ran");
            Assert.AreEqual(9f, before - enemy.Current, 0.001f,
                "DMG-002 B10: repeated sweeps inside the cooldown must add nothing");
        }

        // B11 — the F09 path must be untouched
        [UnityTest]
        public IEnumerator Spk001_SurfaceContact_StillNeedsBodyCollision()
        {
            BuildRig();
            Attach(Spike());
            EnemyHealth enemy = PlaceEnemy(new Vector3(0f, 0f, 3f));
            float before = enemy.Current;

            yield return Steps(40);

            Assert.AreEqual(before, enemy.Current, 0.001f,
                "SPK-001 B11: surface contact is a modifier on a body collision — with no collision there is no damage");
            Assert.AreEqual(0, _sweep.HitCount, "and the sweep controller must ignore surface weapons");
        }

        // ---- B12: Decision 0002's claim, tested ----------------------------------

        [UnityTest]
        public IEnumerator Wpn007_PetFiresProjectiles_WithNoNewAttackCode()
        {
            BuildRig();

            var poolGo = new GameObject("projectile_pool", typeof(ProjectilePool));
            var pool = poolGo.GetComponent<ProjectilePool>();
            _cleanup.Add(poolGo);

            // The unmodified F10 controller, pointed at a follow-mounted weapon.
            var projectiles = _ball.AddComponent<ProjectileWeaponController>();
            projectiles.Weapons = _slots;
            projectiles.Pool = pool;
            projectiles.ResetCooldowns();

            Attach(Pet());

            yield return Steps(40);

            Assert.Greater(projectiles.ShotCount, 0,
                "B12: the pet must fire through the existing projectile code with no changes to it — " +
                "this is the half of Decision 0002's prediction that held");
        }

        // ---- B13 -----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn007_MountedWeapon_HasNoCollider()
        {
            BuildRig();
            GameObject axe = Attach(Axe());
            GameObject pet = Attach(Pet());

            // Checked immediately: Destroy is deferred, so "next frame" would hide a
            // collider that is alive for one physics step.
            Assert.AreEqual(0, axe.GetComponentsInChildren<Collider>(true).Length);
            Assert.AreEqual(0, pet.GetComponentsInChildren<Collider>(true).Length);
            Assert.IsNull(axe.GetComponent<Rigidbody>(), "B13: no Rigidbody either — nothing for physics to do");
            yield return null;
        }

        // ---- B14 -----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn007_SessionEnd_ClearsMountedWeapons()
        {
            BuildRig();

            var sessionGo = new GameObject("session", typeof(GameSession));
            var session = sessionGo.GetComponent<GameSession>();
            session.PlayerHealth = _playerHealth;
            _cleanup.Add(sessionGo);

            _container.Session = session;
            _container.enabled = false;
            _container.enabled = true; // re-run OnEnable so it subscribes

            Attach(Axe());
            Attach(Pet());
            Assert.AreEqual(2, _container.MountedCount, "precondition");

            session.End(SessionOutcome.Defeat);
            yield return null; // Ended is raised in LateUpdate
            yield return null; // Destroy is deferred by a frame

            Assert.AreEqual(0, _container.MountedCount,
                "WPN-007 Exception B14: these weapons are not children of the ball, so they outlive the player unless cleaned up");
        }

        // ---- B16 -----------------------------------------------------------------

        [UnityTest]
        public IEnumerator Wpn005_SameKind_CannotBeAttachedTwice()
        {
            BuildRig();
            Attach(Axe());

            int second = _slots.TryAttach(Axe(), _ball.transform.TransformPoint(Vector3.right * 0.5f));

            Assert.AreEqual(-1, second, "WPN-005 B16: a kind appears at most once per run, even with five kinds");
            Assert.AreEqual(1, _slots.AttachedCount);
            yield return null;
        }
    }
}
