# Web Smoke Build — 결과

- 날짜: 2026-08-01
- 기준: [체크리스트](2026-08-01_WEB-SMOKE_CHECKLIST.md) (빌드 전 작성)
- 빌드: `Build/WebGL`, Unity 6000.5.6f1, IL2CPP + Gzip

## 판정

| # | 항목 | 결과 | 근거 |
|---|---|---|---|
| W1 | 빌드 성공 | ✅ PASS | 에러 0, 경고 5, 386초, 17.7MB |
| W2 | 로딩·기동 | ✅ PASS | WebGL 2.0, PhysX 4.1.2, Input System 초기화 |
| W3 | 브라우저 콘솔 | ✅ PASS | 로드 단계 에러 0 |
| W4 | 입력 | ✅ PASS (user-reported) | "실행하니까 잘 되네" |
| W5 | 물리 동등성 | ✅ PASS (user-reported) | 동일 — PhysX 버전도 에디터와 같음 |
| W6 | 전투 동등성 | ✅ PASS (user-reported) | — |
| W7 | 세션 종료·재시작 | ✅ PASS (user-reported) | — |
| W8 | **프레임** | ⚠ **UNVERIFIED** | 아래 참조 |
| W9 | 다운로드 크기 | ✅ PASS | **17.7MB** / 50MB |
| W10 | 커서 잠금 | ✅ PASS (user-reported) | — |

## 발견·수정한 결함 3건

1. **빌드 씬 목록이 `SampleScene`(Unity 템플릿 기본 씬) 하나뿐** — 그대로 빌드했으면 빈 씬이 나왔다. 스모크 빌드를 F04에서 F07까지 미룬 대가
2. **MSAA가 양쪽 RP 에셋에서 꺼져 있었음** (`m_MSAA: 1`) — 안티에일리어싱 부재. 에디터에서도 동일했으나 게임 뷰가 작아 덜 티었다
3. **WebGL이 Mobile 품질 티어를 쓰고 있었음** (`WebGL: 0`) — 섀도맵 1024 + cascade 1개로 50m를 덮어 20 texel/m. 그림자가 자글거린 실제 원인. 타깃이 PC WebGL이므로 티어 선택 자체가 틀렸다 → PC 티어(2048 + cascade 4)로 변경

수정 후 재빌드·재확인 완료 ("잘 나오는군").

## W8을 UNVERIFIED로 남기는 이유

브라우저 창이 숨겨진 상태에서는 `requestAnimationFrame`이 스로틀되어 자동 계측이 불가능했고, 사용자 수치도 받지 못했다.

**의도적으로 미룬다.** F08~F11에서 무기·투사체가, F12에서 경험치 오브가 붙으면 프레임 예산이 근본적으로 달라진다. 지금 재면 두 번 재게 된다.
다만 아래 두 가지가 이번에 무거워졌으므로 **다음 측정 시 함께 볼 것**:

- MSAA 4x (픽셀 처리량 증가)
- PC 품질 티어 (섀도맵 2048 + cascade 4)
- PhysX가 **Single-Threaded** — 드론 상한 30 근처에서 에디터보다 불리할 수 있다

## 남은 리스크

- **W8 미측정** — F11(무기 3종 완료) 또는 F13 시점에 반드시 측정. 30fps 미만이면 P11이 아니라 그 시점에 처리
- 탭 제목이 `Unity Web Player | My project` — 제출 전 제품명 설정 필요
- `My project_BurstDebugInformation_DoNotShip` 폴더가 출력에 포함 — 배포 시 제외
