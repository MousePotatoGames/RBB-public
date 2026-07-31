# Feature: F06 Collision Combat (충돌 전투 · 넉백 · 적 사망 · 히트스톱)

## Status

Verified (automated) — EditMode 71/71, PlayMode 34/34, 런타임에서 배선·B19 공중부양 해소·"빠를수록 강하다"·다이나믹 전환 확인 ([검증 보고서](../Reports/2026-08-01_F06-collision-combat_VERIFICATION.md)). 히트스톱의 씬 내 발동만 `UNVERIFIED`(관측 한계 + 밸런스 안건). Manual play checks는 사용자 판단 대기. (스펙 승인: 2026-08-01 — 승인 시 **히트스톱을 P08에서 F06으로 이관**하기로 결정)

## Purpose

이 게임의 존재 이유를 구현한다 — **공격 버튼 없이 이동·충돌이 곧 공격**. F05까지는 드론에게 일방적으로 당하기만 했다. F06이 들어와야 "빠를수록 강하다"(HYP-003)와 대시의 쓸모(플레이테스트 O4에서 보류)를 처음으로 평가할 수 있다.

## Player's perspective

속도를 붙여 드론에 박으면 드론이 튕겨 날아간다. 천천히 굴러가 닿으면 밀리기만 하고 잘 안 죽는다. 대시로 정면에서 들이받으면 확실하게 부순다. 점프해서 위로 떨어져도 박살난다. 느리게 다니면 오히려 내가 깎인다.

## Referenced rules

- DMG-001 충돌 피해 공식 (속도 × 정면도 × 대시 × 낙하 × 패시브, 정면도 하한 클램프)
- DMG-002 중복 히트 방지 (적 개체별 피해 쿨다운)
- DMG-003 대형 적 넉백 감쇠
- DMG-004 낙하 충돌 보너스
- DMG-005 히트스톱 (임계·상한·동시 누적 금지·**연속 발동 최소 간격**) — 플레이테스트 이후 GAME_RULES에 신설
- ENM-004 적 공통 (사망 시 즉시 삭제하지 않고 짧은 물리 날아가기)
- SPD-001 속도 단계 (대시 활성 창 = 럼블, 대시 배율 판정에 사용)
- Decision 0001 적 물리 하이브리드 (**넉백·사망 시 다이나믹 전환** — 이번에 구현)

## In scope

- `Game.Core`: `DamageLogic`(DMG-001 공식), `HitCooldownLogic`(DMG-002), `KnockbackLogic`(방향·세기 + DMG-003 감쇠), **`HitStopLogic`(지속시간 산출·중첩 방지)**
- `Game.Gameplay`: `EnemyHealth`(적 HP·사망), `PlayerDamageDealer`(플레이어 접촉 시 피해 계산 + 타격 이벤트 발행), `ScrapDrone.TakeKnockback` 구현(키네마틱 ↔ 다이나믹 전환), 사망 후 풀 반환
- **`Game.Presentation`: `HitStopController`** — 타격 이벤트 구독 → `Time.timeScale` 일시 정지 (의존 방향 준수: Presentation → Gameplay)
- `DroneConfig` 확장: 적 HP, 기본 충돌 피해, 각종 배율, 넉백 세기·저항, 시체 유지 시간, 히트스톱 수치
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- 경험치 오브 드랍 (XP-001 → F12)
- 피격 플래시·파편·화면 셰이크·디졸브 (P08) — F06은 물리 날아가기 + 히트스톱까지만
  - **P08로 이월된 과제 (2026-08-01 플레이테스트)**: 히트스톱을 타격 피드백의 *주* 채널로 쓰는 것이 구조적으로 맞지 않다. 접촉이 끊임없이 일어나는 게임에서 시간 정지는 계속 걸리면 저속 재생처럼 보인다. P08에서 **쉐이크 = 일상 타격 / 히트스톱 = 결정타**로 역할을 나누고, 그때 임계값(5)과 최소 간격(0.5s)을 재평가한다 ([기록](../Playtests/2026-08-01_F05-F06.md))
- 피해 숫자 표시·콤보 UI (F13)
- 무기(스파이크·캐논·테슬라)의 추가 피해 (F09~F11) — F06은 **본체 충돌 피해만**
- 램 비틀·아이언 브루트 (P01/P02) — DMG-003 감쇠는 **설정값으로 구현**하되 실제 대형 적은 없음
- 패시브 피해 배율 (F12) — 공식에는 인자로 넣되 값은 1 고정

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 피해 = 기본 피해 × 속도 배율 × 정면도 × 대시 배율 × 낙하 배율 × 패시브 배율 | DMG-001 |
| B2 | 속도 배율에는 하한이 있어 저속 접촉도 0이 되지 않는다 | DMG-001 |
| B3 | 정면도는 이동 방향과 적 방향의 내적이며, 하한으로 클램프되어 음수·0이 되지 않는다 | DMG-001 |
| B4 | 대시 활성 창 동안 충돌하면 대시 배율이 곱해진다 | DMG-001, DASH-001 |
| B5 | 하강 속도가 임계치 이상이면 낙하 배율이 곱해진다 | DMG-004 |
| B6 | 같은 적에게는 쿨다운 안에 두 번 피해를 주지 않는다 (프레임당 중복 금지) | DMG-002 |
| B7 | 쿨다운이 지나면 같은 적에게 다시 피해를 줄 수 있다 | DMG-002 |
| B8 | 피해를 입은 적은 피격 방향으로 넉백된다 | ENM-004, 기획서 11.2 |
| B9 | 넉백 세기는 넉백 저항 계수로 감쇠된다 (대형 적은 밀리되 날아가지 않음) | DMG-003 |
| B10 | 넉백을 받은 적은 **다이나믹으로 전환**되고, 회복 시간 후 살아 있으면 키네마틱 추적으로 복귀한다 | Decision 0001 |
| B11 | HP가 0이 된 적은 즉시 사라지지 않고 다이나믹 상태로 날아간 뒤 시체 유지 시간이 지나면 풀로 반환된다 | ENM-004 |
| B12 | 죽은 적은 더 이상 플레이어에게 접촉 피해를 주지 않으며, 추가 피해도 받지 않는다 | ENM-004, 중복 방지 |
| B13 | 적 사망은 한 번만 처리된다 (중복 사망·중복 반환 없음) | 기획서 21.2 |
| B14 | 풀로 반환된 적은 재사용 시 HP·상태·키네마틱이 초기화된다 | B11, 풀링 |
| B15 | 일정 피해 이상의 타격에만 히트스톱이 걸린다 (약한 접촉마다 화면이 멈추지 않는다) | 기획서 11.2 |
| B16 | 히트스톱 지속시간은 상한이 있고, 동시에 여러 타격이 나도 **누적되지 않는다** (더 긴 쪽만 유지) | 기획서 11.2, 성능·가독성 |
| B17 | 히트스톱은 실시간(unscaled) 기준으로 끝나며, 종료 시 `Time.timeScale`이 반드시 1로 복구된다 | 안전성 (기획서 21.3 "시간 초기화") |
| B18 | 컴포넌트가 비활성화·파괴되어도 `Time.timeScale`은 1로 복구된다 | 안전성 |
| B19 | 넉백으로 떠오른 적은 키네마틱 복귀 시 **추적 높이로 되돌아온다** (공중에 뜬 채 추적하지 않는다) | 검증 중 발견한 버그 수정 — 키네마틱 이동이 Y를 보존하는 특성 |
| B20 | 직전 정지로부터 **최소 간격**이 지나기 전에는 히트스톱이 다시 걸리지 않는다 | **DMG-005** — 플레이테스트 EXP-004에서 확인. B16(동시 누적 방지)만으로는 **순차 연쇄**를 막지 못한다 |

## Architecture

```text
Game.Core (엔진 참조 없음)
├─ DamageLogic       # CollisionDamage(speed, maxSpeed, moveDir, toEnemyDir, isDashing, fallSpeed, passive, config)
│                    #   + SpeedMultiplier / Frontality (하한 클램프) / FallMultiplier
├─ HitCooldownLogic  # CanHit(lastHitTime, now, cooldown) — 적 개체별 (DMG-002)
├─ KnockbackLogic    # Impulse(fromPlayerToEnemy, damage, baseForce, resistance) (B8, B9)
└─ HitStopLogic      # DurationFor(damage, threshold, min, max) / Merge(remaining, incoming) (B15, B16)

Game.Gameplay
├─ CombatConfig 확장 (DroneConfig) # 적 HP, 기본 피해, 배율, 넉백, 시체 시간 — 전부 TEMPORARY
├─ EnemyHealth        # 적 HP·사망 1회 처리. Died 이벤트 → 스포너가 풀 반환 예약
├─ PlayerDamageDealer # 플레이어에 부착. 적 접촉 시 Core 공식으로 피해 계산 후 EnemyHealth에 전달
└─ ScrapDrone         # TakeKnockback 구현: 다이나믹 전환 → 회복 후 키네마틱 복귀 / 사망 시 유지
```

**피해 방향 정리**: 드론 → 플레이어(F05, `ScrapDrone.TryDamage`)와 플레이어 → 드론(F06, `PlayerDamageDealer`)은 서로 독립적으로 동작한다. 한 번의 접촉에서 양쪽이 모두 발생할 수 있다 (기획서 의도: 느리게 부딪히면 손해).

## Initial tuning values (전부 `DroneConfig`)

| Item | Value | Status |
|---|---:|---|
| 드론 최대 HP | 10 | TEMPORARY |
| 기본 충돌 피해 | 10 | TEMPORARY |
| 속도 배율 하한 | 0.25 | TEMPORARY (DMG-001 "저속도 무의미하지 않게") |
| 속도 배율 상한 | 1.5 | TEMPORARY (대시 오버스피드 보상) |
| 정면도 하한 | 0.3 | TEMPORARY (GAME_RULES DMG-001과 동일) |
| 대시 배율 | 1.6 | TEMPORARY |
| 낙하 배율 / 임계 하강 속도 | 1.5 / 6 m/s | TEMPORARY (GAME_RULES DMG-004과 동일) |
| 히트 쿨다운 (적 개체별) | 0.25 s | TEMPORARY |
| 기본 넉백 세기 | **20 m/s** | **확정 (EXP-003)** — 9에서는 "넉백 전혀 모르겠어". 세기가 `피해/드론HP`로 감쇠되어 무리 안 실제 값이 2.25 m/s였다. 20에서 "날라가는건 딱 좋아" ([기록](../Playtests/2026-08-01_F05-F06.md)) |
| 드론 넉백 저항 | 0 (완전히 날아감) | TEMPORARY |
| 넉백 회복 시간 (생존 시) | 0.35 s | TEMPORARY |
| 시체 유지 시간 | 1.0 s | TEMPORARY (기획서 15.3 High 기준) |
| 히트스톱 최소 피해 임계 | 5 | TEMPORARY (약한 접촉은 멈추지 않음) — 12로 올려도 연쇄가 남아 EXP-005에서 되돌림 |
| 히트스톱 최소/최대 지속 | 0.05 / 0.09 s | TEMPORARY (기획서 11.2 범위 0.05~0.1) |
| **히트스톱 연속 발동 최소 간격** | **0.5 s** | **TEMPORARY (DMG-005 / B20)** — EXP-006에서 개선 확인("조금 자연스러운데")했으나 확정하지 않음. P08에서 카메라 쉐이크와 함께 재평가 |
| 히트스톱 기준 피해 (최대 지속에 도달하는 피해량) | 15 | TEMPORARY |

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01·F03·F04·F05 회귀 포함)
3. PlayMode 테스트 전체 통과 (F01~F05 회귀 포함)
4. FirstPlayable 씬 플레이: 고속 충돌로 드론이 날아가며 죽고, 저속 접촉은 잘 안 죽으며, 죽은 드론이 풀로 돌아온다
5. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core)

| 테스트 | 검증 |
|---|---|
| `Dmg001_Damage_ScalesWithSpeed` | B1, B2 — 고속이 저속보다 큼 |
| `Dmg001_LowSpeed_StillDealsMinimumDamage` | B2 — 정지 근처에서도 0이 아님 |
| `Dmg001_Frontality_HeadOnBeatsGlancing` | B3 — 정면이 측면보다 큼 |
| `Dmg001_Frontality_RearContact_ClampedNotNegative` | B3 — 뒤에서 닿아도 음수 아님 |
| `Dmg001_Dashing_MultipliesDamage` | B4 |
| `Dmg004_FallSpeedAboveThreshold_MultipliesDamage` | B5 |
| `Dmg004_FallSpeedBelowThreshold_NoBonus` | B5 |
| `Dmg002_SameEnemy_WithinCooldown_CannotBeHit` | B6 |
| `Dmg002_SameEnemy_AfterCooldown_CanBeHitAgain` | B7 |
| `Enm004_Knockback_PointsAwayFromPlayer` | B8 |
| `Dmg003_Knockback_ReducedByResistance` | B9 |
| `Dmg003_FullResistance_ProducesNoKnockback` | B9 |
| `Dmg001_PassiveMultiplier_ScalesDamage` | B1 (F12 대비 인자 검증) |
| `HitStop_BelowThreshold_ProducesNoFreeze` | B15 |
| `HitStop_Duration_ScalesWithDamage_AndIsCapped` | B15, B16 |
| `HitStop_Merge_TakesLongerNotSum` | B16 — 중첩 시 합산 금지 |

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Dmg001_FastCollision_KillsDrone` | B1, B11 — 고속 충돌 후 드론 사망 |
| `Dmg001_SlowContact_DoesNotKillInstantly` | B1, B2 — 저속 접촉으로는 즉사하지 않음 |
| `Enm004_HitDrone_BecomesDynamicThenRecovers` | B10 — 다이나믹 전환 후 키네마틱 복귀 |
| `Enm004_LoftedDrone_ReturnsToChaseHeight` | B19 — 넉백으로 떠오른 뒤 추적 높이 복귀 |
| `Enm004_DeadDrone_ReturnsToPoolAfterCorpseTime` | B11, B14 — 시체 시간 후 반환·재사용 |
| `Dmg002_SingleCollision_DamagesOnce` | B6 — 한 번의 접촉이 프레임 수만큼 중복 피해를 주지 않음 |
| `Enm004_DeadDrone_StopsDamagingPlayer` | B12 |
| `HitStop_StrongHit_FreezesThenRestoresTimeScale` | B17 — 정지 후 timeScale 1 복구 |
| `HitStop_OnDisable_RestoresTimeScale` | B18 — 비활성화 시에도 복구 |

## Manual play checks

- **"빠를수록 강하다"가 조작만으로 학습되는가** (HYP-003 — 이번 기능의 핵심)
- 대시로 들이받는 게 통쾌한가 — 지난 플레이테스트에서 보류한 O4 재평가
- 저속 접촉의 손해(내 HP만 깎임)가 납득되는가, 억울한가
- 드론이 날아가는 반응이 시원한가, 과한가 (넉백 9 m/s)
- 점프 낙하 공격이 쓸 만한가, 있으나 마나 한가
- 무리를 뚫고 지나가는 "돌파" 감각이 나는가 (기획서 "파괴의 재미")
- 죽은 드론이 1초간 남는 게 자연스러운가, 지저분한가

## Open questions

- 없음 (수치는 전부 TEMPORARY — 플레이테스트로 조정)
