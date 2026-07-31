# Verification Report — F02 Camera

- 날짜: 2026-07-30
- 검증 방식: Unity MCP 자동 검증 (`Unity_RunCommand` + `Unity_GetConsoleLogs` + `TestRunAutomation`)

## Scope

[Docs/Features/F02-camera.md](../Features/F02-camera.md) — CAM-001 / CAM-002 / CAM-003 / MOVE-001 연동.
검증 대상: Cinemachine 리그(OrbitalFollow·RotationComposer·InputAxisController), CinemachineBrain, CursorLockController, FirstPlayable 씬 통합.

## Compilation

- `VERIFIED` — `scriptCompilationFailed = False`, 에러 0건
- 새 경고 0건 — 구현 중 발생했던 CS0618(`FindFirstObjectByType` 폐기)은 `FindAnyObjectByType`로 수정되어 사라짐. 콘솔의 경고 1건은 무관한 AI Assistant 계정 API 경고

## EditMode Tests

- `VERIFIED` — **10 / 10 통과** (F01 회귀 — F02는 신규 엔진 무관 로직이 없어 EditMode 추가 없음, 스펙에 근거 명시)

## PlayMode Tests

- `VERIFIED` — **9 / 9 통과, 실패 0** (F01 4개 회귀 + F02 5개)
- F02: `Cam001_HorizontalAxis_OrbitsCameraAroundPlayer`, `Cam001_Pitch_RemainsFixed_WhenOrbiting`, `Cam002_Camera_FollowsMovingPlayer`, `Cam002_Player_FramedInLowerScreenBand`, `Move001_BallFollowsRotatedCameraForward`

## Runtime Verification (FirstPlayable.unity, 실제 Play Mode)

- `VERIFIED` — 리그 배선: Follow=Player, 피치 고정(range 38~38), 수평축만 Look 액션 연결, Brain LateUpdate, CursorLockController 부착 (`F02CameraSetup.Verify()` = PASS)
- `VERIFIED` — 카메라-플레이어 거리 = 11.0 (궤도 반경과 일치), Brain이 Main Camera를 구동 (BallMotor의 기준축도 동일 카메라)
- `VERIFIED` — 플레이어 뷰포트 y = **0.42** (CAM-002 목표 40~45% 적중)
- ~~`VERIFIED` — 공이 스폰에서 벽 부근까지 이동한 뒤에도 카메라 거리 11 유지~~ → **`UNVERIFIED` (2026-08-01 정정)**: 당시 측정 속도가 0.01 m/s로 사실상 정지 상태였고, 에디터가 백그라운드라 플레이 모드 프레임이 진행되지 않았을 가능성이 높다. 카메라 추적은 PlayMode 테스트 `Cam002_Camera_FollowsMovingPlayer`로 통과하므로 기능 판정은 유지. 자세한 경위는 [F03 검증 보고서](2026-08-01_F03-jump-dash_VERIFICATION.md#검증-인프라-문제-발견-f02-보고서-정정-포함) 참조
- `VERIFIED` — 플레이 세션 전체 콘솔 에러 0건
- `INFERRED` — 고속 이동 중 떨림 없음(CAM-003): Rigidbody Interpolate + Brain LateUpdate 구성으로 조건은 충족하나, 떨림 자체는 자동 판정이 취약 → Manual로 이관

### 구현 중 발견·수정 (검증 전 해소)

- CM3 `RotationComposer.ScreenPosition`은 +y가 화면 아래 방향 — 초기 -0.08이 플레이어를 상단(viewport 0.58)에 배치. +0.08로 수정 후 실측 0.42.

## 증빙 캡처

- [2026-07-30_F02_quarterview_framing.png](../Media/2026-07-30_F02_quarterview_framing.png) — 게임 카메라 렌더 (프레이밍 확인)
- [2026-07-30_F02_editor_full.png](../Media/2026-07-30_F02_editor_full.png) — 에디터 전체 (씬 구성·하이어라키·콘솔)
- [2026-07-30_F02_verify_playmode.png](../Media/2026-07-30_F02_verify_playmode.png) — 검증 플레이 모드 중 에디터 전체

## Manual Verification Required (사람 판단)

- `MANUAL_REQUIRED` 마우스 감도가 과하거나 답답하지 않은가 (멀미 — 기획서 21.6 "카메라 불만 10% 미만")
- `MANUAL_REQUIRED` 고속 주행·급선회 중 화면 떨림·덜컹임이 없는가 (CAM-003 체감)
- `MANUAL_REQUIRED` 공을 화면에서 놓치는 순간이 없는가 (벽 근처, 램프 위)
- `MANUAL_REQUIRED` 커서 잠금(클릭)/해제(ESC)가 자연스러운가
- `MANUAL_REQUIRED` 쿼터뷰 각도에서 지형이 잘 읽히는가

## Remaining Risks

- Look 감도(gain 2)·댐핑은 TEMPORARY — 씬의 `CM_GameplayCamera` 인스펙터가 튜닝 표면
- 카메라 충돌(벽 파고들기) 미처리 — 개활지 그레이박스에서는 문제 없음, 기둥·잔해 추가(P07) 시 Deoccluder 필요
- Cinemachine 타입을 직접 참조하는 MCP RunCommand는 dll 캐시 오류 발생 — Game.Editor 스크립트 경유로 우회 (이후 기능도 동일 패턴)

## 판정

자동 검증 항목 전체 `VERIFIED`. Feature Spec 상태를 **Verified (automated)** 로 갱신.
Manual 목록은 사용자 플레이 후 판단 — F01 조작감 체크와 함께 한 번에 플레이테스트 권장.
