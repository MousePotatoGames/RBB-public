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

        [Header("Projectile attack (attack = Projectile only) — WPN-009 / CAN-001")]
        [Tooltip("F10은 직선만 구현한다. 유도는 F12, 즉시(레이저)는 F11")]
        public ProjectileTravel travel = ProjectileTravel.Straight;

        [Tooltip("CAN-001 발사 쿨다운(초)")]
        [Min(0.05f)] public float fireInterval = 0.7f;

        [Tooltip("WPN-009: 1회 발사 수. 이 값과 확산만 올리면 산탄이 된다 — 코드 변경 불필요")]
        [Range(1, 16)] public int shotsPerBurst = 1;

        [Tooltip("WPN-009: 부채꼴 전체 폭(도). 양 끝 탄이 ±절반에 놓인다. 발사 수 1이면 무시된다")]
        [Range(0f, 180f)] public float spreadDegrees;

        [Min(0.1f)] public float projectileSpeed = 18f;

        [Tooltip("이 거리만큼 날아가면 소멸한다. 적 스폰 링보다 짧게 두어 화면 밖 저격을 막는다")]
        [Min(0.5f)] public float projectileRange = 14f;

        [Tooltip("첫 적 이후 추가로 관통하는 수")]
        [Min(0)] public int pierce;

        [Min(0f)] public float projectileDamage = 6f;

        [Tooltip("SphereCast 판정 반경 — 콜라이더 대신 이걸로 맞힌다 (터널링 방지)")]
        [Min(0.01f)] public float projectileRadius = 0.15f;

        [Header("Visual (greybox — P07 replaces this)")]
        public PrimitiveType shape = PrimitiveType.Capsule;

        [Min(0.01f)] public float scale = 0.28f;

        public ProjectileConfig ToProjectileConfig() =>
            new ProjectileConfig(
                travel,
                fireInterval,
                shotsPerBurst,
                spreadDegrees,
                projectileSpeed,
                projectileRange,
                pierce,
                projectileDamage);

        public ContactWeaponConfig ToContactConfig(float minSpeedMultiplier) =>
            new ContactWeaponConfig(
                arcHalfAngleDegrees,
                bonusDamage,
                dashDamageMultiplier,
                dashKnockbackMultiplier,
                minSpeedMultiplier);
    }
}
