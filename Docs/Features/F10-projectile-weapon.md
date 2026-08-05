# Feature: F10 Revolver Cannon (투사체 공격 · 직선 이동 · 발사 수와 확산)

## Status

Verified (자동 검증 범위) — 2026-08-06. EditMode 139/139, PlayMode 75/75, 런타임 실측 완료.
Done conditions 1·2·3·5·6 `VERIFIED`, 4·7은 Manual play checks로 남아 있습니다.
→ [검증 보고서](../Reports/2026-08-06_F10-projectile-weapon_VERIFICATION.md)
스펙 승인 2026-08-05, **CAN-001 변경 반영 재승인 2026-08-05**

> **1차 구현 후 규칙이 바뀌었습니다.** 최초 스펙은 CAN-001 원문대로 "최근접 적이 포신 각도 안일
> 때만 발사"였습니다. 구현·검증까지 마친 뒤 런타임에서 재보니 **8.8초에 3발, 다른 런에서는
> 6.4초에 0발** — 무기가 거의 작동하지 않았습니다. 사용자 판단으로 CAN-001을 **고정 주기 무조건
> 발사**로 바꿨고, 이 스펙의 B2·B3·B5와 대상 탐색·조준 보정 코드가 전부 그에 맞춰 개정됩니다.
> 자세한 근거는 GAME_RULES.md의 CAN-001 "왜 바뀌었나" 참조.

## Purpose

**무기가 스스로 공격하기 시작합니다.** F09의 스파이크는 플레이어가 박아야 발동하는 *수식어*였습니다.
캐논은 **플레이어의 충돌과 무관하게 스스로 발사**하는 첫 무기입니다.

그리고 이것이 [Decision 0002](../Decisions/0002-weapon-mount-types.md)가 예고한 **두 번째 축**을 세웁니다:

| 기능 | 추가되는 코드 축 | 새 무기 |
|---|---|---|
| F09 | 접촉 공격 + 표면 마운트 | 스파이크 |
| **F10** | **투사체 공격 + 직선 이동 + 발사 수·확산** | **캐논** |
| F11 | 방전 공격 + 즉시(레이저) 이동 | 테슬라 |
| F12 | 궤도·추종 마운트 | 도끼·펫 (**공격 코드 추가 0**) |

핵심은 **산탄이 코드가 아니라 수치**라는 점입니다 (WPN-009). 발사 수 5, 확산 30°를 넣으면
샷건이 되고, 발사 수 1, 확산 0을 넣으면 단발이 됩니다. 코드 경로는 하나입니다.

## Player's perspective

캐논이 붙으면 **가만히 있어도 일정 주기로 총알이 나갑니다.** 조준은 없습니다 — 포신이 향한
방향 그대로 나갑니다.

그리고 포신은 공에 붙어 있으니 **공이 구르면 같이 돕니다.** 그래서 굴리는 방향이 곧
**사격 방향**이 됩니다. 적 무리 쪽으로 굴러가면 그쪽으로 쏟아지고, 방향을 틀면 탄도 따라 틉니다.
조준 버튼은 없습니다 (GAME_OVERVIEW 절대 원칙) — 플레이어가 하는 일은 **조준이 아니라 위치잡기**입니다.

## Referenced rules

- **CAN-001 발사 조건** (2026-08-05 개정) — **쿨다운 종료만**이 조건. 포신 방향 그대로 발사하며 조준하지 않는다. 공 회전에 따라 **발사 방향**이 변한다
- **WPN-008 공격 방식** — 투사체(Projectile)
- **WPN-009 투사체 정의** — 이동 방식 1개 + 수치들. 산탄은 수치. 직선은 **반드시 풀링**. 1회 발사 수 상한
- **WPN-007 마운트 타입** — 캐논은 **표면(Surface)** 유지 (Decision 0002: 궤도로 바꾸면 CAN-001의 정체성이 사라짐)
- **WPN-002 부착 방향** — 포신은 표면 노멀을 따르고 공과 함께 회전
- **DMG-002 중복 히트 방지** — 투사체 피해도 적별 히트 쿨다운을 공유
- 기획서 15장 Web 성능 — Object Pool, 매 프레임 Find/GetComponent 금지

## In scope

- `Game.Core`: **`ProjectileLogic`** — 발사 가능 판정, 조준 보정 클램프, 확산 방향 생성, 직선 전진
- `Game.Core`: **`TargetingLogic`** — 위치 배열에서 사거리 내 최근접 대상 선택 (순수 함수)
- `Game.Core`: **`Rotation`** — 대원 회전 / 축 회전 헬퍼. `AttachmentLogic.PushAway`의 중복을 **제거하며 공유**
- `WeaponDefinition`: **투사체 구역 필드 추가** (이동 방식, 간격, 발사 수, 확산, 속도, 사거리, 관통, 위력, 발사 각도, 조준 보정 상한)
- `Game.Gameplay`: **`ProjectileWeaponController`** — 부착된 투사체 무기의 발사 구동
- `Game.Gameplay`: **`Projectile`** + **`ProjectilePool`** — 풀링된 직선 투사체
- 캐논 `WeaponDefinition` 에셋 수치 채우기 (에셋은 이미 존재)
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- **유도(Homing) 이동** — F12. 지금 만들면 쓰는 무기가 없습니다
- **즉시(Hitscan/레이저) 이동** — F11 테슬라와 함께
- **방전 공격** — F11
- **궤도·추종 마운트** — F12
- **캐논 2단계 강화 (CAN-002 2연사)** — 보스가 트리거라 FP에 없음
- **시안색 에너지 볼트·포신 반동·머즐 플래시** (CAN-001 Process 연출) — P08
- 투사체가 벽·지형에 막히는 처리 — FP 아레나에 막는 지형이 없습니다
- 투사체가 **플레이어에게** 피해를 주는 경우 — 적 전용

## 설계 판단 세 가지

### 1. 투사체에 콜라이더를 달지 않습니다 — SphereCast로 판정합니다

F09에서 무기에 콜라이더를 안 단 것과 **같은 이유는 아닙니다.** 이건 별개 판단입니다.

| | 트리거 콜라이더 + 키네마틱 RB | **SphereCast (채택)** |
|---|---|---|
| 고속 관통(터널링) | **실재하는 위험** — 속도 20m/s × 0.02s = 프레임당 0.4m | 이동 구간 전체를 훑으므로 **원천 차단** |
| 필요한 컴포넌트 | Rigidbody + Collider + 레이어 설정 | **없음** |
| 플레이어 오격 방지 | 레이어 매트릭스 설정 필요 | `EnemyHealth` 유무로 판정 |
| 연산 | 물리 엔진이 매 스텝 전부 | 살아있는 투사체 수만큼 캐스트 |
| 관통(Pierce) | 트리거 재진입 관리 필요 | `SphereCastAll` 정렬 결과를 앞에서부터 |

터널링이 결정적입니다. 캐논 탄속을 올리면 콜라이더 방식은 **조용히 적을 통과**하기 시작하고,
그건 "가끔 안 맞는다"로 나타나 원인을 찾기 어렵습니다.

### 2. 대상 탐색을 하지 않습니다 (CAN-001 개정)

**최초 스펙은 `DroneSpawner`의 활성 목록에서 최근접 적을 찾았습니다.** 개정된 CAN-001은
대상을 보지 않으므로 이 경로가 통째로 사라집니다:

| 사라지는 것 | 이유 |
|---|---|
| `TargetingLogic` (Core) | 최근접 적을 쓰지 않는다 |
| `Rotation.Towards` | 조준 보정이 없다 |
| `ProjectileLogic.AimDirection` | 위와 동일 |
| `ShouldFire`의 각도 판정 | 발사 조건이 쿨다운뿐 |
| `WeaponDefinition.firingArcHalfAngleDegrees` / `aimAssistMaxDegrees` | 쓰이지 않는다 |

`DroneSpawner.Active` 접근자만 남깁니다 — 런타임 코드는 더 이상 쓰지 않지만,
**PlayMode 테스트가 적을 원하는 위치에 놓는 데 필요**합니다.

**전부 삭제합니다.** F11 테슬라(TES-001)가 "사거리 내 최근접 적 1기"를 필요로 하지만,
그때 필요한 시그니처는 다릅니다(거리에 비례한 피해). Decision 0002의 원칙 — *"쓸 무기 없이
추상화를 먼저 만들면 검증되지 않은 구조가 된다"* — 대로, **F11이 F11에 맞는 것을 만듭니다.**

### 3. `AttachmentLogic.PushAway`를 꺼내서 공유합니다 (중복 제거)

확산 부채꼴은 축 회전(로드리게스)이 필요하고, `AttachmentLogic`의 밀어내기는 대원 회전이
필요합니다. 둘을 `Rotation`에 모으고 `AttachmentLogic`이 그걸 호출하게 합니다.
→ **새 코드가 아니라 기존 코드의 재사용**이고, F09 테스트가 회귀 그물 역할을 합니다.

(조준 보정용 `Towards`는 위 표대로 삭제되며, `Exactly`·`AroundAxis`·`Perpendicular`만 남습니다.)

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 투사체 무기는 **플레이어의 충돌과 무관하게** 스스로 발사한다 | CAN-001 |
| B2 | 발사 조건은 **쿨다운 종료 하나뿐**이다 | CAN-001 (개정) |
| B3 | 적이 하나도 없어도 **발사한다** — 대상을 탐색하지 않는다 | CAN-001 (개정) |
| B4 | 발사 방향은 **포신의 월드 방향**이며 공 회전을 따른다. 이것이 유일한 방향 결정자다 | WPN-002, CAN-001 |
| B5 | **조준 보정을 하지 않는다.** 탄은 포신 방향 그대로 나간다 | CAN-001 (개정) |
| B6 | 1회 발사 수 N과 확산 각도 S는 **수치**다. N발이 S도 폭에 **균등 분포**한다 | WPN-009 |
| B7 | N=1이면 확산 각도와 무관하게 **정확히 조준 방향**으로 1발 나간다 | WPN-009 (단발이 특수 케이스가 아니어야 함) |
| B8 | 직선 투사체는 **반드시 Object Pool**에서 나온다 | WPN-009, 기획서 15장 |
| B9 | 동시 생존 투사체에 **상한**이 있다. 상한에 걸리면 그 발사는 건너뛴다 (풀 고갈 금지) | WPN-009 Exception |
| B10 | 투사체는 **사거리만큼 이동하면 소멸**하고 풀로 돌아간다 | WPN-009 |
| B11 | 투사체는 적에게 맞으면 피해를 주고, **관통 수만큼** 더 진행한 뒤 소멸한다 | WPN-009 |
| B12 | 투사체 피해는 **DMG-002 적별 쿨다운을 공유**한다 — 쿨다운 중인 적은 통과한다 (관통 수 소모 없음) | DMG-002 |
| B13 | 투사체는 **플레이어를 맞히지 않는다** | — |
| B14 | 투사체는 **터널링하지 않는다** — 한 스텝의 이동 구간 전체에서 판정한다 | 설계 판단 1 |
| B15 | 접촉 공격 무기(F09)는 투사체를 발사하지 않는다. 반대도 같다 | WPN-008 |
| B16 | 세션 종료 시 살아있는 투사체는 **전부 정리**된다 | WPN-007 Exception과 같은 취지 |

## Architecture

```text
Game.Core (엔진 참조 없음)
├─ Rotation             # Exactly(from, toward, deg) / AroundAxis(v, axis, deg)
│                       #   ↳ AttachmentLogic.PushAway가 이걸로 대체됨 (중복 제거)
└─ ProjectileLogic      # CanFire(lastFireTime, now, interval)                   (B2, B3)
                        # SpreadDirections(aim, up, count, spreadDeg, into)      (B6, B7)
                        # Advance(position, direction, speed, dt)                (B10)

Game.Gameplay
├─ WeaponDefinition            # 투사체 구역 필드 추가                            (B6)
├─ ProjectileWeaponController  # 부착 무기를 훑어 포신 방향으로 발사               (B1~B5, B15)
├─ ProjectilePool              # ObjectPool<Projectile> + 동시 생존 상한          (B8, B9, B16)
└─ Projectile                  # 트랜스폼 전진 + SphereCast 판정                  (B10~B14)
```

컨트롤러는 적을 모릅니다. 부착된 무기의 트랜스폼과 쿨다운만 봅니다.

`ProjectileWeaponController`는 **공의 자식임을 가정하지 않습니다** (Decision 0002 제약).
`WeaponSlots.DirectionAt(i)`를 월드로 변환해 쓸 뿐이므로, F12에서 궤도 마운트가 들어와도 그대로 동작합니다.

## Initial tuning values (캐논 `WeaponDefinition` 에셋)

| Item | Value | Status |
|---|---:|---|
| 이동 방식 | 직선(Straight) | CONFIRMED (WPN-009, F10 범위) |
| 발사 간격 | 0.7 s | TEMPORARY — 각도 조건이 이미 게이트라 너무 짧으면 난사가 됩니다 |
| 1회 발사 수 | 1 | TEMPORARY — **캐논의 기본은 단발**. 산탄은 이 값만 올리면 되는 걸 보이는 게 목적 |
| 확산 각도 | 0° | TEMPORARY (발사 수 1이므로 무의미, B7 확인용) |
| 투사체 속도 | 18 m/s | TEMPORARY |
| 사거리 | 14 m | TEMPORARY — 적 스폰 링 반경보다 짧게 (화면 밖까지 날아가지 않도록) |
| 관통 수 | 0 | TEMPORARY (첫 적에게 맞고 소멸) |
| 투사체 위력 | 6 | TEMPORARY (본체 기본 10, 스파이크 추가 8 대비 — 자동 공격이라 낮게) |
| 동시 생존 투사체 상한 | 32 | TEMPORARY (B9) |
| 투사체 반경 (SphereCast) | 0.15 m | TEMPORARY |

> **발사 각도·조준 보정 수치는 삭제되었습니다** (CAN-001 개정). 1차 구현의 25°/8°가 실측에서
> 8.8초에 3발이라는 결과를 냈고, 그게 규칙을 바꾼 직접적인 근거입니다.
>
> 이제 발사 간격 0.7초가 **유일한 발사 빈도 조절 손잡이**입니다. 무조건 발사이므로
> 실제 발사 수는 예측 가능합니다 — 90초 세션에서 약 128발.

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01~F09 회귀 포함 — 특히 `AttachmentLogic` 리팩터링 회귀)
3. PlayMode 테스트 전체 통과
4. FirstPlayable 씬 플레이: 캐논을 얻으면 **적이 있든 없든 일정 주기로 포신 방향으로 총알이 나간다**
5. **발사 수를 5, 확산을 30°로 바꾸면 코드 수정 없이 산탄이 된다** — WPN-009의 핵심 주장
6. 투사체가 풀에서 나오고 풀로 돌아간다 (생성 수가 발사 수보다 훨씬 적다)
7. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core)

| 테스트 | 검증 |
|---|---|
| `Can001_OffCooldown_CanFire` | B2 |
| `Can001_OnCooldown_CannotFire` | B2 |
| `Can001_FirstShot_IsNotGated` | B2 — 첫 발이 한 주기를 기다리지 않음 |
| `Wpn009_SingleShot_UsesExactBarrelDirection` | B5, B7 — 보정 없이 포신 그대로 |
| `Wpn009_Shotgun_SpreadsEvenlyAcrossAngle` | B6 — **산탄이 수치임을 증명** |
| `Wpn009_Shotgun_OutermostShots_MatchHalfSpread` | B6 — 확산 폭이 정확 |
| `Wpn009_ShotCount_ClampedToBurstCeiling` | B9 |
| `Wpn009_Advance_MovesBySpeedTimesDelta` | B10 |
| `Rotation_Exactly_Antipodal_StillRotates` | 공유 헬퍼 — NaN 방어 |
| `Rotation_Exactly_Coincident_StillRotates` | 공유 헬퍼 — WPN-001a가 의존 |
| `Rotation_AroundAxis_MatchesRequestedAngle` | 확산의 기반 |

> 각도 게이트·조준 보정 테스트(`Can001_TargetOutsideArc_*`, `Can001_AimAssist_*`,
> `Target_*`, `Rotation_Towards_*`)는 **해당 코드와 함께 삭제**됩니다.

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Can001_CannonFiresAtEnemy_WithoutPlayerCollision` | B1 — **접촉 없이 피해가 들어감** |
| `Can001_NoEnemy_StillFires` | B3 — **개정의 핵심**. 적이 없어도 쏜다 |
| `Can001_FiresAlongBarrel_NotAtEnemy` | B4, B5 — 적이 옆에 있어도 **포신 방향**으로 나간다 |
| `Can001_BallRotation_ChangesFireDirection` | B4 — 공을 돌리면 탄 방향이 따라 바뀐다 |
| `Can001_FireRate_MatchesInterval` | B2 — 관측 시간 ÷ 간격 만큼 발사된다 |
| `Wpn009_ProjectileSharesHitCooldown` | B12 |
| `Wpn009_ProjectileDoesNotHitPlayer` | B13 |
| `Wpn009_ProjectileExpiresAtRange` | B10 |
| `Wpn009_ProjectilesComeFromPool` | B8 — 발사 N회 후 생성 수 < N |
| `Wpn009_LiveCap_SkipsFire` | B9 |
| `Wpn009_FastProjectile_DoesNotTunnel` | B14 — **고속에서도 관통 안 됨** |
| `Wpn008_ContactWeapon_FiresNothing` | B15 |
| `Wpn009_BurstCount_ComesFromTheDefinitionAlone` | B6 — 산탄이 데이터임을 런타임에서 확인 |
| `Wpn009_SessionEnd_ClearsProjectiles` | B16 |

## Manual play checks

- **"내가 굴리는 방향이 사격 방향이 된다"가 느껴지는가** — 이 기능의 핵심
- 발사 간격 0.7초가 적절한가 — 너무 뜸한가, 난사인가
- **너무 안 맞는가** — 조준 보정을 뺀 것이 옳은 판단이었는지 판정하는 항목입니다.
  "거의 안 맞는다"가 나오면 CAN-001에 보정을 다시 넣습니다
- 공이 빠르게 구를 때 탄이 **난사처럼 흩어져 보이는가**, 아니면 통제되는 느낌인가
- 투사체가 눈에 보이는가 (회색 프리미티브, 연출은 P08)
- 스파이크(접촉)와 캐논(투사체)을 **둘 다 가졌을 때 역할이 구분되는가**
- 산탄 설정으로 바꿨을 때 실제로 재미있는가 (Done condition 5의 체감 버전)

## Open questions

- **조준 보정을 뺀 것이 옳은가** — Manual play checks의 "너무 안 맞는가"가 판정합니다.
  다시 넣게 되면 `Rotation.Towards`와 `ProjectileLogic.AimDirection`을 되살리면 됩니다.
  다만 그때는 **CAN-001부터** 고칩니다
- **투사체가 죽은 적(시체)을 맞히는가** — 지금 `EnemyHealth.CanBeHit`이 `IsAlive`를 보므로
  자연히 통과합니다. 관통 수를 소모시키지 않는 게 맞다고 보고 그렇게 구현하지만,
  "시체에 총알이 박히는" 연출이 필요하면 P08에서 재검토
- **발사 각도를 무기별 수치로 둔 이유** — 접촉(F09)은 60°, 캐논은 25°입니다. 같은 `arcHalfAngle`
  필드를 재사용할지 별도 필드를 둘지 고민했으나, **의미가 다르므로 분리**합니다
  (접촉 = 피해 판정 범위, 투사체 = 발사 허용 범위)
- **R1(무기 도달 가능성)이 이 기능에도 걸립니다** — 캐논을 못 얻으면 F10 전체를 못 봅니다.
  F09 검증 보고서 R1 참조. F12 이후 페이싱을 한 번에 조정할 예정
