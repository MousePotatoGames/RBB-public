# Verification Report — F06 Collision Combat

- 날짜: 2026-08-01
- 검증 방식: Unity MCP 자동 검증 (`Unity_RunCommand` + `Unity_GetConsoleLogs` + `TestRunAutomation`) + FirstPlayable 씬 런타임 계측

## Scope

[Docs/Features/F06-collision-combat.md](../Features/F06-collision-combat.md) — DMG-001 / DMG-002 / DMG-003 / DMG-004 / ENM-004 / SPD-001 / [Decision 0001](../Decisions/0001-enemy-physics-hybrid.md).
검증 대상: `DamageLogic`·`HitCooldownLogic`·`KnockbackLogic`·`HitStopLogic`(Core), `EnemyHealth`·`PlayerDamageDealer`·`ScrapDrone.TakeKnockback`(Gameplay), `HitStopController`(Presentation), `DroneConfig` 확장, FirstPlayable 씬 배선.

## Compilation

- `VERIFIED` — 컴파일 성공, **새 에러 0건**
- `VERIFIED` — 최종 콘솔 경고 1건뿐이며 이 기능과 무관 (`com.unity.ai.assistant`의 Account API 접근 경고)
- 검증 중 발견해 제거한 경고 2종:
  - `FindObjectsSortMode` 폐기 API (F05 PlayMode 테스트) → `FindObjectsInactive.Exclude`
  - **`Setting linear/angular velocity of a kinematic body is not supported`** — `ScrapDrone.Initialise`가 이미 키네마틱인 바디에 속도를 써서 **스폰마다** 경고를 뱉고 있었다 → 다이나믹일 때만 속도를 0으로 초기화하도록 수정

## EditMode Tests

- `VERIFIED` — **71 / 71 통과, 실패 0, 스킵 0** (F01·F03·F04·F05 회귀 포함, F06 신규 16)
- F06: `Dmg001` × 6, `Dmg002` × 2, `Dmg003` × 2, `Dmg004` × 2, `Enm004` × 1, `HitStop` × 3

## PlayMode Tests

- `VERIFIED` — **34 / 34 통과, 실패 0** (F01~F05 회귀 포함, F06 신규 9)
- 검증 과정에서 **테스트 자체를 3건 보강**했다 (아래 "검증 중 발견한 결함" 참조)

| 테스트 | 검증 |
|---|---|
| `Dmg001_FastCollision_KillsDrone` | B1, B11 |
| `Dmg001_SlowContact_DoesNotKillInstantly` | B1, B2 |
| `Enm004_HitDrone_BecomesDynamicThenRecovers` | B10 |
| `Enm004_LoftedDrone_ReturnsToChaseHeight` | **B19 (신규)** |
| `Enm004_DeadDrone_ReturnsToPoolAfterCorpseTime` | B11, B14 |
| `Dmg002_SingleCollision_DamagesOnce` | B6 |
| `Enm004_DeadDrone_StopsDamagingPlayer` | B12 |
| `HitStop_StrongHit_FreezesThenRestoresTimeScale` | B15, B17 |
| `HitStop_OnDisable_RestoresTimeScale` | B18 |

## 검증 중 발견한 결함 (전부 수정 완료)

### 1. 넉백 회복이 스포너에 의존 — **제품 코드 결함**

- 증상: `Enm004_HitDrone_BecomesDynamicThenRecovers` 실패. 맞은 드론이 다이나믹 상태에서 영원히 회복하지 못함
- 원인: 회복·시체 타이머가 `ScrapDrone.Tick()` 안에 있었는데, `Tick`은 **스포너가 호출**한다. 스포너가 틱하지 않는 경로(테스트, 풀 반환 대기 등)에서는 타이머가 멈춘다
- 조치: 타이머를 `ScrapDrone.FixedUpdate()`로 이동 — 드론이 자기 상태를 스스로 소유한다

### 2. 공중부양 — **제품 코드 결함 (사용자 제보)**

- 증상: 넉백으로 떠오른 드론이 공중에 뜬 채로 플레이어를 추적 (사용자 스크린샷: 수직으로 쌓인 드론 기둥)
- 원인: 키네마틱 이동(`MovePosition`)은 **현재 Y를 보존**한다. 넉백 중 물리 충돌로 떠오른 드론이 키네마틱으로 복귀하면 그 높이에 고정된다
- 조치: 스폰 시 추적 높이를 기억했다가 키네마틱 복귀 시 되돌린다 (규칙 **B19** 신설)
- **1차 수정은 실패했다** — `Rigidbody.position` 쓰기가 보간(interpolation)에 의해 되돌려져 드론이 y=3.98에 그대로 남았다. 신규 테스트가 이걸 잡아냈고, 보간을 끄고 텔레포트하도록 다시 고쳤다

### 3. 히트스톱 테스트가 허술했음 — **테스트 결함**

- `HitStop_StrongHit_FreezesThenRestoresTimeScale`은 "정지가 끝났는가"만 봤기 때문에 **정지가 한 번도 안 걸려도 통과**하는 구조였다
- 조치: `HitStopController.FreezeCount`를 추가하고 `FreezeCount > 0`을 단언 — 이제 실제 발동을 검증한다
- 같은 이유로 `Enm004_DeadDrone_LingersThenDespawns`는 "시체가 남는가"만 봤고 **풀 반환·재사용(B11/B14)을 검증하지 않았다** → 스포너 풀을 실제로 사용하도록 재작성

## Runtime Verification (FirstPlayable.unity, 실제 Play Mode)

- `VERIFIED` — **씬 배선 전항목**: `HitStopController`(활성, dealer/config 연결), `PlayerDamageDealer`(config/movementConfig/motor 전부 연결)
- `VERIFIED` — **B19 공중부양 해소**: 생존 드론 **전원에게 +10 m/s 상향 넉백**을 가한 뒤 회복 시점 측정 — 9마리 전부 `y = 0.49~0.50` (플레이어 높이 0.50), **플레이어보다 0.5m 이상 높은 개체 0마리**
- `VERIFIED` — **B1/B2 "빠를수록 강하다"**: 저속 접촉으로는 잘 죽지 않음(38타에 1킬), 전속력 돌진 시 킬 수가 2 → 13 → 23으로 급증
- `VERIFIED` — **B10 다이나믹 전환**: 관측 시점에 3마리 다이나믹(ragdoll) / 7마리 키네마틱 공존
- `VERIFIED` — **B17 timeScale 안전성**: 전체 세션 동안 `Time.timeScale`이 1에서 벗어난 상태로 남은 적 없음
- `VERIFIED` — **히트스톱 설정값·공식**: 임계 5 / 기준 15 / 0.05~0.09s. 피해 10 → 정지 **0.070s**, 피해 4 → **0s** (B15 임계 아래는 정지 없음)
- `UNVERIFIED` — **씬 내 히트스톱 발동 순간 포착**: 정지 시간이 0.05~0.09s로 MCP 샘플링 간격(초 단위)보다 훨씬 짧아 스냅샷으로는 관측 불가. PlayMode 테스트(`FreezeCount > 0`)로는 통과
- `UNVERIFIED` — **정상 플레이 중 히트스톱 발생 여부** — 아래 "Remaining Risks" 1번 참조. 계측된 190타 동안 `FreezeCount = 0`

## 계측으로 드러난 밸런스 사실 (버그 아님)

플레이어를 14 m/s로 가속시켜 드론 무리에 진입시킨 뒤 속도를 추적한 결과:

| 상황 | 속도 | 정면 충돌 시 피해 | 히트스톱 |
|---|---:|---:|---|
| 무리 진입 직전 | 14.0 m/s | 11.7 | 0.077s |
| 무리 안 | 2.7 m/s | 2.5 | 없음 |
| 무리 깊숙이 | 1.7 m/s | 2.5 (하한) | 없음 |

**드론 무리 안에서 공이 사실상 정지한다.** 피해 공식은 명세대로 동작하지만(2.5 = 10 × 0.25 속도 하한 × 1.0 정면도), 히트스톱 임계 5를 넘는 타격이 무리 안에서는 거의 발생하지 않는다.
드론 콜라이더는 트리거라 물리적으로 감속시키지 않으므로, 감속 원인은 **입력이 없을 때 `BallMotor`가 속도를 감쇠시키는 것**이다 — 즉 실제 플레이에서는 계속 입력을 넣으면 다를 수 있다. 판단은 사람 플레이가 필요하다.

## Manual Verification Required (사람 판단)

- `MANUAL_REQUIRED` **"빠를수록 강하다"가 조작만으로 학습되는가** (HYP-003 — 이번 기능의 핵심)
- `MANUAL_REQUIRED` **무리 안에서 속도가 유지되는가** — 계측상 정지에 가까웠다. 실제 플레이에서 "돌파" 감각이 나는가, 아니면 늪에 빠지는가
- `MANUAL_REQUIRED` **히트스톱이 실제로 느껴지는가** — 걸리기는 하는가, 걸린다면 0.05~0.09s가 적절한가
- `MANUAL_REQUIRED` 대시로 들이받는 게 통쾌한가 — 지난 플레이테스트에서 보류한 O4 재평가
- `MANUAL_REQUIRED` 저속 접촉의 손해(내 HP만 깎임)가 납득되는가, 억울한가
- `MANUAL_REQUIRED` 드론이 날아가는 반응이 시원한가, 과한가 (넉백 9 m/s)
- `MANUAL_REQUIRED` 점프 낙하 공격이 쓸 만한가, 있으나 마나 한가
- `MANUAL_REQUIRED` 죽은 드론이 1초간 남는 게 자연스러운가, 지저분한가

## Remaining Risks

1. **히트스톱이 정상 플레이에서 발동하지 않을 가능성** — 계측 190타 동안 0회. 임계(5)를 낮출지, 속도 하한(0.25)을 올릴지, 아니면 감속 자체를 손댈지는 **사람 플레이 후에** 결정할 것. 지금 수치를 만지면 근거 없는 튜닝이 된다
2. 넉백으로 뜬 드론의 높이 복귀는 **순간 이동(snap)** 이다. 실제 넉백 임펄스는 수평(Y=0)이라 뜨는 높이가 작아 눈에 띄지 않을 것으로 보이지만, 큰 낙차에서는 부자연스러울 수 있다 — P08 연출 단계에서 재검토
3. 30마리 상한 구간의 프레임 부하는 여전히 미측정 (F05에서 이월)
4. 검증 도중 **플레이 모드 중 스크립트 수정 → Unity 교착** 발생. 플레이 모드에서는 재컴파일이 보류되는데 MCP는 "컴파일 중"이라며 명령을 거부해 정지시킬 수단이 없어졌다. **런타임 계측 중에는 코드를 수정하지 말 것**

## 판정

자동 검증(컴파일·EditMode 71/71·PlayMode 34/34) 전체 `VERIFIED`.
검증 과정에서 **제품 코드 결함 3건**(넉백 회복 의존성, 공중부양, 키네마틱 속도 쓰기 경고)과 **테스트 결함 3건**(허술한 히트스톱 단언, 풀 반환 미검증, 테스트 기하)을 발견해 전부 수정했다.
히트스톱의 씬 내 발동만 `UNVERIFIED`로 남으며, 이는 관측 한계이자 동시에 **밸런스 안건**이다 — 플레이테스트에서 판단한다.
Feature Spec 상태를 **Verified (automated)** 로 갱신.
