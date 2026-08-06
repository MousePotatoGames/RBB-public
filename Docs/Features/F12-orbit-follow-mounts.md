# Feature: F12 Orbit & Follow Mounts (공전 도끼 · 추종 펫)

## Status

Implementing — 구현 완료. EditMode 174/174, PlayMode 103/103.
`/first-playable:verify F12-orbit-follow-mounts` 대기.
스펙 승인: 2026-08-06 (WPN-008a 신설 + Decision 0002 개정 포함 승인)

## Purpose

**[Decision 0002](../Decisions/0002-weapon-mount-types.md)의 주장을 검증합니다.**

그 결정은 "무기 = 데이터"라는 구조를 세우면서, F12에 대해 이렇게 예측했습니다:

> F12 시점에는 새 무기가 **순수 데이터**입니다. 공격 코드 추가 **0**.

이번 기능이 그 예측을 실제로 시험합니다. 그리고 **절반만 맞습니다** — 아래 설계 판단 1 참조.
틀린 절반이 이 기능에서 가장 값진 결과입니다.

동시에 실질적인 목적이 하나 더 있습니다: **무기 종류를 슬롯 수보다 많게 만드는 것**입니다.
지금은 3종 / 3슬롯이라 매 판 **획득하는 집합이 항상 동일**합니다. 5종이 되어야
WPN-005가 노린 "판마다 조합이 달라진다"가 처음으로 실제가 됩니다 (HYP-006의 근거).

## Player's perspective

**공전 도끼**는 플레이어 주위를 계속 돕니다. 공이 굴러도 도끼는 자기 궤도를 유지하며 직립합니다.
가만히 서 있어도 주변을 쓸어내므로, **적에게 둘러싸였을 때 가장 강합니다.**

**추종 펫**은 뒤를 따라다니며 스스로 총을 쏩니다. 공과 별개로 움직여서, 급하게 방향을 틀면
살짝 늦게 따라옵니다. 캐논과 달리 **공 회전과 무관하게** 발사됩니다.

셋과 둘의 대비가 생깁니다 — 표면 무기(스파이크·캐논·테슬라)는 **공과 함께 구르고**,
궤도·추종 무기는 **자기 운동을 합니다.**

## Referenced rules

- **WPN-007 마운트 타입** — 궤도(Orbit): 일정 반경 공전, 공 회전과 무관, 직립 유지 / 추종(Follow): 지연 추종, 진행 방향
- **WPN-007 Exception** — 궤도·추종 무기는 플레이어 사망 시 **명시적으로 정리**한다
- **WPN-008 공격 방식** — 접촉은 "무기 판정 범위에 닿은 적에게 피해 (스파이크, **궤도 무기**)"
- **WPN-005 캡슐 무기 종류** — 보유 종류 중 슬롯 수만큼 무중복 추첨. **종류 > 슬롯이어야 조합이 달라진다**
- **WPN-003 무기 슬롯** — 최대 3개
- **DMG-002 중복 히트 방지** — 모든 공격 방식이 공유
- **CAN-001 / WPN-009** — 펫의 투사체는 기존 규칙 그대로
- 기획서 15장 Web 성능 — 매 프레임 Find/GetComponent 금지

## In scope

- `Game.Core`: **`OrbitLogic`** — 각도 전진, 공전 위치
- `Game.Core`: **`FollowLogic`** — 지연 추종 (프레임률 독립)
- `Game.Core`: **`SweepLogic`** — 반경 안의 **모든** 적 인덱스 (궤도 접촉용)
- `Game.Core`: `WeaponKind`에 **Axe / Pet 추가**
- `WeaponSlots`: 마운트별 부착 분기 (궤도·추종은 **공의 자식이 아님**), `_kindTaken` 크기 버그 수정
- `Game.Gameplay`: **`MountedWeapon`** — 궤도·추종 무기의 자체 운동
- `Game.Gameplay`: **`SweepContactController`** — 궤도·추종 접촉 무기의 피해 (설계 판단 1)
- `Game.Gameplay`: **`WeaponContainer`** — 정지 컨테이너 + 세션 종료 시 일괄 정리
- `WeaponDefinition`: 마운트 구역 필드 추가 (반경, 각속도, 높이, 추종 속도·거리, 접촉 반경)
- 도끼·펫 `WeaponDefinition` 에셋 **2개 신규**, 캡슐 풀을 **5종**으로
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- **유도(Homing) 투사체** — 펫은 직선으로 충분합니다. 쓸 무기가 생기면 그때
- **Hitscan** — F11에서 뺀 그대로
- 무기 2단계 강화 (WPN-004) — 보스가 트리거라 FP에 없음
- **도끼·펫 아트** — P07. 그레이박스 프리미티브 유지
- **궤도 궤적·펫 애니메이션** — P08
- 궤도 무기가 지형·벽에 막히는 처리 — FP 아레나에 막는 지형이 없습니다

## 설계 판단 네 가지

### 1. Decision 0002의 "공격 코드 추가 0"은 절반만 맞습니다

| 신규 무기 | 마운트 | 공격 | 새 공격 코드 |
|---|---|---|---|
| **추종 펫** | Follow | Projectile | **0** ✅ |
| **공전 도끼** | Orbit | Contact | **필요함** ❌ |

**펫은 예측대로입니다.** `ProjectileWeaponController`가 무기의 `transform.position`과
`transform.up`만 읽도록 만들어져 있어(Decision 0002 제약), 무기가 공의 자식이 아니어도
그대로 동작합니다. **에셋만 만들면 끝납니다.**

**도끼는 아닙니다.** F09의 접촉 피해는 *"본체 충돌에 더해지는 추가 피해"* 입니다 —
공이 적에게 닿아야 성립합니다. 궤도 도끼는 **공이 닿지 않아도 스스로 적을 맞혀야** 합니다.
같은 `Contact` 이름이지만 **피해가 발생하는 경로가 다릅니다.**

| | 표면 접촉 (F09) | **궤도·추종 접촉 (신규)** |
|---|---|---|
| 계기 | 공의 충돌 이벤트 | 자체 주기 판정 |
| 피해 | 본체 피해의 **수식어** | **독립 피해원** |
| 속도 비례 | 공 속도 | 없음 (자기 각속도가 곧 위력) |
| 대상 | 충돌한 적 1기 | 반경 안 **모두** |

이것은 규칙에도 없던 사실입니다. WPN-008은 접촉을 하나로 묶었지만
**마운트에 따라 두 경로**가 필요합니다. → 아래 "필요한 규칙 갱신".

### 2. 궤도·추종 무기도 콜라이더를 달지 않습니다 (Decision 0002에서 이탈)

Decision 0002는 궤도·추종 무기에 **트리거 콜라이더 + 키네마틱 Rigidbody**를 예고했습니다.
**반경 판정으로 대체합니다.**

| | 트리거 콜라이더 | **반경 판정 (채택)** |
|---|---|---|
| 필요한 컴포넌트 | Rigidbody + Collider + 레이어 | **없음** |
| 물리 상호작용 위험 | 키네마틱이라도 밀어냄·이벤트 순서 문제 | **없음** |
| 관통·다중 히트 | 트리거 재진입 관리 | 반경 안 전부, 매 판정 |
| 일관성 | 무기 중 유일하게 콜라이더 보유 | **F09~F11과 동일** |

F09(원뿔), F10(SphereCast), F11(거리)이 전부 **콜라이더 없이** 판정합니다.
궤도만 콜라이더를 쓰면 무기 시스템에 두 가지 판정 철학이 섞입니다.
적 목록은 이미 스포너가 들고 있으므로 반경 판정이 더 싸기도 합니다.

> Decision 0002에서 이탈하는 것이 **F11의 Hitscan에 이어 두 번째**입니다.
> 개별 판단으로 넘기지 않고 **Decision 0002를 개정**해 실제 구현을 반영합니다
> (아래 "필요한 문서 갱신"). 결정 문서와 코드가 벌어진 채로 두지 않습니다.

### 3. 갱신 타이밍은 Decision 0002 그대로 — FixedUpdate

공은 `FixedUpdate`에서 물리로 움직이고 보간됩니다. 무기를 `Update`에서 옮기면
**시각적으로는 맞지만 판정 시점의 위치가 어긋납니다.**

`FixedUpdate`에서 위치를 갱신하고, Rigidbody 없이 트랜스폼만 씁니다
(판정도 같은 `FixedUpdate`에서 하므로 물리 쿼리와 어긋날 일이 없습니다).

> Decision 0002는 키네마틱 Rigidbody + `MovePosition` + 보간을 제안했지만,
> 그건 **콜라이더가 있을 때** 필요한 구성입니다. 콜라이더가 없으면 Rigidbody도 불필요합니다.
> 다만 보간이 없어 60fps 미만에서 궤도가 각져 보일 수 있습니다 — Open questions에 남깁니다.

### 4. 정지 컨테이너 하나로 라이프사이클을 잡습니다

Decision 0002가 경고한 실제 위험입니다:

> 궤도·펫 무기는 **플레이어의 자식이 아니므로 플레이어가 죽어도 남습니다.**

`WeaponContainer` 하나를 두고 궤도·추종 무기를 전부 그 밑에 둡니다.
세션 종료 시 컨테이너가 자식을 정리합니다. 컨테이너 자체는 **움직이지 않습니다** —
각 무기가 플레이어 위치를 직접 읽습니다 (갱신 순서 의존을 만들지 않기 위해).

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 궤도 무기는 플레이어 주위를 **일정 반경으로 공전**한다 | WPN-007 |
| B2 | 궤도 무기는 **공의 회전을 상속하지 않는다** — 직립을 유지한다 | WPN-007 |
| B3 | 궤도 각도는 **각속도 × 시간**으로 전진하며 프레임률에 무관하다 | — |
| B4 | 추종 무기는 플레이어를 **지연 추종**한다 — 즉시 붙지 않는다 | WPN-007 |
| B5 | 추종 무기는 플레이어에게서 **일정 거리를 유지**한다 (겹치지 않는다) | WPN-007 |
| B6 | 추종 무기의 지연은 **프레임률에 무관**하다 | — |
| B7 | 궤도·추종 무기는 **공의 자식이 아니다** — 컨테이너 밑에 있다 | 설계 판단 4 |
| B8 | 표면 무기는 **기존대로 공의 자식**이며 함께 회전한다 | WPN-002 유지 |
| B9 | 궤도·추종 **접촉** 무기는 주기적으로 **반경 안의 모든 적**에게 피해를 준다 | 설계 판단 1 |
| B10 | 그 피해는 **DMG-002 적별 쿨다운을 공유**한다 | DMG-002 |
| B11 | 표면 접촉 무기는 **기존 경로 그대로**다 (본체 충돌의 추가 피해) | SPK-001 유지 |
| B12 | 궤도·추종 **투사체** 무기는 **기존 투사체 코드를 그대로** 쓴다 | 설계 판단 1 — 검증 대상 |
| B13 | 궤도·추종 무기도 **콜라이더를 갖지 않는다** | 설계 판단 2 |
| B14 | 세션 종료 시 궤도·추종 무기가 **정리된다** | WPN-007 Exception |
| B15 | 무기 종류가 **5종**이 되어 추첨 조합이 판마다 달라진다 | WPN-005 |
| B16 | 같은 종류는 한 판에 한 번만 등장한다 (종류가 늘어도 유지) | WPN-005 |

## Architecture

```text
Game.Core (엔진 참조 없음)
├─ WeaponKind        # Axe, Pet 추가                                   (B15)
├─ OrbitLogic        # Advance(angle, angularSpeed, dt)                (B3)
│                    # Position(centre, angle, radius, height)         (B1)
├─ FollowLogic       # DesiredPosition(current, target, standoff)      (B5)
│                    # Step(current, desired, speed, dt)               (B4, B6)
└─ SweepLogic        # AllInRange(origin, positions, count, r, into)   (B9)

Game.Gameplay
├─ WeaponDefinition       # 마운트 구역 필드 추가
├─ WeaponSlots            # 마운트별 부착 분기                          (B7, B8)
├─ WeaponContainer        # 정지 부모 + 세션 종료 정리                   (B14)
├─ MountedWeapon          # 궤도·추종 자체 운동 (FixedUpdate)           (B1~B6)
└─ SweepContactController # 궤도·추종 접촉 피해                         (B9, B10)
```

`ProjectileWeaponController`는 **손대지 않습니다.** 펫이 그대로 동작해야 B12가 참입니다.

## Initial tuning values

### 공전 도끼 (`Weapon_Axe`) — 궤도 / 접촉

| Item | Value | Status |
|---|---:|---|
| 공전 반경 | 2.2 m | TEMPORARY — 공 반경 0.5보다 충분히 커야 "돈다"가 보입니다 |
| 각속도 | 180 °/s | TEMPORARY (2초에 한 바퀴) |
| 공전 높이 | 0.3 m | TEMPORARY (지면보다 살짝 위) |
| 접촉 판정 반경 | 0.7 m | TEMPORARY |
| 접촉 주기 | 0.25 s | TEMPORARY — DMG-002 쿨다운이 실질 상한이라 짧아도 됩니다 |
| 접촉 피해 | 9 | TEMPORARY (스파이크 추가 8, 테슬라 최대 12 사이) |

### 추종 펫 (`Weapon_Pet`) — 추종 / 투사체

| Item | Value | Status |
|---|---:|---|
| 유지 거리 | 1.8 m | TEMPORARY |
| 추종 속도 | 6 | TEMPORARY (지수 감쇠 계수 — 클수록 빨리 붙습니다) |
| 발사 간격 | 1.0 s | TEMPORARY (캐논 0.7보다 느리게) |
| 투사체 위력 | 5 | TEMPORARY (캐논 6보다 약하게 — 조준이 공 회전에 안 묶여 편하므로) |
| 투사체 속도·사거리 | 14 m/s · 10 m | TEMPORARY |
| 1회 발사 수 / 확산 | 1 / 0° | TEMPORARY |

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01~F11 회귀 포함)
3. PlayMode 테스트 전체 통과
4. FirstPlayable 씬 플레이: **도끼가 공 회전과 무관하게 돈다**, **펫이 뒤따라오며 쏜다**
5. **펫은 공격 코드 추가 0으로 동작한다** — Decision 0002 주장의 검증 (B12)
6. 무기 5종에서 판마다 **추첨 조합이 달라진다** (B15)
7. 세션 종료 후 궤도·추종 무기가 **남아 있지 않다** (B14)
8. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core)

| 테스트 | 검증 |
|---|---|
| `Wpn007_Orbit_AngleAdvancesWithTime` | B3 |
| `Wpn007_Orbit_AngleWraps` | B3 — 360° 넘김 |
| `Wpn007_Orbit_SameElapsed_SameAngle_RegardlessOfStepCount` | B3 — **프레임률 독립** |
| `Wpn007_Orbit_PositionIsOnTheCircle` | B1 — 중심에서 정확히 반경만큼 |
| `Wpn007_Orbit_PositionRespectsHeight` | B1 |
| `Wpn007_Orbit_MovesCentreWithPlayer` | B1 — 중심이 바뀌면 궤도도 따라감 |
| `Wpn007_Follow_ApproachesButDoesNotSnap` | B4 |
| `Wpn007_Follow_ConvergesToStandoffDistance` | B5 |
| `Wpn007_Follow_SameElapsed_SameResult_RegardlessOfStepCount` | B6 — **프레임률 독립** |
| `Wpn007_Follow_AtTarget_DoesNotJitter` | B4 — 퇴화 입력 방어 |
| `Sweep_AllInRange_ReturnsEveryoneInside` | B9 |
| `Sweep_AllInRange_ExcludesOutside` | B9 |
| `Sweep_AllInRange_ExactlyAtRadius_IsIncluded` | B9 — 경계 |
| `Sweep_AllInRange_ClampsToBuffer` | B9 — 버퍼 초과 방어 |
| `Sweep_AllInRange_OnlyConsidersLiveCount` | B9 — 풀 재사용 배열 방어 |
| `Wpn005_FiveKinds_ProduceDifferentDraws` | B15 — **씨앗이 다르면 조합이 달라진다** |
| `Wpn005_Draw_NeverRepeatsAKind` | B16 |

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Wpn007_OrbitWeapon_IsNotChildOfBall` | B7 |
| `Wpn007_SurfaceWeapon_IsStillChildOfBall` | B8 — 회귀 |
| `Wpn007_OrbitWeapon_IgnoresBallRotation` | B2 — **공을 돌려도 궤도가 안 흔들린다** |
| `Wpn007_OrbitWeapon_CirclesThePlayer` | B1 — 반경 유지하며 각도 변화 |
| `Wpn007_OrbitWeapon_FollowsMovingPlayer` | B1 |
| `Wpn007_FollowWeapon_LagsBehindThePlayer` | B4 |
| `Wpn007_FollowWeapon_KeepsStandoffDistance` | B5 |
| `Wpn007_OrbitContact_DamagesEnemiesInRadius` | B9 — **본체 충돌 없이** |
| `Wpn007_OrbitContact_DamagesMultipleAtOnce` | B9 — 반경 안 모두 |
| `Wpn007_OrbitContact_SharesHitCooldown` | B10 |
| `Spk001_SurfaceContact_StillNeedsBodyCollision` | B11 — **F09 경로 회귀** |
| `Wpn007_PetFiresProjectiles_WithNoNewAttackCode` | B12 — **Decision 0002 검증** |
| `Wpn007_MountedWeapon_HasNoCollider` | B13 |
| `Wpn007_SessionEnd_ClearsMountedWeapons` | B14 |

## Manual play checks

- **도끼가 도는 게 보이는가**, 그리고 공 회전과 **무관하다는 게 읽히는가**
- 반경 2.2m / 각속도 180°/s가 적절한가 — 너무 멀거나 너무 빠른가
- **적에게 둘러싸였을 때 도끼가 강한 게 체감되는가** (도끼의 정체성)
- **펫이 따라오는 지연이 자연스러운가** — 너무 붙거나 너무 처지는가
- 펫이 쏘는 게 캐논과 **구분되는가** (공 회전에 안 묶임)
- 무기 5종에서 **판마다 다른 조합이 나오는 게 느껴지는가** (HYP-006)
- 무기가 3개 붙었을 때 화면이 **너무 정신없지 않은가** (기획서 25장 실루엣 리스크)

## 필요한 규칙·문서 갱신 (승인 시 함께 진행)

### GAME_RULES.md — WPN-008 접촉 방식에 마운트별 경로 명시

접촉 공격이 **마운트에 따라 두 경로**임을 규칙에 적습니다.
지금 WPN-008은 "무기 판정 범위에 닿은 적에게 피해"라고만 되어 있어,
표면 접촉이 *본체 충돌의 수식어*라는 사실(SPK-001)과 궤도 접촉이 *독립 피해원*이라는 사실을
구분하지 못합니다.

### Decision 0002 — 실제 구현 반영 개정

두 가지가 예고와 달라졌습니다:

| 항목 | 예고 | 실제 | 이유 |
|---|---|---|---|
| 궤도·추종 콜라이더 | 트리거 + 키네마틱 RB | **콜라이더 없음** | 설계 판단 2 |
| F11 Hitscan | F11에 추가 | **만들지 않음** | 쓰는 무기가 없음 |
| "공격 코드 추가 0" | 신규 2종 모두 | **펫만 해당** | 설계 판단 1 |

결정 문서를 사실에 맞게 고칩니다. **틀린 예측을 지우는 게 아니라, 무엇이 왜 달라졌는지 남깁니다** —
그 차이가 이 결정에서 배운 것입니다.

## Open questions

- **보간 없는 궤도가 60fps 미만에서 각져 보이는가** — Rigidbody와 보간을 뺐으므로
  프레임이 낮으면 궤도가 다각형처럼 보일 수 있습니다. WebGL 30fps에서 확인 필요
- **무기 3개가 전부 궤도·추종이면 공 주변이 산만한가** — 5종 중 3종 추첨이라
  가능한 조합입니다. 기획서 25장의 실루엣 리스크가 여기서 처음 현실이 됩니다
- **도끼 접촉 주기 0.25초가 의미 있는가** — DMG-002 쿨다운이 실질 상한이라
  주기를 줄여도 피해가 안 늘 수 있습니다. 그렇다면 주기 필드 자체가 불필요합니다
- **R1(무기 도달 가능성)** — 종류가 5종이 되면 원하는 무기를 뽑을 확률이 더 낮아집니다.
  F12 이후 페이싱 조정에서 `spawnTimes`와 함께 다뤄야 합니다
