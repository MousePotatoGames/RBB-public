# Verification Report — F12 궤도·추종 마운트

- 일시: 2026-08-07
- Feature Spec: [F12-orbit-follow-mounts.md](../Features/F12-orbit-follow-mounts.md)
- 관련 결정: [0002-weapon-mount-types.md](../Decisions/0002-weapon-mount-types.md)
- 대상 씬: `Assets/_Game/Scenes/Gameplay/FirstPlayable.unity`
- 미디어: `Docs/Media/2026-08-07_F12-orbit-follow.png`

## Scope

WPN-007 마운트 타입(궤도·추종)과 WPN-008a 궤도 접촉 경로, 그리고 무기 5종화(WPN-005).
B1~B16 전체와 Done conditions 1~8.

**검증 중 결함 1건을 발견해 고쳤습니다** — 아래 "검증 중 발견·수정한 결함" 참조.
그래서 이 보고서의 테스트 수치는 **수정 후 재실행한 값**입니다.

## Compilation

| | |
|---|---|
| 새 에러 | **0** |
| 새 경고 | **0** |

콘솔에 남은 경고 1건은 `com.unity.ai.assistant` 패키지의 계정 API 타임아웃으로,
이 기능과 무관합니다. 세 번의 플레이 세션(총 90초 이상) 동안에도 에러 0건이었습니다.

## EditMode Tests

**174 / 174 통과** (실패 0, 스킵 0). 회귀 포함 전체 실행.

F12가 추가한 항목: `OrbitLogic` 6종, `FollowLogic` 4종, `SweepLogic` 5종,
`Wpn005_FiveKinds_ProduceDifferentDraws`, `Wpn005_Draw_NeverRepeatsAKind`.

## PlayMode Tests

**106 / 106 통과** (실패 0, 스킵 0).

105 → 106은 이번 검증에서 추가한 회귀 테스트 1개입니다
(`Wpn007_FollowWeapon_AppearsBesideThePlayer_NotAtWorldOrigin`).

## 검증 중 발견·수정한 결함

### 추종 무기가 월드 원점에서 생성되어 맵 중앙에서 날아왔습니다

**증상 (VERIFIED — 런타임 측정)**: 펫을 부착한 직후 위치가 `(0.000, 0.000, 0.000)`,
플레이어와의 평면 거리 `0.164`(플레이어가 원점 근처였을 때). 플레이어가 아레나 외곽에서
캡슐을 먹으면 펫이 **맵 한가운데에서 플레이어까지 미끄러져 옵니다.**

**원인**: `MountedWeapon.Initialise`가 `Advance(0f)`로 초기 자세를 잡는데,
궤도는 `OrbitLogic.Position`이 즉시 원 위의 점을 주지만 **추종은 `deltaTime = 0`이면
지수 감쇠 계수가 0이라 제자리를 반환**합니다. 그 "제자리"가 `CreatePrimitive`가 만든
월드 원점입니다. 설계된 위치가 아니라 **생성 순서의 부산물**이었습니다.

**수정**: 추종 마운트일 때 부착 방향(궤도 각도로 이미 전달받고 있던 값)을 써서
유지 거리만큼 떨어진 지점에 미리 놓습니다. 궤도 무기와 같은 규칙 —
*캡슐을 어디서 만졌는지가 무기가 어디에 나타나는지를 정한다* (WPN-001a) — 를 추종에도 적용합니다.

**왜 테스트가 못 잡았는가**: PlayMode 테스트 리그는 공을 항상 `Vector3.zero`에 둡니다.
버그가 있는 위치와 정답 위치가 **정확히 겹쳐 있었습니다.** 추가한 회귀 테스트는
공을 `(12, 0, -9)`로 옮긴 뒤 부착하고, **물리 스텝을 한 번도 돌리지 않은 상태에서**
유지 거리를 검사합니다 (수렴이 아니라 생성 시점 자세를 보기 위해).

> 이건 계기가 못 보는 걸 플레이가 봤다는 뜻이 아니라, **계기의 초기 조건이 결함과 같은 값이었다**는
> 뜻입니다. 좌표를 0으로 두는 테스트 리그는 위치 관련 결함에 대해 구조적으로 눈이 멉니다.

## Runtime Verification

### 측정 조건 (실제 플레이와 다른 점)

| 항목 | 이번 측정 | 실제 플레이 |
|---|---|---|
| 입력 | **없음** — Rigidbody에 속도를 직접 넣어 이동시킴 | WASD 연속 입력 |
| 적 | **스포너 비활성 + 활성 드론 전부 제거** (관측 세션) | 8기까지 증가 |
| 무기 획득 | 코드로 직접 부착 | 캡슐 접촉 (10/35/60초) |
| 샘플 간격 | MCP 왕복 8~18초 | — |

적을 끈 이유는 R1입니다: 입력이 없으면 플레이어가 **약 11초에 사망**해
궤도·추종 운동을 관측할 창이 안 나옵니다 (첫 두 세션이 실제로 그렇게 끝났습니다).
적을 켠 세션의 측정치는 따로 표기했습니다.

### 측정값

세션 3 (적 없음), 세 시점 — t=5.74 / 13.87 / 31.51 / 50.24:

| 시각 | 플레이어 | 공 회전(euler) | 도끼 각도 | 도끼 평면거리 | 도끼 높이 | 도끼 회전 | 펫 거리 |
|---|---|---|---|---|---|---|---|
| 5.74 | (0.16, 0.50, -0.05) | (357, 360, 352) | 89.4° | **2.200** | **0.300** | (0,0,0) | 0.164 |
| 13.87 | (8.28, 0.50, -0.13) | (4, 4, 210) | 111.0° | **2.200** | **0.300** | (0,0,0) | **1.816** |
| 31.51 | (7.92, 0.50, 7.10) | (13, 45, 271) | 46.2° | **2.200** | **0.300** | (0,0,0) | **1.800** |
| 50.24 | (-0.30, 0.50, 7.10) | — | 175.9° | **2.200** | — | (0,0,0) | **1.800** |

### 규칙별 판정

| 규칙 | 판정 | 근거 |
|---|---|---|
| B1 반경 유지 | `VERIFIED` | 플레이어가 8m 이동하는 동안 평면거리가 **2.200에서 벗어나지 않음** (4회 측정) |
| B2 공 회전 무시·직립 | `VERIFIED` | 공 오일러 z가 352→210→271로 도는 동안 도끼 회전은 **항상 (0,0,0)**, `up = (0,1,0)` |
| B3 각속도 × 시간 | `VERIFIED` | 8.13초에 89.4°→111.0°. 180°/s면 1463° = 4바퀴 + 23.4°, 예측 112.8° |
| B4 지연 추종 | `VERIFIED` (PlayMode) | 런타임에서는 왕복 8초 > 시정수 0.17초라 **추격 중간을 못 잡음**. 진행 방향 정렬은 런타임 확인 (`up`이 이동축과 일치) |
| B5 유지 거리 | `VERIFIED` | 1.816 → 1.800 → 1.800 (목표 1.8) |
| B6 프레임률 독립 | `VERIFIED` (EditMode) | 런타임에서 프레임률을 바꿔 재현하지 않음 |
| B7 공의 자식 아님 | `VERIFIED` | `parent = WeaponContainer` (도끼·펫 둘 다) |
| B8 표면 무기는 자식 유지 | `VERIFIED` (PlayMode) | `Wpn007_SurfaceWeapon_IsStillChildOfBall` |
| B9 반경 안 모두 타격 | `VERIFIED` (PlayMode) | 런타임 보조 근거: 적 8기 세션에서 `SweepCount=27, HitCount=2` — 스윕이 실제로 돌고 명중함. **본체 충돌 없음을 런타임에서 분리 증명하진 못함** |
| B10 DMG-002 쿨다운 공유 | `VERIFIED` (PlayMode) | `Wpn007_OrbitContact_SharesHitCooldown` |
| B11 표면 접촉 경로 유지 | `VERIFIED` (PlayMode) | `Spk001_SurfaceContact_StillNeedsBodyCollision` |
| B12 펫 = 공격 코드 추가 0 | `VERIFIED` | 런타임 `liveProjectiles = 1`. `ProjectileWeaponController`는 F12에서 **한 줄도 고치지 않음** |
| B13 콜라이더 없음 | `VERIFIED` | 두 무기 모두 `collider=False, rigidbody=False` |
| B14 세션 종료 시 정리 | `VERIFIED` | 세션 1에서 사망 시 `IsRunning=False`, 컨테이너 자식 **2 → 0** |
| B15 5종 추첨 | `VERIFIED` | 풀 로그: `Spike(Surface/Contact), Cannon(Surface/Projectile), Tesla(Surface/Zap), Axe(Orbit/Contact), Pet(Follow/Projectile)` |
| B16 종류 중복 없음 | `VERIFIED` (EditMode) | `Wpn005_Draw_NeverRepeatsAKind` |

### 떨림 수정이 런타임에서 살아 있는가

`MountedWeapon.LogicPosition`(FixedUpdate 판정 자세)과 `transform.position`(Update 렌더 자세)의
차이를 같이 측정했습니다:

| 시각 | 도끼 gap | 펫 gap |
|---|---|---|
| 13.87 | 0.0404 | 0.0006 |
| 31.51 | 0.0613 | 0.0000 |
| 50.24 | 0.0260 | 0.0000 |

`VERIFIED` — 도끼는 180°/s × 반경 2.2 = 약 6.9 m/s이므로 한 물리 스텝(0.02s)에 0.138m 움직입니다.
gap 0.026~0.061은 스텝 중간 지점에 해당합니다. **보간이 실제로 렌더 자세를 옮기고 있습니다.**
펫 gap이 0인 것은 안착해서 스텝당 이동이 없기 때문으로, 회전 노이즈 방어와 같은 이유입니다.

다만 **떨려 보이는지 아닌지는 계기가 판정할 수 없습니다** → Manual Verification.

### Wiring

```text
slots.container=True | sweep: weapons=True enemies=True session=True |
container.session=True | pool[5] = Spike, Cannon, Tesla, Axe, Pet
```

## Diagnostic Changes Restored

세 항목 모두 **런타임 전용**이라 플레이 모드 종료로 되돌아가며, 종료 후 되읽어 확인했습니다:

| 진단 변경 | 복원 확인 |
|---|---|
| `DroneSpawner.enabled = false` + 활성 드론 전부 Despawn | `spawnerEnabled = True` |
| `Rigidbody.linearVelocity` 직접 주입 (최대 5 m/s) | `vel = (0.000, 0.000, 0.000)`, 플레이어 `(0.000, 0.500, 0.000)` |
| `PlayerHealth.ResetHealth()` 반복 호출 | 상태 아닌 호출이라 잔재 없음 |
| 코드로 부착한 도끼·펫 | `slotsAttached = 0`, `containerChildren = 0` |

씬 더티 여부: `sceneDirty = False` — **씬 에셋에 변경이 남지 않았습니다.**
`WeaponConfig`·`WeaponDefinition` 같은 ScriptableObject는 **건드리지 않았습니다**
(플레이 모드에서 SO를 고치면 에셋에 그대로 남기 때문에 의도적으로 피했습니다).

⚠ NOT RESTORED: 없음.

## Manual Verification Required

계기가 판정할 수 없는 항목입니다.

**떨림 수정 확인 (최우선)**
- 펫이 더 이상 **덜덜 떨지 않는가** — 이번 수정의 유일한 판정 기준입니다
- 도끼 궤도가 매끄럽게 도는가

**Feature Spec의 Manual play checks**
- 도끼가 도는 게 보이는가, 그리고 **공 회전과 무관하다는 게 읽히는가**
- 반경 2.2m / 각속도 180°/s가 적절한가
- **적에게 둘러싸였을 때 도끼가 강한 게 체감되는가** (도끼의 정체성)
- 펫이 따라오는 지연이 자연스러운가
- 펫이 쏘는 게 캐논과 구분되는가 (공 회전에 안 묶임)
- 5종에서 판마다 다른 조합이 나오는 게 느껴지는가 (HYP-006)
- 무기 3개일 때 화면이 너무 정신없지 않은가

**이번 검증에서 새로 생긴 항목**
- **펫과 펫의 투사체가 구분되는가** — 스크린샷에서 펫(구 0.32)과 투사체(구 0.30)가
  거의 같은 크기·같은 회색으로 보입니다. 그레이박스 한계일 수 있지만 P07 아트 때
  **의도적으로 다르게** 해야 할 항목입니다
- 펫이 캡슐 접촉 위치에서 나타나는 게 자연스러운가 (위 결함 수정의 결과)

## Remaining Risks

**R1 (F09→F12 이월, 이번에도 검증을 막았음)** — 입력이 없으면 **약 11초에 사망**하는데
`WeaponConfig.spawnTimes`는 10/35/60초입니다. 이번 검증에서도 두 세션이 무기를 관측하기 전에
끝나 **적을 꺼야 했습니다.** 계기가 세 번 연속 같은 벽에 부딪혔다는 건 페이싱이
설계 의도와 어긋나 있다는 신호입니다. **F13 전에 처리하는 것을 권합니다.**

**R2 (신규)** — 세션 종료로 컨테이너가 무기를 파괴한 뒤에도 `WeaponSlots.AttachedCount`는
**2를 그대로 유지**합니다. 지금은 무해합니다 — 세 공격 컨트롤러가 전부 `session.IsRunning`을
먼저 보고 `AttachedAt`의 null도 검사합니다(런타임 사후 11초간 에러 0건으로 확인).
다만 **파괴된 오브젝트를 가리키는 카운트가 남아 있는 상태**이므로, 재시작이 씬 리로드가 아닌
방식으로 바뀌면 이게 먼저 깨집니다.

**R3** — 궤도 무기의 로컬 +Y가 항상 위를 향합니다(B2). 궤도 마운트에 투사체 무기를 붙이면
**하늘로 쏩니다.** 지금은 그런 무기가 없어 코드로 막지 않았고, 주석으로만 남겨뒀습니다.
5종 중 궤도는 도끼(접촉) 하나뿐이라 현재 조합에서는 발생하지 않습니다.

**R4** — 도끼 접촉 주기 0.25초의 실효성은 여전히 미검증입니다. DMG-002 쿨다운이 실질 상한이면
주기 필드 자체가 불필요할 수 있습니다 (Feature Spec Open question).

## 결론

자동 검증 항목 **전부 통과**했고, 검증 과정에서 결함 1건을 찾아 고친 뒤 재검증했습니다.
Feature Spec 상태를 `Verified (automated)`로 올립니다.

**떨림이 실제로 사라졌는지는 사람이 봐야 확정됩니다.** 그것이 이 기능의 마지막 관문입니다.
