# Feature: F13 경험치 → 레벨업 → 패시브

## Status

**Implementing — 1·2단계 구현 완료.** EditMode 218/218, PlayMode 133/133, 새 콘솔 에러 0건.
`/first-playable:verify F13-xp-levelup-passives` 대기.
스펙 승인: 2026-08-07 (카드 텍스트 ASCII 결정 포함)

> **구현 중 고친 결함 2건** (둘 다 테스트가 잡았습니다):
> 1. **물리 콜백 안의 `DestroyImmediate`** — 오브 풀이 기존 풀 3개의 `CreatePrimitive` +
>    `DestroyImmediate(collider)` 패턴을 복사했는데, 그 패턴은 `FixedUpdate`에서만 성립합니다.
>    오브는 **처치 이벤트(= `OnCollisionStay` 안)** 에서 생성되므로 Unity가 거부합니다.
>    콜라이더를 **애초에 만들지 않도록** 바꿨습니다 (메시만 캐시 → 빈 GameObject에 얹음).
> 2. **테스트가 프레임 수로 기다렸음** — 테스트 러너는 프레임 상한이 없어 한 프레임이 2.3ms입니다.
>    60프레임은 1초가 아니라 **0.14초**였고, 초당 9m로 오는 오브가 도착하지 못했습니다.
>    코드가 아니라 계기가 틀렸습니다 — 단언 메시지에 상태를 실어 재측정해 5분에 찾았습니다.
>
> 둘 다 [WORKFLOW_PITFALLS.md](../Process/WORKFLOW_PITFALLS.md) B5·B6에 기록했습니다.

> **구현 중 찾은 충돌 2건** (테스트 전에 코드를 읽다 발견):
> `Time.timeScale`과 커서를 **이미 소유한 컴포넌트가 둘 더 있었습니다.**
> `HitStopController`는 0.05초 정지가 끝나면 `timeScale`을 1로 되돌리는데, 그게 레벨업 카드 뒤에서
> 만료되면 **카드 화면을 조용히 재개**시킵니다. `CursorLockController`는 좌클릭마다 커서를
> 다시 잠그므로 **카드를 고르는 그 클릭이 마우스를 빼앗습니다.**
> 둘 다 이미 "세션 종료 시 소유권을 넘긴다"는 패턴을 갖고 있어, 레벨업에도 같은 양보를 붙였습니다.
> 회귀 테스트 `Lvl001_ExpiringHitStop_DoesNotResumeTheCardScreen`이 첫 번째를 고정합니다.

## Purpose

지금까지 90초 동안 **플레이어는 변하지 않았습니다.** 무기가 붙어 화력이 늘 뿐,
공 자체는 0초와 89초가 똑같습니다. 이 기능이 **세션 안에서 성장하는 축**을 만듭니다.

무기(WPN)는 *바깥에서 붙는* 성장이고, 패시브(PAS)는 *안에서 바뀌는* 성장입니다.
둘이 같이 있어야 로그라이트의 "이번 판은 이렇게 굴러갔다"가 성립합니다.

동시에 이 기능은 **F14(HUD)가 표시할 데이터를 만듭니다.** 순서가 이쪽이 먼저인 이유입니다.

## Player's perspective

적을 부수면 **경험치 오브가 떨어지고**, 가까이 가면 빨려 들어옵니다.
일정량이 모이면 **화면이 멈추고 카드 3장**이 뜹니다. 하나를 고르면 바로 재개됩니다.

같은 카드를 계속 고르면 그 방향으로 뾰족해지고, 나눠 고르면 두루뭉술해집니다.
**90초 안에 어떤 공이 되어 나올지가 매 판 달라집니다.**

## Referenced rules

- **XP-001 경험치 오브** — 적 처치 시 드랍, 일정 거리에서 흡수, `ExperienceCollected` 발생, 풀링. **중복 지급 금지**
- **XP-002 초기 경험치 곡선** — 드론 1 XP / 레벨 요구량 누적 15·35·65·105 (전부 TEMPORARY)
- **LVL-001 레벨업** — 시간 정지 → 카드 3장 → 1/2/3 + 마우스 선택 → 0.2초 이내 재개. `PlayerLevelUp` 발생. **정지 중 게임 시간은 완전히 멈춘다**
- **PAS-001 강화 외피** — 최대 HP 증가 + 일부 즉시 회복
- **PAS-002 고밀도 코어** — 이동속도·충돌 피해 증가
- **PAS-003 자기장 증폭** — 획득 범위·경험치 배율 증가
- **PAS-004 중복 선택과 상한** — 다시 고르면 강화, **상한 3단계**, 3단계는 카드에서 제외
- **DMG-001** — 충돌 피해의 `passiveMultiplier` (F06이 이미 자리를 비워둠)
- **HP-001** — 최대 HP와 무적 시간
- **SPD-001** — 속도 4단계 (설계 판단 3에서 충돌)
- 기획서 15장 — 오브 풀링, 매 프레임 Find/GetComponent 금지

## In scope

- `Game.Core`: **`ExperienceLogic`** — 누적 XP → 레벨, 요구량 곡선, 다중 레벨업
- `Game.Core`: **`PassiveState` / `PassiveLogic`** — 단계 누적, 상한, 카드 후보 산출, **실효 수치 계산**
- `Game.Core`: **`MagnetLogic`** — 흡수 반경 판정과 끌림 (콜라이더 없음)
- `Game.Gameplay`: **`ExperienceOrb` / `ExperienceOrbPool`** — 드랍·흡수·풀링
- `Game.Gameplay`: **`PlayerProgress`** — XP 누적, 레벨업 큐, 패시브 보유 상태의 단일 소유자
- `Game.Gameplay`: **`LevelUpDirector`** — 시간 정지·재개, 카드 후보 전달
- `Game.Gameplay`: 기존 소비자에 **수정자 연결** 5곳 (아래 Architecture)
- `Game.Presentation`: **`LevelUpScreen`** — 카드 3장, 키보드 1/2/3 + 마우스
- `Game.Gameplay`: **`PassiveDefinition`** SO 3종 에셋 + `ProgressConfig` SO 1종
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- **HUD (XP 바, 레벨 표시, 무기 슬롯 아이콘)** — F14. F13은 콘솔 로그로 확인합니다 (F05~F12와 같은 방식)
- **승리 조건** — F14
- **오브 원거리 합산** — XP-001이 *"합산 가능"* 이라고 허용했을 뿐 요구하지 않습니다.
  FP는 동시 적 8기 상한이라 합칠 오브가 안 쌓입니다. 쓸 곳이 생기면 그때
- **PAS의 "강화 방향" 효과** (피해 감소 / 대시 쿨다운 / 흡수 시 가속) — 설계 판단 5
- **레벨업 연출** (카드 등장 애니메이션, 정지 전환) — P08
- **오브 아트·흡수 트레일** — P07/P08. 그레이박스 프리미티브
- 패시브 4종 이상, 무기와 패시브의 상호작용 — MVP

## 설계 판단 여섯 가지

### 1. 패시브는 ScriptableObject 에셋을 절대 수정하지 않습니다

이게 이 기능에서 **가장 큰 함정**입니다.

플레이어의 튜닝값이 전부 공유 SO 에셋에 있습니다 — `DroneConfig.playerMaxHealth`,
`BallMovementConfig.maxSpeed`, `WeaponConfig.pickupRadius`. PAS-001이 소박하게
`config.playerMaxHealth += 20`을 하면:

- 플레이 모드를 나가도 **에셋 파일에 그대로 남습니다** (런타임 컴포넌트 값과 달리 SO는 되돌아가지 않습니다)
- 다음 판이 버프된 상태로 시작합니다
- **아무도 안 만진 config 변경이 git diff에 뜹니다**

> F12 검증 때 진단용으로 SO를 고치지 않고 피해 간 것이 정확히 이 이유였습니다.
> 그때는 제가 조심해서 피했지만, 이번엔 **기능 자체가 그 길로 걸어 들어갑니다.**

그래서 패시브는 **수정자 계층**입니다. `PassiveState`가 단계만 들고 있고,
소비자가 실효값을 물어봅니다:

```csharp
// 나쁨 — 에셋이 오염됨
config.playerMaxHealth += 20f;

// 좋음 — 기준값은 그대로, 실효값만 계산
public float Max => PassiveLogic.MaxHealth(config.playerMaxHealth, _passives);
```

**B18이 이 판단을 테스트로 고정합니다** — 패시브 3단계를 다 찍은 뒤
SO 필드가 처음 값 그대로인지 검사합니다.

### 2. 카드 추첨 로직이 필요 없습니다

패시브 3종에 카드 3장입니다. **고를 게 3개고 보여줄 자리가 3개면 추첨할 것이 없습니다.**
`WeaponDrawLogic` 같은 무중복 추첨을 만들 이유가 없고, 매번 같은 3장이 뜹니다.

이건 결함이 아니라 FP 범위의 사실입니다. 플레이어의 선택은
*"랜덤 3개 중 뭘 고를까"* 가 아니라 **"내 세 축 중 뭘 키울까"** 입니다.
패시브가 4종 이상이 되는 MVP에서 추첨이 처음 의미를 갖습니다.

필요한 건 추첨이 아니라 **필터**입니다 — 3단계 도달분을 빼는 것 (PAS-004 Exception).

### 3. PAS-002의 속도 증가는 SPD-001 단계 기준선을 올리지 않습니다

`SpeedTierTracker`는 `PlanarSpeed / config.maxSpeed`로 단계를 정합니다.
PAS-002가 최대 속도를 올리면 **분모도 같이 올릴 것인가**가 갈림길입니다.

| | 분모도 올림 | **기준선 고정 (채택)** |
|---|---|---|
| 럼블 진입 난이도 | 그대로 | **쉬워짐** |
| 성장의 체감 | 빨라졌는데 **화면은 똑같음** | 색·트레일이 더 자주 바뀜 |
| SPD-001의 의미 | "내 최대치 대비" | **"절대적으로 얼마나 빠른가"** |

**기준선을 고정합니다.** 속도 단계는 성장의 눈에 보이는 보상이어야 합니다.
분모가 같이 올라가면 *성장했는데 아무것도 안 변한 것처럼 보입니다* — 성장 시스템의
목적과 정면으로 충돌합니다.

> 이건 SPD-001을 해석한 것이지 바꾼 게 아닙니다. 규칙은 분모를 명시하지 않았고,
> 지금까지 소비자가 하나뿐이라 물어볼 일이 없었습니다. **필요한 규칙 갱신**에 올립니다.

### 4. 충돌 피해 훅은 이미 있습니다

```csharp
// PlayerDamageDealer.cs:101
passiveMultiplier: 1f, // F12 will feed this
```

F06이 `DamageLogic.CollisionDamage`에 자리를 비워두고 주석까지 남겨놨습니다
(당시 번호로 F12, 지금 번호로 F13). **PAS-002의 충돌 피해는 인자 하나를 채우면 끝납니다.**

> Decision 0002의 "공격 코드 추가 0" 예측은 절반이 틀렸는데(F12), 이 예측은 맞았습니다.
> 차이는 **F06이 구조를 미리 만든 게 아니라 경계에 구멍만 뚫어뒀다는 점**입니다.
> F12에서 얻은 교훈 — *"구조를 미리 정하는 것보다 경계 조건을 미리 정하는 것이 수익률이 높다"* —
> 의 실증 사례입니다.

### 5. FP에서는 단계 효과를 선형 누적합니다 (PAS-004에서 이탈)

PAS-004는 *"1단계: 기본 효과, 2~3단계: '강화 방향' 효과 누적"* 이라고 씁니다.
강화 방향은 각각 **피해 감소**(PAS-001) / **대시 재사용 감소**(PAS-002) /
**흡수 시 짧은 가속**(PAS-003)입니다.

**FP에서는 만들지 않습니다.** 세 가지 다 새 시스템입니다 — 피해 감소는 `HealthLogic`에
저항 항을, 대시 쿨다운은 `DashLogic`에 배율을, 흡수 가속은 오브에 속도 부여를 요구합니다.
패시브 하나당 효과 삽입점이 **5개에서 8개로** 늘어납니다.

대신 **1단계 효과를 단계마다 그대로 더합니다.** 3단계 강화 외피 = 최대 HP +60.
플레이어가 체감하는 것 — *"고를수록 세진다"* — 은 그대로 성립합니다.

> **이건 규칙에서 벗어나는 것이므로 숨기지 않습니다.** F11의 Hitscan, F12의 콜라이더에
> 이어 세 번째입니다. 규칙을 고치는 게 아니라 **FP 범위에서의 축소임을 PAS-004에 명시**합니다.

### 6. 카드 텍스트는 ASCII입니다 (2026-08-07 결정)

`ResultScreen`이 이미 이유를 적어놨습니다:

> ASCII only: the default TMP font has no Hangul glyphs, and shipping a Korean atlas
> costs far more than the 50MB WebGL budget allows (기획서 15장)

같은 규칙을 카드에도 적용하면 `ARMOR PLATING` / `DENSE CORE` / `MAGNET FIELD`가 됩니다.

**다만 결과 화면과 카드는 부담이 다릅니다.** 결과 화면은 `TIME` / `KILLS` 두 단어지만,
카드는 **효과를 읽고 판단해야 하는 화면**입니다. 한국어 사용자에게 영문 카드를 읽히는 건
0.2초 안에 고르라는 LVL-001과 어울리지 않습니다.

**대안도 검토했습니다**: TMP 정적 아틀라스에 **실제로 쓰는 한글 음절만** 넣으면
(카드 3장 + HUD 라벨 = 대략 40자) 폰트 전체가 아니라 **수십 KB**입니다.
"한글 아틀라스는 비싸다"는 건 **동자 아틀라스나 전체 음절 기준**이고, 서브셋은 해당하지 않습니다.

**→ 사용자 결정 (2026-08-07): ASCII.** 자유 라이선스 한글 TTF를 프로젝트에 넣는 단계가
막힘 없이 진행되는 것보다 값어치가 낮다는 판단입니다. 카드는 이렇게 나갑니다:

```text
┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│ ARMOR PLATING│ │  DENSE CORE  │ │ MAGNET FIELD │
│   [|..]      │ │   [||.]      │ │   [...]      │
│ MAX HP  +20  │ │ SPEED  x1.08 │ │ RANGE  x1.6  │
│ HEAL     20  │ │ DAMAGE x1.15 │ │ XP     x1.25 │
│     [1]      │ │     [2]      │ │     [3]      │
└──────────────┘ └──────────────┘ └──────────────┘
```

단계 표시는 `[|..]` 파이프 3칸으로, 숫자보다 한눈에 읽힙니다.
**F14 HUD도 이 결정을 물려받습니다** — 한글이 필요해지면 카드와 HUD를 한 번에 교체합니다.

> 남는 비용은 기록해 둡니다: **한국어 사용자가 0.2초 안에 영문 카드를 읽고 판단해야 합니다.**
> Manual play checks에 판정 항목으로 넣었습니다. 실제로 느리면 그때 서브셋 아틀라스로 갑니다.

## Behavior rules

### 경험치 오브 (XP-001)

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 적이 죽으면 **그 위치에** 경험치 오브가 드랍된다 | XP-001 |
| B2 | 한 번의 처치에 오브는 **한 번만** 드랍된다 | XP-001 Exception |
| B3 | 오브는 **흡수 반경 안**에 들어오면 플레이어 쪽으로 끌려온다 | XP-001 |
| B4 | 플레이어에 닿으면 흡수되고 **`ExperienceCollected`** 가 발생한다 | XP-001 |
| B5 | 오브는 **콜라이더를 갖지 않는다** — 거리 판정 | F09~F12 일관성 |
| B6 | 오브는 **풀링**되며, 세션 종료 시 전부 회수된다 | XP-001, WPN-007 Exception과 같은 이유 |

### 곡선과 레벨 (XP-002)

| # | 규칙 | 근거 |
|---|---|---|
| B7 | 누적 XP가 요구량에 도달하면 레벨이 오른다 | XP-002 |
| B8 | 한 번의 흡수로 **두 레벨을 넘길 수 있으면 레벨업이 두 번** 일어난다 | XP-002 |
| B9 | 최고 레벨 이후의 XP는 누적만 되고 레벨업을 만들지 않는다 | XP-002 (곡선 유한) |

### 레벨업 (LVL-001)

| # | 규칙 | 근거 |
|---|---|---|
| B10 | 레벨업 시 **게임 시간이 완전히 멈춘다** (`timeScale = 0`) | LVL-001 Exception |
| B11 | 카드가 표시되고 **키보드 1/2/3 또는 마우스 클릭**으로 하나를 고른다 | LVL-001 |
| B12 | 선택 즉시 시간이 재개된다 (**0.2초 이내**) | LVL-001 |
| B13 | **`PlayerLevelUp`** 이 레벨당 정확히 한 번 발생한다 | LVL-001 |
| B14 | 정지 중 UI 입력·타이머는 **unscaled time**으로 동작한다 | B10의 필연 |
| B15 | **세션이 끝난 뒤에는 레벨업 화면이 뜨지 않는다** | 결과 화면과 충돌 금지 |
| B16 | 레벨업이 밀려 있으면 **한 장씩 순서대로** 처리한다 | B8의 필연 |

### 패시브 (PAS)

| # | 규칙 | 근거 |
|---|---|---|
| B17 | 같은 패시브를 다시 고르면 **다음 단계**로 강화된다, 상한 **3** | PAS-004 |
| B18 | 패시브는 **ScriptableObject 에셋을 수정하지 않는다** | 설계 판단 1 |
| B19 | 3단계 도달한 패시브는 **카드에서 제외**된다 | PAS-004 Exception |
| B20 | 고를 카드가 하나도 없으면 **정지 없이** 넘어간다 | PAS-004 Exception |
| B21 | PAS-001: **최대 HP 증가 + 증가분만큼 즉시 회복** | PAS-001 |
| B22 | PAS-002: **최대 속도 배율 + 충돌 피해 배율**. 속도 단계 기준선은 안 바뀐다 | PAS-002, 설계 판단 3 |
| B23 | PAS-003: **흡수 반경 배율 + 경험치 배율** | PAS-003 |
| B24 | 재시작하면 패시브가 **전부 초기화**된다 | LOSE-001 (상태 누수 금지) |

## Architecture

```text
Game.Core (엔진 참조 없음)
├─ ExperienceLogic   # LevelFor(totalXp, curve)                       (B7~B9)
│                    # PendingLevelUps(before, after)                 (B8, B16)
├─ PassiveState      # 3종 단계 배열 + 상한                             (B17)
├─ PassiveLogic      # Upgrade / Candidates / 실효값 계산               (B18~B23)
│                    # MaxHealth·SpeedMultiplier·DamageMultiplier
│                    # ·MagnetRadius·ExperienceMultiplier
└─ MagnetLogic       # InRange / Step (끌림)                           (B3, B5)

Game.Gameplay
├─ ProgressConfig       # XP 곡선·오브·자석 튜닝 SO
├─ PassiveDefinition    # 패시브 1종의 표시명·단계별 수치 SO
├─ ExperienceOrb        # 자체 운동 + 흡수 (콜라이더 없음)                (B3~B5)
├─ ExperienceOrbPool    # ObjectPool + 세션 종료 회수                    (B6)
├─ PlayerProgress       # XP·레벨·패시브의 단일 소유자                    (B7~B9, B13, B17)
└─ LevelUpDirector      # 정지·재개, 카드 후보 전달                       (B10~B16, B20)

Game.Presentation
└─ LevelUpScreen        # 카드 3장 + 1/2/3 + 마우스                      (B11, B14)
```

### 기존 코드에 뚫는 구멍 5개 (설계 판단 1의 실제 적용)

| 소비자 | 지금 | 바뀐 뒤 |
|---|---|---|
| `PlayerHealth.Max` | `config.playerMaxHealth` | `PassiveLogic.MaxHealth(base, state)` |
| `BallMotor` | `config.ToMoveConfig()` | maxSpeed에 배율 적용 |
| `PlayerDamageDealer` | `passiveMultiplier: 1f` | `PassiveLogic.DamageMultiplier(state)` |
| `ExperienceOrb` (신규) | — | `PassiveLogic.MagnetRadius(base, state)` |
| `PlayerProgress` (신규) | — | `PassiveLogic.ExperienceMultiplier(state)` |

`SpeedTierTracker`는 **손대지 않습니다** — 그게 설계 판단 3입니다.

## Initial tuning values

### `ProgressConfig`

| Item | Value | Status |
|---|---:|---|
| 드론 처치 XP | 1 | TEMPORARY — XP-002 |
| 레벨 요구량 (누적) | 15 / 35 / 65 / 105 | TEMPORARY — XP-002 |
| 오브 기본 흡수 반경 | 2.5 m | TEMPORARY |
| 오브 끌림 속도 | 9 m/s | TEMPORARY — 캐논 투사체 14보다 느리게 |
| 오브 흡수 판정 거리 | 0.6 m | TEMPORARY |
| 오브 풀 상한 | 64 | TEMPORARY — 동시 적 8기 대비 넉넉히 |
| 레벨업 재개 지연 | 0 s | LVL-001의 0.2초는 **상한**이지 목표가 아닙니다 |

### 패시브 3종 (단계당 누적, 설계 판단 5)

| 패시브 | 효과 A | 효과 B | Status |
|---|---|---|---|
| **강화 외피** (PAS-001) | 최대 HP **+20** | 즉시 **+20** 회복 | TEMPORARY |
| **고밀도 코어** (PAS-002) | 최대 속도 **×1.08** | 충돌 피해 **×1.15** | TEMPORARY |
| **자기장 증폭** (PAS-003) | 흡수 반경 **×1.6** | 경험치 **×1.25** | TEMPORARY |

> 배율은 **곱연산**입니다 — 3단계 자기장 = 반경 ×4.1. 자석 패시브는 원래 눈덩이형이고,
> 그 눈덩이가 90초 안에 체감되는지가 이 수치의 검증 대상입니다.

## 구현 순서

**한 기능이지만 두 번 플레이 가능한 지점을 만듭니다.** 1단계가 끝나면 루프가 완결되므로,
2단계로 넘어가기 전에 직접 플레이해서 타이밍을 먼저 볼 수 있습니다.

| 단계 | 내용 | 끝났을 때 플레이 가능한 것 |
|---|---|---|
| **1** | 오브 · 곡선 · 정지 · 카드 선택 (패시브는 **단계만 누적**, 효과 없음) | 처치 → 흡수 → 정지 → 선택 → 재개 **전체 루프** |
| **2** | 패시브 3종 실효과 (삽입점 5개) | 성장이 실제로 체감되는 상태 |

1단계에서 가장 위험한 건 **레벨업 타이밍**입니다 (곡선이 90초 세션용이 아님 — 아래).
효과보다 타이밍을 먼저 보는 게 순서상 맞습니다.

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01~F12 회귀 포함)
3. PlayMode 테스트 전체 통과
4. FirstPlayable 씬 플레이: **처치 → 오브 → 흡수 → 정지 → 카드 → 재개**가 끊김 없이 돈다
5. **패시브 3단계를 다 찍은 뒤 SO 에셋 파일이 변경되지 않았다** (B18 — git status로 확인)
6. 재시작 후 XP·레벨·패시브가 **전부 0에서 시작한다** (B24)
7. 레벨업 중 게임 시간이 **완전히 멈춘다** — 적도, 무기도, 세션 타이머도 (B10)
8. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core)

| 테스트 | 검증 |
|---|---|
| `Xp002_LevelFor_ReturnsOneBelowFirstThreshold` | B7 |
| `Xp002_LevelFor_ExactThreshold_LevelsUp` | B7 — 경계 |
| `Xp002_LevelFor_BeyondLastThreshold_StaysAtMax` | B9 |
| `Xp002_PendingLevelUps_SingleGain_ReturnsOne` | B8 |
| `Xp002_PendingLevelUps_BigGain_ReturnsTwo` | B8 — **한 번에 두 레벨** |
| `Xp002_PendingLevelUps_NoChange_ReturnsZero` | B8 |
| `Lvl001_Magnet_InRange_IsTrueAtExactRadius` | B3 — 경계 |
| `Lvl001_Magnet_Step_ApproachesButDoesNotOvershoot` | B3 |
| `Pas004_Upgrade_RaisesStageByOne` | B17 |
| `Pas004_Upgrade_StopsAtThree` | B17 — 상한 |
| `Pas004_Candidates_ExcludesMaxedPassive` | B19 |
| `Pas004_Candidates_AllMaxed_ReturnsEmpty` | B20 |
| `Pas001_MaxHealth_AddsPerStage` | B21 |
| `Pas002_SpeedMultiplier_CompoundsPerStage` | B22 |
| `Pas002_DamageMultiplier_CompoundsPerStage` | B22 |
| `Pas003_MagnetRadius_CompoundsPerStage` | B23 |
| `Pas003_ExperienceMultiplier_CompoundsPerStage` | B23 |
| `Pas004_ZeroStages_AllMultipliersAreNeutral` | B18 — **패시브 없으면 기준값 그대로** |

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Xp001_EnemyKill_DropsOrbAtDeathPosition` | B1 |
| `Xp001_OneKill_DropsExactlyOneOrb` | B2 — **중복 지급 금지** |
| `Xp001_OrbOutsideRadius_DoesNotMove` | B3 |
| `Xp001_OrbInsideRadius_IsAbsorbed` | B3, B4 |
| `Xp001_Orb_HasNoCollider` | B5 |
| `Xp001_SessionEnd_RecallsAllOrbs` | B6 |
| `Lvl001_LevelUp_StopsGameTime` | B10 — `Time.timeScale == 0` |
| `Lvl001_Selection_ResumesTime` | B12 |
| `Lvl001_LevelUpEvent_FiresOncePerLevel` | B13 |
| `Lvl001_TwoLevelsAtOnce_ShowsCardsTwice` | B16 |
| `Lvl001_AfterSessionEnd_DoesNotShowCards` | B15 — **결과 화면과 충돌 금지** |
| `Pas004_AllMaxed_DoesNotStopTime` | B20 |
| `Pas001_Acquire_RaisesMaxHealthAndHeals` | B21 |
| `Pas002_Acquire_DoesNotChangeSpeedTierThresholds` | B22 — **설계 판단 3** |
| `Pas004_ThreeStages_DoNotMutateScriptableObjects` | B18 — **설계 판단 1의 방어선** |
| `Lose001_Restart_ResetsProgressAndPassives` | B24 |

> `Lvl001_*` 테스트는 **`WaitForFixedUpdate`를 쓰지 않습니다** — `timeScale = 0`에서
> 영원히 멈춥니다 (F11에서 겪은 함정, `WORKFLOW_PITFALLS.md` 참조).

## Manual play checks

- **첫 레벨업이 언제 오는가** — 90초 안에 몇 번 오르는가 (아래 규칙 갱신 항목의 실측)
- 오브가 **빨려 들어오는 게 보이는가**, 반경 2.5m가 적절한가
- 정지가 **답답하지 않은가** — 90초 세션에서 3~4번 멈추면 흐름이 끊기는가
- **영문 카드를 0.2초 안에 읽고 고를 수 있는가** — 설계 판단 6이 미룬 유일한 비용입니다.
  느리다고 느껴지면 한글 서브셋 아틀라스가 답입니다 (수십 KB, 예산 문제 없음)
- 같은 패시브를 세 번 고른 것과 나눠 고른 것의 **차이가 느껴지는가**
- **강화 외피를 먹었을 때 더 오래 버티는 게 체감되는가**
- **고밀도 코어를 먹었을 때 럼블 단계에 더 쉽게 드는가** (설계 판단 3의 의도)
- 자기장 3단계의 눈덩이가 **재미있는가, 아니면 그냥 자동인가**

## 필요한 규칙 갱신 (승인 시 함께 진행)

### 1. XP-002 — 검증 기준이 FP 세션 길이와 맞지 않습니다

XP-002의 검증 기준은 이렇게 되어 있습니다:

> 기획서 4.1의 레벨업 타이밍(첫 레벨업 ~0:45, 두 번째 ~1:30)과 **3:30 세션** 총 3~5회 레벨업

**FP는 90초 세션입니다.** 이 기준으로는 F13을 검증할 수 없습니다 —
"첫 레벨업 0:45"는 90초 세션의 **절반**이고, "3:30 세션 3~5회"는 아예 적용 대상이 아닙니다.

곡선(15/35/65/105)은 그대로 두고, **90초 세션용 검증 기준을 XP-002에 병기**합니다.
실제 값은 플레이테스트에서 측정해 채웁니다 — 지금 숫자를 지어내면 그게 곧 근거 없는 기준이 됩니다.

### 2. PAS-004 — FP 범위에서의 축소 명시

설계 판단 5. *"2~3단계: 강화 방향 효과 누적"* 이 FP에서는 **1단계 효과의 선형 누적**입니다.
규칙을 바꾸는 게 아니라 **FP 범위 축소임을 Exception에 적습니다** (MVP에서 원안 복귀).

### 3. SPD-001 — 속도 단계의 분모 명시

설계 판단 3. 지금까지 소비자가 하나뿐이라 물어볼 일이 없었지만, PAS-002가 생기면서
*"기준 최대 속도"* 인지 *"현재 실효 최대 속도"* 인지 정해야 합니다. **기준값 고정**으로 명시합니다.

## Open questions

- ~~**카드 텍스트를 한글로 할 것인가**~~ — **2026-08-07 ASCII로 결정** (설계 판단 6).
  남은 판정은 하나: **영문 카드를 0.2초 안에 읽을 수 있는가.** 못 읽으면 서브셋 아틀라스로 갑니다
- **90초에 레벨업이 몇 번 오는 게 맞는가** — 3~4번이면 25초에 한 번 멈춥니다.
  흐름이 끊길 수 있고, 그렇다고 1~2번이면 성장이 안 보입니다. 실측 후 곡선 조정
- **정지가 진짜 완전 정지여야 하는가** — LVL-001은 "완전히 멈춘다"이지만,
  적이 얼어붙은 화면이 어색할 수 있습니다. 규칙대로 완전 정지로 만들고 플레이에서 판단합니다
- **오브를 못 먹고 흘리는 일이 생기는가** — 오브에 수명을 두지 않았습니다. 90초 세션에
  최대 몇 개나 바닥에 남는지는 실측 대상입니다. 많이 남으면 XP-001의 "합산"이 그때 의미를 갖습니다
- **W8 프레임 미측정** — 오브 풀까지 얹히면 예산이 또 달라집니다.
  `DEVELOPMENT_ORDER.md`에 적어둔 대로 **F14 전에 한 번 재야 합니다**
