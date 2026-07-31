# Verification Report — F04 Speed Tiers

- 날짜: 2026-08-01
- 검증 방식: Unity MCP 자동 검증 (`Unity_RunCommand` + `Unity_GetConsoleLogs` + `TestRunAutomation`)
- 참고: 이번 검증부터 Player Settings의 **Run In Background 활성화** 상태 — 런타임 프레임이 정상 진행됨을 `Time.frameCount` 1441 → 5662로 확인

## Scope

[Docs/Features/F04-speed-tiers.md](../Features/F04-speed-tiers.md) — SPD-001 (+ DASH-001 예외, MOVE-005 기준값).
검증 대상: `SpeedTierLogic`(Core), `SpeedTierTracker`(Gameplay), `SpeedTierVisuals`(Presentation), `BallMotor`의 대시 활성 창, FirstPlayable 씬 배선.

## Compilation

- `VERIFIED` — `scriptCompilationFailed = False`, `Game.Core.SpeedTierLogic` / `Game.Presentation.SpeedTierVisuals` 타입 로드 확인
- 새 에러 0건, 새 경고 0건 (콘솔의 경고 1건은 무관한 AI Assistant 계정 API 경고)
- 구현 중 발생한 에러 1건은 검증 전 해소: `Object.GetInstanceID()`가 이 Unity 버전에서 폐기 API(CS0619) → 참조 비교(`Assert.AreSame`)로 대체

## EditMode Tests

- `VERIFIED` — **35 / 35 통과, 실패 0, 스킵 0** (F01 10 + F03 17 회귀 + F04 신규 8)
- F04: `Spd001_Ratios_MapToExpectedTiers`, `Spd001_Boundaries_AreInclusiveAsSpecified`, `Spd001_Dashing_ForcesRumble_RegardlessOfSpeed`, `Spd001_Hysteresis_HoldsTierNearBoundary`, `Spd001_Hysteresis_ReleasesTierBeyondBoundary`, `Spd001_LargeDrop_FallsMultipleTiers`, `Spd001_OverMaxSpeed_ClampsToRumble`, `Spd001_ZeroMaxSpeed_DoesNotDivideByZero`

## PlayMode Tests

- `VERIFIED` — **17 / 17 통과, 실패 0** (F01 4 + F02 5 + F03 4 회귀 + F04 신규 4)
- F04: `Spd001_TierRises_AsBallAccelerates`, `Spd001_EventFires_OnlyOnTierChange`, `Spd001_Dash_ImmediatelyEntersRumble`, `Spd001_Visuals_UpdateWithoutLeakingMaterials`

## Runtime Verification (FirstPlayable.unity, 실제 Play Mode)

- `VERIFIED` — 씬 배선: Player에 `SpeedTierTracker`(config 연결) + `TrailRenderer`(M_SpeedTrail) + `SpeedTierVisuals`
- `VERIFIED` — 시작 상태: tier=Low, ratio 0, 트레일 `emitting=False`, `time=0` (B6 저속엔 트레일 없음)
- `VERIFIED` — **B1 단계 상승**: ratio 0.588에서 tier=**Mid** (밴드 0.35~0.70), 트레일 `time=0.25` + 시안 `RGBA(0.2, 0.8, 1.0)`
- `VERIFIED` — **B1 상단 밴드**: ratio 0.989에서 tier=**Rumble** (밴드 0.95+)
- `VERIFIED` — **B2 대시 강제 럼블(분리 검증)**: 저속 대시 직후 `IsDashActive=True`, ratio **0.685**(속도만으론 Mid)인데 tier=**Rumble**
- `VERIFIED` — **B5 상한**: 대시 오버스피드 ratio **1.428**에서도 tier=Rumble 유지
- `VERIFIED` — **B6 시각 피드백**: 럼블에서 트레일 `time=0.6` + 백열색 `RGBA(1, 1, 0.9)`, MaterialPropertyBlock의 `_BaseColor`/`_EmissionColor`(6배 강도) 갱신 확인
- `VERIFIED` — **B7 머티리얼 누수 없음**: 다수 전환 후에도 `renderer.sharedMaterial` 이름이 원본 `Lit` 그대로 (인스턴스 미생성)
- `VERIFIED` — **B4 이벤트 빈도**: 전체 세션에서 `ChangeCount = 9` — 수천 프레임 동안 단계 전환 횟수만큼만 발행됨
- `VERIFIED` — 플레이 세션 전체 새 콘솔 에러 0건
- `UNVERIFIED` — B3 히스테리시스의 씬 내 재현: 경계 근처에 속도를 고정 유지하는 조작을 만들지 못함. EditMode 테스트 2건으로는 통과

## 증빙 캡처

- [2026-08-01_F04_rumble_tier.png](../Media/2026-08-01_F04_rumble_tier.png) — 럼블 단계 상태의 에디터 전체

## Manual Verification Required (사람 판단)

- `MANUAL_REQUIRED` 단계 전환이 눈에 읽히는가 — 색·트레일만으로 지금 상태를 알 수 있는가
- `MANUAL_REQUIRED` 경계에서 깜빡임이 없는가 (히스테리시스 체감 — 자동 검증 미확보 항목)
- `MANUAL_REQUIRED` 대시 시 럼블 진입이 통쾌한가, 과한가 (트레일 0.6s·발광 6배가 적절한가)
- `MANUAL_REQUIRED` 그레이박스 배경에서 트레일이 지저분하지 않은가 (가독성)
- `MANUAL_REQUIRED` 저속→중속 전환(35%)이 너무 이르거나 늦지 않은가

## Remaining Risks

- 단계 경계·색·트레일 길이는 전부 TEMPORARY — 조정은 `BallMovementConfig` 에셋과 Player의 `SpeedTierVisuals` 인스펙터에서 (코드 수정 불필요)
- 현재 코어 머티리얼이 URP `Lit` 기본 에셋이라 Emission이 씬 조명·후처리 설정에 따라 약하게 보일 수 있음 — Bloom은 P08에서 도입
- 트레일 머티리얼(`M_SpeedTrail`)은 URP Unlit 기본값 — 알파 블렌딩·색 적용은 아트 단계(P07/P08)에서 다듬을 것
- 그레이박스 아레나가 60×60이라 최고 속도 구간을 길게 유지하기 어려움 — 플레이테스트 시 체감이 짧을 수 있음

## 판정

자동 검증(컴파일·EditMode·PlayMode) 전체 `VERIFIED`. 런타임에서 B1·B2·B4·B5·B6·B7을 씬에서 직접 확인했고, B3(히스테리시스)만 씬 내 재현 실패로 `UNVERIFIED`(EditMode 테스트로는 통과).
Feature Spec 상태를 **Verified (automated)** 로 갱신.
