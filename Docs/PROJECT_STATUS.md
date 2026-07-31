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
| F06 충돌 전투·넉백·히트스톱 | Approved | Done | Verified (automated) | Manual 대기 |

**플레이테스트 1회차 (2026-08-01)**: F01~F04 조작감 확인 완료 — [기록](Playtests/2026-08-01_F01-F04.md). 가속 시간 1.2→1.0s 확정, HYP-001 1차 검증. 대시 체감은 적 구현 후 재평가.

전체 순서는 [Docs/Design/DEVELOPMENT_ORDER.md](Design/DEVELOPMENT_ORDER.md) (F01~F13 = First Playable).

## What is playable right now (갱신)

`FirstPlayable.unity` — WASD로 코어볼 이동, 램프 주행 가능 (정적 쿼터뷰 카메라). PlayMode 검증 전.

## Next up

- **사용자 수동 플레이 (F05·F06)** → 특히 HYP-003 "빠를수록 강하다"와 무리 속 돌파 감각 → `/first-playable:playtest`
- 이후 `/first-playable:feature F07-xp-orbs` (DEVELOPMENT_ORDER.md 기준 다음 순서)

## Known issues

- **히트스톱이 정상 플레이에서 발동하지 않을 수 있음** (F06 검증에서 계측). 무리 안에서 공 속도가 1.7~2.7 m/s로 떨어져 피해가 임계값 5를 못 넘는다. 사람 플레이 판단 후 튜닝할 것 — [F06 검증 보고서](Reports/2026-08-01_F06-collision-combat_VERIFICATION.md)
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
