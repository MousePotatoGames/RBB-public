# Feature: F07 Lose & Restart (패배 · 결과 화면 · 원클릭 재시작)

## Status

Verified (automated) — EditMode 85/85, PlayMode 43/43. 런타임에서 패배 전이·시간 정지·결과 화면·재시작 3회 연속 무누수 확인 ([검증 보고서](../Reports/2026-08-01_F07-lose-restart_VERIFICATION.md)). 정지 중 마우스 카메라 회전만 `UNVERIFIED`. Manual play checks는 사용자 판단 대기. 스펙 승인: 2026-08-01

## Purpose

**세션에 끝을 만든다.** F06까지는 HP가 0이 되어도 게임이 그냥 계속됐다. 끝이 없으니 "한 판"이라는 단위가 없고, 재도전도 없고, 성과를 돌아볼 지점도 없다.

F07이 들어오면 처음으로 **세션을 완주할 수 있게 된다.** 이건 다음 두 가지를 가능하게 한다:

- 플레이테스트 2회차에서 보류한 **O6 "저속 접촉의 손해가 억울해"** 재판단 — 한 판을 끝까지 해봐야 억울한지 납득되는지 알 수 있다
- **HYP-006(90초 세션의 재도전 유도)** 의 첫 관찰 — 권유 없이 Retry를 누르는가

## Player's perspective

무리에 갇혀 HP가 0이 되면 화면이 멈추고 결과가 뜬다. 얼마나 버텼는지, 몇 마리를 부쉈는지 보인다. 버튼 하나(또는 키 하나)로 곧바로 다시 시작되고, 처음과 완전히 같은 상태에서 다시 굴러간다.

## Referenced rules

- LOSE-001 패배 — HP 0 → `GameEnded` → 결과 화면(생존 시간·처치 수·최고 연속 처치·도달 레벨·획득 무기) → 원클릭 Retry
- LOSE-001 Exception — **결과는 한 번만 처리된다.** 플레이어 사망과 승리 조건이 같은 프레임이면 **승리가 우선**
- FP-001 (구조만 선반영) — 승리 판정 자체는 F13. 여기서는 **승리가 들어올 자리만** 만들어 F13에서 재작업이 없게 한다
- HP-001 — 사망 전이의 출처 (`PlayerHealth.Died`, F05에서 이미 발행 중)
- 기획서 21.3 — 재시작 시 **시간 초기화**, 결과 1회 처리

## In scope

- `Game.Core`: `SessionLogic` — 세션 상태 기계(진행 → 종료), **1회 종료 보장**, 승패 동시 발생 시 우선순위, 경과 시간·처치 수 집계
- `Game.Gameplay`: `GameSession` — `PlayerHealth.Died` 구독 → 세션 종료, `PlayerDamageDealer.Killed` 구독 → 처치 수 집계, `Ended` 이벤트 발행, 게임 시간 정지
- `Game.Presentation`: `ResultScreen` — uGUI + TMP 결과 화면, Retry 버튼 + 키 입력, 커서 해제
- 재시작: **씬 리로드** 방식 (아래 "재시작 방식" 참조)
- `SessionConfig`(신규 ScriptableObject) — 결과 화면 표시 지연 등. F13의 90초도 여기 들어올 자리를 만든다
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- **승리 판정(90초 생존)** — FP-001은 F13. 여기서는 `SessionOutcome.Victory`가 들어올 **구조만** 만들고 발동시키지 않는다
- **HUD**(HP 바·남은 시간·XP·무기 슬롯) — F13. F07의 UI는 **결과 화면 하나뿐**이다
- 결과 항목 중 **최고 연속 처치·도달 레벨·획득 무기** — 콤보(F13)·레벨(F12)·무기(F08~F11)가 아직 없다. 시스템이 생길 때 각 기능에서 추가한다. F07은 **생존 시간·처치 수**만 표시한다
- 타이틀 화면·일시정지 메뉴 (P10)
- 결과 화면 연출(페이드·슬라이드·사망 디졸브) — P08. F07은 즉시 표시
- 점수 저장·랭킹

## 재시작 방식 — 씬 리로드 (결정)

두 가지 선택지가 있다:

| | 씬 리로드 | 제자리 초기화(`ResetSession`) |
|---|---|---|
| 상태 누수 | 구조적으로 불가능 | 풀·이벤트·정적 상태를 **하나씩** 되돌려야 함 |
| 비용 | 씬이 작아 무시할 수준 | 더 저렴 |
| 코드 | 한 줄 | 컴포넌트마다 초기화 경로 유지 필요 |

**씬 리로드를 택한다.** FIRST_PLAYABLE 완료 조건에 "재시작 3회 연속 후에도 상태 누수 없음(풀, 이벤트, 시간)"이 명시되어 있고, 이 게임은 앞으로 무기·레벨·패시브가 붙어 초기화 대상이 계속 늘어난다. 누수를 **하나씩 막는 것보다 발생 자체를 없애는 쪽**이 맞다.

단, 씬 리로드는 `Time.timeScale`을 되돌려주지 않는다 — 로드 **전에** 반드시 1로 복구한다 (기획서 21.3).
`DroneSpawner.ResetSession()`은 제거하지 않는다. PlayMode 테스트가 쓰고, 나중에 제자리 재시작이 필요해지면 그 경로가 살아 있어야 한다.

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 플레이어 HP가 0이 되면 세션이 **패배로 종료**된다 | LOSE-001 |
| B2 | 세션 종료는 **정확히 한 번만** 처리된다 — 사망 이벤트가 여러 번 와도 결과는 한 번 | LOSE-001 Exception, 기획서 21.3 |
| B3 | 같은 프레임에 승리와 패배가 함께 발생하면 **승리가 우선**한다 | LOSE-001 Exception (F13 대비 선반영) |
| B4 | 종료 후에는 경과 시간이 더 이상 늘어나지 않는다 | 결과의 "생존 시간"이 확정값이어야 함 |
| B5 | 종료 시 게임 시간이 멈춘다 (`Time.timeScale = 0`) | 결과 화면 동안 드론이 계속 움직이면 안 됨 |
| B6 | 종료 시 히트스톱은 **해제되고 비활성화**된다 — 정지 복구가 세션 정지를 덮어쓰지 못한다 | B5와 `HitStopController`의 `timeScale` 소유권 충돌 방지 |
| B7 | 종료 시 결과 화면이 표시되고 **커서 잠금이 해제**된다 | 버튼을 클릭할 수 없으면 원클릭 재시작이 성립하지 않음 |
| B8 | 결과 화면은 **생존 시간**과 **처치 수**를 표시한다. 표시 문자열은 **ASCII로 유지**한다 | LOSE-001 (구현된 항목만) + 기본 TMP 폰트에 한글 글리프가 없음 (검증에서 발견) |
| B9 | 처치 수는 `PlayerDamageDealer.Killed` 발생 횟수와 일치한다 | DMG-001 결과의 집계 |
| B10 | Retry는 **버튼 클릭 또는 키 입력** 한 번으로 동작한다 | LOSE-001 "원클릭" |
| B11 | 재시작은 씬을 리로드하며, **리로드 전에 `Time.timeScale`을 1로 복구**한다 | 기획서 21.3 "시간 초기화" |
| B12 | 재시작 후 HP·타이머·처치 수·드론·풀이 전부 초기 상태다 | FIRST_PLAYABLE 완료 조건 4 |
| B13 | 세션 진행 중에는 결과 화면이 보이지 않는다 | — |
| B14 | 종료 후에는 플레이어 입력이 게임에 영향을 주지 않는다 (Retry 입력 제외) | 정지 중 조작이 상태를 바꾸면 안 됨 |

## Architecture

```text
Game.Core (엔진 참조 없음)
└─ SessionLogic          # SessionState / SessionOutcome
                         #   Start() / Tick(state, dt) / End(state, outcome) / RegisterKill(state)
                         #   B2 1회 종료, B3 승리 우선, B4 종료 후 시간 정지

Game.Gameplay
├─ SessionConfig         # 결과 표시 지연 (TEMPORARY). F13의 90초가 들어올 자리
└─ GameSession           # PlayerHealth.Died / PlayerDamageDealer.Killed 구독
                         # Core 상태를 소유하고 Ended 이벤트 발행 (기획서 14.3의 GameEnded)
                         # B5 timeScale 정지, B6 히트스톱 무력화

Game.Presentation
└─ ResultScreen          # GameSession.Ended 구독 → Canvas 표시 (B7 커서 해제)
                         # Retry 버튼/키 → B11 timeScale 복구 후 씬 리로드
```

의존 방향은 Presentation → Gameplay → Core로 유지된다. `GameSession`은 UI를 알지 못한다.

## Initial tuning values

| Item | Value | Status |
|---|---:|---|
| 결과 화면 표시 지연 | 0.6 s (실시간) | TEMPORARY — 사망 순간이 읽히도록. 0이면 너무 급작스럽고 길면 답답하다 |
| Retry 키 | `R` / `Space` | TEMPORARY |

> 지연은 `Time.timeScale = 0` 상태에서 흐르므로 **실시간(unscaled)** 으로 잰다.

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01·F03·F04·F05·F06 회귀 포함)
3. PlayMode 테스트 전체 통과 (회귀 포함)
4. FirstPlayable 씬 플레이: HP 0 → 화면 정지 → 결과 표시 → Retry → 처음 상태로 재시작
5. **재시작 3회 연속** 후에도 드론 수·HP·처치 수·`Time.timeScale`에 누수가 없다 (FIRST_PLAYABLE 완료 조건 4)
6. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core)

| 테스트 | 검증 |
|---|---|
| `Lose001_ZeroHealth_EndsSessionAsDefeat` | B1 |
| `Lose001_SecondEnd_IsIgnored` | B2 — 두 번째 종료 요청이 결과를 바꾸지 않음 |
| `Lose001_VictoryAndDefeat_SameFrame_VictoryWins` | B3 |
| `Lose001_DefeatAfterVictory_DoesNotOverride` | B3 — 순서가 반대여도 승리 유지 |
| `Lose001_ElapsedStops_AfterEnd` | B4 |
| `Lose001_ElapsedAccumulates_WhileRunning` | B4 대조군 |
| `Lose001_KillCount_AccumulatesWhileRunning` | B9 |
| `Lose001_KillsAfterEnd_AreIgnored` | B9 — 종료 후 집계 금지 |

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Lose001_PlayerDeath_EndsSessionOnce` | B1, B2 — `Ended` 이벤트가 정확히 1회 |
| `Lose001_RepeatedDeathReports_StillEndOnce` | B2 — 종료 요청이 반복돼도 결과는 1회 |
| `Lose001_ElapsedFreezes_AtDeath` | B4 — 씬에서도 생존 시간이 사망 시점에 고정 |
| `Lose001_SessionEnd_StopsTime` | B5 |
| `Lose001_SessionEnd_NeutralisesHitStop` | B6 — 히트스톱이 세션 정지를 덮어쓰지 않음 |
| `Lose001_ResultScreen_ShowsAfterDelay` | B7, B8 — 지연 후 표시, 그 전엔 숨김 |
| `Lose001_KillCount_MatchesDealer` | B9 — 실제 충돌로 죽인 수가 세션 집계와 일치 |
| `Lose001_RestoreTime_ResetsTimeScale` | B11 — 리로드 전 `timeScale` 복구 |

> 씬 리로드 자체는 PlayMode 테스트에서 다루지 않는다 (테스트 러너의 씬을 갈아엎는다). 리로드 **직전 상태**(timeScale 복구)까지 검증하고, 실제 재시작 결과는 **런타임 검증**과 수동 체크로 확인한다.

## Manual play checks

- 죽는 순간이 읽히는가 — 갑자기 멈춘 느낌인가, 납득되는가 (지연 0.6s가 적절한가)
- 결과 화면이 한눈에 들어오는가 (생존 시간·처치 수)
- **다시 하고 싶어지는가** (HYP-006 — 권유 없이 Retry를 누르는가)
- **저속 접촉의 손해가 여전히 억울한가** — 플레이테스트 2회차 O6 재판단. 한 판을 완주해봐야 판단 가능
- Retry가 즉각적인가, 로딩이 느껴지는가
- 재시작 직후 조작이 처음과 같게 느껴지는가

## 구현 노트

- **TMP 필수 리소스 도입**: 결과 화면 텍스트를 위해 `Assets/TextMesh Pro`(TMP Essential Resources)를 프로젝트에 가져왔다. F13 HUD도 이걸 쓴다
- **`GameSession.Ended`는 `LateUpdate`에서 발행한다**. 접촉 피해가 `FixedUpdate`에서 오고 `FixedUpdate`는 항상 `LateUpdate`보다 먼저 도므로, 같은 프레임에 도착한 승리가 패배를 이길 시간이 확보된다 (B2 + B3 동시 충족)
- **B6은 Presentation 쪽에서 구현했다** — Gameplay가 Presentation을 참조할 수 없기 때문에 `GameSession`이 히트스톱을 끄는 게 아니라, `HitStopController`가 세션 종료를 구독해 **`timeScale`을 건드리지 않고** 물러난다. 여기서 `Release()`를 부르면 세션 정지가 풀린다
- **B7의 커서 재잠금 문제**: `CursorLockController`가 클릭마다 커서를 다시 잠그므로, 세션이 끝난 뒤에는 동작하지 않도록 세션 참조를 추가했다 (Open question 해소)

## Open questions

- 사망 순간의 카메라 처리 — 지금은 그대로 멈춘다. 연출은 P08
- **한글 UI 폰트** — 검증에서 결과 화면의 "생존/처치"가 □□로 깨졌다. 기본 `LiberationSans SDF`에 한글 글리프가 없다. F07은 ASCII(`TIME` / `KILLS`)로 우회했지만, **F13 HUD에서 한글이 필요하면 폰트 결정이 선행되어야 한다**. 한글 전체 아틀라스는 WebGL 50MB 목표(기획서 15장)에 부담이므로, 필요한 글자만 담은 정적 SDF 또는 영문 유지 중 택일할 것
- **B14(종료 후 입력 무효)** — `Time.timeScale = 0`이 `FixedUpdate`를 멈추므로 이동·물리는 확실히 정지한다. 다만 카메라 궤도(Cinemachine 입력)는 `Update`에서 도는데, 이게 실제로 멈추는지는 **런타임 검증에서 확인**할 것. 게임 상태를 바꾸지는 않으므로 규칙 위반은 아니지만, 결과 화면에서 화면이 돌면 어색하다
