# Feature: F02 Camera (3인칭 쿼터뷰 카메라)

## Status

Verified (automated) — EditMode 10/10 회귀, PlayMode 9/9, 런타임 검증 통과 ([검증 보고서](../Reports/2026-07-30_F02-camera_VERIFICATION.md)). Manual play checks는 사용자 판단 대기. (스펙 승인: 2026-07-30)

> 구현 중 확정된 사실: CM3 RotationComposer의 ScreenPosition은 +y가 화면 아래 방향. 초기 -0.08은 플레이어를 상단(0.58)에 놓아 +0.08로 수정, 실측 0.42 확인 (스크린샷: [Docs/Media/2026-07-30_F02_quarterview_framing.png](../Media/2026-07-30_F02_quarterview_framing.png))

## Purpose

"카메라 기준 이동"(MOVE-001)을 실전으로 완성한다. F01은 정적 카메라였다 — 마우스 궤도 회전이 붙어야 플레이어가 진행 방향을 스스로 결정하고, 고속에서도 떨리지 않는 시야가 이후 모든 전투 가독성의 토대가 된다.

## Player's perspective

마우스를 움직이면 카메라가 공 주위를 부드럽게 돈다. 공은 항상 화면 하단 40~45%에 있어 전방이 넓게 보인다. W는 언제나 "카메라가 보는 쪽"이다. 전속력으로 달려도 화면이 덜덜거리지 않는다.

## Referenced rules

- CAM-001 마우스 궤도 회전 (고정 피치 쿼터뷰)
- CAM-002 프레이밍 (하단 40~45%, 무기 가독 거리)
- CAM-003 물리 보간 분리 (고속 떨림 없음)
- MOVE-001 카메라 기준 이동 (F01 연동 — 회전한 카메라가 즉시 이동 기준축이 되는지)

## In scope

- **Cinemachine 3.1.7 기반** (부트스트랩에서 사용자 결정으로 설치):
  - `CinemachineCamera` + `OrbitalFollow`(yaw 궤도, 고정 피치) + `RotationComposer`(하단 프레이밍) + `CinemachineInputAxisController`(Input System `Player/Look` 바인딩)
  - Main Camera에 `CinemachineBrain` (LateUpdate 갱신 — CAM-003)
- `Game.Presentation`: `CursorLockController` (클릭으로 커서 잠금, ESC로 해제 — 마우스 궤도 조작의 전제)
- FirstPlayable 씬 통합: 카메라 리그 배치, BallMotor의 cameraTransform이 Brain 구동 Main Camera를 그대로 사용하는지 확인
- PlayMode 테스트

## Out of scope

- 속도·적 수 기반 카메라 거리 증가 (F04 — 속도 단계 연동)
- 대시 FOV 확대 (F03), 화면 셰이크·임펄스 (P08)
- 마우스 Y 피치 조작 — 쿼터뷰 고정 피치 (기획서 11.1의 "가까운 3인칭 쿼터뷰" 해석, 필요 시 플레이테스트 후 재검토)
- 카메라 충돌(벽 파고들기) 처리 — 그레이박스 개활지라 후순위, 기둥·잔해 추가(P07) 시 Deoccluder 도입

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 마우스 X 이동이 카메라를 플레이어 중심 yaw 궤도로 회전시킨다 | CAM-001 |
| B2 | 피치는 고정(쿼터뷰) — 마우스 Y는 카메라를 기울이지 않는다 | CAM-001 |
| B3 | 카메라는 이동하는 플레이어를 부드럽게 추적한다 (하드 락 아님, 댐핑) | CAM-002 |
| B4 | 일반 상태에서 플레이어가 화면 세로 하단 40~45% 부근에 위치한다 | CAM-002 |
| B5 | 카메라 회전 후 W 입력은 새 카메라 전방을 따라간다 (F01 연동) | MOVE-001 |
| B6 | 카메라 갱신은 물리 보간과 분리되어 고속에서 떨림이 없다 (Brain LateUpdate + Rigidbody Interpolate) | CAM-003 |
| B7 | 클릭 시 커서가 잠기고 ESC로 해제된다 (에디터·데스크톱 기준, Web 첫 클릭 안내는 P11) | 조작 전제 |

## Architecture

```text
Game.Presentation
└─ CursorLockController      # 유일한 신규 코드. 클릭 → Cursor.lockState = Locked, ESC → None

씬 구성 (코드 아님 — Cinemachine 컴포넌트 설정)
├─ Main Camera + CinemachineBrain (LateUpdate)
└─ CM_GameplayCamera (CinemachineCamera)
   ├─ OrbitalFollow: Follow = Player, 반경/고정 피치, 수평축만 입력
   ├─ RotationComposer: 화면 Y 오프셋으로 하단 프레이밍
   └─ InputAxisController: Horizontal ← "Player/Look" X
```

순수 Core 로직 없음 — 이 기능의 행동은 전부 Cinemachine 컴포넌트 구성이라 EditMode로 검증할 엔진 무관 규칙이 없다. 커버리지는 PlayMode + Manual로 충족한다 (커버리지 계약 §5의 선택지 2·3).

## Initial tuning values (씬의 Cinemachine 컴포넌트에 직렬화 — 튜닝 표면)

| Item | Value | Status |
|---|---:|---|
| 궤도 반경 (거리) | 11 m | TEMPORARY |
| 고정 피치 | 38° | TEMPORARY |
| 프레이밍 화면 Y (Composer ScreenPosition.y) | +0.08 → 실측 viewport y 0.42 (CM3는 +y가 화면 아래) | TEMPORARY |
| Look 감도 (InputAxisController gain) | 기본값에서 시작 | TEMPORARY |
| 추적 댐핑 | 위치 0.5 / 회전 0.3 | TEMPORARY |
| Brain 업데이트 | LateUpdate | CONFIRMED (CAM-003) |

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. PlayMode 테스트 전체 통과
3. FirstPlayable 씬 플레이: 마우스로 카메라를 돌리며 WASD로 어느 방향이든 주행 가능, 공이 화면 하단에 유지
4. Manual play checks가 별도 목록으로 보고됨

## EditMode tests

- 없음 — 근거: 신규 엔진 무관 로직이 없음 (Architecture 참조). B1~B7은 아래 PlayMode/Manual에 매핑.

## PlayMode tests (Game.Tests.PlayMode — 런타임 구성, 씬 로드 없음)

| 테스트 | 검증 규칙 |
|---|---|
| `Cam001_HorizontalAxis_OrbitsCameraAroundPlayer` | B1 — OrbitalFollow 수평축 값 변경 → 카메라 위치가 플레이어 중심 원호 이동, 거리 유지 |
| `Cam001_Pitch_RemainsFixed_WhenOrbiting` | B2 — 궤도 회전 전후 카메라 피치 각 불변 (±1°) |
| `Cam002_Camera_FollowsMovingPlayer` | B3 — 플레이어 강제 이동 후 카메라가 따라옴 (거리 유지 ±25%) |
| `Cam002_Player_FramedInLowerScreenBand` | B4 — WorldToViewportPoint(player).y ∈ [0.35, 0.55] (여유 밴드) |
| `Move001_BallFollowsRotatedCameraForward` | B5 — 궤도 90° 회전 후 전진 입력 → 이동 방향이 새 카메라 전방과 <20° |

## Manual play checks (사람만 판단 가능)

- 마우스 감도가 과하거나 답답하지 않은가 (멀미 위험 — 기획서 21.6 "카메라 불만 10% 미만")
- 고속 주행·급선회 중 화면 떨림이나 덜컹임이 없는가 (B6 — 떨림은 자동 검증이 취약해 사람 판단)
- 공이 화면에서 놓치는 순간이 없는가 (벽 근처, 램프 위)
- 커서 잠금/해제가 자연스러운가 (B7)
- 쿼터뷰 각도(38°)에서 지형·적(향후)이 읽히는가

## Open questions

- 없음 — 마우스 Y 피치는 Out of scope에 기록된 대로 플레이테스트 후 재검토
