# Verification Report — F03 Jump & Dash

- 날짜: 2026-08-01
- 검증 방식: Unity MCP 자동 검증 (`Unity_RunCommand` + `Unity_GetConsoleLogs` + `TestRunAutomation`)

## Scope

[Docs/Features/F03-jump-dash.md](../Features/F03-jump-dash.md) — JUMP-001 / DASH-001 / MOVE-002 / MOVE-003.
검증 대상: `JumpLogic`·`DashLogic`·조향 감쇠(Core), `BallMotor`의 점프·대시 적용, `PlayerInputReader`의 Jump/Sprint 배선, FirstPlayable 씬의 `Jump_Obstacle`.

## Compilation

- `VERIFIED` — `scriptCompilationFailed = False`, `Game.Core.JumpLogic` / `Game.Core.DashLogic` 타입 로드 확인
- 새 에러 0건, 새 경고 0건 (콘솔의 경고 1건은 무관한 AI Assistant 계정 API 경고)

## EditMode Tests

- `VERIFIED` — **27 / 27 통과, 실패 0, 스킵 0** (F01 10 회귀 + F03 신규 17)
- F03: Jump001 × 6, Dash001 × 5, Move002 × 1, Move003 × 2, Move005 회귀 × 1 등

## PlayMode Tests

- `VERIFIED` — **13 / 13 통과, 실패 0** (F01 4 + F02 5 회귀 + F03 신규 4)
- F03: `Jump001_BallLeavesGround_WhenJumpPressed`, `Jump001_NoDoubleJump_InAir`, `Dash001_PlanarSpeed_JumpsAfterDash`, `Dash001_SecondDash_BlockedByCooldown`

## Runtime Verification (FirstPlayable.unity, 실제 Play Mode)

- `VERIFIED` — 씬에 `Jump_Obstacle` 존재, Player에 Config 연결(jumpAirTime 0.65 → jumpSpeed 3.188, dashSpeed 8, cooldown 1.8, airControl 0.35, steeringAtMaxSpeed 0.4)
- `VERIFIED` — **점프 발동**: y 0.50 → 0.965, `IsGrounded=False` 관측 (이론 정점 0.52m와 부합)
- `VERIFIED` — **F01 가속 회귀**: 개활지에서 평면 속도 11.179 m/s 도달 (max 12)
- `VERIFIED` — **대시 발동**: `DashCooldownRemaining` 0 → 1.8로 전환 후 시간에 따라 감소(1.52 → 0.42 → 0.24)
- `VERIFIED` — **대시 쿨다운 차단(B6)**: 쿨다운 중 두 번째 대시 요청 후에도 타이머가 1.8로 재설정되지 않고 계속 감소 → 요청이 거부됨
- `UNVERIFIED` — 대시의 속도 증가폭(런타임): 측정 시점마다 공이 아레나 벽(z=±30)에 접촉해 속도가 소실됨. **PlayMode 테스트로는 통과**했으므로 로직은 검증됨, 씬 내 수치 재현만 미확보
- `UNVERIFIED` — 이중 점프 차단(B3)의 씬 내 재현: 호출 간격 동안 공이 이미 착지해 공중 상태를 포착하지 못함. PlayMode 테스트로는 통과
- `VERIFIED` — 플레이 세션 전체 새 콘솔 에러 0건

### 검증 인프라 문제 발견 (F02 보고서 정정 포함)

**증상**: 플레이 모드에서 `Time.frameCount`가 두 번의 샘플에서 13139로 동일 — 프레임이 전혀 진행되지 않고 있었다.
**원인**: Unity 에디터 창이 백그라운드일 때 플레이 모드가 정지한다 (Run In Background 비활성). 즉 "플레이 모드 진입 후 값 읽기" 방식은 에디터가 포그라운드가 아니면 **정지된 첫 프레임의 값**을 읽는다.
**대응(이번 검증)**: 런타임에 `Application.runInBackground = true`를 설정해 프레임을 진행시킨 뒤 측정. 이후 `Time.timeScale`을 0.05~0.3으로 낮춰 관측 창을 확보.

> **F02 보고서 정정**: [2026-07-30_F02-camera_VERIFICATION.md](2026-07-30_F02-camera_VERIFICATION.md)의 런타임 항목 중 "공이 스폰에서 벽 부근까지 이동한 뒤에도 카메라 거리 11 유지"는 근거가 불충분했다. 당시 측정된 속도는 0.01 m/s로 사실상 정지 상태였고, 프레임이 진행되지 않았을 가능성이 높다. 카메라 추적(B3)은 **PlayMode 테스트 `Cam002_Camera_FollowsMovingPlayer`로는 통과**하므로 기능 판정은 유지되나, 해당 런타임 문장은 `VERIFIED`가 아니라 `UNVERIFIED`로 낮춰 읽어야 한다. 거리 11.0·뷰포트 0.42 측정값 자체는 정지 프레임에서도 유효하다.

## 증빙 캡처

- [2026-08-01_F03_playmode_focus.png](../Media/2026-08-01_F03_playmode_focus.png) — 검증 중 에디터 전체

## Manual Verification Required (사람 판단)

- `MANUAL_REQUIRED` 점프가 "관용적"으로 느껴지는가 (가장자리·착지 직전 입력이 먹는 느낌 — 코요테 0.1s + 버퍼 0.12s)
- `MANUAL_REQUIRED` 점프 높이(약 0.52m)가 시각적으로 만족스러운가 — 체공 0.65s가 너무 낮게 느껴지면 `jumpAirTime` 상향
- `MANUAL_REQUIRED` 대시 쿨다운 1.8s가 답답하지 않은가 (회피·공격 준비 양쪽으로 쓸 만한가)
- `MANUAL_REQUIRED` 공중 조향 0.35가 무력감이 아니라 "공중이라 그렇다"로 납득되는가
- `MANUAL_REQUIRED` 고속 선회 감쇠(0.4)가 관성의 재미인가 조작 불능인가 (**HYP-001 핵심**)
- `MANUAL_REQUIRED` 점프 후 착지 시 카메라가 튀지 않는가 (F02 연동)

## Remaining Risks

- 점프 높이가 기획서 체공시간 범위(0.55~0.8s)를 따르면 0.37~0.78m로 낮은 편 — 장애물 설계와 함께 플레이테스트에서 재조정 필요
- 벽 반발 계수 미조정 — 대시로 벽에 부딪히면 속도가 급감. 아레나 아트 단계(P07)에서 물리 머티리얼 검토
- **검증 인프라**: 앞으로 런타임 검증 시 `Application.runInBackground`를 켜거나 에디터를 포그라운드로 두어야 한다. Player Settings의 Run In Background를 켜는 것을 권장 (Web 빌드의 포커스 상실 대응, 기획서 21.4와도 부합) — 프로젝트 설정 변경이라 사용자 판단으로 남김
- 대시 중 벽 끼임 정밀 처리는 스펙상 Out of scope

## 판정

자동 검증(컴파일·EditMode·PlayMode) 전체 `VERIFIED`. 런타임은 점프 발동·대시 발동·쿨다운 차단을 씬에서 확인했고, 대시 속도 증가폭과 이중 점프 차단 2건은 씬 내 재현에 실패해 `UNVERIFIED`로 남긴다(해당 규칙은 PlayMode 테스트로 통과).
Feature Spec 상태를 **Verified (automated)** 로 갱신.
