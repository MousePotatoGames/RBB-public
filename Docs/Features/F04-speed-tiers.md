# Feature: F04 Speed Tiers (속도 4단계 + 최소 피드백)

## Status

Verified (automated) — EditMode 35/35, PlayMode 17/17, 런타임에서 단계 상승·대시 럼블·시각 피드백 확인 ([검증 보고서](../Reports/2026-08-01_F04-speed-tiers_VERIFICATION.md)). Manual play checks는 사용자 판단 대기. (스펙 승인: 2026-08-01)

## Purpose

"속도가 곧 공격력이자 위험"이라는 이 게임의 절대 원칙을 **플레이어가 눈으로 읽을 수 있게** 만든다. F06 전투가 속도 배율로 피해를 계산하기 전에, 지금 내가 어느 단계인지 보이지 않으면 그 인과를 학습할 수 없다. HYP-003(속도=공격 학습)의 전제 조건이다.

## Player's perspective

천천히 굴릴 때는 조용하다. 속도가 붙으면 공에 색이 돌고 바닥에 궤적이 남는다. 최고 속도나 대시 순간에는 확연히 달라져서 "지금이 제일 센 상태"라는 게 느껴진다. 속도가 경계에서 오르내려도 화면이 깜빡거리지 않는다.

## Referenced rules

- SPD-001 속도 4단계 (저속/중속/고속/럼블, `PlayerSpeedTierChanged` 이벤트)
- DASH-001 대시 중에는 속도와 무관하게 럼블 단계 (SPD-001 Exception)
- MOVE-005 최대속도 (단계 판정의 기준값)

## In scope

- `Game.Core`: `SpeedTier` 열거, `SpeedTierLogic` (비율→단계 판정 + 히스테리시스), `SpeedTierConfig`
- `Game.Gameplay`: `BallMotor`에 대시 활성 창(dash active window) 노출, `SpeedTierTracker` (단계 계산 + `PlayerSpeedTierChanged` 이벤트 발행)
- `Game.Presentation`: `SpeedTierVisuals` — 단계별 최소 피드백 (코어 Emission 색 + TrailRenderer on/off·색)
- FirstPlayable 씬: 플레이어에 트레일·머티리얼 인스턴스 배선
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- 기획서 5.4의 완성형 VFX (먼지 파티클, 불꽃, 전기 아크, 화면 왜곡) → P08
- FOV 변화·카메라 후퇴 (F02 Out of scope와 동일하게 P08/속도 연동 단계)
- 충돌 피해의 속도 배율 **적용** → F06 (F04는 단계 판정과 표시까지만)
- 오디오(구름음 피치) → P09
- HUD의 속도 표시 → F13

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 수평 속도 / 최대속도 비율로 단계를 판정한다: 저속 0~35%, 중속 35~70%, 고속 70~95%, 럼블 95%+ | SPD-001 |
| B2 | 대시 활성 창 동안에는 속도와 무관하게 럼블 단계다 | SPD-001 Exception, DASH-001 |
| B3 | 단계 경계에서 진동하지 않도록 히스테리시스를 둔다 — 올라갈 때와 내려갈 때의 임계값이 다르다 | SPD-001 (구현 안정성) |
| B4 | 단계가 바뀔 때만 `PlayerSpeedTierChanged`가 발행된다 (매 프레임 발행 금지) | SPD-001, 기획서 14.5 성능 원칙 |
| B5 | 대시로 최대속도를 초과해도 단계는 럼블에서 더 올라가지 않는다 (상한) | SPD-001 |
| B6 | 단계별로 코어 색과 트레일이 구분되게 바뀐다 (저속에는 트레일 없음) | SPD-001 시각 효과 열 |
| B7 | 시각 피드백은 단계 전환 이벤트에만 반응한다 — 매 프레임 머티리얼을 새로 만들지 않는다 | 기획서 14.5 (머티리얼 인스턴스 무한 생성 금지) |

## Architecture

```text
Game.Core (엔진 참조 없음)
├─ SpeedTier            # enum: Low, Mid, High, Rumble
├─ SpeedTierConfig      # 경계 비율 4개 + 히스테리시스 폭
└─ SpeedTierLogic       # Evaluate(speed, maxSpeed, isDashing, currentTier) → SpeedTier

Game.Gameplay
├─ BallMotor            # IsDashActive / DashActiveRemaining 노출 (대시 후 짧은 창)
└─ SpeedTierTracker     # 매 FixedUpdate 판정, 변경 시 event PlayerSpeedTierChanged(SpeedTier)

Game.Presentation
└─ SpeedTierVisuals     # 이벤트 구독 → 코어 Emission 색 + TrailRenderer 설정 (MaterialPropertyBlock 사용)
```

## Initial tuning values

| Item | Value | Status |
|---|---:|---|
| 저속/중속 경계 | 35% | TEMPORARY (SPD-001) |
| 중속/고속 경계 | 70% | TEMPORARY |
| 고속/럼블 경계 | 95% | TEMPORARY |
| 히스테리시스 폭 | 5%p | TEMPORARY |
| 대시 활성 창 | 0.35 s | TEMPORARY |
| 단계별 코어 색 | 흰 → 시안 → 밝은 시안 → 백열 | TEMPORARY (기획서 10.4 팔레트) |
| 트레일 지속 | 저속 0 / 중속 0.25s / 고속 0.4s / 럼블 0.6s | TEMPORARY |

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01·F03 회귀 포함)
3. PlayMode 테스트 전체 통과 (F01~F03 회귀 포함)
4. FirstPlayable 씬 플레이: 가속하면 색·트레일이 단계적으로 변하고, 대시하면 즉시 럼블 표시가 뜬다
5. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core)

| 테스트 | 검증 |
|---|---|
| `Spd001_Ratios_MapToExpectedTiers` | B1 — 0/50/80/100% → Low/Mid/High/Rumble |
| `Spd001_Boundaries_AreInclusiveAsSpecified` | B1 — 경계값 정확성 |
| `Spd001_Dashing_ForcesRumble_RegardlessOfSpeed` | B2 |
| `Spd001_Hysteresis_HoldsTierNearBoundary` | B3 — 경계 ±히스테리시스 내에서 단계 유지 |
| `Spd001_Hysteresis_ReleasesTierBeyondBoundary` | B3 — 폭을 넘으면 내려감 |
| `Spd001_OverMaxSpeed_ClampsToRumble` | B5 |
| `Spd001_ZeroMaxSpeed_DoesNotDivideByZero` | 방어 |

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Spd001_TierRises_AsBallAccelerates` | B1 — 정지→가속 중 Low→…→상위 단계 관측 |
| `Spd001_EventFires_OnlyOnTierChange` | B4 — 이벤트 수 < 프레임 수, 단계 수 이하 |
| `Spd001_Dash_ImmediatelyEntersRumble` | B2 |
| `Spd001_Visuals_UpdateWithoutLeakingMaterials` | B7 — 전환 반복 후 머티리얼 인스턴스 증가 없음 |

## Manual play checks

- 단계 전환이 눈에 "읽히는가" — 색·트레일만으로 지금 상태를 알 수 있는가
- 경계에서 깜빡임이 없는가 (B3 체감)
- 대시할 때 럼블 진입이 통쾌한가, 과한가
- 그레이박스 배경에서 트레일이 지저분하지 않은가 (가독성)

## Open questions

- 없음
