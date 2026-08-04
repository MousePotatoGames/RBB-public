# Feature: F08 Weapon Attachment (무기 캡슐 · 12슬롯 스냅 · 표면 부착)

## Status

**Verified (automated)** — EditMode 100/100, PlayMode 56/56. 런타임에서 접촉 지점 유지(오차 0.000°)·최소 간격 50°·표면 정렬·공과 함께 회전·정상 플레이 캡슐 획득 확인 ([검증 보고서](../Reports/2026-08-05_F08-weapon-attachment_VERIFICATION.md)). Manual play checks는 사용자 판단 대기.

검증 도중 **부착 방식을 12슬롯 스냅 → 자유 부착 + 최소 간격**으로 변경했습니다 (2026-08-05). `GAME_RULES.md`의 WPN-001 / WPN-001a를 먼저 갱신했습니다. 최초 스펙 승인: 2026-08-01, 개정 승인: 2026-08-05.

## Purpose

**이 게임의 존재 이유를 처음으로 화면에 올립니다.**

지금까지 검증된 건 HYP-001(관성 조작의 재미)뿐이고, HYP-003(빠를수록 강하다)은 1차 반증 상태입니다.
정작 이 게임이 "일반 뱀서라이크와 다르다"고 주장하는 근거 — **무기가 공 표면에 실제로 붙고 공과 함께 회전한다** — 는 아직 손도 대지 않았습니다.

F08이 들어와야 **HYP-002를 처음 관찰할 수 있습니다.** 무기의 공격 효과(F09~F11)는 그 다음이며,
F08은 "붙는다"까지만 다룹니다.

## Player's perspective

전장 어딘가에 빛기둥이 서 있습니다. 굴러가서 부딪히면 공 표면에 무기가 **붙습니다.**
어느 각도로 부딪혔느냐에 따라 붙는 위치가 달라집니다. 굴러가면 무기도 같이 돕니다 — 위로 갔다가, 바닥에 닿았다가 합니다.
캡슐을 놓쳐도 사라지지 않아서 나중에 다시 주우러 갈 수 있습니다.

## Referenced rules

- WPN-001 무기 획득과 부착 — **실제 접촉 지점 그대로** 부착, `WeaponAcquired` 발생
- WPN-001a 부착 간격 — 기존 무기와 **최소 각도 간격**(50°) 미만이면 밀어냅니다 (2026-08-05 규칙 변경: 12슬롯 스냅 → 자유 부착)
- WPN-002 부착 방향 — 무기 기준축은 **중심 → 부착점 표면 노멀**. 공과 함께 회전합니다
- WPN-003 무기 슬롯 상한 — **최대 3개**
- WPN-005 캡슐 무기 종류 — 3종이 **매 판 랜덤 순서, 중복 없이** 등장합니다
- WPN-006 캡슐 지속과 비컨 — 주울 때까지 **영구히 남고**, 수직 광선 비컨을 표시합니다

## In scope

- `Game.Core`: `AttachmentSlotLogic`(정20면체 12슬롯 생성 + 최근접 빈 슬롯 탐색), `WeaponDrawLogic`(WPN-005 무중복 랜덤 순서)
- `Game.Gameplay`: `WeaponSlots`(플레이어 부착 상태 소유), `WeaponCapsule`(접촉 감지 + 비컨), `CapsuleSpawner`(타임라인 스폰), `WeaponConfig`
- `Game.Presentation`: 비컨 시각 표현 (스텁 — 실린더/라인)
- 무기 본체는 **그레이박스 프리미티브 3종**으로 구분만 되게 (스파이크=캡슐, 캐논=실린더, 테슬라=큐브)
  - 원래 스펙은 스파이크=콘이었으나 **Unity에 콘 프리미티브가 없어** 캡슐로 대체합니다
  - **구체는 쓰지 않습니다** — 구체 위의 구체는 공의 일부로 보여서 "붙었다"가 전달되지 않습니다 (검증에서 확인)
- EditMode 테스트(주력) + PlayMode 테스트

## Out of scope

- **무기의 공격 효과 전부** — 스파이크 추가 피해(F09), 캐논 발사(F10), 테슬라 방전(F11). F08은 **붙기만** 합니다
- 장착 연출 8단계 (시간 감속·조립 애니메이션) — **P06**. FP 스코프가 "연출은 스텁(즉시 등장+플래시)"으로 이미 확정돼 있습니다
- 무기 2단계 강화 (WPN-004) — 보스 등장이 트리거라 FP에 보스가 없습니다
- 무기 아트·모델 — P07. 그레이박스 프리미티브로 갑니다
- 결과 화면의 "획득 무기" 표시 — F13 HUD와 함께

## Behavior rules

| # | 규칙 | 근거 |
|---|---|---|
| B1 | 무기는 **접촉 지점 방향 그대로** 부착됩니다 — 사전 정의 슬롯으로 스냅하지 않습니다 | WPN-001 |
| B2 | 부착 방향은 항상 단위 벡터입니다 (공 중심 → 표면) | WPN-002 |
| B3 | **접촉 각도가 조금만 달라도 부착 위치가 달라집니다** — 연속적입니다 | WPN-001 Result |
| B4 | 기존 무기와 각도가 **최소 간격 미만**이면 기존 무기에서 멀어지는 방향으로 밀어냅니다 | WPN-001a |
| B5 | 무기 수가 상한이면 부착하지 않습니다 (캡슐도 소비되지 않습니다) | WPN-003 |
| B6 | 밀어낸 뒤에도 **모든 무기 쌍이 최소 간격 이상** 떨어져 있습니다 | WPN-001a Result (실루엣 통제) |
| B7 | 무기는 부착 방향의 **표면 노멀로 정렬**되어 배치됩니다 | WPN-002 |
| B8 | 무기는 공의 자식으로 붙어 **공과 함께 회전**합니다 | WPN-002 Result |
| B9 | 무기는 **최대 3개**까지 부착됩니다 | WPN-003 |
| B10 | 같은 종류의 무기는 두 번 부착되지 않습니다 | WPN-005 (무중복) |
| B11 | 캡슐이 스폰하는 무기 종류는 **매 판 랜덤 순서, 중복 없이** 결정됩니다 | WPN-005 |
| B12 | 캡슐은 획득 전까지 **사라지지 않습니다** (시간 경과·거리로 소멸하지 않습니다) | WPN-006 |
| B13 | 미획득 캡슐은 **수직 비컨**을 표시하고, 획득 시 비컨과 캡슐이 함께 사라집니다 | WPN-006 |
| B14 | 부착 시 `WeaponAcquired(종류, 슬롯)` 이벤트가 발생합니다 | WPN-001 |
| B15 | 재시작 후 부착 상태·캡슐이 초기 상태로 돌아갑니다 | F07 B12 (씬 리로드) |

## Architecture

```text
Game.Core (엔진 참조 없음)
├─ AttachmentLogic       # Resolve(contactDir, taken, minSeparation) (B1~B6)
│                        #   접촉 방향을 그대로 쓰되, 기존 무기와 가까우면 밀어낸다
└─ WeaponDrawLogic       # 무중복 랜덤 순서 (B11) — 시드를 받아 결정적으로 동작

Game.Gameplay
├─ WeaponConfig          # 슬롯 수, 캡슐 스폰 시각, 비컨 높이 등 TEMPORARY
├─ WeaponSlots           # 플레이어에 부착. 점유 상태 소유, Attach() (B3~B10, B14)
├─ WeaponCapsule         # 접촉 감지 → WeaponSlots.Attach (B12)
└─ CapsuleSpawner        # 타임라인대로 캡슐 스폰 (B11)

Game.Presentation
└─ CapsuleBeacon         # 수직 비컨 표시/해제 (B13)
```

무기 종류는 `WeaponKind { Spike, Cannon, Tesla }` enum으로 Core에 둡니다 — F09~F11이 이 값으로 분기합니다.

## Initial tuning values

| Item | Value | Status |
|---|---:|---|
| **무기 간 최소 각도 간격** | **50°** | TEMPORARY (WPN-001a) — 무기 3개가 구체에 겹치지 않게 놓이려면 이론상 최대 약 109°까지 가능하지만, 너무 크면 접촉 지점이 무시되고 사실상 스냅이 됩니다 |
| 최대 무기 수 | 3 | CONFIRMED (WPN-003) |
| 캡슐 스폰 시각 | 10 / 35 / 60 s | TEMPORARY (FIRST_PLAYABLE 세션 타임라인) |
| 캡슐 스폰 거리 (플레이어 기준) | 12~20 m | TEMPORARY |
| 무기 부착 반경 (공 중심에서) | 0.55 | TEMPORARY (공 반지름 0.5 + 살짝 바깥) |
| 비컨 높이 | 8 m | TEMPORARY |

## Done conditions

1. 컴파일 에러·새 콘솔 에러 0건
2. EditMode 테스트 전체 통과 (F01~F07 회귀 포함)
3. PlayMode 테스트 전체 통과 (회귀 포함)
4. FirstPlayable 씬 플레이: 캡슐이 스폰되고 비컨이 보이며, 부딪히면 무기가 표면에 붙고 **공과 함께 회전**한다
5. **접촉 각도를 달리하면 다른 슬롯에 붙는다** (B6 — 이 기능의 핵심)
6. Manual play checks가 별도 목록으로 보고됨

## EditMode tests (주력 — Core)

| 테스트 | 검증 |
|---|---|
| `Wpn001_FirstWeapon_UsesContactDirectionExactly` | B1 — 스냅하지 않음 |
| `Wpn001_Result_IsAlwaysUnitLength` | B2 |
| `Wpn001_TinyAngleDifference_ProducesDifferentPosition` | B3 — **연속성. 12슬롯이었다면 실패합니다** |
| `Wpn001a_TooCloseToExisting_IsPushedAway` | B4 |
| `Wpn001a_FarFromExisting_IsNotMoved` | B4 대조군 — 멀면 접촉점 그대로 |
| `Wpn001a_AllPairs_KeepMinimumSeparation` | B6 — 무기 3개를 같은 방향에 몰아도 간격 유지 |
| `Wpn001a_PushedResult_IsStillUnitLength` | B2, B4 |
| `Wpn001_OppositeContact_StaysOpposite` | B3 — 정반대로 부딪히면 정반대에 붙음 |
| `Wpn005_Draw_HasNoDuplicates` | B11 |
| `Wpn005_Draw_ContainsEveryKind` | B11 |
| `Wpn005_Draw_OrderVariesWithSeed` | B11 — 매 판 순서가 달라짐 |

## PlayMode tests

| 테스트 | 검증 |
|---|---|
| `Wpn001_CapsuleContact_AttachesWeapon` | B3, B14 — 부착 + 이벤트 1회 |
| `Wpn002_AttachedWeapon_IsChildOfBall` | B8 — 공과 함께 회전 |
| `Wpn002_AttachedWeapon_AlignsToSurfaceNormal` | B7 |
| `Wpn003_FourthWeapon_IsNotAttached` | B9, B5 |
| `Wpn005_SameKind_AttachesOnlyOnce` | B10 |
| `Wpn001_DifferentApproachAngles_UseDifferentSlots` | B6 — 씬에서도 접촉 각도가 슬롯을 가름 |
| `Wpn001_ContactAtBallCentre_StillAttaches` | B3 — 구현 중 발견한 엣지 케이스 (아래 참조) |
| `Wpn003_CapsuleAtCap_IsNotConsumed` | B5, B9 — 못 붙이면 캡슐도 소비되지 않음 |
| `Wpn006_UnclaimedCapsule_PersistsOverTime` | B12 — 시간이 지나도 남아 있음 |
| `Wpn006_Capsule_HasBeaconUntilClaimed` | B13 |

## 구현 노트

- **무기는 콜라이더 없이 붙습니다** — 스펙 Open question대로 시각만. `GameObject.CreatePrimitive`가 자동으로 붙이는 콜라이더를 즉시 제거합니다. 물리 판정은 F09(스파이크)에서 도입합니다
- **캡슐은 트리거가 아니라 거리 검사**로 접촉을 감지합니다. 공이 최대 12 m/s로 굴러서, 얇은 트리거는 물리 스텝 사이에 통과해버릴 수 있습니다
- **접촉 방향이 0인 경우** — 캡슐이 공 중심과 정확히 겹치면 방향 벡터가 0이 되어 슬롯을 못 고르고, 그 캡슐은 **영원히 주울 수 없게** 됩니다. PlayMode 테스트에서 발견했습니다. `TryAttach`에서 위쪽 방향으로 폴백하도록 수정하고 회귀 테스트를 추가했습니다

## Manual play checks

- **"무기가 실제로 붙는다"가 전달되는가** (HYP-002 — 이번 기능의 핵심)
- 굴러갈 때 무기가 같이 도는 게 보이는가, 아니면 그냥 붙어만 있는 것처럼 보이는가
- **부착 위치가 다르다는 걸 인지하는가** (HYP-005 — 2회차 이상에서 캡슐 접촉 각도를 의식하기 시작하는가)
- 비컨이 멀리서 보이는가, 주우러 가고 싶어지는가
- 무기가 붙은 뒤 공의 실루엣이 읽히는가, 지저분한가
- 무기 3개가 붙었을 때 서로 겹쳐 보이지 않는가

## Open questions

- **무기가 바닥과 충돌하는가** — 표면에 튀어나온 무기가 물리 콜라이더를 가지면 공의 구름이 달라집니다. F08은 **비물리(트리거 없음, 시각만)** 로 붙이고, 충돌 판정은 F09(스파이크)에서 도입할 것을 제안합니다. 지금 물리를 넣으면 F01~F04에서 잡은 조작 튜닝이 흔들립니다
- 캡슐 스폰 위치가 드론 스폰 링(18~26m)과 겹치는지 — 구현 시 확인
