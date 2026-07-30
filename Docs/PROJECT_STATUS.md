# Project Status

## Current phase

feature development (bootstrap 완료 — 다음: DEVELOPMENT_ORDER.md의 F01부터)

## What is playable right now

Nothing yet. `Assets/_Game/Scenes/Gameplay/FirstPlayable.unity`에 바닥과 플레이어 placeholder만 존재.

## Feature progress

| Feature | Spec | Implementation | Verification | Playtest |
|---|---|---|---|---|
| F01 그레이박스 아레나 + 이동 | Approved | Done | Verified (automated) | Manual 대기 |

전체 순서는 [Docs/Design/DEVELOPMENT_ORDER.md](Design/DEVELOPMENT_ORDER.md) (F01~F13 = First Playable).

## What is playable right now (갱신)

`FirstPlayable.unity` — WASD로 코어볼 이동, 램프 주행 가능 (정적 쿼터뷰 카메라). PlayMode 검증 전.

## Next up

- 사용자 수동 플레이 → Manual play checks 판단 (조작감, HYP-001) → 필요 시 `/first-playable:playtest`
- `/first-playable:feature F02-camera` — 3인칭 쿼터뷰 카메라 (Cinemachine 3.1.7)

## Known issues

- 없음 (부트스트랩 직후)

## Environment

- Unity version: 6000.5.6f1 (Unity 6)
- Render pipeline: URP 17.5.0
- Unity MCP server: 연결됨 (com.unity.ai.assistant 경유)
- Project mode: Standard
- 추가 패키지: com.unity.cinemachine 3.1.7 (3.1.4는 이 에디터와 비호환 — GetInstanceID 폐기 API 컴파일 에러)
