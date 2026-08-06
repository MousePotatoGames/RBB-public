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

        [Header("Orbit mount (mount = Orbit only) — WPN-007")]
        [Tooltip("공전 반경. 공 반경보다 충분히 커야 '돈다'가 읽힌다")]
        [Min(0.5f)] public float orbitRadius = 2.2f;

        [Tooltip("초당 공전 각도. 음수면 반대로 돈다")]
        public float orbitAngularSpeed = 180f;

        [Tooltip("플레이어 기준 높이")]
        public float orbitHeight = 0.3f;

        [Header("Follow mount (mount = Follow only) — WPN-007")]
        [Tooltip("플레이어에게서 유지하는 거리. 0이면 공 안으로 파고든다")]
        [Min(0.1f)] public float followStandoff = 1.8f;

        [Tooltip("지수 추종 계수. 클수록 빨리 붙는다 — 지연 자체가 이 마운트의 정체성이다")]
        [Min(0.1f)] public float followSpeed = 6f;

        [Header("Sweep contact (mount = Orbit/Follow + attack = Contact) — WPN-008a")]
        [Tooltip("이 반경 안의 적 '전부'가 맞는다. 표면 접촉(SPK-001)과 다른 경로다")]
        [Min(0.05f)] public float sweepRadius = 0.7f;

        [Tooltip("판정 주기. DMG-002 쿨다운이 실질 상한이라 짧아도 피해가 늘지는 않는다")]
        [Min(0.02f)] public float sweepInterval = 0.25f;

        [Min(0f)] public float sweepDamage = 9f;

        [Header("Zap attack (attack = Zap only) — TES-001")]
        [Tooltip("TES-001 방전 주기(초). 무조건 맞으므로 캐논보다 느리게 둔다")]
        [Min(0.05f)] public float zapInterval = 0.9f;

        [Tooltip("링 부착점 기준 사거리. 짧아야 무리 속으로 파고들 이유가 생긴다 — 캐논과의 대비가 정체성")]
        [Min(0.5f)] public float zapRange = 5f;

        [Tooltip("거리 0에서의 피해")]
        [Min(0f)] public float zapMaxDamage = 12f;

        [Tooltip("사거리 끝에서의 피해. 최대치와 차이가 클수록 '붙어야 한다'가 강해진다")]
        [Min(0f)] public float zapMinDamage = 4f;

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

        public ZapConfig ToZapConfig() =>
            new ZapConfig(zapInterval, zapRange, zapMaxDamage, zapMinDamage);

        public ContactWeaponConfig ToContactConfig(float minSpeedMultiplier) =>
            new ContactWeaponConfig(
                arcHalfAngleDegrees,
                bonusDamage,
                dashDamageMultiplier,
                dashKnockbackMultiplier,
                minSpeedMultiplier);
    }
}
