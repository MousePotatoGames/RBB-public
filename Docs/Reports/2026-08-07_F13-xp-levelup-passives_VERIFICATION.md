# Verification Report — F13 경험치 → 레벨업 → 패시브

- 일시: 2026-08-07
- Feature Spec: [F13-xp-levelup-passives.md](../Features/F13-xp-levelup-passives.md)
- 대상 씬: `Assets/_Game/Scenes/Gameplay/FirstPlayable.unity`
- 미디어: `Docs/Media/2026-08-07_F13-levelup-cards.png`

## Scope

XP-001 오브, XP-002 곡선, LVL-001 정지·카드 선택, PAS-001~004 패시브 3종.
B1~B24 전체와 Done conditions 1~8. 스펙의 1·2단계를 모두 구현한 상태에서 검증했습니다.

## Compilation

| | |
|---|---|
| 새 에러 | **0** |
| 새 경고 | **0** |

남은 경고 1건은 `com.unity.ai.assistant` 패키지의 계정 API 타임아웃으로 이 기능과 무관합니다.
**폰트·글리프 경고 없음**, 누락 참조 경고 없음 — 카드 UI가 ASCII만 쓰기 때문입니다(설계 판단 6).

## EditMode Tests

**218 / 218 통과** (실패 0, 스킵 0). 회귀 포함 전체 실행. F13이 174 → 218로 44개를 더했습니다.

`ProgressLogicTests` 19개 (곡선·경계·자석), `PassiveLogicTests` 21개 (단계·상한·카드 후보·효과 누적),
`HealLogicTests` 4개 (PAS-001 회복).

## PlayMode Tests

**133 / 133 통과** (실패 0, 스킵 0). 105 → 133으로 28개 추가.

## 구현 중 테스트가 잡은 결함 2건

두 건 다 **테스트를 돌리기 전에는 보이지 않았고**, 원인이 서로 반대였습니다.

### 1. 물리 콜백 안의 `DestroyImmediate` — 제품 코드 결함

오브 풀이 기존 풀 3개의 `CreatePrimitive` + `DestroyImmediate(collider)` 패턴을 복사했습니다.
그 패턴은 **`FixedUpdate`에서 생성될 때만** 성립하는데, 오브는 **처치 이벤트
(= `OnCollisionStay` 안)** 에서 생성되므로 Unity가 거부합니다.

> 패턴을 복사하면서 **패턴이 성립하는 조건은 복사되지 않았습니다** — 그 조건이 코드 어디에도
> 적혀 있지 않았기 때문입니다.

콜라이더를 **애초에 만들지 않도록** 고쳤습니다 (메시·머티리얼을 한 번 캐시 → 빈 GameObject).
파괴할 것이 없으면 파괴 시점 문제도 없습니다.

### 2. 프레임 수로 기다린 테스트 — 계기 결함

오브가 흡수되지 않아 코드를 의심했습니다. 단언 메시지에 상태를 실어 재측정하니:

```text
[timeScale=1 dt=0.0023 player=(0,0.5,0) orb=(0,0.5,0.64) chasing=True]
```

**한 프레임이 2.3ms.** 테스트 러너는 렌더링을 안 하고 프레임 상한도 없어 초당 400프레임 넘게 돕니다.
60프레임은 1초가 아니라 **0.14초**였고, 초당 9m로 오는 오브가 도착하지 못했을 뿐입니다.
코드는 정상이었습니다.

> **계기를 먼저 의심한 판단이 이번에도 맞았습니다.** 다만 그 판단만으로는 부족했고,
> **단언 메시지에 판정에 쓴 상태를 실어 재측정한 것**이 5분 만에 답을 줬습니다.

둘 다 [WORKFLOW_PITFALLS.md](../Process/WORKFLOW_PITFALLS.md) B5·B6에 기록했습니다.

## 구현 중 코드를 읽다 찾은 충돌 2건

테스트가 아니라 **읽다가** 찾았습니다. `Time.timeScale`과 커서를 **이미 소유한 컴포넌트가 둘 더** 있었습니다.

| 컴포넌트 | 충돌 | 증상 |
|---|---|---|
| `HitStopController` | 0.05초 정지 만료 시 `timeScale = 1` | 카드 화면 뒤에서 만료되면 **정지가 조용히 풀린다** |
| `CursorLockController` | 좌클릭마다 커서 재잠금 | **카드를 고르는 그 클릭이 마우스를 빼앗는다** |

둘 다 이미 *"세션이 끝나면 소유권을 넘긴다"* 는 패턴을 갖고 있어서, 레벨업에도 같은 양보를
붙이는 것으로 끝났습니다. 새 구조가 필요 없었습니다.
회귀 테스트 `Lvl001_ExpiringHitStop_DoesNotResumeTheCardScreen`이 첫 번째를 고정합니다.

## Runtime Verification

### 측정 조건 (실제 플레이와 다른 점)

| 항목 | 이번 측정 | 실제 플레이 |
|---|---|---|
| 입력 | **없음** — Rigidbody에 속도를 직접 넣어 드론에 돌진 | WASD 연속 입력 |
| 적 | 세션 1은 정상(6~8기), **세션 2는 스포너 비활성** | 8기까지 증가 |
| 경험치 | 세션 1은 **실제 처치로 획득**, 세션 2는 `AddExperience` 직접 호출 | 전부 처치로 획득 |
| 카드 선택 | **`director.Choose(index)` 직접 호출** | 키보드 1/2/3 또는 마우스 클릭 |

세션 2에서 적을 끈 이유는 R1입니다 — 세션 1이 **13.9초에 사망**해 카드 화면을 볼 창이
없었습니다(아래 R1 참조).

### 세션 1 — 실제 처치로 얻은 경험치 (적 정상)

프레임 전진 확인: `frameCount` 1455 → 4407 (`runInBackground` 동작).

```text
kills=1 | orbs: drops=1 collects=1 live=0 created=1 | xp=1.0 lv=1
```

`created=1`이 `drops=1`과 같은 것은 풀의 첫 생성이므로 정상입니다.

### 세션 2 — 카드 화면 (적 비활성)

```text
자석 선택성 — 1.5m 오브: 흡수됨 / 9.0m 오브: chasing=False, 제자리
                remaining orb collider=False rb=False

레벨업      — xp 15 → lv2, open=True, timeScale=0, candidates=3, shown=True
              card[0] ARMOR PLATING  stage=0 | MAX HP +20 / HEAL 20
              card[1] DENSE CORE     stage=0 | SPEED x1.08 / DAMAGE x1.15
              card[2] MAGNET FIELD   stage=0 | RANGE x1.60 / XP x1.25

MAGNET 선택 — timeScale=1, open=False, magnetStage=1
              흡수 반경 2.50 → 4.00, xp배율 1.25, 속도·피해 배율 1.00 (미획득)

ARMOR 선택  — 최대 HP 100 → 120, 현재 HP 60 → 80  (증가분만큼 즉시 회복)

세션 종료   — orbsLive 1 → 0
              종료 후 AddExperience(200) → lv5, 그러나 open=False, opens=2 (불변), timeScale=0 유지
```

### 카드 렌더링 (시각 확인)

`Docs/Media/2026-08-07_F13-levelup-cards.png` — 세 장 모두 **글리프 누락 없이 렌더**됩니다.
문자열 단언이 아니라 실제 화면을 확인했습니다.

### 규칙별 판정

| 규칙 | 판정 | 근거 |
|---|---|---|
| B1 사망 위치에 오브 드랍 | `VERIFIED` | 실제 처치 1회 → `drops=1` |
| B2 처치당 1개 | `VERIFIED` | `KillCount=1`, `DropCount=1` |
| B3 반경 안에서만 끌림 | `VERIFIED` | 1.5m 흡수 / 9.0m `chasing=False` 제자리 |
| B4 흡수 → 경험치 | `VERIFIED` | `collects=1`, `xp=1.0` |
| B5 콜라이더 없음 | `VERIFIED` | 런타임 `collider=False rb=False` |
| B6 세션 종료 시 회수 | `VERIFIED` | `orbsLive 1 → 0` |
| B7 요구량 도달 시 레벨업 | `VERIFIED` | 15 XP → Lv2, 47.25 → Lv3 |
| B8 한 번에 두 레벨 | `VERIFIED` (PlayMode) | 런타임에서는 한 칸씩만 관측 |
| B9 최고 레벨에서 정지 | `VERIFIED` | 종료 후 247 XP → Lv5 (곡선 상한) |
| B10 게임 시간 완전 정지 | `VERIFIED` | 카드 표시 중 `timeScale=0` |
| B11 키보드 1/2/3 | `VERIFIED` (2026-10-12, 시뮬레이션 이벤트) | `QueueStateEvent`로 Digit1/2/3 → 해당 카드 단계 +1 · 다음 카드 열림 · 마지막에 재개. [추가 검증](#추가-검증--b11-키보드-경로-2026-10-12) |
| B11 마우스 클릭 | `UNVERIFIED` | 여전히 Manual |
| B12 선택 시 재개 | `VERIFIED` | 선택 직후 `timeScale=1` |
| B13 레벨당 이벤트 1회 | `VERIFIED` | `OpenCount`가 레벨당 1씩 증가 |
| B14 정지 중 unscaled | `VERIFIED` (PlayMode) | 히트스톱 회귀 테스트가 실시간으로 대기 |
| B15 종료 후 카드 없음 | `VERIFIED` | 종료 후 Lv5까지 올려도 `open=False`, `timeScale=0` 유지 |
| B16 밀린 레벨업 순차 처리 | `VERIFIED` (PlayMode) | `Lvl001_TwoLevelsAtOnce_ShowsCardsTwice` |
| B17 단계 강화·상한 3 | `VERIFIED` | 런타임 stage 1 관측 + EditMode 상한 |
| B18 **SO 에셋 불변** | `VERIFIED` | 패시브 2종 보유 상태에서 `playerMaxHealth=100 maxSpeed=12 magnetRadius=2.50 droneXp=1.0` |
| B19 3단계는 카드 제외 | `VERIFIED` (PlayMode) | `Pas004_MaxedPassive_LeavesTheCardPool` |
| B20 후보 없으면 정지 없음 | `VERIFIED` (PlayMode) | `Pas004_AllMaxed_DoesNotStopTime` |
| B21 최대 HP + 즉시 회복 | `VERIFIED` | 100 → 120, HP 60 → 80 |
| B22 속도·피해 배율 | `INFERRED` | 배율 계산은 테스트로 확인. **실제로 공이 더 빨리 구르는 것은 미관측** (입력이 없어 최고 속도에 도달하지 않음) |
| B23 흡수 반경·경험치 배율 | `VERIFIED` | 반경 2.50 → 4.00, 배율 1.25 |
| B24 재시작 시 초기화 | `VERIFIED` | 플레이 종료 후 `xp=0 lv=1 stages=0` |

## Diagnostic Changes Restored

전부 **런타임 전용**이라 플레이 모드 종료로 되돌아가며, 종료 후 되읽어 확인했습니다:

| 진단 변경 | 복원 확인 |
|---|---|
| `DroneSpawner.enabled = false` + 활성 드론 Despawn | `spawnerEnabled = True` |
| `Rigidbody.linearVelocity` 직접 주입 (12 m/s) | 씬 재로드로 초기화 |
| `PlayerHealth.ResetHealth()` / `TakeDamage(40)` | 상태 아닌 호출 — `xp=0 lv=1 stages=0` |
| `AddExperience` 직접 호출 (15 / 25 / 200) | 위와 같음 |
| `Choose(index)` 직접 호출 | `directorOpen=False` |

- 씬 더티: `sceneDirty = False`
- `Time.timeScale = 1`
- **ScriptableObject 에셋 전부 원값** — 이건 B18이 요구하는 것이기도 해서 두 번 확인했습니다

⚠ NOT RESTORED: 없음.

## Manual Verification — 사용자 플레이 결과 (2026-08-07)

사용자 플레이 후 보고: **"잘 된다"** → `VERIFIED (user-reported)`, 단 **전체 인상에 한합니다.**

아래 개별 항목은 **하나씩 확인받지 않았습니다.** 특히 다음 두 가지는 여전히 열려 있습니다:

- **90초에 레벨업이 몇 번 오는가** — XP-002의 90초 검증 기준이 아직 비어 있고,
  이 숫자로만 채울 수 있습니다
- **단계 표시 `[...]`의 가독성** — 스크린샷 기준으로 제기한 우려이며 판정받지 않았습니다

전체가 잘 돌아간다는 보고는 **루프가 막히지 않았다**는 뜻이므로, 카드 선택 입력(B11)이
동작한다는 간접 근거는 됩니다 — 카드가 뜬 채로 입력이 안 먹으면 게임이 멈춘 채 끝나기
때문입니다. 다만 **레벨업에 도달했는지 자체를 확인받지 않았으므로 B11은 `UNVERIFIED`로 둡니다.**

## Manual Verification Required

**입력 경로 (B11) — 계기가 건드리지 못한 유일한 코드 경로**
- ~~키보드 1 / 2 / 3으로 카드가 선택되는가~~ → 2026-10-12 자동 검증 ([추가 검증](#추가-검증--b11-키보드-경로-2026-10-12)). 실제 키보드로 한 번 눌러보는 것만 남음
- **마우스 클릭으로 카드가 선택되는가** (커서 양보가 실제로 동작하는가)

**Feature Spec의 Manual play checks**
- **첫 레벨업이 언제 오는가** — 90초에 몇 번 오르는가 (XP-002의 미측정 항목을 채웁니다)
- 오브가 빨려 들어오는 게 보이는가, 반경 2.5m가 적절한가
- 정지가 답답하지 않은가 — 90초에 3~4번 멈추면 흐름이 끊기는가
- **영문 카드를 0.2초 안에 읽고 고를 수 있는가** (설계 판단 6이 미룬 유일한 비용)
- 같은 패시브를 세 번 고른 것과 나눠 고른 것의 차이가 느껴지는가
- 강화 외피로 더 오래 버티는 게 체감되는가
- **고밀도 코어로 럼블 단계에 더 쉽게 드는가** (설계 판단 3의 의도 — 계기로는 미관측)
- 자기장 3단계의 눈덩이가 재미있는가

**이번 검증에서 새로 생긴 항목**
- **단계 표시 `[...]`가 "0단계"로 읽히는가** — 스크린샷에서 stage 0이 `[...]`로 나오는데,
  **말줄임표처럼 보입니다.** 값 자체는 정확하지만(점 3개 = 0단계) 기호 선택이 모호할 수 있습니다.
  읽히지 않으면 `[ㆍㆍㆍ]`나 빈 칸 대신 다른 기호로 바꿔야 합니다

## Remaining Risks

**R1 (F09→F13 이월, 네 번째)** — 입력이 없으면 이번에도 **13.9초에 사망**해, 카드 화면을 보려고
적을 꺼야 했습니다. **네 번 연속 같은 벽입니다.**

> 다만 이건 여전히 **계기의 문제일 뿐일 수 있습니다.** F09 플레이테스트에서 직접 해보시고
> "잘 된다"고 하셨습니다. 실제로 플레이하실 때 첫 레벨업까지 살아남는지가 판정 기준입니다.
> **F13 플레이테스트가 R1을 확정하거나 기각합니다** — XP-002의 90초 기준을 재는 것과
> 같은 세션에서 답이 나옵니다.

**R2 (신규)** — 레벨업 카드 캔버스는 `sortingOrder = 90`, 결과 화면은 100입니다.
정렬 자체는 맞지만, **둘이 동시에 뜨는 경우는 코드로 막았을 뿐**(B15) 렌더 순서로 막지
않았습니다. 지금은 안전하지만 순서에 의존하지 않는다는 점은 기록해 둡니다.

**R3 (이월)** — B22의 속도 증가가 **실제 주행에서 체감되는지 미관측**입니다.
입력이 없으면 최고 속도 근처에 가지 않아 배율이 드러나지 않습니다. 사람이 몰아야 보입니다.

**R4 (이월, W8)** — Web 스모크 빌드의 프레임 측정이 **F11·F12에 이어 F13까지 미이행**입니다.
오브 풀까지 얹혔으므로 예산이 또 달라졌습니다. `DEVELOPMENT_ORDER.md`에 적어둔 대로
**F14 전에 한 번 재야 합니다** — HUD가 올라가면 무엇이 예산을 먹었는지 분리하기 어려워집니다.

## 결론

자동 검증 항목 **전부 통과**했고, 런타임에서 루프 전체(처치 → 오브 → 흡수 → 정지 → 카드 →
재개 → 효과)를 관측했습니다. Feature Spec 상태를 `Verified (automated)`로 올립니다.

**계기가 건드리지 못한 것은 입력 경로 하나입니다** — 키보드 1/2/3과 마우스 클릭.
그리고 *레벨업이 90초에 몇 번 오는 게 맞는가*는 처음부터 사람이 답할 질문이었습니다.

---

## 추가 검증 — B11 키보드 경로 (2026-10-12)

`LevelUpScreen.Update`의 `Keyboard.current.digitNKey.wasPressedThisFrame` → `Choose(index)` 경로.
지난 검증이 건너뛴 앞단입니다. Unity CLI(`unity command`)로 라이브 에디터를 조작했습니다.

### 측정 조건

| 항목 | 이번 측정 | 실제 플레이 |
|---|---|---|
| 레벨업 | `PlayerProgress.AddExperience(5)` × 13 → Lv1→4, 카드 3장 대기 | 처치로 획득 |
| 키 입력 | `InputSystem.QueueStateEvent(Keyboard.current, KeyboardState(Key.DigitN))` → 0.5초 뒤 전체 해제 | 물리 키보드 |
| 판정 | **결과로**: 해당 순번 카드의 패시브 단계, 다음 카드 열림, `timeScale` | — |

`QueueStateEvent`는 Input System이 공식 테스트에서 쓰는 방식으로, 하드웨어 이벤트와 같은 이벤트
큐·업데이트를 탑니다. 키 이벤트가 `Dynamic` 업데이트에서 "막 눌림"으로 잡히는 것을 매번 로그로 확인했습니다.

### 결과

| 입력 | 막 눌림 감지 | 결과 |
|---|---|---|
| Digit4 (대조) | 1회 | **변화 없음** — 카드 그대로 (감지는 됐으므로 "입력이 안 들어감"이 아니라 무시한 것) |
| Digit1 | 1회 | ArmorPlating 0→1, 다음 카드 열림 (B16), 대기 3→2 |
| Digit2 | 1회 | DenseCore 0→1, 다음 카드 열림, 대기 2→1 |
| Digit3 | 1회 | MagnetField 0→1, 카드 닫힘, `timeScale` 0→1 (B12) |

콘솔 에러 0건. 카드 화면은 CLI의 `capture_game_view`로 찍었습니다 —
오버레이 UI까지 담깁니다: `Docs/Media/2026-10-12_F13-B11-card-screen.png`

### 같은 날 겪은 함정 — `simulate_key`로는 이 경로를 못 연다

처음에는 CLI의 `simulate_key`(`press`, `down`/`up` 모두)로 시도했는데 **카드가 한 번도 선택되지 않았습니다.**
결론을 내기 전에 원인을 갈랐습니다:

- 키는 `Keyboard.current`에 정확히 들어감 — 누르는 동안 `digit1Key.isPressed=True` (키보드는 1개)
- 그런데 99번의 업데이트 동안 **`wasPressedThisFrame`이 한 번도 True가 아니었고**, 놓을 때의
  `wasReleasedThisFrame`은 정상 감지

즉 `simulate_key`(com.unity.pipeline 0.8.0-exp.1)의 key-down은 "눌린 상태"는 만들지만
"막 눌림" 엣지를 만들지 못합니다. 같은 코드가 `QueueStateEvent`로는 매번 동작했으므로 게임 결함이 아닌
**도구 한계**로 판정했습니다. 계기만 보고 "B11 고장"이라 적었다면 F02·F06 때와 같은 실수였을 것입니다.

