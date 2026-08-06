# Verification Report — F11 Tesla Ring

- 일자: 2026-08-06
- Feature Spec: [F11-tesla-zap.md](../Features/F11-tesla-zap.md)
- 씬: `Assets/_Game/Scenes/Gameplay/FirstPlayable.unity`
- 스크린샷: [2026-08-06_F11-zap.png](../Media/2026-08-06_F11-zap.png)

## Scope

방전 무기가 **스스로 작동하는지**, 그리고 TES-001의 핵심인 **"가까울수록 아프다"** 가
실제 수치로 성립하는지 검증합니다.

이 기능으로 WPN-008의 **세 공격 방식이 전부 구현 완료**됩니다 (접촉·투사체·방전).

## Diagnostic Changes Restored

진단을 위해 바꾼 것은 **플레이 모드 런타임 상태뿐**입니다.

1. `WeaponSlots.TryAttach`를 직접 호출해 캡슐을 기다리지 않고 테슬라를 부착
2. 번개선을 캡처하려고 `ZapVisual.FlashDuration`을 **0.08 → 999**로 변경
   (0.08초는 MCP 왕복 시간보다 짧아 캡처가 불가능합니다)

플레이 모드 종료 후 되읽어 확인:

```
isPlaying=False  timeScale=1  sceneDirty=False
ZapVisual.flashDuration = 0.08   <-- 999 아님, 복구됨
Weapon_Tesla: 'Tesla Ring' attack=Zap zapInterval=0.9 zapRange=5 zapDmg=12->4
leftover runtime objects: 0
```

에셋 파일은 하나도 쓰지 않았습니다.

## Compilation

| 항목 | 결과 |
|---|---|
| 컴파일 | `VERIFIED` — 성공, 에러 0 |
| 새 Console 에러 | `VERIFIED` — 0건 |
| 새 Console 경고 | `VERIFIED` — 프로젝트 경고 0건 |

경고 1건은 `com.unity.ai.assistant` 패키지의 `Account API did not become accessible`이며
프로젝트 코드와 무관합니다. F11은 UI 텍스트를 추가하지 않으므로 폰트·글리프 검사 대상이 아닙니다.

> 구현 중 `F11TeslaSetup.cs`의 `using Game.Core;` 누락으로 `Game.Editor` 어셈블리 빌드가
> 실패했는데, `isCompiling=False`를 완료로 오독해 발견이 늦었습니다.
> 원인·교훈은 [WORKFLOW_PITFALLS A5](../Process/WORKFLOW_PITFALLS.md)에 기록했습니다.

## EditMode Tests

`VERIFIED` — **158 / 158 통과**, 실패 0, 스킵 0 (F01~F10 회귀 포함).

F10의 139 + 신규 19. 스펙이 요구한 12개를 전부 포함하며, 아래 항목을 추가했습니다:

- `Tes001_Damage_HalfRange_IsHalfway` — 선형 감소 확인
- `Tes001_Damage_NegativeDistance_ClampsToMax` / `ZeroRange_IsMax` — 퇴화 입력 방어
- `Tes001_OriginIsRespected` — B5의 순수 로직 부분
- `Tes001_ContactIsWorthSeveralTimesTheEdge` — **접촉 대비 최소 2배**를 규칙으로 고정

## PlayMode Tests

`VERIFIED` — **88 / 88 통과**, 실패 0, 스킵 0.

F10의 75 + 신규 13. 스펙이 요구한 11개 + `Tes001_ChoosesTheNearerOfTwo`,
`Tes001_ZapVisual_FlashesOnDischarge`.

### 구현 중 잡은 테스트 결함 두 건

| 결함 | 증상이 됐을 것 |
|---|---|
| 세션 종료 후 `WaitForFixedUpdate` | `timeScale=0`이면 FixedUpdate가 안 돌아 **테스트가 영원히 멈춤** |
| 번개선 0.08초인데 0.4초 뒤 "켜져 있다" 단언 | 항상 실패 |

둘 다 실행 전에 수정했습니다. 번개선 테스트에는 **양 끝점이 실제로 다른지**도 추가했습니다 —
길이 0인 선은 아무것도 그리지 않으면서 `enabled=true`로 통과할 수 있습니다.

## Runtime Verification

### 측정 조건

| 항목 | 값 |
|---|---|
| 씬 | `FirstPlayable` |
| 입력 | **없음** — 에이전트 무입력 |
| 프레임 진행 | `VERIFIED` — frame 1453 → 4874 |
| 무기 부착 | 캡슐을 기다리지 않고 t≈5.5s에 직접 부착 |
| 관측 창 | 5.76초 (세션 종료로 닫힘) |

### 실측

```
elapsed=5.76s   zaps=7   (0.9초 주기 기준 예상 7)
lastDamage=10.6   nearestEnemy=0.95m
visual flashes=7   ← 방전 횟수와 정확히 일치
kills=1
attached weapon colliders=0
```

| 규칙 | 측정 | 결과 |
|---|---|---|
| B1 | 주기적 자체 발동 | `VERIFIED` — 5.76초에 7회, 예상치와 일치 |
| B2 | 최근접 자동 선택 | `VERIFIED` (PlayMode `Tes001_ChoosesTheNearerOfTwo`) |
| B4 | 거리 비례 피해 | `VERIFIED` — 0.95m에서 **10.6** (공식값 10.48, 측정 사이 적 이동분) |
| B5 | 링 기준 사거리 | `VERIFIED` (PlayMode `Tes001_RangeIsFromWeapon_NotBallCentre`) |
| B12 | 콜라이더 없음 | `VERIFIED` — 부착 무기 콜라이더 0 |
| B14 | 번개선 | `VERIFIED` — 발동 7회 / 점멸 7회, 좌표 `(0.0,0.5,0.6) → (2.6,0.5,-0.2)` 길이 2.62m |

### Done condition 5 — 거리에 따른 피해 차이

실제 배포 에셋의 `ToZapConfig()`를 그대로 통과시킨 값입니다:

```
   0m -> 12.00
   1m -> 10.40
   2m ->  8.80
 2.5m ->  8.00
   3m ->  7.20
   4m ->  5.60
   5m ->  4.00
   8m ->  4.00   (사거리 밖은 하한에 고정)
```

**접촉 12 vs 사거리 끝 4 — 정확히 3배**입니다. 이 기울기가 테슬라를 "빗나가지 않는 짧은 캐논"이
아니게 만드는 유일한 요소이고, EditMode `Tes001_ContactIsWorthSeveralTimesTheEdge`가
최소 2배를 규칙으로 고정합니다.

### 시각 확인

스크린샷에 **공에서 적으로 이어지는 청록색 선**이 찍혔습니다. 어느 적을 때렸는지 읽힙니다.

## 주목할 관측 — 생존 시간이 늘었습니다

| 조건 | 생존 |
|---|---|
| 무기 없음 / 스파이크만 (F09·F10 검증) | **10.3 ~ 10.8초** (여러 런에서 일관) |
| 캐논 부착 (F10 검증) | 10.5초 |
| **테슬라 부착 (이번)** | **18.4초** |

`INFERRED (measured with: no input, n=1)` — **단일 런이므로 결론이 아닙니다.**
다만 테슬라는 무입력에서도 붙어 있는 적을 계속 처리하므로 방어적으로 작동한다는
가설이 성립하고, 사실이라면 R1(무기 도달 가능성)에 영향이 있습니다.
플레이테스트에서 확인할 항목입니다.

## Manual Verification Required

- `MANUAL_REQUIRED` **"붙으면 알아서 지져진다"가 느껴지는가** — 이 기능의 핵심
- `MANUAL_REQUIRED` **가까울수록 아픈 것이 체감되는가** (수치상 3배)
- `MANUAL_REQUIRED` 사거리 5m가 적절한가 — 너무 짧아 안 닿는가, 너무 길어 캐논과 구분이 안 되는가
- `MANUAL_REQUIRED` 주기 0.9초가 적절한가
- `MANUAL_REQUIRED` **무기 3종의 역할이 구분되는가** — 스파이크(부딪히기) / 캐논(굴려 조준) / 테슬라(파고들기)
- `MANUAL_REQUIRED` 번개선이 0.08초 점멸로 **충분히 읽히는가** (검증에서는 999초로 늘려야 캡처됐습니다)
- `MANUAL_REQUIRED` 적 무리로 파고드는 것이 **재미있는 선택으로 느껴지는가**

## Remaining Risks

### R1 — 무기 도달 가능성 (F09·F10에서 이월)

캡슐은 10/35/60초 스폰이고 무입력 사망은 약 10.5초입니다. 이번에도 무기를 **코드로 직접
부착**해야 관측이 가능했습니다. 다만 위 "생존 시간" 관측이 사실이라면 테슬라가 이 문제를
일부 완화할 수 있습니다 — **어느 무기를 먼저 뽑느냐에 따라 난이도가 크게 달라진다**는
새로운 변수이기도 합니다.

### R2 — B8이 테슬라를 강하게 만들 수 있음

쿨다운 중인 적을 후보에서 빼므로 테슬라는 "항상 뭔가는 때립니다". 주기 낭비를 막는 것이
목적이었지만, 결과적으로 **사거리 안에 적이 하나라도 있으면 매 주기 피해가 들어갑니다.**
플레이테스트에서 "너무 강하다"가 나오면 이 규칙부터 의심해야 합니다.

### R3 — 번개선 0.08초의 가독성 `UNVERIFIED`

검증에서는 999초로 늘려야 캡처할 수 있었습니다. **0.08초가 사람 눈에 충분한지는
측정하지 못했습니다** — 사람이 플레이해야 알 수 있습니다. 짧으면 "뭔가 죽는데 이유를 모르겠다"가
됩니다. P08 이전이라도 이 수치만은 조정이 필요할 수 있습니다.

### R4 — 회색 계열 그레이박스 (F09·F10에서 이월)

번개선은 청록색이라 그나마 보이지만, 무기 자체는 여전히 회색 프리미티브입니다.

## 결론

**자동 검증 항목은 전부 통과했습니다.** Done conditions 1·2·3·5·6 `VERIFIED`,
4는 부분 확인, 7은 위 목록입니다.

이로써 **WPN-008의 세 공격 방식이 전부 구현되었습니다.**
[Decision 0002](../Decisions/0002-weapon-mount-types.md)의 예측대로라면 F12의 신규 무기는
**마운트만 추가하고 공격 코드는 한 줄도 늘지 않아야** 합니다 — 그 주장이 F12에서 검증됩니다.

Decision 0002가 F11에 예고했던 **Hitscan 이동 방식은 만들지 않았습니다.** 테슬라는 방전이고
Hitscan을 쓰는 무기가 FP에 없기 때문입니다. F10에서 조준 코드를 삭제한 것과 같은 판단이며,
문서의 예고보다 **실제 소비자의 유무**를 기준으로 삼았습니다.
