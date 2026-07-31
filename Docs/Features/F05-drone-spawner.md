# Feature: F05 Drone & Spawner (스크랩 드론 · 스포너 · 풀링 · 접촉 피해)

## Status

Verified (automated) — EditMode 55/55, PlayMode 25/25, 런타임에서 예산 곡선·풀 재사용·추적·접촉 피해 확인 ([검증 보고서](../Reports/2026-08-01_F05-drone-spawner_VERIFICATION.md)). Manual play checks는 사용자 판단 대기. (스펙 승인: 2026-08-01 — 승인 시 **접촉 피해와 플레이어 HP를 F06에서 F05로 이관**하기로 결정)

## Purpose

전장에 **부딪힐 대상**을 만든다. F01~F04로 조작 토대는 확인됐지만, 적이 없어서 대시의 쓸모(플레이테스트 O4 "감이 안 잡히네")도 HYP-003(속도=공격)도 평가할 수 없었다. 드론이 들어오면 다음 기능(F06 충돌 전투)이 붙을 표적과, Web 성능의 실제 부하가 동시에 생긴다.

## Player's perspective

굴러다니다 보면 화면 밖에서 붉은 드론들이 나타나 나를 향해 곧장 몰려온다. 시간이 갈수록 수가 늘어난다. 아직 부딪혀도 아무 일도 없지만, 무리가 나를 둘러싸되 완전히 가두지는 않는다.

## Referenced rules

- ENM-001 스크랩 드론 (직선 추적, **접촉 피해**, 낮은 HP 물량형)
- ENM-004 적 공통 (둘러싸되 가두지 않음, 접근 슬롯 분산)
- WAVE-001 스폰 디렉터 (경과 시간 기준 생성 예산) — 90초 FP 축소판
- WAVE-002 스폰 위치 (카메라 밖 링, 진행 방향 정면 집중 금지)
- HP-001 피격과 무적 (`PlayerDamaged` 이벤트)
- HP-002 초기 피해 수치표 (플레이어 HP 100, 드론 접촉 8)
- Decision 0001 적 물리 하이브리드 (키네마틱 기본)

## In scope

- `Game.Core`: `SpawnBudgetLogic`(경과 시간 → 목표 동시 수), `SpawnRingLogic`(링 위 스폰 각도 선택 + 카메라 정면 회피), `ChaseLogic`(추적 이동 스텝), `SeparationLogic`(적끼리 겹침 완화), **`HealthLogic`(피해 적용 + 무적 시간)**
- `Game.Gameplay`: `ScrapDrone`(키네마틱 이동 어댑터), `DroneSpawner`(예산 관리 + 풀 사용), `EnemyPool`(UnityEngine.Pool 기반), `DroneConfig`(ScriptableObject), **`PlayerHealth`(HP-001/002)**
- FirstPlayable 씬: 스포너 오브젝트, 드론 프리팹(프리미티브 그레이박스, 마젠타 계열), Player에 PlayerHealth
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- **플레이어가 적에게 주는 피해·적 HP·넉백·적 사망** → F06 (DMG-001~004). F05에서 드론은 죽지 않는다 — 피해는 **드론 → 플레이어 방향만** 흐른다
- 다이나믹 전환 자체도 F06 (Decision 0001의 "넉백·사망 시 전환" 지점은 적 피해 시스템이 있어야 트리거된다). F05는 **키네마틱 기본 상태만** 구현하고 전환 진입점(메서드)만 비워둔다
- **패배 처리·결과 화면·재시작** → F07 (LOSE-001). F05에서 HP가 0이 되면 이벤트만 발행하고 게임은 계속된다
- 피격 시각 피드백(플래시·화면 효과) → P08. F05는 무적 시간의 기능적 동작까지만
- 램 비틀·아이언 브루트 (P01/P02), 경험치 오브 (F12)
- 3:30 풀 웨이브 곡선 (P03) — F05는 90초 선형 곡선만
- 사망 연출(디졸브), 피격 플래시 → F06/P08
- 품질 단계별 적 상한 자동 전환 (P11) — 상한값은 두되 수동 설정

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 드론은 플레이어를 향해 직선으로 이동한다 (경로탐색 없음) | ENM-001 |
| B2 | 드론은 **키네마틱 Rigidbody**로 스크립트 이동한다 — 물리 솔버가 위치를 결정하지 않는다 | Decision 0001 |
| B3 | 목표 동시 적 수는 경과 시간에 따라 선형 증가한다 (0초 5마리 → 90초 25마리) | WAVE-001 축소판 |
| B4 | 현재 수가 목표보다 적을 때만, 스폰 간격을 두고 생성한다 | WAVE-001 |
| B5 | 동시 적 수는 상한을 넘지 않는다 (기본 30) | WAVE-001 Exception, 기획서 15.3 |
| B6 | 스폰 위치는 플레이어 중심 링(반경 18~26m) 위이며, 카메라 정면 원뿔(±55°) 안에는 생성하지 않는다 | WAVE-002 |
| B7 | 연속 스폰이 한쪽에 몰리지 않도록 각도를 분산한다 | WAVE-002 |
| B8 | 드론끼리 최소 간격 미만으로 겹치면 서로 밀어내는 보정을 받는다 (완전히 겹쳐 한 덩어리가 되지 않음) | ENM-004 |
| B9 | 분리 보정은 추적보다 약하다 — 드론은 결국 플레이어에게 도달한다 (가두지도, 흩어지지도 않음) | ENM-004 |
| B10 | 플레이어에게서 너무 멀어진 드론(45m 초과)은 풀로 반환된다 | 성능 (기획서 14.5) |
| B11 | 드론은 Instantiate/Destroy가 아니라 **풀에서 재사용**된다 — 전투 중 생성 호출이 반복되지 않는다 | 기획서 14.5 |
| B12 | 매 프레임 `Find`/`GetComponent`로 플레이어를 찾지 않는다 (스폰 시 1회 주입) | 기획서 14.5 |
| B13 | 드론이 플레이어에 접촉하면 플레이어 HP가 감소하고 `PlayerDamaged`가 발행된다 | ENM-001, HP-001 |
| B14 | 피격 후 무적 시간(0.45s) 동안 추가 피해를 받지 않는다 | HP-001 |
| B15 | 무적 시간이 끝나면 다시 피해를 받는다 | HP-001 |
| B16 | HP는 0 미만으로 내려가지 않으며, 0이 되면 `PlayerDied`가 **한 번만** 발행된다 | HP-001, LOSE-001 준비 |
| B17 | 사망 후에도 F05에서는 게임이 멈추지 않는다 (패배 처리는 F07) | 스코프 경계 |
| B18 | 드론 콜라이더는 **트리거**다 — 플레이어를 물리적으로 막지 않는다 | ENM-004 "완전히 가두지 않는다" (구현 중 추가: 키네마틱 솔리드 30마리는 벽이 되어 규칙 위반) |

## Architecture

```text
Game.Core (엔진 참조 없음)
├─ SpawnBudgetLogic   # TargetCount(elapsed) / ShouldSpawn(current, target, sinceLastSpawn)
├─ SpawnRingLogic     # SpawnDirection(angleDeg) / IsInsideCameraCone(dir, camForward, halfAngle)
│                     #   + NextAngle(previousAngle, step) 각도 분산
├─ ChaseLogic         # Step(position, target, speed, dt) → 새 위치 (B1)
├─ SeparationLogic    # Push(self, neighbors, minDistance) → 보정 벡터 (B8, B9)
└─ HealthLogic        # HealthState(현재 HP, 무적 잔여) + ApplyDamage / Tick (B13~B16)

Game.Gameplay
├─ DroneConfig        # ScriptableObject: 속도, 링 반경, 예산 곡선, 분리 계수 (전부 TEMPORARY)
├─ ScrapDrone         # 키네마틱 Rigidbody 어댑터. SetTarget()으로 플레이어 주입 (B12)
│                     #   TakeKnockback() 진입점만 선언 (F06에서 구현)
├─ EnemyPool          # UnityEngine.Pool.ObjectPool<ScrapDrone> 래퍼
├─ DroneSpawner       # 예산 판정 → 링 위치 계산 → 풀에서 Get, 거리 초과 시 Release
└─ PlayerHealth       # HealthLogic 어댑터. TakeDamage(amount) / event PlayerDamaged, PlayerDied
```

## Initial tuning values (전부 `DroneConfig`)

| Item | Value | Status |
|---|---:|---|
| 드론 이동 속도 | 4 m/s | TEMPORARY (플레이어 12보다 훨씬 느림 — 물량형) |
| 시작 목표 수 (0초) | 5 | TEMPORARY (FIRST_PLAYABLE 90초 곡선) |
| 종료 목표 수 (90초) | 25 | TEMPORARY |
| 동시 적 상한 | 30 | TEMPORARY (기획서 15.3 Low 기준) |
| 스폰 간격 | 0.35 s | TEMPORARY |
| 스폰 링 반경 | 18 ~ 26 m | TEMPORARY |
| 카메라 정면 회피 각 | ±55° | TEMPORARY |
| 각도 분산 스텝 | 137° (황금각) | TEMPORARY |
| 드론 간 최소 간격 | 1.2 m | TEMPORARY |
| 분리 강도 | 추적 속도의 0.5배 | TEMPORARY |
| 반환 거리 | 45 m | TEMPORARY |
| 플레이어 최대 HP | 100 | TEMPORARY (HP-002) |
| 드론 접촉 피해 | 8 | TEMPORARY (HP-002) |
| 피격 무적 시간 | 0.45 s | TEMPORARY (HP-001 범위 0.35~0.55) |

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01·F03·F04 회귀 포함)
3. PlayMode 테스트 전체 통과 (F01~F04 회귀 포함)
4. FirstPlayable 씬 플레이: 화면 밖에서 드론이 나타나 추적하고, 시간에 따라 수가 늘며, 풀 재사용으로 생성 호출이 반복되지 않는다
5. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core)

| 테스트 | 검증 |
|---|---|
| `Wave001_TargetCount_RampsLinearlyOverTime` | B3 — 0s→5, 45s→15, 90s→25 |
| `Wave001_TargetCount_ClampsAfterSessionEnd` | B3 — 90초 이후에도 25 유지 |
| `Wave001_ShouldSpawn_OnlyWhenBelowTarget` | B4 |
| `Wave001_ShouldSpawn_RespectsInterval` | B4 |
| `Wave001_ShouldSpawn_NeverExceedsHardCap` | B5 |
| `Wave002_SpawnDirection_IsUnitAndPlanar` | B6 |
| `Wave002_SpawnPosition_AvoidsCameraFrontCone` | B6 |
| `Wave002_NextAngle_DistributesAcrossRing` | B7 — 연속 8회 각도가 한쪽에 몰리지 않음 |
| `Enm001_ChaseStep_MovesTowardTarget` | B1 |
| `Enm001_ChaseStep_DoesNotOvershoot` | B1 — dt가 커도 목표를 지나치지 않음 |
| `Enm004_Separation_PushesApartWhenTooClose` | B8 |
| `Enm004_Separation_IsZeroWhenFarEnough` | B8 |
| `Enm004_Separation_IsWeakerThanChase` | B9 |
| `Hp001_Damage_ReducesHealth` | B13 |
| `Hp001_DuringInvulnerability_DamageIgnored` | B14 |
| `Hp001_AfterInvulnerability_DamageAppliesAgain` | B15 |
| `Hp001_Health_NeverGoesBelowZero` | B16 |
| `Hp001_DeathTransition_ReportedOnlyOnce` | B16 |

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Wave001_Spawner_ReachesTargetCount` | B3, B4 — 일정 시간 후 목표 수 근접 |
| `Wave002_SpawnedDrones_AppearOutsideCameraCone` | B6 |
| `Enm001_Drone_ClosesDistanceToPlayer` | B1 — 거리 감소 관측 |
| `Enm001_Drone_IsKinematic` | B2 |
| `Wave001_Pool_ReusesInstances` | B11 — 반복 스폰/반환 후 씬의 드론 오브젝트 총수가 상한 이하 |
| `Hp001_DroneContact_DamagesPlayer` | B13 — 드론이 닿으면 HP 감소 + 이벤트 |
| `Hp001_ContinuousContact_LimitedByInvulnerability` | B14 — 계속 붙어 있어도 피해가 무적 주기마다 1회 |

## Manual play checks

- 드론이 "몰려오는" 느낌인가, 흩어져 보이는가
- 화면 밖에서 나타나는 게 자연스러운가 (눈앞에서 튀어나오지 않는가)
- 시간에 따른 밀도 증가가 체감되는가
- 드론 무리 속에서 플레이어(공)가 여전히 잘 보이는가 — 색 대비 가독성
- 30마리 근처에서 프레임이 눈에 띄게 떨어지지 않는가
- 접촉 피해가 납득되는가 — 무적 0.45s가 짧아서 순식간에 녹는 느낌은 아닌가 (HUD가 없어 HP는 콘솔 로그로 확인)
- 드론 무리에 갇혔을 때 빠져나갈 여지가 있는가 (ENM-004 "가두지 않음" 체감)

## Open questions

- 없음 (드론 외형·색은 그레이박스 프리미티브 + 마젠타 계열, 아트는 P07)
