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

전체 순서는 [Docs/Design/DEVELOPMENT_ORDER.md](Design/DEVELOPMENT_ORDER.md) (F01~F13 = First Playable).

## What is playable right now (갱신)

`FirstPlayable.unity` — WASD로 코어볼 이동, 램프 주행 가능 (정적 쿼터뷰 카메라). PlayMode 검증 전.

## Next up

- 사용자 수동 플레이 → F01~F04 Manual checks 한 번에 판단 (조작감·카메라·점프/대시·속도 가독성) → `/first-playable:playtest`
- `/first-playable:feature F05-drone-spawner` — 스크랩 드론 + 스포너 + 풀링 (ENM-001, ENM-004, WAVE-001/002)
  - F05 스펙에 적 이동 구조 결정 반영 필요: 키네마틱 기본 + 넉백/사망 시만 다이나믹 전환 (`Docs/Decisions/`에 기록)

## Known issues

- 없음. (해결됨: 에디터 백그라운드 시 플레이 모드 프레임 정지 → 2026-08-01 Player Settings의 **Run In Background 활성화**. 기획서 21.4의 포커스 상실 대응과도 부합)

## Environment

- Unity version: 6000.5.6f1 (Unity 6)
- Render pipeline: URP 17.5.0
- Unity MCP server: 연결됨 (com.unity.ai.assistant 경유)
- Project mode: Standard
- 추가 패키지: com.unity.cinemachine 3.1.7 (3.1.4는 이 에디터와 비호환 — GetInstanceID 폐기 API 컴파일 에러)
