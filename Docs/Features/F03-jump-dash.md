# Feature: F03 Jump & Dash (점프 · 대시 · 조향 감쇠)

## Status

Verified (automated) — EditMode 27/27, PlayMode 13/13, 런타임 점프·대시·쿨다운 확인 ([검증 보고서](../Reports/2026-08-01_F03-jump-dash_VERIFICATION.md)). Manual play checks는 사용자 판단 대기. (스펙 승인: 2026-07-30)

## Purpose

F01의 이동에 "리스크를 감수하는 순간 선택"을 더한다. 대시는 곧 공격 준비(DMG-001의 대시 배율)이자 긴급 회피이고, 점프는 회피 겸 낙하 충돌(DMG-004) 수단이다. 동시에 공중·고속 조향 감쇠를 붙여 관성을 진짜 제약으로 만든다 — F01은 조향이 아직 너무 자유롭다.

## Player's perspective

Space를 누르면 공이 튀어오른다. 착지 직후·가장자리 직전에도 관용적으로 점프가 먹지만, 공중에서는 방향을 크게 못 바꾼다. Shift를 누르면 지금 가는 방향으로 확 튀어나가고, 잠시 다시 쓸 수 없다. 전속력에 가까울수록 꺾기가 무거워져서 진로를 미리 잡아야 한다.

## Referenced rules

- JUMP-001 점프 (코요테 타임, 더블 점프 없음)
- DASH-001 대시 (쿨다운, 순간이동 아님 — 현재 입력 방향으로 속도 추가)
- MOVE-002 지상/공중 조향 구분
- MOVE-003 고속 조향 감쇠
- MOVE-005 초기 튜닝 수치 (TEMPORARY 범위)

## In scope

- `Game.Core`: `JumpLogic`, `DashLogic` (순수 상태 + 판정), `MoveConfig` 확장(공중 조향·고속 조향·점프·대시 계수), `BallMovementLogic`의 조향 감쇠 적용
- `Game.Gameplay`: `BallMotor`에 점프·대시 적용, `PlayerInputReader`에 `Player/Jump`·`Player/Sprint` 폴링, `BallMovementConfig`에 신규 TEMPORARY 필드
- FirstPlayable 씬: 점프 검증용 낮은 장애물 1개 추가 (그레이박스)
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- 속도 단계 판정·VFX·`PlayerSpeedTierChanged` 이벤트 (F04) — 대시가 럼블 단계에 진입한다는 SPD-001 연동은 F04에서 배선
- 대시 FOV 확대·잔상 등 연출 (F04/P08)
- 낙하 충돌 피해 DMG-004 (F06 전투) — F03은 점프 자체만
- 대시 무적·관통 (설계에 없음), 대시 중 벽 끼임 정밀 처리 (플레이테스트 후 필요 시)
- 대시 쿨다운 UI 표시 (F13 HUD)

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 접지 상태에서 점프 입력 시 위쪽 속도를 부여한다 (설정 체공 시간에 맞는 초기 속도) | JUMP-001 |
| B2 | 접지가 끊긴 뒤 코요테 타임(0.1s) 안에는 점프가 허용된다 | JUMP-001 |
| B3 | 공중에서는 재점프 불가 — 한 번의 점프는 착지 후에만 다시 쓸 수 있다 | JUMP-001 |
| B4 | 점프 입력을 착지 직전에 눌러도 접지 순간 발동한다 (입력 버퍼 0.12s) | JUMP-001 (관용성 — 스펙에서 추가) |
| B5 | 대시는 순간이동이 아니라 현재 입력 방향(입력 없으면 현재 진행 방향)으로 수평 속도를 더한다 | DASH-001 |
| B6 | 대시는 쿨다운 중 재발동되지 않는다 | DASH-001 |
| B7 | 대시로 최대속도를 초과한 상태는 이동 입력에 의해 제동되지 않는다 | DASH-001 (F01 Move005 계약 유지) |
| B8 | 공중에서는 이동 가속이 지상보다 낮다 (공중 조향 계수) | MOVE-002 |
| B9 | 현재 속도가 최대속도에 가까울수록 진행 방향을 꺾는 성분이 감쇠된다 | MOVE-003 |
| B10 | 조향 감쇠는 가속 성분(진행 방향과 같은 방향)에는 적용되지 않는다 | MOVE-003 (감쇠가 가속을 죽이면 안 됨) |

## Architecture

```text
Game.Core (엔진 참조 없음)
├─ MoveConfig                        # 확장: AirControl, SteeringAtMaxSpeed (선택 인자, 기본값 = 효과 없음)
├─ BallMovementLogic                 # ComputeVelocityChange에 isGrounded 인자 추가(기본값 true — F01 호출부 호환)
│                                    #   + ApplySteeringDamping (B9, B10)
├─ JumpLogic + JumpState + JumpConfig # 코요테 타임·입력 버퍼·1회 제한 (B1~B4)
└─ DashLogic + DashState + DashConfig # 쿨다운·대시 속도 벡터 (B5, B6)

# 구현 시 조정: 점프·대시 수치는 MoveConfig에 합치지 않고 JumpConfig/DashConfig로 분리했다.
# JumpLogic이 코요테·버퍼 시간을 함께 필요로 해 이동 수치와 섞이면 응집도가 떨어지기 때문.

Game.Gameplay
├─ BallMovementConfig    # 신규 TEMPORARY 필드 + 체공시간→점프속도 변환
├─ BallMotor             # SetJumpInput / SetDashInput, FixedUpdate에서 Core 상태 갱신 후 적용
└─ PlayerInputReader     # Player/Jump(Space), Player/Sprint(Shift) 폴링
```

기존 `MoveConfig` 생성자는 선택 인자로 확장해 F01 테스트·호출부가 그대로 컴파일되게 한다.

## Initial tuning values (전부 `BallMovementConfig` 에셋)

| Item | Value | Status |
|---|---:|---|
| 체공 시간 (점프 속도 산출 기준) | 0.65 s | TEMPORARY (MOVE-005 범위 0.55~0.8) |
| 코요테 타임 | 0.10 s | TEMPORARY (범위 0.08~0.12) |
| 점프 입력 버퍼 | 0.12 s | TEMPORARY |
| 대시 추가 속도 | 8 m/s | TEMPORARY (범위 7~10) |
| 대시 쿨다운 | 1.8 s | TEMPORARY (범위 1.5~2.2) |
| 공중 조향 계수 | 0.35 (지상 대비) | TEMPORARY |
| 최대속도에서의 조향 계수 | 0.4 | TEMPORARY |

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01 회귀 포함)
3. PlayMode 테스트 전체 통과 (F01·F02 회귀 포함)
4. FirstPlayable 씬 플레이: Space로 장애물을 넘고, Shift로 튀어나가며 쿨다운이 걸리고, 전속력 선회가 저속보다 무겁다
5. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core 로직)

| 테스트 | 검증 |
|---|---|
| `Jump001_Grounded_AllowsJump` | B1 |
| `Jump001_CoyoteTime_AllowsJumpShortlyAfterLeavingGround` | B2 |
| `Jump001_AfterCoyoteWindow_DeniesJump` | B2 |
| `Jump001_InAir_DeniesSecondJump` | B3 |
| `Jump001_BufferedInput_FiresOnLanding` | B4 |
| `Jump001_JumpSpeed_ScalesWithAirTime` | B1 (관계 검증: 체공시간↑ → 초기속도↑) |
| `Dash001_Ready_ProducesVelocityAlongInput` | B5 |
| `Dash001_NoInput_UsesCurrentHeading` | B5 |
| `Dash001_DuringCooldown_Denied` | B6 |
| `Dash001_AfterCooldown_ReadyAgain` | B6 |
| `Move002_AirInput_AcceleratesLessThanGround` | B8 |
| `Move003_TurnComponent_DampedAtHighSpeed` | B9 |
| `Move003_ForwardAcceleration_NotDampedAtHighSpeed` | B10 |
| `Move005_DashOverspeed_NotBrakedByInput` | B7 (F01 계약 회귀) |

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Jump001_BallLeavesGround_WhenJumpPressed` | B1 — y 상승 관측 |
| `Jump001_NoDoubleJump_InAir` | B3 — 공중 재입력 시 상승 속도 증가 없음 |
| `Dash001_PlanarSpeed_JumpsAfterDash` | B5 — 대시 직후 수평 속도 급증 |
| `Dash001_SecondDash_BlockedByCooldown` | B6 |

## Manual play checks (사람만 판단 가능)

- 점프가 "관용적"으로 느껴지는가 (가장자리·착지 직전 입력이 먹는 느낌)
- 대시가 회피 수단과 공격 준비 양쪽으로 쓸 만한가 — 쿨다운 1.8s가 답답하지 않은가
- 공중 조향 0.35가 무력감이 아니라 "공중이라 그렇다"로 납득되는가
- 고속 선회 감쇠가 관성의 재미인가, 조작 불능인가 (HYP-001 핵심)
- 점프 후 착지 시 카메라가 튀지 않는가 (F02 연동)

## Open questions

- 없음
