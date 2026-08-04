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
    /// PlayMode coverage for F08 (attachment) and F09 (definitions, contact attack).
    /// </summary>
    public sealed class WeaponAttachmentPlayModeTests
    {
        private readonly List<Object> _cleanup = new List<Object>();

        private WeaponConfig _config;
        private GameObject _ball;
        private WeaponSlots _slots;

        private WeaponDefinition Definition(
            WeaponKind kind,
            WeaponAttack attack = WeaponAttack.Contact,
            float arcHalfAngle = 60f,
            float bonus = 8f)
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.kind = kind;
            def.displayName = kind.ToString();
            def.mount = WeaponMount.Surface;
            def.attack = attack;
            def.arcHalfAngleDegrees = arcHalfAngle;
            def.bonusDamage = bonus;
            def.dashDamageMultiplier = 1.5f;
            def.dashKnockbackMultiplier = 1.4f;
            def.shape = PrimitiveType.Capsule;
            def.scale = 0.28f;
            _cleanup.Add(def);
            return def;
        }

        private void BuildRig()
        {
            _config = ScriptableObject.CreateInstance<WeaponConfig>();
            _config.maxWeapons = 3;
            _config.attachRadius = 0.55f;
            _config.pickupRadius = 1.1f;
            _config.minSeparationDegrees = 50f;
            _config.spawnTimes = new[] { 0.05f, 0.1f, 0.15f };
            _config.spawnDistanceMin = 3f;
            _config.spawnDistanceMax = 3f;
            _cleanup.Add(_config);

            _ball = new GameObject("test_ball");
            _ball.transform.position = Vector3.zero;
            _slots = _ball.AddComponent<WeaponSlots>();
            _slots.Config = _config;
            _cleanup.Add(_ball);
        }

        private WeaponCapsule SpawnCapsule(WeaponDefinition definition, Vector3 position)
        {
            var go = new GameObject("capsule");
            go.transform.position = position;
            var capsule = go.AddComponent<WeaponCapsule>();
            capsule.Initialise(definition, _slots, _config);
            _cleanup.Add(go);
            return capsule;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _cleanup)
            {
                if (o != null) Object.Destroy(o);
            }

            _cleanup.Clear();
        }

        // F09 B1, B2
        [UnityTest]
        public IEnumerator Wpn001_WeaponBuiltFromDefinition()
        {
            BuildRig();
            WeaponDefinition def = Definition(WeaponKind.Tesla);
            def.shape = PrimitiveType.Cube;
            def.scale = 0.4f;

            int index = _slots.TryAttach(def, new Vector3(0f, 1f, 0f));
            yield return null;

            GameObject weapon = _slots.AttachedAt(index);
            Assert.AreSame(def, _slots.DefinitionAt(index), "the attached weapon must remember its definition");
            Assert.AreEqual("Cube", weapon.GetComponent<MeshFilter>().sharedMesh.name,
                "the definition's shape must be used — a new weapon is data, not code");
            Assert.That(weapon.transform.localScale.x, Is.EqualTo(0.4f).Within(1e-3f));
        }

        // F08 B1, B14
        [UnityTest]
        public IEnumerator Wpn001_CapsuleContact_AttachesWeapon()
        {
            BuildRig();
            int events = 0;
            WeaponDefinition got = null;
            _slots.Acquired += (d, _) => { events++; got = d; };

            WeaponDefinition def = Definition(WeaponKind.Spike);
            SpawnCapsule(def, new Vector3(0f, 0f, 0.5f));
            yield return null;
            yield return null;

            Assert.AreEqual(1, events, "WPN-001: attaching must raise WeaponAcquired exactly once");
            Assert.AreSame(def, got);
            Assert.AreEqual(1, _slots.AttachedCount);
        }

        // F08 B1
        [UnityTest]
        public IEnumerator Wpn001_Weapon_LandsOnTheContactSide()
        {
            BuildRig();
            _slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, 0f, 5f));
            yield return null;

            Assert.Greater(Vector3.Dot(_slots.DirectionAt(0), Vector3.forward), 0.99f,
                "WPN-001: with nothing in the way the weapon must sit exactly where it was hit");
        }

        // F08 B3 — continuity (the 2026-08-05 rule change). Slot snapping would fail this.
        [UnityTest]
        public IEnumerator Wpn001_SmallAngleChange_MovesTheWeapon()
        {
            BuildRig();
            _slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, 5f, 0f));
            Vector3 first = _slots.DirectionAt(0);

            var ball2 = new GameObject("test_ball_2");
            var slots2 = ball2.AddComponent<WeaponSlots>();
            slots2.Config = _config;
            _cleanup.Add(ball2);
            yield return null;

            slots2.TryAttach(Definition(WeaponKind.Spike), new Vector3(0.1f, 5f, 0f));

            Assert.Greater(Vector3.Angle(first, slots2.DirectionAt(0)), 0.1f,
                "WPN-001: attachment must be continuous, not quantised to slots");
        }

        // F08 B4, B6
        [UnityTest]
        public IEnumerator Wpn001a_CrowdedWeapons_KeepMinimumSeparation()
        {
            BuildRig();
            _slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, 5f, 0f));
            _slots.TryAttach(Definition(WeaponKind.Cannon), new Vector3(0.2f, 5f, 0f));
            _slots.TryAttach(Definition(WeaponKind.Tesla), new Vector3(0.2f, 5f, 0.2f));
            yield return null;

            Assert.AreEqual(3, _slots.AttachedCount);
            for (int i = 0; i < 3; i++)
            {
                for (int j = i + 1; j < 3; j++)
                {
                    Assert.GreaterOrEqual(Vector3.Angle(_slots.DirectionAt(i), _slots.DirectionAt(j)),
                        _config.minSeparationDegrees - 0.5f, $"WPN-001a: weapons {i} and {j} overlap");
                }
            }
        }

        // F09 B11 — a collider would change how the ball rolls
        [UnityTest]
        public IEnumerator Spk001_AttachedWeapon_HasNoColliderImmediately()
        {
            BuildRig();
            int index = _slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, 1f, 0f));

            Assert.IsNull(_slots.AttachedAt(index).GetComponent<Collider>(),
                "a deferred Destroy would leave the collider alive for a physics step and jolt the ball");

            yield return null;
        }

        // F09 B3, B5 — the weapon side hits harder
        [UnityTest]
        public IEnumerator Spk001_SpikeFacingEnemy_AddsBonus()
        {
            BuildRig();
            _slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, 0f, 5f)); // spike on +Z
            yield return null;

            float facing = _slots.BonusDamageAgainst(Vector3.forward, 12f, 12f, 1f, false, 0.25f);

            Assert.Greater(facing, 0f, "SPK-001: an enemy inside the spike's cone must take extra damage");
        }

        // F09 B4 — the point of the feature
        [UnityTest]
        public IEnumerator Spk001_SpikeAwayFromEnemy_AddsNothing()
        {
            BuildRig();
            _slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, 0f, 5f)); // spike on +Z
            yield return null;

            float behind = _slots.BonusDamageAgainst(Vector3.back, 12f, 12f, 1f, false, 0.25f);

            Assert.AreEqual(0f, behind,
                "SPK-001: hitting with the opposite side must add nothing — attachment position has to matter");
        }

        // F09 B12 — the ball rotates, so the cone must rotate with it
        [UnityTest]
        public IEnumerator Spk001_ArcFollowsBallRotation()
        {
            BuildRig();
            _slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, 0f, 5f)); // spike on +Z
            yield return null;

            float before = _slots.BonusDamageAgainst(Vector3.forward, 12f, 12f, 1f, false, 0.25f);
            Assert.Greater(before, 0f, "sanity: the enemy is in the cone to start with");

            _ball.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // spike now faces -Z
            yield return null;

            float after = _slots.BonusDamageAgainst(Vector3.forward, 12f, 12f, 1f, false, 0.25f);

            Assert.AreEqual(0f, after,
                "WPN-002: rolling the ball must carry the weapon's cone with it");
        }

        // F09 B9
        [UnityTest]
        public IEnumerator Wpn008_ProjectileWeapon_AddsNoContactDamage()
        {
            BuildRig();
            _slots.TryAttach(Definition(WeaponKind.Cannon, WeaponAttack.Projectile), new Vector3(0f, 0f, 5f));
            yield return null;

            Assert.AreEqual(0f, _slots.BonusDamageAgainst(Vector3.forward, 12f, 12f, 1f, false, 0.25f),
                "WPN-008: only contact weapons add contact damage");
        }

        // F09 B8
        [UnityTest]
        public IEnumerator Wpn008_NoWeapon_AddsNothing()
        {
            BuildRig();
            yield return null;

            Assert.AreEqual(0f, _slots.BonusDamageAgainst(Vector3.forward, 12f, 12f, 1f, false, 0.25f));
        }

        // F09 B10
        [UnityTest]
        public IEnumerator Wpn008_TwoContactWeapons_Sum()
        {
            BuildRig();
            _slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, 0f, 5f));
            float one = _slots.BonusDamageAgainst(Vector3.forward, 12f, 12f, 1f, false, 0.25f);

            // Second weapon is pushed 50 degrees away but its 60 degree cone still covers +Z.
            _slots.TryAttach(Definition(WeaponKind.Cannon), new Vector3(0f, 0f, 5f));
            yield return null;

            float two = _slots.BonusDamageAgainst(Vector3.forward, 12f, 12f, 1f, false, 0.25f);

            Assert.Greater(two, one, "B10: overlapping contact weapons must add up");
        }

        // WPN-003
        [UnityTest]
        public IEnumerator Wpn003_FourthWeapon_IsNotAttached()
        {
            BuildRig();
            _slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, 1f, 0f));
            _slots.TryAttach(Definition(WeaponKind.Cannon), new Vector3(1f, 0f, 0f));
            _slots.TryAttach(Definition(WeaponKind.Tesla), new Vector3(0f, 0f, 1f));
            yield return null;

            Assert.AreEqual(3, _slots.AttachedCount);
            Assert.Less(_slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, -1f, 0f)), 0,
                "WPN-003: never more than three weapons");
        }

        // WPN-005
        [UnityTest]
        public IEnumerator Wpn005_SameKind_AttachesOnlyOnce()
        {
            BuildRig();
            Assert.GreaterOrEqual(_slots.TryAttach(Definition(WeaponKind.Tesla), new Vector3(0f, 1f, 0f)), 0);
            Assert.Less(_slots.TryAttach(Definition(WeaponKind.Tesla), new Vector3(0f, -1f, 0f)), 0,
                "WPN-005: kinds do not repeat");
            yield return null;

            Assert.AreEqual(1, _slots.AttachedCount);
        }

        // F08 B1 — a capsule at the ball's centre must still be claimable
        [UnityTest]
        public IEnumerator Wpn001_ContactAtBallCentre_StillAttaches()
        {
            BuildRig();
            SpawnCapsule(Definition(WeaponKind.Spike), Vector3.zero);
            yield return null;
            yield return null;

            Assert.AreEqual(1, _slots.AttachedCount,
                "WPN-001: a degenerate contact direction must not make the pickup unclaimable");
        }

        // WPN-006
        [UnityTest]
        public IEnumerator Wpn006_UnclaimedCapsule_PersistsOverTime()
        {
            BuildRig();
            WeaponCapsule capsule = SpawnCapsule(Definition(WeaponKind.Spike), new Vector3(0f, 0f, 30f));

            yield return new WaitForSeconds(1.5f);

            Assert.IsFalse(capsule == null, "WPN-006: an unclaimed capsule must not despawn");
            Assert.IsFalse(capsule.IsClaimed);
            Assert.AreEqual(0, _slots.AttachedCount);
        }

        // WPN-006
        [UnityTest]
        public IEnumerator Wpn006_Capsule_HasBeaconUntilClaimed()
        {
            BuildRig();
            WeaponCapsule capsule = SpawnCapsule(Definition(WeaponKind.Spike), new Vector3(0f, 0f, 30f));
            yield return null;

            Assert.IsNotNull(capsule.Beacon, "WPN-006: an unclaimed capsule must show a beacon");
            GameObject beacon = capsule.Beacon;

            capsule.transform.position = new Vector3(0f, 0f, 0.5f);
            yield return null;
            yield return null;

            Assert.IsTrue(beacon == null, "WPN-006: the beacon must go with the capsule");
        }

        // WPN-003 — a capsule that cannot attach is not consumed
        [UnityTest]
        public IEnumerator Wpn003_CapsuleAtCap_IsNotConsumed()
        {
            BuildRig();
            _slots.TryAttach(Definition(WeaponKind.Spike), new Vector3(0f, 1f, 0f));
            _slots.TryAttach(Definition(WeaponKind.Cannon), new Vector3(1f, 0f, 0f));
            _slots.TryAttach(Definition(WeaponKind.Tesla), new Vector3(0f, 0f, 1f));

            WeaponCapsule capsule = SpawnCapsule(Definition(WeaponKind.Spike), new Vector3(0f, 0f, 0.5f));
            yield return null;
            yield return null;

            Assert.IsFalse(capsule == null, "a capsule that could not attach must stay on the field");
            Assert.IsFalse(capsule.IsClaimed);
        }
    }
}
