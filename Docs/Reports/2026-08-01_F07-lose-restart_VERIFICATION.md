# Verification Report — F07 Lose & Restart

- 날짜: 2026-08-01
- 검증 방식: Unity MCP 자동 검증 (`Unity_RunCommand` + `Unity_GetConsoleLogs` + `TestRunAutomation`) + FirstPlayable 씬 런타임 계측

## Scope

[Docs/Features/F07-lose-restart.md](../Features/F07-lose-restart.md) — LOSE-001 (+ FP-001 구조 선반영, HP-001, 기획서 21.3).
검증 대상: `SessionLogic`(Core), `SessionConfig`·`GameSession`(Gameplay), `ResultScreen`(Presentation), `HitStopController`·`CursorLockController`의 세션 연동, FirstPlayable 씬 배선.

## Compilation

- `VERIFIED` — `scriptCompilationFailed = False`, 새 에러 **0건**
- `VERIFIED` — 최종 콘솔 경고 0건 (검증 중 발생한 TMP 글리프 경고 16건은 아래 결함 수정으로 해소)
- 신규 asmdef 참조: `Game.Presentation` ← `Unity.TextMeshPro`, `UnityEngine.UI` / `Game.Editor` ← `Unity.InputSystem.ForUI`, `Unity.TextMeshPro`, `UnityEngine.UI` / `Game.Tests.PlayMode` ← `Unity.TextMeshPro`, `UnityEngine.UI`
- 의존 방향(Core ← Gameplay ← Presentation)은 유지됨 — 아래 "B6을 Presentation에 둔 이유" 참조

## EditMode Tests

- `VERIFIED` — **85 / 85 통과, 실패 0, 스킵 0** (F01·F03·F04·F05·F06 회귀 포함, F07 신규 10)
- F07: `Lose001_ZeroHealth_EndsSessionAsDefeat`, `Lose001_SecondEnd_IsIgnored`, `Lose001_VictoryAndDefeat_SameFrame_VictoryWins`, `Lose001_DefeatAfterVictory_DoesNotOverride`, `Lose001_ElapsedAccumulates_WhileRunning`, `Lose001_ElapsedStops_AfterEnd`, `Lose001_KillCount_AccumulatesWhileRunning`, `Lose001_KillsAfterEnd_AreIgnored`, `Lose001_Start_IsRunningAndEmpty`, `Lose001_EndWithNoOutcome_KeepsSessionRunning`

## PlayMode Tests

- `VERIFIED` — **43 / 43 통과, 실패 0** (F01~F06 회귀 포함, F07 신규 7)
- F07: `Lose001_PlayerDeath_EndsSessionOnce`, `Lose001_RepeatedDeathReports_StillEndOnce`, `Lose001_SessionEnd_StopsTime`, `Lose001_ElapsedFreezes_AtDeath`, `Lose001_SessionEnd_NeutralisesHitStop`, `Lose001_ResultScreen_ShowsAfterDelay`, `Lose001_KillCount_MatchesDealer`, `Lose001_RestoreTime_ResetsTimeScale`
- **씬 리로드 자체는 PlayMode 테스트 범위 밖이다** — 테스트 러너의 씬을 갈아엎기 때문. 리로드 직전 상태(`timeScale` 복구)까지만 테스트하고, 실제 재시작은 아래 런타임 검증에서 확인했다

## Runtime Verification (FirstPlayable.unity, 실제 Play Mode)

플레이어가 드론에게 죽을 때까지 개입 없이 두었고, 전체 루프가 자연 발생했다.

- `VERIFIED` — **B1 패배 전이**: 11.6초 시점 HP 0 → `running=False`, `outcome=Defeat`
- `VERIFIED` — **B2 1회 처리**: `EndReportCount = 1` (매 세션). 사망 이벤트가 여러 번 와도 결과는 한 번
- `VERIFIED` — **B4 생존 시간 고정**: 종료 후 `elapsed`가 11.6에서 더 이상 증가하지 않음
- `VERIFIED` — **B5 시간 정지**: `Time.timeScale = 0`
- `VERIFIED` — **B6 히트스톱 무력화**: 종료 후 `HitStopController.enabled = False`이면서 `timeScale`은 0 유지 — 정지 복구가 세션 정지를 덮어쓰지 않았다
- `VERIFIED` — **B7 결과 화면 + 커서**: `Cursor.lockState = None`, `visible = True`
- `VERIFIED` — **B7 표시 지연**: 3회차에서 사망 직후 샘플링 시 `shown=False`인데 stats는 이미 채워져 있었다 — 0.6초 지연이 실제로 동작
- `VERIFIED` — **B8 결과 내용**: `OUTCOME='DEFEAT'`, `STATS='TIME 10.9s | KILLS 1'`
- `VERIFIED` — **B9 처치 수**: 세션 집계가 `PlayerDamageDealer`의 킬 수와 일치
- `VERIFIED` — **B11/B12 재시작**: `ResultScreen.Retry()` 호출 후 `running=True`, `timeScale=1`, 결과 화면 숨김, 히트스톱 재활성, HP 회복, `kills=0`
- `VERIFIED` — **B12/완료조건 5 — 재시작 3회 연속 누수 없음**:

  | 항목 | 3회 재시작 후 |
  |---|---|
  | GameSession / ResultScreen / EventSystem / DroneSpawner / PlayerHealth | 각 **1개** (중복 생성 없음) |
  | `DroneSpawner.TotalCreated` | **7** = 동시 생존 수. 세션마다 풀이 새로 만들어지고 누적되지 않음 |
  | `Time.timeScale` | 재시작 직후 항상 **1** |
  | `EndReportCount` | 세션마다 **1** (누적되지 않음) |

- `VERIFIED` — **B14 물리·카메라 정지**: 정지 상태에서 **1572프레임** 경과 동안 카메라 회전·위치, 플레이어 위치·속도가 **완전히 동일**. 자체 드리프트 없음
- `VERIFIED` — 세션 전체 새 콘솔 에러 0건

### 증빙 캡처

- [2026-08-01_F07_result_screen.png](../Media/2026-08-01_F07_result_screen.png) — 결과 화면(DEFEAT / TIME 10.9s / KILLS 1 / RETRY)이 표시된 에디터 전체

## 검증 중 발견한 결함 (수정 완료)

### 결과 화면의 한글이 □□로 깨짐 — **자동 테스트가 놓친 실제 결함**

- 증상: 콘솔에 `The character with Unicode value 생 was not found in the [LiberationSans SDF] font asset` 경고가 글자 수만큼 반복. 화면에는 "생존", "처치"가 **□□**로 렌더링
- 원인: TMP 기본 폰트에 한글 글리프가 없다
- **테스트가 못 잡은 이유**: PlayMode 테스트가 `stats.text.Contains("생존")`으로 **문자열**만 확인했다. 문자열은 맞고 **렌더링이 틀린** 경우라 통과했다
- 조치:
  - 표시 문자열을 ASCII로 변경 (`TIME 10.9s` / `KILLS 1`). 한글 전체 아틀라스는 WebGL 50MB 목표(기획서 15장)에 부담이고, First Playable은 그레이박스 단계라 영문으로 충분하다
  - 규칙 **B8에 "표시 문자열은 ASCII로 유지"** 명시
  - 테스트에 **모든 문자가 ASCII 범위인지 검사하는 단언**을 추가 — 누군가 한글을 다시 넣으면 폰트 없이는 실패한다
- 재검증: 글리프 경고 **0건**, 화면 렌더링 육안 확인 (증빙 캡처)

## Manual Verification Required (사람 판단)

- `MANUAL_REQUIRED` 죽는 순간이 읽히는가 — 갑자기 멈춘 느낌인가, 납득되는가 (**지연 0.6초가 적절한가**)
- `MANUAL_REQUIRED` 결과 화면이 한눈에 들어오는가
- `MANUAL_REQUIRED` **다시 하고 싶어지는가** (HYP-006 — 권유 없이 Retry를 누르는가)
- `MANUAL_REQUIRED` **저속 접촉의 손해가 여전히 억울한가** — 플레이테스트 2회차 O6 재판단. 한 판을 완주할 수 있게 됐으므로 이제 판단 가능
- `MANUAL_REQUIRED` Retry가 즉각적인가, 로딩이 느껴지는가
- `MANUAL_REQUIRED` 재시작 직후 조작이 처음과 같게 느껴지는가
- `MANUAL_REQUIRED` **결과 화면에서 마우스를 움직이면 카메라가 도는가** — 아래 참조

## Remaining Risks

- `UNVERIFIED` — **정지 중 마우스 입력에 의한 카메라 회전**: 자체 드리프트가 없다는 것은 확인했지만(1572프레임 동일), **마우스를 실제로 움직였을 때** Cinemachine 입력 축이 반응하는지는 확인하지 못했다. 게임 상태를 바꾸지는 않으므로 B14 위반은 아니지만, 결과 화면에서 배경이 돌면 어색하다. 사람 플레이로 판단 필요
- **한글 UI 폰트 결정이 F13 전에 필요하다** — F07은 ASCII로 우회했지만 F13 HUD(HP·시간·XP·무기)에서 한글이 필요하면 폰트 결정이 선행되어야 한다. 필요한 글자만 담은 정적 SDF 아틀라스 또는 영문 유지 중 택일
- **결과 항목이 아직 2개뿐** — LOSE-001은 최고 연속 처치·도달 레벨·획득 무기도 요구한다. 콤보(F13)·레벨(F12)·무기(F08~F11)가 생길 때 각 기능에서 추가해야 하며, 누락되면 LOSE-001 미충족 상태로 남는다
- **씬 리로드 비용은 지금 씬 기준** — 무기·VFX가 붙어 씬이 무거워지면 Retry 체감이 느려질 수 있다. 그때 제자리 초기화(`DroneSpawner.ResetSession` 경로는 살려둠)로 전환할지 재판단할 것
- **승리 경로는 미구현** — `SessionOutcome.Victory`는 구조와 우선순위 규칙(B3)만 있고 발동 지점이 없다. F13에서 FP-001을 붙일 때 `GameSession.End(Victory)` 호출만 추가하면 된다

## 판정

자동 검증(컴파일·EditMode 85/85·PlayMode 43/43) 전체 `VERIFIED`.
런타임에서 패배 전이·1회 처리·시간 정지·히트스톱 양보·커서 해제·표시 지연·결과 내용·재시작을 씬에서 직접 확인했고, **재시작 3회 연속 후 상태 누수가 없음**(FIRST_PLAYABLE 완료 조건 4)을 계측으로 확인했다.
검증 과정에서 **자동 테스트가 놓친 렌더링 결함 1건**(한글 글리프)을 발견해 수정하고, 재발 방지 단언을 추가했다.
정지 중 마우스 입력에 의한 카메라 회전만 `UNVERIFIED`로 남는다.
Feature Spec 상태를 **Verified (automated)** 로 갱신.
