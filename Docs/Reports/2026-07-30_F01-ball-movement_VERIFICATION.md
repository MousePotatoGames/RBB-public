# Verification Report — F01 Ball Movement

- 날짜: 2026-07-30
- 검증 방식: Unity MCP 자동 검증 (com.unity.ai.assistant, `Unity_RunCommand` + `Unity_GetConsoleLogs`)
- 테스트 실행 인프라: `Game.Editor.TestRunAutomation` (TestRunnerApi → Temp 결과 파일, 이후 기능 검증에 재사용)

## Scope

[Docs/Features/F01-ball-movement.md](../Features/F01-ball-movement.md) — MOVE-001 / MOVE-004 / MOVE-005.
검증 대상: Game.Core 이동 로직, BallMotor/PlayerInputReader/BallMovementConfig, FirstPlayable 씬 통합.

## Compilation

- `VERIFIED` — `scriptCompilationFailed = False`, Game.Core / Game.Gameplay / Game.Editor 어셈블리 로드 확인
- 새 에러 0건 (부트스트랩 직후 기준선 0건 → 검증 종료 시점 0건)
- 새 경고 0건 — 콘솔의 경고 1건은 `com.unity.ai.assistant` 계정 API 네트워크 경고로 F01과 무관

## EditMode Tests

- `VERIFIED` — **10 / 10 통과, 실패 0, 스킵 0** (`Game.Tests.EditMode`)
- Move001 × 5, Move004 × 3, Move005 × 2 — 스펙 커버리지 계약 충족 (B1~B7 전부 테스트 또는 Manual check에 매핑)

## PlayMode Tests

- `VERIFIED` — **4 / 4 통과, 실패 0, 스킵 0** (`Game.Tests.PlayMode`)
- `Move001_BallAccelerates_WhenInputHeld`, `Move001_MovesAlongCameraDirection`, `Move005_PlanarSpeed_StaysUnderMaxSpeed`, `Move001_KeepsRolling_AfterInputReleased`

## Runtime Verification (FirstPlayable.unity, 실제 Play Mode)

- `VERIFIED` — 플레이 모드 진입/이탈 정상, Player에 BallMotor·Config·CameraTransform 연결 확인
- `VERIFIED` — 정지 상태 접지 판정 정상 (`IsGrounded = True`, y = 0.5)
- `VERIFIED` — 전방 입력 주입 후 평면 속도 **11.988 m/s로 수렴** (config max 12 — 런타임에서도 B4 성립)
- `VERIFIED` — 속도 방향이 카메라 전방과 **0.0° 오차** (B2)
- `VERIFIED` — 플레이 세션 전체에서 새 콘솔 에러 0건

### 발견·수정된 결함 1건

- **발견**: 그레이박스 아레나에 경계가 없어 공이 가장자리(±30m) 밖으로 굴러 무한 낙하 (y = -1494 관측). 기획서 10.2 경기장 필수 조건 **"낙사 없음" 위반**.
- **수정**: `Arena_Bounds` (벽 4면, 높이 4m) 씬에 추가 — Unity MCP로 수정 후 저장.
- **재검증**: `VERIFIED` — 동일 시나리오에서 공이 벽 앞(z = 29.5)에서 정지, 접지 유지, 경계 내 잔류.

## Manual Verification Required (사람 판단 — 자동 검증 불가)

에디터에서 FirstPlayable.unity를 Play로 직접 조작해 판단해 주세요:

- `MANUAL_REQUIRED` 조작이 "손에 붙는" 느낌인가 — 미끄럽지도 답답하지도 않은가 (HYP-001, 기획서 25장 1순위 리스크)
- `MANUAL_REQUIRED` 관성이 리스크/숙련 요소로 느껴지는가, 조작 불능감인가
- `MANUAL_REQUIRED` 램프(Ramp_Test) 주행 시 흐름이 끊기는 순간이 있는가 (MOVE-004 체감)
- `MANUAL_REQUIRED` Scene 뷰에서 카메라를 회전시켜 봐도 이동 방향에 위화감이 없는가
- `MANUAL_REQUIRED` 벽 충돌 반응이 어색하지 않은가 (신규 Arena_Bounds)

## Remaining Risks

- 튜닝 수치는 전부 TEMPORARY — 조작감 판단에 따라 `Assets/_Game/Configs/BallMovementConfig.asset`에서 조정 (코드 수정 불필요)
- 정적 카메라라 "카메라 기준 이동"의 실전 검증은 F02(카메라 회전) 이후에 완결됨
- 고속 조향 감쇠(MOVE-003)가 아직 없어 최대속도 선회가 실제 게임보다 민첩하게 느껴질 수 있음 — F03에서 해소
- Arena_Bounds는 F01 스펙에 없던 항목을 검증 중 추가한 것 — 스펙 In scope에 반영함

## 판정

자동 검증 항목 전체 `VERIFIED`. Feature Spec 상태를 **Verified (automated)** 로 갱신.
Manual Verification 목록은 사용자 플레이 후 `/first-playable:playtest`로 기록 권장.
