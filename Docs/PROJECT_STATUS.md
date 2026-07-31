# Project Status

## Current phase

feature development (bootstrap 완료 — 다음: DEVELOPMENT_ORDER.md의 F01부터)

## What is playable right now

Nothing yet. `Assets/_Game/Scenes/Gameplay/FirstPlayable.unity`에 바닥과 플레이어 placeholder만 존재.

## Feature progress

| Feature | Spec | Implementation | Verification | Playtest |
|---|---|---|---|---|
| F01 그레이박스 아레나 + 이동 | Approved | Done | Verified (automated) | Manual 대기 |
| F02 3인칭 쿼터뷰 카메라 | Approved | Done | Verified (automated) | Manual 대기 |
| F03 점프·대시·조향 감쇠 | Approved | Done | Verified (automated) | Manual 대기 |
| F04 속도 4단계 + 최소 피드백 | Approved | Done | Verified (automated) | Manual 대기 |

| F05 드론·스포너·풀링·접촉 피해 | Approved | Done | Verified (automated) | Manual 대기 |
| F06 충돌 전투·넉백·히트스톱 | Approved | Done | Verified (automated) | **1회차 완료** |

**플레이테스트 1회차 (2026-08-01)**: F01~F04 조작감 확인 완료 — [기록](Playtests/2026-08-01_F01-F04.md). 가속 시간 1.2→1.0s 확정, HYP-001 1차 검증. 대시 체감은 적 구현 후 재평가.

**플레이테스트 2회차 (2026-08-01)**: F05·F06 전투 감각 — [기록](Playtests/2026-08-01_F05-F06.md).
- **넉백 9 → 20 확정** ("날라가는건 딱 좋아"). 세기가 피해에 비례 감쇠되어 실제로는 2.25 m/s밖에 안 나오고 있었다
- **DMG-005 신설** — 히트스톱 연속 발동 최소 간격. B16(동시 누적 금지)이 순차 연쇄를 못 막던 규칙의 빈틈
- **HYP-003 "빠를수록 강하다" 1차 반증** — 무기·경험치·연출이 붙어야 재평가 가능
- 타격감의 질은 **P08로 이월** (쉐이크 = 일상 타격 / 히트스톱 = 결정타 역할 분담)

전체 순서는 [Docs/Design/DEVELOPMENT_ORDER.md](Design/DEVELOPMENT_ORDER.md) (F01~F13 = First Playable).

## What is playable right now (갱신)

`FirstPlayable.unity` — WASD로 코어볼 이동, 램프 주행 가능 (정적 쿼터뷰 카메라). PlayMode 검증 전.

## Next up

- `/first-playable:feature F07-lose-restart` — 패배 + 재시작 (LOSE-001). HP 0 → 결과 화면 → 원클릭 재시작, 상태 완전 초기화
- F07이 들어오면 **세션 완주가 가능해지므로** O6("저속 접촉의 손해가 억울하다")를 다시 판단할 수 있다

## Known issues

- **타격감이 아직 "딱 좋아"가 아님** — P08로 이월. 히트스톱을 주 피드백 채널로 쓰는 구조 자체가 이 게임(접촉이 끊임없는 구조)과 안 맞는다는 진단까지 나왔다. P08에서 쉐이크와 역할을 나눌 것
- (해결됨) 히트스톱이 정상 플레이에서 발동하지 않는 것처럼 보이던 문제 — **검증 계측이 틀렸다.** 입력 없이 측정해 공이 멈춘 조건이었고, 실제 플레이에서는 오히려 **과하게** 걸리고 있었다 (DMG-005로 해결). **검증 수치를 실제 플레이에 그대로 적용하면 안 된다는 사례**
- (해결됨) 에디터 백그라운드 시 플레이 모드 프레임 정지 → 2026-08-01 Player Settings의 **Run In Background 활성화**. 기획서 21.4의 포커스 상실 대응과도 부합
- (해결됨) 넉백당한 드론이 공중에 뜬 채 추적 → F06 규칙 B19

## 작업 순서 주의

- (2026-08-01 F05에서 학습) 씬 배선 직후 스크립트를 수정하면 재컴파일 중 인스펙터 참조가 유실될 수 있다.
  **스크립트 수정 → 컴파일 완료 확인 → 씬 배선 → 저장 직후 참조 재확인** 순서를 지킬 것.
- (2026-08-01 F06에서 학습) **플레이 모드 중에는 스크립트를 수정하지 말 것.** 플레이 중에는 재컴파일이 보류되는데 Unity MCP는 "컴파일 중"이라며 명령을 거부하므로, 플레이 모드를 정지시킬 수단이 사라져 교착에 빠진다 (사람이 직접 Stop을 눌러야 풀림).

## Environment

- Unity version: 6000.5.6f1 (Unity 6)
- Render pipeline: URP 17.5.0
- Unity MCP server: 연결됨 (com.unity.ai.assistant 경유)
- Project mode: Standard
- 추가 패키지: com.unity.cinemachine 3.1.7 (3.1.4는 이 에디터와 비호환 — GetInstanceID 폐기 API 컴파일 에러)
