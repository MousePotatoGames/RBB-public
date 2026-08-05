# Verification Report — F10 Revolver Cannon

- 일자: 2026-08-06
- Feature Spec: [F10-projectile-weapon.md](../Features/F10-projectile-weapon.md)
- 씬: `Assets/_Game/Scenes/Gameplay/FirstPlayable.unity`
- 스크린샷: [2026-08-06_F10-buckshot.png](../Media/2026-08-06_F10-buckshot.png)

## Scope

투사체 무기가 **스스로 작동하는지**, 그리고 WPN-009의 핵심 주장인 **"산탄은 코드가 아니라
수치"** 가 실제로 성립하는지 검증합니다.

> **이 기능은 구현 도중 규칙이 바뀌었습니다.** 1차 구현은 CAN-001 원문("최근접 적이 포신
> 각도 안일 때만 발사")대로였고, 그 상태로 EditMode 148 / PlayMode 75를 통과했습니다.
> 런타임 측정에서 무기가 거의 작동하지 않는 것이 드러나 CAN-001을 개정했고, 이 보고서는
> **개정 후 코드**에 대한 것입니다. 아래 "규칙 개정" 절에 근거 수치를 남깁니다.

## Diagnostic Changes Restored

진단을 위해 바꾼 것은 **플레이 모드 런타임 상태뿐**입니다.

1. `WeaponSlots.TryAttach`를 직접 호출해 캡슐을 기다리지 않고 무기를 부착
2. 산탄 실증을 위해 `Object.Instantiate(cannonAsset)`로 **런타임 전용 복제본**을 만들어
   `shotsPerBurst=5`, `spreadDegrees=30`, `fireInterval=0.35`, `projectileSpeed=8`로 설정

**에셋 파일은 한 번도 쓰지 않았습니다.** 복제본을 쓴 이유가 그것입니다.
플레이 모드 종료 후 되읽어 확인:

```
isPlaying=False  timeScale=1  sceneDirty=False
Weapon_Spike  : 'Crusher Spike'   attack=Contact     interval=0.7 shots=1 spread=0 speed=18
Weapon_Cannon : 'Revolver Cannon' attack=Projectile  interval=0.7 shots=1 spread=0 speed=18
Weapon_Tesla  : 'Tesla Ring'      attack=Zap         interval=0.7 shots=1 spread=0 speed=18
leftover runtime objects in scene: 0
```

## Compilation

| 항목 | 결과 |
|---|---|
| 컴파일 | `VERIFIED` — 성공, 에러 0 |
| 새 Console 에러 | `VERIFIED` — 0건 |
| 새 Console 경고 | `VERIFIED` — 프로젝트 경고 0건 |

경고 1건은 `com.unity.ai.assistant` 패키지의 `Account API did not become accessible`이며
프로젝트 코드와 무관합니다. F10은 UI 텍스트를 추가하지 않으므로 폰트·글리프 검사 대상이 아닙니다.

개정으로 코드를 삭제했으므로 **삭제가 실제로 반영됐는지** 런타임 리플렉션으로 확인했습니다:

```
ProjectileWeaponController.Enemies          = 없음  ✓
WeaponDefinition.firingArcHalfAngleDegrees  = 없음  ✓
Game.Core.TargetingLogic                    = 없음  ✓
```

## EditMode Tests

`VERIFIED` — **139 / 139 통과**, 실패 0, 스킵 0 (F01~F09 회귀 포함).

148 → 139로 줄어든 것은 조준 보정·대상 탐색 테스트를 **해당 코드와 함께 삭제**했기 때문입니다.

`AttachmentLogic`이 `Rotation.Exactly`를 쓰도록 리팩터링한 회귀도 이 실행에 포함됩니다 —
F09 부착 테스트가 그대로 통과하는 것이 그 그물입니다.

## PlayMode Tests

`VERIFIED` — **75 / 75 통과**, 실패 0, 스킵 0.

개정 전 코드에서도 75/75를 **2회 연속** 확인했고(플레이키 수정 검증), 개정 후 다시 75/75입니다.

### 개정으로 뒤집힌 단언

| 테스트 | 이전 | 지금 |
|---|---|---|
| `Can001_NoEnemy_*` | `FiresNothing` | **`StillFires`** |
| `Can001_TargetOutsideArc_DoesNotFire` | 발사 안 함 | **`Can001_FiresAlongBarrel_NotAtEnemy`** — 쏘긴 쏘되 빗나감 |
| `Can001_BallRotation_BringsTargetIntoArc` | 회전이 **타이밍**을 연다 | **`ChangesFireDirection`** — 회전이 **방향**을 바꾼다 |

## Runtime Verification

### 측정 조건

| 항목 | 값 |
|---|---|
| 씬 | `FirstPlayable` |
| 입력 | **없음** — 에이전트 무입력 |
| 프레임 진행 | `VERIFIED` — frame 441 → 3696 (진행 확인) |
| 무기 부착 | 캡슐을 기다리지 않고 t≈2.0s에 직접 부착 |
| 관측 창 | t=1.971s ~ 10.520s (8.55초). 세션 종료(Defeat)로 창이 닫힘 |

### 개정 후 실측 — 발사가 살아났습니다

```
WINDOW  elapsed=8.55s
fires=12   shots=12   shotsPerFire=1.00
poolLive=0 poolCreated=2   cap=32
kills=3
```

| 규칙 | 측정 | 결과 |
|---|---|---|
| B1 | 충돌 없이 자체 발사 | `VERIFIED` — 12회 발사, **처치 3** |
| B2 | 쿨다운만이 조건 | `VERIFIED` — 8.55초 / 12발 = **0.71초 간격**, 설정값 0.7과 일치 |
| B3 | 적 없어도 발사 | `VERIFIED` (PlayMode `Can001_NoEnemy_StillFires`) |
| B8 | 풀링 | `VERIFIED` — 12발에 **객체 2개**. 산탄 세션에서는 90발에 **25개** |
| B6 | 산탄이 수치 | `VERIFIED` — 아래 항목 |
| B11 | 무기 콜라이더 없음 | `VERIFIED` — 부착 무기 콜라이더 0 |

### Done condition 5 — 산탄이 코드가 아님을 런타임에서 증명

런타임 복제본에서 **`shotsPerBurst`와 `spreadDegrees` 두 값만** 바꿨습니다. 코드 변경 없음.

```
clone shots=5 spread=30  |  asset untouched shots=1 spread=0
fires=18   shots=90   shotsPerFire=5.00
```

스크린샷에 **5발 부채꼴이 그대로 찍혔습니다** — 균등 간격으로 퍼진 투사체 5개가 보입니다.
이것이 WPN-009의 핵심 주장에 대한 시각 증거입니다.

## 규칙 개정 — CAN-001 (이 검증에서 나온 가장 중요한 결과)

1차 구현은 스펙과 CAN-001 원문대로 만들어졌고 **테스트를 전부 통과했습니다.**
그런데 실제로 재보니:

| 조건 | 발사 수 | 처치 |
|---|---:|---:|
| **개정 전** (반각 25° 게이트, 8.8초) | **3** | 0 |
| **개정 전** (다른 런, 6.4초) | **0** | 0 |
| **개정 후** (무조건 발사, 8.55초) | **12** | **3** |

25° 게이트가 발사 기회의 대부분을 막아, 무기가 붙어 있어도 거의 아무 일도 하지 않았습니다.
**테스트는 "규칙대로 동작하는가"만 답했지 "규칙이 좋은가"는 답하지 못했습니다.**

사용자 판단으로 CAN-001을 **고정 주기 무조건 발사**로 개정했습니다. 규칙이 노린
"공의 회전이 발사에 영향을 준다"는 **타이밍 대신 방향**으로 옮기면 그대로 성립합니다.
근거와 실측치는 `GAME_RULES.md`의 CAN-001 "왜 바뀌었나"에 남겼습니다.

부수 효과로 `TargetingLogic`, `Rotation.Towards`, `ProjectileLogic.AimDirection`,
각도 판정, 발사각·조준보정 필드가 전부 삭제됐습니다 — 소비자가 없어졌기 때문입니다.

## Manual Verification Required

- `VERIFIED (user-reported)` **빗나가는 것이 의도인가** — 사용자 확인: *"의도한거야 그게"*.
  조준 보정을 넣지 않은 판단이 승인되었습니다
- `MANUAL_REQUIRED` **"내가 굴리는 방향이 사격 방향이 된다"가 느껴지는가** — 이 기능의 핵심
- `MANUAL_REQUIRED` 발사 간격 0.7초가 적절한가 — 너무 뜸한가, 난사인가
- `MANUAL_REQUIRED` 공이 빠르게 구를 때 탄이 난사처럼 흩어져 보이는가, 통제되는 느낌인가
- `MANUAL_REQUIRED` 스파이크(접촉)와 캐논(투사체)의 **역할이 구분되는가**
- `MANUAL_REQUIRED` 회색 구체 투사체가 회색 바닥에서 **눈에 보이는가** (연출은 P08)

## Remaining Risks

### R1 — 무기 도달 가능성 (F09에서 이월, 여전히 유효)

무입력 런에서 **10.3~10.8초에 사망**이 일관되게 재현됩니다. 캡슐은 10/35/60초 스폰이므로
2·3번째 무기는 정상 플레이로 도달이 의심스럽습니다. 이번 검증에서도 무기를 **코드로 직접
부착**해야 관측이 가능했습니다.

F10이 무기를 실제로 작동하게 만들었으므로, 무기를 못 얻는다는 것은 이제
"이 게임의 차별점을 못 본다"와 같습니다. **F12 이후 페이싱을 한 번에 조정할 예정입니다.**

### R2 — 발사 간격 0.7초는 아직 근거 없는 수치

이제 발사 빈도를 정하는 **유일한 손잡이**입니다. 각도 게이트가 사라졌으므로 90초 세션에서
약 128발이 나갑니다. 이 밀도가 적절한지는 플레이테스트 전까지 알 수 없습니다.

### R3 — 투사체가 시각적으로 회색 구체

F09의 R3와 같은 문제입니다. 회색 바닥 위 회색 구체라 스크린샷에서도 잘 안 보입니다.
아트는 P07, 연출은 P08이지만 **색 대비만이라도** F13 이전에 필요할 수 있습니다.

### R4 — 조준 보정을 되살릴 여지

빼는 것이 옳다고 사용자가 확인했지만, 플레이테스트에서 "너무 안 맞는다"가 재발하면
`Rotation.Towards`와 `AimDirection`을 되살리면 됩니다. 그때도 **CAN-001부터** 고칩니다.

## 결론

**자동 검증 항목은 전부 통과했습니다.** Done conditions 1·2·3·5·6 `VERIFIED`,
4는 사용자 플레이로 일부 확인, 7은 위 목록입니다.

F10이 세운 축(투사체 공격 + 직선 이동 + 발사 수·확산)은 실제로 동작하며,
**산탄이 수치라는 주장이 런타임에서 증명되었습니다.** F11(방전·즉시 이동)과
F12(궤도·추종 마운트)는 여기에 축을 하나씩 더하는 방식으로 붙습니다.

이번 기능이 남긴 가장 큰 교훈은 코드가 아니라 방법론입니다 —
**통과한 테스트가 좋은 규칙을 보증하지 않습니다.** 실제로 재보기 전까지는 몰랐습니다.
