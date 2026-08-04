# Feature: F09 Crusher Spike (무기 정의 SO · 접촉 공격 · 표면 마운트)

## Status

Implementing — 구현 완료 (EditMode 116/116, PlayMode 61/61), `/first-playable:verify F09-crusher-spike` 대기. 스펙 승인: 2026-08-05

## Purpose

**무기가 붙기만 하던 것을 실제로 작동하게 만듭니다.** F08은 "붙는다"까지였고, 무기는 지금 아무 일도 하지 않습니다.

그리고 이번 기능이 **무기 시스템의 뼈대**를 세웁니다 ([Decision 0002](../Decisions/0002-weapon-mount-types.md)):
무기 하나 = ScriptableObject 에셋 하나. F10·F11·F12는 여기에 **공격 방식과 마운트만 추가**하고, 새 무기는 에셋만 만들면 됩니다.

이 기능이 들어와야 FIRST_PLAYABLE의 목표 2번 **"부착 위치가 의미를 갖는가"**(HYP-005)를 처음 평가할 수 있습니다.

## Player's perspective

스파이크가 붙은 쪽으로 적을 들이받으면 훨씬 세게 부숩니다. 반대쪽으로 부딪히면 맨몸으로 박은 것과 같습니다.
공이 구르면서 스파이크가 돌기 때문에, **어느 순간에 부딪히느냐가 달라집니다.** 대시로 스파이크를 정면에 두고 박으면 가장 아프게 들어갑니다.

## Referenced rules

- SPK-001 스파이크 접촉 피해 — 충돌 피해에 **추가 피해**, 속도·정면도 비례, 대시 중 피해·넉백 증가, DMG-002 공유
- WPN-007 마운트 타입 — 스파이크는 **표면(Surface)** 마운트
- WPN-008 공격 방식 — 스파이크는 **접촉(Contact)**
- DMG-001 충돌 피해 공식 — 추가 피해도 같은 속도·정면도 인자를 씁니다
- DMG-002 중복 히트 방지 — 본체 피해와 **같은 쿨다운을 공유**합니다
- WPN-002 부착 방향 (표면 마운트)

## In scope

- `Game.Gameplay`: **`WeaponDefinition`** ScriptableObject — 마운트 타입, 공격 방식, 수치, 프리미티브 형상
- `Game.Core`: **`ContactWeaponLogic`** — 적이 무기 방향 원뿔 안인지 판정 + 추가 피해 계산
- `WeaponSlots`: 무기 생성을 `WeaponKind` 하드코딩에서 **정의 기반**으로 이관
- `PlayerDamageDealer`: 본체 피해에 접촉 무기 추가 피해를 더함
- `CapsuleSpawner` / `WeaponDrawLogic`: 정의 목록에서 추첨 (WPN-005 개정 반영)
- 스파이크 `WeaponDefinition` 에셋 1개
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- **투사체 공격 (WPN-009)** — F10. 발사 수·확산·이동 방식 전부 그때
- **방전 공격** — F11
- **궤도·추종 마운트 (WPN-007)** — F12. 이번엔 표면만
- 무기 2단계 강화 (SPK-002, WPN-004) — 보스가 트리거라 FP에 없음
- **스파크 VFX·금속 충돌음** (SPK-001 Result) — P08 / P09
- 무기 아트 — P07. 그레이박스 프리미티브 유지

## 설계 판단: 스파이크에 콜라이더를 달지 않습니다

SPK-001은 "스파이크 콜라이더가 적과 접촉"이라고 적혀 있지만, **콜라이더를 실제로 붙이지 않고 원뿔 판정으로 구현합니다.**

| | 콜라이더 방식 | **원뿔 판정 (채택)** |
|---|---|---|
| 공 구름 | 트리거라도 컴파운드 콜라이더가 되어 위험 | 영향 없음 |
| 트리거 이벤트 귀속 | 자식 콜라이더의 이벤트가 **공의 Rigidbody로 전달**되어 `PlayerDamageDealer`가 본체 충돌로 오인 | 문제 없음 |
| 연산 | 콜라이더 추가 | **내적 하나** |
| 부착 위치의 의미 | 콜라이더가 크면 방향이 흐려짐 | **방향이 곧 판정** |

결정적인 이유는 두 번째입니다. 자식 트리거 콜라이더의 이벤트는 부모 Rigidbody의 GameObject로 가므로, 스파이크만 스친 경우에도 `PlayerDamageDealer`가 본체 충돌 피해를 넣게 됩니다. 이걸 막으려면 무기마다 Rigidbody를 중첩해야 하는데 Unity가 권장하지 않는 구조입니다.

SPK-001의 **Result("충돌 피해에 추가 피해를 더한다")** 는 그대로 지켜집니다 — 추가 피해는 독립 피해원이 아니라 이미 일어난 본체 충돌의 **수식어**입니다.

부수 효과가 오히려 목적에 부합합니다: **적이 스파이크 쪽에 있을 때만 추가 피해가 들어가므로 부착 위치가 실제로 의미를 갖습니다** (FIRST_PLAYABLE 목표 2, HYP-005).

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 무기는 `WeaponDefinition` 에셋으로 정의되며, **코드 수정 없이** 새 무기를 만들 수 있습니다 | Decision 0002 |
| B2 | 정의는 **마운트 타입**과 **공격 방식**을 갖습니다 | WPN-007, WPN-008 |
| B3 | 접촉 공격 무기는 적이 **무기 방향 원뿔 안**에 있을 때만 추가 피해를 줍니다 | SPK-001, 위 설계 판단 |
| B4 | 원뿔 밖의 적에게는 추가 피해가 **0**입니다 — 부착 위치가 의미를 갖는 지점 | SPK-001 |
| B5 | 추가 피해는 **속도와 정면도에 비례**합니다 | SPK-001, DMG-001 |
| B6 | **대시 중**에는 추가 피해와 넉백이 증가합니다 | SPK-001 |
| B7 | 추가 피해는 본체 충돌 피해와 **같은 히트 쿨다운을 공유**합니다 (별도 판정 없음) | SPK-001 Exception, DMG-002 |
| B8 | 접촉 공격 무기가 없으면 추가 피해는 0이고, 본체 충돌 피해만 들어갑니다 | — |
| B9 | 접촉 공격이 아닌 무기(투사체·방전)는 추가 피해에 기여하지 않습니다 | WPN-008 |
| B10 | 접촉 무기가 여럿이면 각각의 기여를 **합산**합니다 | F12 궤도 무기 대비 |
| B11 | 무기는 여전히 **물리 콜라이더를 갖지 않습니다** | F08 유지 — 공 구름 보존 |
| B12 | 무기 원뿔 판정은 **공의 현재 회전을 반영**합니다 (무기가 공과 함께 돌므로) | WPN-002 |
| B13 | 캡슐이 스폰하는 무기는 **정의 목록에서 슬롯 수만큼 무중복 추첨**됩니다 | WPN-005 (개정) |

## Architecture

```text
Game.Core (엔진 참조 없음)
└─ ContactWeaponLogic   # InArc(weaponDir, toEnemyDir, halfAngle)          (B3, B4)
                        # BonusDamage(speed, maxSpeed, frontality, dashing, config) (B5, B6)

Game.Gameplay
├─ WeaponDefinition (SO) # 마운트 / 공격 방식 / 수치 / 형상               (B1, B2)
├─ WeaponSlots           # 정의 기반 생성으로 이관, 부착된 정의를 보관       (B12)
│                        # BonusDamageAgainst(enemyDir, ...) 제공          (B8~B10)
└─ PlayerDamageDealer    # 본체 피해 + 접촉 무기 추가 피해                 (B7)
```

`WeaponKind` enum은 **정의 에셋의 식별자로 남기되**, 동작 분기에는 더 이상 쓰지 않습니다.

## Initial tuning values (`WeaponDefinition` 에셋)

| Item | Value | Status |
|---|---:|---|
| 스파이크 원뿔 반각 | 60° | TEMPORARY — 좁으면 안 맞고, 넓으면 부착 위치가 무의미해집니다 |
| 스파이크 기본 추가 피해 | 8 | TEMPORARY (본체 기본 10 대비 — 스파이크 쪽 충돌이 약 1.8배) |
| 대시 시 추가 피해 배율 | 1.5 | TEMPORARY (SPK-001 "대시 중 피해 증가") |
| 대시 시 넉백 배율 | 1.4 | TEMPORARY (SPK-001 "대시 중 넉백 증가") |
| 마운트 / 공격 방식 | 표면 / 접촉 | CONFIRMED (WPN-007, WPN-008) |

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01~F08 회귀 포함)
3. PlayMode 테스트 전체 통과 (회귀 포함)
4. FirstPlayable 씬 플레이: 스파이크 쪽으로 박으면 반대쪽보다 **눈에 띄게 세게** 부순다
5. **무기가 여전히 콜라이더를 갖지 않는다** (공 구름 불변)
6. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core)

| 테스트 | 검증 |
|---|---|
| `Spk001_EnemyInsideArc_AddsDamage` | B3 |
| `Spk001_EnemyOutsideArc_AddsNothing` | B4 — 이 기능의 핵심 |
| `Spk001_ArcBoundary_IsInclusive` | B3 — 경계에서 튀지 않음 |
| `Spk001_BonusDamage_ScalesWithSpeed` | B5 |
| `Spk001_BonusDamage_ScalesWithFrontality` | B5 |
| `Spk001_Dashing_IncreasesBonusDamage` | B6 |
| `Spk001_Dashing_IncreasesKnockback` | B6 |
| `Spk001_ZeroSpeed_StillHasFloor` | B5 — DMG-001 속도 하한과 일관 |
| `Wpn008_NonContactWeapon_AddsNothing` | B9 |
| `Wpn008_NoWeapon_AddsNothing` | B8 |

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Wpn001_WeaponBuiltFromDefinition` | B1, B2 — 정의의 형상·마운트가 적용됨 |
| `Spk001_SpikeFacingEnemy_DealsMoreThanBareBall` | B3, B5 — **동일 속도 대조** |
| `Spk001_SpikeAwayFromEnemy_DealsBaseDamageOnly` | B4 |
| `Spk001_BonusShares_HitCooldown` | B7 — 추가 피해가 별도 히트를 만들지 않음 |
| `Wpn008_ProjectileWeapon_AddsNoContactDamage` | B9 |
| `Spk001_AttachedWeapon_HasNoCollider` | B11 |
| `Wpn005_Draw_PicksSlotCountFromPool` | B13 — 종류가 슬롯보다 많을 때 무중복 추첨 |

## Manual play checks

- **부착 위치가 의미 있게 느껴지는가** (HYP-005 — 이번 기능의 핵심)
- 스파이크 쪽으로 박는 것과 반대로 박는 것의 **차이가 체감되는가**
- 공이 구르면서 스파이크가 도는 게 전투에 영향을 주는가, 아니면 무작위로 느껴지는가
- 원뿔 반각 60°가 적절한가 — 너무 좁아 안 맞는가, 너무 넓어 방향이 무의미한가
- 대시 + 스파이크 정면 충돌이 **결정타처럼 느껴지는가**
- 스파크·소리 없이도 "더 세게 박았다"가 읽히는가 (연출은 P08)

## Open questions

- **추가 피해에도 히트스톱이 걸려야 하는가** — 지금 히트스톱은 총 피해 기준(DMG-005)이라 추가 피해가 임계를 넘기면 자연히 걸립니다. 별도 규칙은 두지 않되, 플레이테스트에서 "너무 자주 걸린다"가 재발하면 재검토
- 원뿔 반각을 무기별 수치로 둘지, 전역 상수로 둘지 — 지금은 **정의별 수치**로 둡니다 (F12 궤도 도끼는 다른 각이 어울릴 수 있음)
