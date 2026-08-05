# Verification Report — F09 Crusher Spike

- 일자: 2026-08-05
- Feature Spec: [F09-crusher-spike.md](../Features/F09-crusher-spike.md)
- 씬: `Assets/_Game/Scenes/Gameplay/FirstPlayable.unity`
- 스크린샷: [2026-08-05_F09-spike-attached.png](../Media/2026-08-05_F09-spike-attached.png)

## Scope

접촉 무기가 **실제로 작동하는지** 검증합니다. B1~B13 중 자동 검증 가능한 항목과,
사람만 판단할 수 있는 항목(부착 위치의 체감)을 분리해서 보고합니다.

## Diagnostic Changes Restored

진단을 위해 바꾼 것은 **플레이 모드 런타임 상태뿐**입니다 — `WeaponSlots.TryAttach`를
직접 호출해 무기를 강제 부착했습니다. 플레이 모드 종료 시 폐기되는 변경입니다.

종료 후 되읽어 확인:

```
isPlaying=False  timeScale=1  sceneDirty=False
attached=0  Weapon_* objects=0
```

- 에셋·씬·설정값은 **하나도 건드리지 않았습니다**. `WeaponConfig`, 스파이크 정의 에셋 모두 스펙 표의 값 그대로입니다.
- `timeScale`은 1로 복구되었습니다 (세션 종료 시 0이 되지만 플레이 모드 종료가 되돌립니다).

## Compilation

| 항목 | 결과 |
|---|---|
| 컴파일 | `VERIFIED` — 성공, 에러 0 |
| 새 Console 에러 | `VERIFIED` — 0건 |
| 새 Console 경고 | `VERIFIED` — 프로젝트 경고 0건 |

경고 1건이 있으나 `com.unity.ai.assistant` 패키지의 `Account API did not become
accessible within 30 seconds` 이며 프로젝트 코드와 무관합니다.

폰트·글리프 경고 없음 — F09는 UI 텍스트를 추가하지 않습니다.

## EditMode Tests

`VERIFIED` — **116 / 116 통과**, 실패 0, 스킵 0 (F01~F08 회귀 포함).

스펙이 요구한 10개 테스트 전부 포함:
`Spk001_EnemyInsideArc_AddsDamage`, `Spk001_EnemyOutsideArc_AddsNothing`,
`Spk001_ArcBoundary_IsInclusive`, `Spk001_BonusDamage_ScalesWithSpeed`,
`Spk001_BonusDamage_ScalesWithFrontality`, `Spk001_Dashing_IncreasesBonusDamage`,
`Spk001_Dashing_IncreasesKnockback`, `Spk001_ZeroSpeed_StillHasFloor`,
`Wpn008_NonContactWeapon_AddsNothing`, `Wpn008_NoWeapon_AddsNothing`

## PlayMode Tests

`VERIFIED` — **61 / 61 통과**, 실패 0, 스킵 0 (회귀 포함).

## Runtime Verification

### 측정 조건 (중요)

| 항목 | 값 |
|---|---|
| 씬 | `FirstPlayable` |
| 입력 | **없음** — 에이전트 무입력 |
| 공 회전 | `(354.8, 358.3, 2.3)` — 실제로 구르던 중의 회전 |
| 프레임 진행 | `VERIFIED` — frame 856 → 7723 (진행 확인) |
| 시점 | 측정은 세션 종료 직후(`timeScale=0`)에 수행 |

`timeScale=0` 시점의 측정이지만 `BonusDamageAgainst` / `KnockbackMultiplierAgainst`는
**시간에 의존하지 않는 순수 계산**이므로 결과는 유효합니다. 다만 공 회전은 정지 직전의
실제 회전값을 그대로 썼습니다.

### 씬 배선 (에디터 모드 직렬화 값 — 유효)

```
PlayerDamageDealer.weapons = Player        (F09에서 새로 추가된 참조)
PlayerDamageDealer.config / movementConfig / motor = 모두 할당됨
CapsuleSpawner.pool = 3개
   Crusher Spike    kind=Spike   mount=Surface  attack=Contact
   Revolver Cannon  kind=Cannon  mount=Surface  attack=Projectile
   Tesla Ring       kind=Tesla   mount=Surface  attack=Zap
CapsuleSpawner.player / session = 할당됨
WeaponConfig: maxWeapons=3 minSep=50 attachRadius=0.55 spawnTimes=[10,35,60]
Spike 에셋: arc=60 bonus=8 dashDmg=1.5 dashKb=1.4 shape=Capsule scale=0.28
```

스펙의 Initial tuning values 표와 **전부 일치**합니다.

### 규칙별 실측

공의 로컬 +Z 표면에 스파이크를 부착한 뒤, **월드 벡터**로 판정을 호출했습니다.
로컬 `(0.000, 0.000, 1.000)` → 월드 `(-0.029, 0.091, 0.995)` 로, 공이 실제로 기울어져
있었고 두 값이 다릅니다. 즉 아래 결과는 전부 **공의 현재 회전을 거친 결과**입니다 (B12).

| 규칙 | 측정 | 결과 |
|---|---|---|
| B1, B2 | 정의에서 생성 | `VERIFIED` — Capsule / Surface / Contact, 정의 그대로 |
| B3 | 원뿔 안 | `VERIFIED` — 추가 피해 **8.000** |
| B4 | 원뿔 밖 (반대 방향) | `VERIFIED` — 추가 피해 **0.000** |
| B3 경계 | 59° / 61° | `VERIFIED` — **8.000 / 0.000** (반각 60° 정확) |
| B5 속도 | 속도 절반 | `VERIFIED` — **4.000** (선형) |
| B5 정면도 | 정면도 0.5 | `VERIFIED` — **4.000** (선형) |
| B6 피해 | 대시 중 | `VERIFIED` — **12.000** (= 8 × 1.5) |
| B6 넉백 | 대시 중 / 밖 | `VERIFIED` — 1.00 → **1.40**, 원뿔 밖은 대시해도 **1.00** |
| B11 | 콜라이더 | `VERIFIED` — 무기 GO 콜라이더 **0**, 자식까지 **0**. 공 계층 전체에 `Player(SphereCollider)` 하나뿐 |
| B12 | 공 회전 반영 | `VERIFIED` — 위 로컬/월드 벡터 차이로 확인 |
| B13 | 추첨 | `VERIFIED` — 풀 순서 `Spike, Cannon, Tesla` → 추첨 결과 `Crusher Spike, Tesla Ring, Revolver Cannon` (섞임, 무중복) |
| WPN-001a | 최소 간격 | `VERIFIED` — 3개 동시 부착 시 상호 각도 **90.0° / 90.0° / 90.0°** (최소 50° 충족) |
| B7 | 쿨다운 공유 | `INFERRED` — `PlayerDamageDealer.TryHit`에서 `enemy.TryTakeDamage` **한 번**만 호출하고 보너스를 그 값에 더함. PlayMode `Spk001_BonusShares_HitCooldown` 통과 |
| B8, B9, B10 | 무기 없음 / 비접촉 / 합산 | `VERIFIED` (EditMode + PlayMode) — 런타임에서는 단일 접촉 무기만 실측 |

### 시각 확인

스크린샷에서 세 무기가 **서로 다른 프리미티브로 공 표면에 돌출**해 있는 것이 보입니다 —
Cylinder(Revolver Cannon)가 오른쪽, Cube(Tesla Ring)가 위. Capsule(Spike)은 로컬 +Z,
즉 카메라 반대편이라 공에 가려져 있습니다.

> 참고: `ScreenCapture.CaptureScreenshot`은 Game 뷰만 캡처합니다. 에디터 창 전체 캡처는
> MCP에서 불가능하므로, 필요하면 사람이 직접 찍어야 합니다.

## Manual Verification Required

이번 기능의 **핵심 가치는 전부 여기 있습니다.** 자동 검증은 "수식이 맞다"까지만 말합니다.

- `MANUAL_REQUIRED` **부착 위치가 의미 있게 느껴지는가** (HYP-005 — 이 기능의 존재 이유)
- `MANUAL_REQUIRED` 스파이크 쪽으로 박는 것과 반대로 박는 것의 차이가 체감되는가
  (수치상 8 → 0이므로 총 피해가 약 1.8배 차이납니다. 체감되는지는 별개)
- `MANUAL_REQUIRED` 공이 구르며 스파이크가 도는 게 **전략적으로** 느껴지는가, 무작위로 느껴지는가
- `MANUAL_REQUIRED` 원뿔 반각 60°가 적절한가 — 좁아서 안 맞는가, 넓어서 방향이 무의미한가
- `MANUAL_REQUIRED` 대시 + 스파이크 정면이 **결정타처럼** 느껴지는가 (수치상 12, 본체 포함 최대 2.2배)
- `MANUAL_REQUIRED` 스파크·소리 없이도 "더 세게 박았다"가 읽히는가 (연출은 P08/P09)
- `MANUAL_REQUIRED` Done condition 4 — "스파이크 쪽으로 박으면 반대쪽보다 **눈에 띄게 세게** 부순다"

## Remaining Risks

### R1 — 무기를 얻기 전에 죽습니다 (심각, F09 결함 아님)

무입력 런에서 **매번 10.54초에 패배**했습니다. 첫 캡슐은 10초에 스폰되므로,
캡슐이 나온 지 0.5초 만에 게임이 끝납니다. 실측:

```
elapsed=10.54  outcome=Defeat  kills=2  playerHealth=0.0
spawned=1  live=1        ← 캡슐은 떴지만 줍지 못함
```

무입력 기준이라 실제 플레이는 더 오래 버티겠지만, 2·3번째 무기(35초·60초)는
**정상 플레이로 도달 가능한지 매우 의심스럽습니다.** F08 보고서에서 제기한 문제가
숫자로 확인되었습니다.

이건 F09의 결함이 아니라 **난이도·페이싱 문제**입니다. F09가 무기를 작동하게 만들었으므로,
무기를 못 얻는다는 건 이제 곧 "이 게임의 차별점을 못 본다"와 같습니다.
→ F10·F11 진행 중 `spawnTimes`와 적 압력을 함께 재조정해야 합니다.

### R2 — 원뿔 반각 60°는 아직 근거 없는 수치

경계가 정확히 60°에서 끊긴다는 것만 확인했습니다. 60°가 **재미있는 값인지**는
플레이테스트 전까지 알 수 없습니다. 너무 넓으면 B4(부착 위치의 의미)가 사실상 무력화됩니다.

### R3 — 무기가 시각적으로 회색 프리미티브

스크린샷 기준, 흰 공에 회색 무기가 붙어 있어 **어느 쪽이 무기인지 순간적으로 읽기 어렵습니다.**
"공에 무기가 실제로 붙는다"가 이 게임의 차별점인데 지금은 잘 안 보입니다.
아트는 P07이지만, 색만이라도 대비를 주는 게 F13(HUD) 전에 필요할 수 있습니다.

### R4 — 히트스톱과 추가 피해의 상호작용 미검증

추가 피해가 총 피해를 최대 1.8배(대시 2.2배)로 올리므로 DMG-005 히트스톱 임계를
더 자주 넘길 수 있습니다. 스펙 Open questions에 적힌 대로, 플레이테스트에서
"또 버벅인다"가 재발하면 `HitStopConfig`를 먼저 의심해야 합니다.

## 결론

**자동 검증 항목은 전부 통과했습니다.** Done conditions 1·2·3·5 `VERIFIED`,
4·6은 사람의 판단이 필요합니다.

F09가 세운 뼈대(`WeaponDefinition` SO → 마운트·공격 방식·수치)는 실제로 동작합니다.
F10(투사체)·F11(방전)·F12(궤도·추종)는 여기에 **에셋을 추가**하는 방식으로 붙일 수 있습니다.
