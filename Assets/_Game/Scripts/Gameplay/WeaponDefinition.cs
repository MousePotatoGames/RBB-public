using Game.Core;
using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// One weapon = one asset (Decision 0002). Mount type, attack type and numbers
    /// live here, so a new weapon is authored rather than coded.
    ///
    /// Fields are grouped by which section they belong to; a field outside the
    /// section that matches this weapon's mount/attack is simply unused.
    /// </summary>
    [CreateAssetMenu(menuName = "RumbleBall/Weapon Definition", fileName = "Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("WPN-005 무중복 추첨의 식별자. 같은 kind는 한 판에 한 번만 등장한다")]
        public WeaponKind kind = WeaponKind.Spike;

        public string displayName = "Crusher Spike";

        [Header("WPN-007 — mount")]
        public WeaponMount mount = WeaponMount.Surface;

        [Header("WPN-008 — attack")]
        public WeaponAttack attack = WeaponAttack.Contact;

        [Header("Contact attack (attack = Contact only)")]
        [Tooltip("SPK-001: 이 각도 안의 적에게만 추가 피해. 좁으면 안 맞고 넓으면 부착 위치가 무의미해진다")]
        [Range(1f, 180f)] public float arcHalfAngleDegrees = 60f;

        [Min(0f)] public float bonusDamage = 8f;
        [Min(1f)] public float dashDamageMultiplier = 1.5f;
        [Min(1f)] public float dashKnockbackMultiplier = 1.4f;

        [Header("Visual (greybox — P07 replaces this)")]
        public PrimitiveType shape = PrimitiveType.Capsule;

        [Min(0.01f)] public float scale = 0.28f;

        public ContactWeaponConfig ToContactConfig(float minSpeedMultiplier) =>
            new ContactWeaponConfig(
                arcHalfAngleDegrees,
                bonusDamage,
                dashDamageMultiplier,
                dashKnockbackMultiplier,
                minSpeedMultiplier);
    }
}
