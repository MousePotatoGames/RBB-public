# Feature: F11 Tesla Ring (방전 공격 · 근거리 자동 타격)

## Status

Verified (자동 검증 범위) — 2026-08-06. EditMode 158/158, PlayMode 88/88, 런타임 실측 완료.
Done conditions 1·2·3·5·6 `VERIFIED`, 4·7은 Manual play checks로 남아 있습니다.
→ [검증 보고서](../Reports/2026-08-06_F11-tesla-zap_VERIFICATION.md) · 스펙 승인 2026-08-06

## Purpose

**무기 3종이 전부 살아납니다.** 지금 테슬라 링은 붙어도 아무 일도 하지 않는 마지막 무기입니다.

그리고 이번이 **세 번째이자 마지막 공격 방식 축**입니다 ([Decision 0002](../Decisions/0002-weapon-mount-types.md)):

| 기능 | 추가되는 공격 축 | 새 무기 |
|---|---|---|
| F09 | 접촉(Contact) | 스파이크 |
| F10 | 투사체(Projectile) + 직선 | 캐논 |
| **F11** | **방전(Zap)** | **테슬라** |
| F12 | 공격 축 추가 **없음** — 마운트만 | 도끼·펫 (**데이터만**) |

F11이 끝나면 WPN-008의 세 방식이 전부 구현되고, F12의 신규 무기는 **순수 데이터**가 됩니다.
그게 Decision 0002가 주장한 것이고, F12에서 실제로 검증됩니다.

## Player's perspective

테슬라가 붙으면 **바짝 붙은 적이 알아서 지져집니다.** 조준도, 방향도 필요 없습니다 —
사거리 안에 들어온 가장 가까운 적 하나에게 주기적으로 번개가 튑니다.

캐논과 정반대입니다. 캐논은 멀리(14m) 나가지만 **어디로 갈지는 공 회전이 정합니다.**
테슬라는 짧지만(5m) **무조건 맞습니다.** 대신 **가까울수록 아픕니다** — 적 무리 한가운데로
파고드는 것이 손해가 아니라 이득이 되는 유일한 무기입니다.

## Referenced rules

- **TES-001 방전 공격** — 주기적 탐색, 최근접 적 1기에게 번개 연결, **부착점과 적이 가까울수록 피해 증가**, 공격 순간에만 쿼리
- **WPN-008 공격 방식** — 방전(Zap): 사거리 내 최근접 적 1기에게 **즉시 피해**, 조준 없이 자동 대상
- **WPN-007 마운트 타입** — 테슬라는 **표면(Surface)**
- **DMG-002 중복 히트 방지** — 방전 피해도 적별 히트 쿨다운 공유
- 기획서 15장 Web 성능 — 매 프레임 Find/GetComponent 금지

## In scope

- `Game.Core`: **`ZapLogic`** — 주기 판정, 사거리 내 최근접 대상 선택(거리 포함), 거리 비례 피해
- `WeaponDefinition`: **방전 구역 필드 추가** (주기, 사거리, 최대·최소 피해)
- `Game.Gameplay`: **`ZapWeaponController`** — 부착된 방전 무기 구동, `Zapped` 이벤트 발생
- `Game.Presentation`: **`ZapVisual`** — 그레이박스 번개선 (아래 설계 판단 3)
- 테슬라 `WeaponDefinition` 에셋 수치 채우기 (에셋은 이미 존재)
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- **즉시(Hitscan) 이동 방식** — 아래 설계 판단 1. Decision 0002의 예고와 다릅니다
- **연쇄 방전 (TES-002)** — 보스가 트리거라 FP에 없음
- **궤도·추종 마운트** — F12
- **번개 VFX·전기 사운드** — P08 / P09. 이번엔 판정 가능한 최소 선만
- 방전이 벽·장애물에 막히는 처리 — FP 아레나에 막는 지형이 없습니다

## 설계 판단 세 가지

### 1. Hitscan 이동 방식을 만들지 않습니다 (Decision 0002에서 이탈)

[Decision 0002](../Decisions/0002-weapon-mount-types.md)는 F11이 *"방전 공격 + 즉시(레이저) 이동"*
을 추가한다고 적었습니다. **방전만 만들고 Hitscan은 만들지 않습니다.**

둘은 다른 것입니다:

| | 방전(Zap) — WPN-008 | 즉시(Hitscan) — WPN-009 |
|---|---|---|
| 대상 | **최근접 적을 자동 선택** | 발사 방향으로 직진 |
| 조준 | 없음 (대상이 곧 방향) | 포신 방향 |
| 소속 | 공격 방식 | 투사체 **이동 방식** |

테슬라는 **방전**입니다. Hitscan을 쓰는 무기가 FP에 하나도 없습니다.
지금 만들면 소비자 없는 코드가 남습니다 — Decision 0002가 스스로 경고한 것과 같은 상황입니다:

> 쓸 무기 없이 추상화를 먼저 만들면 검증되지 않은 구조가 된다.

레이저 무기가 실제로 필요해지는 시점(F12 이후 신규 무기)에 만듭니다.
**F10에서 조준 코드를 삭제한 것과 같은 판단입니다.**

### 2. 대상 탐색은 `DroneSpawner`의 활성 목록에서 합니다

TES-001은 *"공격 순간에만 Physics 쿼리 수행"* 이라고 적혀 있습니다.
스포너가 이미 살아있는 적 전부를 들고 있으므로 **쿼리를 아예 하지 않습니다** —
규칙의 의도(매 프레임 물리 쿼리 금지)를 더 강하게 지킵니다.

> F10이 이 경로를 만들었다가 CAN-001 개정으로 삭제했습니다. F11은 **다른 시그니처**로
> 되살립니다 — TES-001은 거리에 비례한 피해가 필요해서 인덱스만으로는 부족하고
> **거리도 함께** 돌려받아야 합니다. 그때 지운 판단이 옳았던 이유입니다.

### 3. 그레이박스 번개선을 넣습니다 (연출이 아니라 판정 가능성)

F09·F10은 무기와 투사체가 **눈에 보이는 물체**였습니다. 방전은 다릅니다 —
**시각 표현이 없으면 아무것도 안 보입니다.** 적 HP만 조용히 줄어듭니다.

그러면 "테슬라가 작동하는가"를 **사람이 판단할 수 없고**, Manual play checks가 전부 무의미해집니다.
그래서 `LineRenderer` 한 줄짜리 최소 표시를 넣습니다. **연출이 아니라 검증 장비입니다.**

P08이 진짜 번개로 교체합니다. 이번 것은 회색 직선 + 짧은 점멸이 전부입니다.

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 방전 무기는 **주기적으로** 스스로 발동한다 | TES-001 |
| B2 | 발동 시 **사거리 내 최근접 적 1기**를 자동 선택한다 | TES-001, WPN-008 |
| B3 | 사거리 안에 적이 없으면 **발동하지 않는다** (주기는 흐른다) | TES-001 |
| B4 | 피해는 **거리에 반비례**한다 — 붙어 있을수록 최대, 사거리 끝에서 최소 | TES-001 |
| B5 | 사거리 기준점은 **링 부착점**이지 공 중심이 아니다 | TES-001 "링 부착점 근처" |
| B6 | 방전은 **즉시** 적용된다 — 투사체를 만들지 않는다 | WPN-008 |
| B7 | 방전 피해는 **DMG-002 적별 쿨다운을 공유**한다 | DMG-002 |
| B8 | 쿨다운 중인 적은 **대상으로 선택되지 않는다** — 헛방전 금지 | DMG-002, B3 |
| B9 | 죽은 적(시체)은 대상이 되지 않는다 | — |
| B10 | 방전 무기가 없으면 아무 일도 없다 | — |
| B11 | 접촉·투사체 무기는 방전하지 않는다. 반대도 같다 | WPN-008 |
| B12 | 방전 무기도 **콜라이더를 갖지 않는다** | F08~F10 유지 |
| B13 | 세션 종료 시 방전이 멈춘다 | F07 |
| B14 | 발동 시 **부착점 → 적**을 잇는 선이 잠깐 보인다 | 설계 판단 3 |

> **B8이 중요한 이유**: 최근접 적이 DMG-002 쿨다운 중이면, 그 적을 골라놓고 피해가 0이 되어
> **주기 하나를 통째로 낭비**합니다. 붙어 있는 적을 계속 지지는 것이 테슬라의 정체성이므로,
> 쿨다운 중인 적은 후보에서 빼고 그 다음 가까운 적을 고릅니다.

## Architecture

```text
Game.Core (엔진 참조 없음)
└─ ZapLogic     # CanZap(lastZapTime, now, interval)                      (B1)
                # NearestInRange(origin, positions, count, range, out d)  (B2, B3)
                # Damage(distance, config)                                (B4)

Game.Gameplay
├─ WeaponDefinition      # 방전 구역 필드 추가                             (B4)
└─ ZapWeaponController   # 부착 무기 훑기 → 대상 선택 → 즉시 피해          (B1~B13)
                         # event Zapped(from, to)

Game.Presentation
└─ ZapVisual             # Zapped를 받아 LineRenderer 점멸               (B14)
```

`ZapWeaponController`는 `ProjectileWeaponController`와 같은 규약을 따릅니다 —
**무기가 공의 자식이라고 가정하지 않고** 무기 트랜스폼만 읽습니다 (Decision 0002 제약).
그래야 F12의 궤도·추종 마운트에서 재작성이 없습니다.

## Initial tuning values (테슬라 `WeaponDefinition` 에셋)

| Item | Value | Status |
|---|---:|---|
| 방전 주기 | 0.9 s | TEMPORARY — 무조건 맞으므로 캐논(0.7)보다 느리게 |
| 사거리 | 5 m | TEMPORARY — **캐논 14m와의 대비가 정체성**입니다 |
| 최대 피해 (거리 0) | 12 | TEMPORARY (본체 기본 10, 캐논 6 대비 — 붙어야 하는 대가) |
| 최소 피해 (사거리 끝) | 4 | TEMPORARY — 3배 차이가 "가까울수록 아프다"를 체감시킬 최소치 |
| 번개선 표시 시간 | 0.08 s | TEMPORARY (그레이박스) |

> **왜 사거리가 5m인가**: TES-001의 Result가 "근거리 군중 처리"입니다. 길면 캐논과 구분이
> 사라지고, 거리 비례 피해(B4)도 의미가 없어집니다. **짧아야 무리 속으로 파고들 이유가 생깁니다.**

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01~F10 회귀 포함)
3. PlayMode 테스트 전체 통과
4. FirstPlayable 씬 플레이: 테슬라를 얻으면 **가까운 적이 알아서 지져진다**
5. **붙어 있을 때와 사거리 끝일 때 피해가 눈에 띄게 다르다** — TES-001의 핵심
6. 번개선이 보인다 (그레이박스라도)
7. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core)

| 테스트 | 검증 |
|---|---|
| `Tes001_OffCooldown_CanZap` | B1 |
| `Tes001_OnCooldown_CannotZap` | B1 |
| `Tes001_NearestInRange_IsChosen` | B2 |
| `Tes001_NoEnemyInRange_ReturnsNone` | B3 |
| `Tes001_ExactlyAtRange_CountsAsInRange` | B3 — 경계 |
| `Tes001_ReturnsDistanceOfChosenTarget` | B4 — 피해 계산의 입력 |
| `Tes001_Damage_MaxAtZeroDistance` | B4 |
| `Tes001_Damage_MinAtMaxRange` | B4 |
| `Tes001_Damage_FallsOffWithDistance` | B4 — 단조 감소 |
| `Tes001_Damage_BeyondRange_ClampsToMin` | B4 — 경계 방어 |
| `Tes001_EmptyList_ReturnsNone` | B2 — 경계 |
| `Tes001_OnlyConsidersTheLiveCount` | B2 — 풀 재사용 배열 방어 |

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Tes001_TeslaDamagesNearestEnemy` | B1, B2 — **접촉 없이 피해** |
| `Tes001_NoEnemyInRange_DoesNothing` | B3 |
| `Tes001_CloserEnemy_TakesMoreDamage` | B4 — **동일 조건 대조**, 이 기능의 핵심 |
| `Tes001_RangeIsFromWeapon_NotBallCentre` | B5 |
| `Tes001_ZapSharesHitCooldown` | B7 |
| `Tes001_CooldownEnemy_IsNotChosen` | B8 — 주기를 낭비하지 않는다 |
| `Tes001_DeadEnemy_IsNotChosen` | B9 |
| `Wpn008_ProjectileWeapon_DoesNotZap` | B11 |
| `Tes001_AttachedWeapon_HasNoCollider` | B12 |
| `Tes001_SessionEnd_StopsZapping` | B13 |
| `Tes001_ZappedEvent_CarriesBothEndpoints` | B14 — 선을 그릴 좌표가 실제로 온다 |

## Manual play checks

- **"붙으면 알아서 지져진다"가 느껴지는가** — 이 기능의 핵심
- **가까울수록 아픈 것이 체감되는가** (수치상 12 → 4, 3배)
- 사거리 5m가 적절한가 — 너무 짧아 안 닿는가, 너무 길어 캐논과 구분이 안 되는가
- 주기 0.9초가 적절한가
- **무기 3종의 역할이 구분되는가** — 스파이크(부딪히기) / 캐논(굴려 조준) / 테슬라(파고들기)
- 번개선이 보이는가, 그리고 **어떤 적을 때렸는지 읽히는가**
- 적 무리 한가운데로 파고드는 것이 **재미있는 선택으로 느껴지는가**

## Open questions

- **번개선의 시작점을 무기로 할지 공 중심으로 할지** — 판정은 무기 기준(B5)이지만,
  시각적으로는 공에서 나가는 게 읽기 쉬울 수 있습니다. 일단 **무기 기준**으로 통일하고
  플레이테스트에서 재검토
- **B8이 과한가** — 쿨다운 중인 적을 건너뛰면 테슬라가 "항상 뭔가는 때린다"가 됩니다.
  그게 강한지는 수치(주기 0.9 vs DMG-002 쿨다운)로 조절 가능하다고 보지만,
  플레이테스트에서 "너무 강하다"가 나오면 이 규칙부터 의심할 것
- **R1(무기 도달 가능성)이 여전히 걸립니다** — F09·F10 보고서 참조.
  테슬라는 추첨 3순위에 걸리면 60초에나 나오는데 무입력 사망이 약 10.5초입니다.
  F12 이후 페이싱을 한 번에 조정할 예정
