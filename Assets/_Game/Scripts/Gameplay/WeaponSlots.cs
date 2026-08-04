using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// WPN-001 / WPN-001a / WPN-002 / WPN-003: attaches weapons where the capsule
    /// was actually touched, keeping them a minimum angle apart, and parents them
    /// to the ball so they roll with it.
    ///
    /// Weapons carry no colliders. A child trigger's events are attributed to the
    /// ball's Rigidbody, so a graze would read as a body hit — contact weapons use
    /// a cone test instead (F09, SPK-001).
    /// </summary>
    public sealed class WeaponSlots : MonoBehaviour
    {
        private const int MaxWeapons = 3; // WPN-003

        [SerializeField] private WeaponConfig config;

        [Tooltip("F09 임시: 부착을 콘솔로 확인한다 (F13에서 HUD로 대체)")]
        [SerializeField] private bool logToConsole = true;

        private readonly Float3[] _directions = new Float3[MaxWeapons];
        private readonly GameObject[] _attached = new GameObject[MaxWeapons];
        private readonly WeaponDefinition[] _definitions = new WeaponDefinition[MaxWeapons];
        private readonly bool[] _kindTaken = new bool[3];

        /// <summary>WeaponAcquired(definition, localDirection).</summary>
        public event Action<WeaponDefinition, Vector3> Acquired;

        public WeaponConfig Config { get => config; set => config = value; }

        /// <summary>How many weapons are attached (WPN-003 caps this at 3).</summary>
        public int AttachedCount { get; private set; }

        public bool HasKind(WeaponKind kind) => _kindTaken[(int)kind];

        public GameObject AttachedAt(int index) =>
            index >= 0 && index < AttachedCount ? _attached[index] : null;

        public WeaponDefinition DefinitionAt(int index) =>
            index >= 0 && index < AttachedCount ? _definitions[index] : null;

        /// <summary>Local surface direction of an attached weapon.</summary>
        public Vector3 DirectionAt(int index) =>
            index >= 0 && index < AttachedCount
                ? new Vector3(_directions[index].X, _directions[index].Y, _directions[index].Z)
                : Vector3.zero;

        /// <summary>
        /// Attaches at the contact point, pushed clear of existing weapons.
        /// Returns the new weapon's index, or -1 when nothing was attached (at the
        /// cap or a duplicate kind) — the caller uses that to decide whether the
        /// capsule was consumed.
        /// </summary>
        public int TryAttach(WeaponDefinition definition, Vector3 worldContactPoint)
        {
            if (definition == null || AttachedCount >= MaxWeapons || _kindTaken[(int)definition.kind])
            {
                return -1;
            }

            Vector3 local = transform.InverseTransformPoint(worldContactPoint);
            if (local.sqrMagnitude < 1e-6f)
            {
                // Contact exactly at the ball's centre carries no direction. Refusing
                // here would make the pickup permanently unclaimable.
                local = Vector3.up;
            }

            float separation = config != null ? config.minSeparationDegrees : 50f;
            Float3 direction = AttachmentLogic.Resolve(
                new Float3(local.x, local.y, local.z), _directions, AttachedCount, separation);

            int index = AttachedCount;
            _directions[index] = direction;
            _definitions[index] = definition;
            _kindTaken[(int)definition.kind] = true;
            AttachedCount++;
            _attached[index] = BuildWeapon(definition, direction);

            if (logToConsole)
            {
                Debug.Log($"[WPN] {definition.displayName} ({definition.mount}/{definition.attack}) " +
                          $"→ {direction} ({AttachedCount}/{MaxWeapons})", this);
            }

            Acquired?.Invoke(definition, new Vector3(direction.X, direction.Y, direction.Z));
            return index;
        }

        /// <summary>
        /// SPK-001 (B8~B10): total bonus damage the attached contact weapons add to
        /// this collision. Directions are local, so the ball's current rotation is
        /// already accounted for by converting the enemy direction into local space (B12).
        /// </summary>
        public float BonusDamageAgainst(Vector3 worldToEnemy, float speed, float maxSpeed, float frontality, bool isDashing, float minSpeedMultiplier)
        {
            Vector3 localToEnemy = transform.InverseTransformDirection(worldToEnemy);
            var toEnemy = new Float3(localToEnemy.x, localToEnemy.y, localToEnemy.z);

            float total = 0f;
            for (int i = 0; i < AttachedCount; i++)
            {
                WeaponDefinition def = _definitions[i];
                if (def == null || def.attack != WeaponAttack.Contact)
                {
                    continue; // B9
                }

                total += ContactWeaponLogic.BonusDamage(
                    _directions[i], toEnemy, speed, maxSpeed, frontality, isDashing,
                    def.ToContactConfig(minSpeedMultiplier)); // B10: contributions add
            }

            return total;
        }

        /// <summary>SPK-001 (B6): the strongest knockback multiplier among weapons covering this enemy.</summary>
        public float KnockbackMultiplierAgainst(Vector3 worldToEnemy, bool isDashing, float minSpeedMultiplier)
        {
            Vector3 localToEnemy = transform.InverseTransformDirection(worldToEnemy);
            var toEnemy = new Float3(localToEnemy.x, localToEnemy.y, localToEnemy.z);

            float best = 1f;
            for (int i = 0; i < AttachedCount; i++)
            {
                WeaponDefinition def = _definitions[i];
                if (def == null || def.attack != WeaponAttack.Contact)
                {
                    continue;
                }

                float m = ContactWeaponLogic.KnockbackMultiplier(
                    _directions[i], toEnemy, isDashing, def.ToContactConfig(minSpeedMultiplier));
                if (m > best)
                {
                    best = m;
                }
            }

            return best;
        }

        /// <summary>WPN-002 (B7/B8): greybox primitive aligned to the surface normal, parented to the ball.</summary>
        private GameObject BuildWeapon(WeaponDefinition definition, Float3 direction)
        {
            var normal = new Vector3(direction.X, direction.Y, direction.Z);

            GameObject go = GameObject.CreatePrimitive(definition.shape);
            go.name = $"Weapon_{definition.kind}";

            // Visual only — DestroyImmediate, not Destroy: deferred destruction leaves
            // the collider alive for one physics step, which is enough to jolt the ball.
            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }

            float radius = config != null ? config.attachRadius : 0.55f;

            go.transform.SetParent(transform, false);
            go.transform.localPosition = normal * radius;
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
            go.transform.localScale = Vector3.one * definition.scale;

            return go;
        }
    }
}
