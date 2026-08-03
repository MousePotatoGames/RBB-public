# RUMBLE BALL

굴러가는 공에 무기가 **실제로 붙고**, 구르면서 그 무기가 작동하는 3D 물리 액션 로그라이트입니다.
공격 버튼이 없습니다 — 이동·회전·충돌이 곧 공격입니다.

- Unity 6 (6000.5.6f1) + URP
- 타깃: WebGL (Chrome/Edge), 90초 세션

## 문서

| | |
|---|---|
| 설계 (단일 진실) | [Docs/Game/](Docs/Game/) — 규칙 ID가 구현·테스트의 기준입니다 |
| 현재 스코프 | [Docs/Design/FIRST_PLAYABLE.md](Docs/Design/FIRST_PLAYABLE.md) |
| 진행 상황 | [Docs/PROJECT_STATUS.md](Docs/PROJECT_STATUS.md) |

## 빌드

Unity 메뉴 → `RumbleBall → Build WebGL (smoke)` → `Build/WebGL/`

로컬 확인 (Unity WebGL은 gzip 헤더가 필요합니다):

```bash
python Tools/serve-webgl.py
```

## 크레딧 · 라이선스

게임 코드는 이 저장소의 라이선스를 따릅니다. 아래 서드파티 에셋은 **전부 CC0 1.0 Universal**
(Public Domain Dedication)이며, 각 배포 페이지에서 직접 확인했습니다 (2026-08-01).

> CC0는 **저작자 표시를 요구하지 않습니다.** 아래 표기는 의무가 아니라 관례이며,
> 잼·공모전이 사용 에셋 목록을 요구하는 경우에 대비한 기록입니다.

| 에셋 | 제작자 | 라이선스 | 출처 | 상태 |
|---|---|---|---|---|
| Castle Kit | Kenney (kenney.nl) | CC0 1.0 | [OpenGameArt](https://opengameart.org/content/castle-kit) | 미도입 |
| LowPoly Animated Animals | Quaternius | CC0 1.0 | [itch.io](https://quaternius.itch.io/lowpoly-animated-animals) | 미도입 |
| Low Poly Medieval Weapons | LowPolyAssets | CC0 1.0 | [itch.io](https://lowpolyassets.itch.io/low-poly-medieval-weapons) | 미도입 |
| Low Poly Firearms | chilly_durango | CC0 1.0 | [itch.io](https://chilly-durango.itch.io/low-poly-firearms) | 미도입 |
| Low Poly Guns | LowPolyAssets | CC0 1.0 | [itch.io](https://lowpolyassets.itch.io/low-poly-guns) | 미도입 |
| Ice Age | Riley (rkuhlf-assets) | CC0 1.0 | [itch.io](https://rkuhlf-assets.itch.io/ice-age) | 미도입 |

**미도입**은 라이선스만 확인했고 아직 프로젝트에 넣지 않았다는 뜻입니다. 현재 빌드는 전부 그레이박스 프리미티브입니다.
도입할 때는 `Assets/ThirdParty/<제작자>/` 아래에 원본 그대로 두고, 이 표의 상태를 갱신합니다.
