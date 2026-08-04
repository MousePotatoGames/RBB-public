using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// WPN-006: a weapon pickup that stays on the field until claimed, with a
    /// beacon so it can be found from across the arena. Contact hands the
    /// contact point to <see cref="WeaponSlots"/>, which decides the slot (WPN-001).
    ///
    /// Distance is checked directly rather than through a trigger: the ball moves
    /// fast enough that a thin trigger can be tunnelled through between physics steps.
    /// </summary>
    public sealed class WeaponCapsule : MonoBehaviour
    {
        [SerializeField] private WeaponConfig config;

        private WeaponDefinition _definition;
        private WeaponSlots _target;
        private GameObject _beacon;

        /// <summary>Claimed(capsule) — the spawner uses this to drop its reference.</summary>
        public event Action<WeaponCapsule> Claimed;

        public WeaponDefinition Definition => _definition;
        public WeaponKind Kind => _definition != null ? _definition.kind : default;
        public bool IsClaimed { get; private set; }
        public GameObject Beacon => _beacon;

        public void Initialise(WeaponDefinition definition, WeaponSlots target, WeaponConfig weaponConfig)
        {
            _definition = definition;
            _target = target;
            config = weaponConfig;
            IsClaimed = false;
            name = definition != null ? $"Capsule_{definition.kind}" : "Capsule";
            BuildBeacon(); // B13
        }

        private void BuildBeacon()
        {
            if (_beacon != null)
            {
                return;
            }

            float height = config != null ? config.beaconHeight : 8f;
            float width = config != null ? config.beaconWidth : 0.18f;

            _beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            _beacon.name = "Beacon";
            var collider = _beacon.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            _beacon.transform.SetParent(transform, false);
            _beacon.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            // Unity's cylinder is 2 units tall, hence the halved Y scale.
            _beacon.transform.localScale = new Vector3(width, height * 0.5f, width);
        }

        private void Update()
        {
            // B12: no lifetime, no despawn distance — the capsule waits.
            if (IsClaimed || _target == null)
            {
                return;
            }

            float radius = config != null ? config.pickupRadius : 1.1f;
            Vector3 toPlayer = _target.transform.position - transform.position;
            if (toPlayer.sqrMagnitude > radius * radius)
            {
                return;
            }

            // WPN-001: only the direction from the ball's centre matters, and the
            // capsule's own position already carries it.
            if (_target.TryAttach(_definition, transform.position) < 0)
            {
                return; // B5/B9/B10: nothing attached, so the capsule is not consumed
            }

            Claim();
        }

        /// <summary>B13: the capsule and its beacon leave together.</summary>
        private void Claim()
        {
            IsClaimed = true;
            Claimed?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
