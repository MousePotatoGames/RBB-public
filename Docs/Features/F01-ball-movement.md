# Feature: F01 Ball Movement (코어볼 이동)

## Status

Verified (automated) — EditMode 10/10, PlayMode 4/4, 런타임 검증 통과 ([검증 보고서](../Reports/2026-07-30_F01-ball-movement_VERIFICATION.md)). Manual play checks는 사용자 판단 대기.

## Purpose

럼블볼의 토대인 "관성을 가진 공 조작"을 만든다. 이후 모든 기능(대시, 충돌 전투, 무기)이 이 이동 위에 쌓이며, 기획서 25장이 1순위 리스크로 꼽은 "공 조작이 미끄럽고 답답함"을 여기서 해결한다.

## Player's perspective

WASD를 누르면 공이 카메라 기준 방향으로 굴러가기 시작한다. 즉시 최고 속도가 되지 않고 미끄러지듯 가속하며, 방향을 꺾으면 관성 때문에 곡선을 그린다. 입력을 놓으면 스르르 굴러가다 멈춘다. 경사와 램프를 오르내려도 흐름이 끊기지 않는다.

## Referenced rules

- MOVE-001 카메라 기준 이동 (관성 유지)
- MOVE-004 경사 접지 보정
- MOVE-005 초기 튜닝 수치 (TEMPORARY 범위)

## In scope

- `Game.Core`: 순수 C# 이동 로직 (`BallMovementLogic`) + 최소 수학 구조체
- `Game.Gameplay`: `BallMotor` (Rigidbody 어댑터), `PlayerInputReader` (Input System → 모터), `BallMovementConfig` (ScriptableObject, TEMPORARY 수치 보관)
- FirstPlayable 씬: Player_Placeholder를 동작하는 Player 볼로 교체, 검증용 램프 1개 추가 (그레이박스 유지)
- 아레나 경계 벽 4면 (`Arena_Bounds`) — 검증 중 낙사 발견으로 추가 (기획서 10.2 "낙사 없음" 이행)
- EditMode/PlayMode 테스트

## Out of scope

- 점프·대시 (F03), 공중 조향 감쇠 MOVE-002 (F03), 고속 조향 감쇠 MOVE-003 (F03)
- 카메라 컨트롤러 (F02) — F01은 씬의 고정 Main Camera transform을 기준축으로 사용
- 속도 단계·VFX (F04), 넉백 중 입력 차단 (MOVE-001 Exception — 전투 F06에서)
- VisualBall 시각 회전 보정 (필요해지면 F04에서)

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 이동 입력은 카메라 yaw 기준 평면 벡터로 변환되어 가속 힘으로 적용된다 | MOVE-001 |
| B2 | 카메라가 어느 방향을 보든 W는 항상 "카메라가 보는 쪽"으로 간다 | MOVE-001 |
| B3 | 가속은 유한하다 — 정지에서 최대속도까지 설정 시간(≈1.2초)이 걸린다 | MOVE-005 |
| B4 | 수평 속도가 최대속도에 도달하면 이동 입력은 그 이상 가속시키지 않는다 | MOVE-005 |
| B5 | 입력이 없으면 인공 제동을 가하지 않는다 — 물리 감속(마찰·댐핑)만으로 서서히 멈춘다 (관성 유지) | MOVE-001 |
| B6 | 접지 중 경사면에서는 이동 방향을 경사 평면에 투영하고 접지 보정력을 더해 과도한 감속을 막는다 | MOVE-004 |
| B7 | 입력 벡터 크기가 1을 넘으면 정규화한다 (대각선 이동이 더 빠르면 안 됨) | MOVE-001 |

## Architecture (humble object)

```text
Game.Core (엔진 참조 없음)
├─ Float3                  # 최소 수학 구조체 (x,y,z + dot/cross/normalize/projectOnPlane)
├─ MoveConfig              # maxSpeed, acceleration, slopeAssist … 순수 데이터
└─ BallMovementLogic       # CameraRelativeDirection / ComputeVelocityChange / ProjectOnSlope

Game.Gameplay
├─ BallMovementConfig      # ScriptableObject — TEMPORARY 수치의 단일 보관처
├─ BallMotor               # Rigidbody 어댑터. FixedUpdate에서 Core 호출 → AddForce(VelocityChange)
│                          #   SetMoveInput(Vector2) 공개 — 테스트가 입력을 직접 주입
└─ PlayerInputReader       # Input System "Player/Move" 폴링 → BallMotor.SetMoveInput
```

## Initial tuning values (전부 `BallMovementConfig` 에셋에 보관)

| Item | Value | Status |
|---|---:|---|
| 기본 최대속도 | 12 m/s | TEMPORARY (MOVE-005 범위 10~13) |
| 0→최대속도 도달 시간 | 1.2 s | TEMPORARY (범위 1.0~1.5) |
| 경사 보정력 계수 | 1.0 | TEMPORARY |
| 접지 판정 거리 (SphereCast) | 0.6 m | TEMPORARY |
| Rigidbody mass / linearDamping / angularDamping | 1 / 0.05 / 1.5 | TEMPORARY |
| 공 반지름 | 0.5 m (Unity Sphere 기본) | TEMPORARY |

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과
3. PlayMode 테스트 전체 통과
4. FirstPlayable 씬 플레이: WASD로 공이 굴러가고, 램프를 올라가며, 입력을 놓으면 관성으로 미끄러지다 멈춘다
5. Manual play checks가 별도 목록으로 보고됨 (통과 주장 없이)

## EditMode tests (Game.Tests.EditMode — Core 로직 대상)

| 테스트 | 검증 규칙 |
|---|---|
| `Move001_ForwardInput_MatchesCameraForwardOnPlane` | B1, B2 — 카메라 forward의 평면 투영과 일치 |
| `Move001_RotatedCamera_RemapsInputDirection` | B2 — 카메라 90° 회전 시 W가 새 방향으로 |
| `Move001_DiagonalInput_IsNormalized` | B7 |
| `Move001_NoInput_ProducesZeroVelocityChange` | B5 — 로직이 제동력을 만들지 않음 |
| `Move005_ReachesMaxSpeed_WithinConfiguredTime` | B3 — dt 스텝 시뮬레이션으로 1.2초 ±20% (관계 검증, 절대값 아님) |
| `Move005_VelocityChange_NeverPushesBeyondMaxSpeed` | B4 |
| `Move004_SlopeProjection_KeepsMagnitude_AndPerpendicularToNormal` | B6 |
| `Move004_SlopeAssist_ZeroOnFlatGround` | B6 — 평지에서 보정력 0 |

## PlayMode tests (Game.Tests.PlayMode — 최소 GameObject 구성, 씬 로드 없음)

| 테스트 | 검증 규칙 |
|---|---|
| `Move001_BallAccelerates_WhenInputHeld` | B1 — 주입 입력 0.5초 후 수평 속도 > 0 |
| `Move001_MovesAlongCameraDirection` | B2 — 회전시킨 더미 카메라 기준 방향 오차 < 15° |
| `Move005_PlanarSpeed_StaysUnderMaxSpeed` | B4 — 3초 가속 후 수평 속도 ≤ max × 1.05 |
| `Move001_KeepsRolling_AfterInputReleased` | B5 — 입력 해제 직후 속도가 0으로 급락하지 않음 |

## Manual play checks (사람만 판단 가능)

- 조작이 "손에 붙는" 느낌인가 — 미끄럽지도 답답하지도 않은가 (HYP-001)
- 관성이 리스크/숙련 요소로 느껴지는가, 조작 불능감으로 느껴지는가
- 램프·경사에서 흐름이 끊기는 순간이 있는가
- 에디터에서 카메라를 수동 회전한 뒤에도 이동 방향에 위화감이 없는가

## Open questions

- 없음 (F02 카메라, F03 대시에서 조향 감쇠 재검토)
