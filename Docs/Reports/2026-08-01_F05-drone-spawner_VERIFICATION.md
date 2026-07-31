# Verification Report — F05 Drone & Spawner

- 날짜: 2026-08-01
- 검증 방식: Unity MCP 자동 검증 (`Unity_RunCommand` + `Unity_GetConsoleLogs` + `TestRunAutomation`)

## Scope

[Docs/Features/F05-drone-spawner.md](../Features/F05-drone-spawner.md) — ENM-001 / ENM-004 / WAVE-001(축소) / WAVE-002 / HP-001 / HP-002 / [Decision 0001](../Decisions/0001-enemy-physics-hybrid.md).
검증 대상: `SpawnBudgetLogic`·`SpawnRingLogic`·`ChaseLogic`·`SeparationLogic`·`HealthLogic`(Core), `ScrapDrone`·`DroneSpawner`·`PlayerHealth`(Gameplay), Drone 프리팹·DroneConfig, FirstPlayable 씬 배선.

## Compilation

- `VERIFIED` — `scriptCompilationFailed = False`, Core/Gameplay 신규 타입 전부 로드 확인
- 새 에러 0건, 새 경고 0건

## EditMode Tests

- `VERIFIED` — **55 / 55 통과, 실패 0, 스킵 0** (F01 10 + F03 17 + F04 8 회귀 + F05 신규 20)
- F05: Wave001 × 5, Wave002 × 4, Enm001 × 2, Enm004 × 3, Hp001 × 6

## PlayMode Tests

- 1차 실행: **23 통과 / 2 실패** — `Hp001_DroneContact_DamagesPlayer`, `Hp001_ContinuousContact_LimitedByInvulnerability`
  - 원인: **테스트 리그 결함** (제품 코드 아님). 테스트용 드론 콜라이더가 트리거가 아니었고 테스트 플레이어를 키네마틱으로 만들어, **키네마틱 ↔ 키네마틱 조합이라 물리 이벤트가 발생하지 않음**
  - 수정: 테스트 리그를 실제 씬과 동일하게 — 드론 콜라이더 `isTrigger = true`(B18), 플레이어 Rigidbody를 동적으로(위치는 제약으로 고정)
- 재실행: `VERIFIED` — **25 / 25 통과, 실패 0** (F01 4 + F02 5 + F03 4 + F04 4 회귀 + F05 신규 8)

## Runtime Verification (FirstPlayable.unity, 실제 Play Mode)

### 1차 시도에서 발견·수정한 씬 배선 결함

- 증상: 플레이 진입 시 `activeCount=0`, `elapsed=0`, `HP=0/0` — 스포너가 전혀 동작하지 않음
- 원인: 저장된 씬에서 **`PlayerHealth.config`와 `DroneSpawner.dronePrefab` 참조가 NULL**. 배선 직후 스크립트를 수정(트리거 콜백·HP 로그)하면서 재컴파일이 일어났고, 그 과정에서 두 참조가 유실된 것으로 보임
- 조치: 재배선 후 `SaveOpenScenes` → 저장 직후 참조 존재를 즉시 재확인(OK), 이후 플레이 모드에서 정상 동작 확인

### 재검증 결과 (모두 실제 플레이 중 측정)

- `VERIFIED` — **B3/B4 예산 곡선**: 8.3초 시점 `activeCount = 7`, 계산된 `target = 7`로 일치. 37.9초 시점 `active = 13` (곡선상 5→25 증가 중)
- `VERIFIED` — **B11 풀 재사용**: 37.9초 동안 `totalCreated = 13` = 동시 생존 수와 동일 — 초과 생성 없음
- `VERIFIED` — **B2 / Decision 0001**: 생존 드론 7마리 전부 `isKinematic = true`
- `VERIFIED` — **B18 트리거**: 생존 드론 7마리 전부 `isTrigger = true` (플레이어를 물리적으로 막지 않음)
- `VERIFIED` — **B1 추적**: 스폰 링이 18~26m인데 관측된 최대 거리 16m, 최소 0m → 전부 플레이어 쪽으로 접근함
- `VERIFIED` — **B13 접촉 피해**: 콘솔 로그 `[HP] -8 → 28/100 → 20 → 12 → 4 → 0`
- `VERIFIED` — **B16 사망 1회 보고**: `[HP] PlayerDied` 로그가 정확히 한 번만 출력
- `VERIFIED` — **B17 사망 후 계속 진행**: 사망 후에도 플레이 모드가 멈추지 않고 스포너가 계속 동작 (37.9초까지 진행)
- `VERIFIED` — 세션 전체 새 콘솔 에러 0건
- `INFERRED` — **B6 스폰 원뿔 회피**: 관측 시점에 카메라 원뿔 안에 있던 드론 1마리는 **스폰 후 추적으로 들어온 것**이며, 스폰 순간 위치가 아님. 스폰 시점 판정은 PlayMode 테스트 `Wave002_SpawnedDrones_AppearOutsideCameraCone`로 통과 — 런타임 스냅샷으로는 구분 불가
- `UNVERIFIED` — **B8/B9 분리 보정의 씬 내 재현**: 드론이 밀집한 순간을 포착하지 못함. EditMode 테스트 3건으로는 통과
- `UNVERIFIED` — **30마리 상한 부하**: 관측 최대 13마리(37.9초 시점). 상한 근처 프레임 부하는 90초 완주 후 측정 필요

## 증빙 캡처

- [2026-08-01_F05_drone_swarm.png](../Media/2026-08-01_F05_drone_swarm.png) — 드론 무리가 활동 중인 에디터 전체

## Manual Verification Required (사람 판단)

- `MANUAL_REQUIRED` 드론이 "몰려오는" 느낌인가, 흩어져 보이는가
- `MANUAL_REQUIRED` 화면 밖에서 나타나는 게 자연스러운가 (눈앞에서 튀어나오지 않는가)
- `MANUAL_REQUIRED` 시간에 따른 밀도 증가가 체감되는가
- `MANUAL_REQUIRED` 드론 무리 속에서 공이 잘 보이는가 (마젠타 vs 시안 대비)
- `MANUAL_REQUIRED` 무리에 갇혔을 때 빠져나갈 여지가 있는가 (ENM-004 체감)
- `MANUAL_REQUIRED` **접촉 피해가 너무 빠르지 않은가** — 검증 중 가만히 서 있던 플레이어가 약 10초 만에 100 → 0. 지금은 반격 수단(F06)이 없어 당연하지만, 무적 0.45s·피해 8이 적절한지는 F06 이후 재판단
- `MANUAL_REQUIRED` 30마리 근처에서 프레임이 눈에 띄게 떨어지지 않는가

## Remaining Risks

- **씬 참조 유실 재발 가능성**: 씬 배선 직후 스크립트를 수정하면 재컴파일 중 참조가 날아갈 수 있다. 앞으로는 **스크립트 수정 → 컴파일 완료 → 씬 배선 → 저장 직후 참조 재확인** 순서를 지킬 것 (이번 검증에서 확인 단계를 추가함)
- 접촉 피해 밸런스는 F06(반격 수단)이 들어와야 판단 가능 — 지금 수치를 조정하면 근거 없는 튜닝이 됨
- 90초 완주 시 25~30마리 구간의 실제 프레임 부하 미측정 — 수동 플레이 또는 F13 이후 세션 완주로 확인 필요
- 드론이 플레이어 위치로 그대로 겹쳐 들어옴(최소 거리 0m) — 시각적으로 파고드는 느낌은 F06 넉백이 들어오면 완화될 것

## 판정

자동 검증(컴파일·EditMode·PlayMode) 전체 `VERIFIED`. 런타임에서 예산 곡선·풀 재사용·키네마틱·트리거·추적·접촉 피해·사망 1회 보고를 씬에서 직접 확인했다.
검증 과정에서 **테스트 리그 결함 1건과 씬 배선 유실 1건을 발견·수정**했다 — 둘 다 제품 로직 결함은 아니었다.
Feature Spec 상태를 **Verified (automated)** 로 갱신.
