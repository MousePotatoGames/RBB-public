# GAME_RULES — RUMBLE BALL

> 규칙 ID는 Feature Spec과 테스트가 참조하는 불변 식별자다.
> 수치가 범위로 적힌 것은 TEMPORARY — 첫 플레이테스트 기준 초기값이며 튜닝으로 확정한다.

---

## 이동 (MOVE)

## MOVE-001 카메라 기준 이동

- Status: CONFIRMED
- Condition: 플레이어가 WASD 입력 중
- Process: 카메라 기준 평면 벡터 방향으로 Rigidbody에 힘을 가한다
- Result: 공이 관성을 유지하며 가속·감속·선회한다
- Exception: 즉시 방향 전환 없음 — 관성이 항상 유지된다 (디자인 원칙)

## MOVE-002 지상/공중 조향 구분

- Status: CONFIRMED
- Condition: 접지 여부 판정
- Process: 지상에서는 가속·최대속도 관리, 공중에서는 조향력을 낮춘다
- Result: 공중 컨트롤이 제한적이다
- Exception: 없음

## MOVE-003 고속 조향 감쇠

- Status: CONFIRMED
- Condition: 현재 속도가 최대속도에 근접
- Process: 조향 반응을 낮춘다
- Result: 빠를수록 곡선이 커져 관성이 체감된다
- Exception: 없음

## MOVE-004 경사 접지 보정

- Status: CONFIRMED
- Condition: 경사면·램프 주행 중
- Process: 접지 보정력으로 과도한 속도 손실을 막는다
- Result: 램프에서 흐름이 끊기지 않는다
- Exception: 없음

## MOVE-005 초기 튜닝 수치

- Status: TEMPORARY
- 기본 최대속도 10~13 m/s, 최대 성장 속도 15~18 m/s, 0→최대속도 1.0~1.5초

---

## 점프 (JUMP)

## JUMP-001 점프

- Status: CONFIRMED
- Condition: 접지 상태(코요테 타임 0.08~0.12초 포함, 수치 TEMPORARY)에서 Space 입력
- Process: 상방 속도 부여, 체공 0.55~0.8초 (TEMPORARY)
- Result: 장애물 회피 및 낙하 충돌 준비
- Exception: 공중에서는 재점프 불가 (더블 점프 없음)

---

## 대시 (DASH)

## DASH-001 대시

- Status: CONFIRMED
- Condition: Left Shift 입력, 쿨다운(1.5~2.2초, TEMPORARY) 종료 상태
- Process: 순간이동이 아니라 현재 입력 방향으로 속도를 추가한다 (+7~10 m/s, TEMPORARY)
- Result: 즉시 럼블 속도 단계 진입(SPD-001), 충돌 피해에 대시 배율 적용(DMG-001)
- Exception: 쿨다운 중 재사용 불가

---

## 속도 단계 (SPD)

## SPD-001 속도 4단계

- Status: CONFIRMED (단계 구조) / 경계 비율 TEMPORARY
- Condition: 현재 속도 / 최대속도 비율
- Process: 저속(0~35%) / 중속(35~70%) / 고속(70~95%) / 럼블(95% 이상 또는 대시 중)
- Result: 단계별 충돌 피해 배율과 VFX(먼지 → 궤적 → 발광·잔상 → 전기 아크)가 바뀐다. 단계 전환 시 `PlayerSpeedTierChanged` 이벤트 발생
- Exception: 대시 중에는 속도와 무관하게 럼블 단계

---

## 카메라 (CAM) — 기획서 11.1 전사

## CAM-001 마우스 궤도 회전

- Status: CONFIRMED
- Condition: 플레이 중 마우스 이동
- Process: 카메라가 플레이어를 중심으로 yaw 궤도 회전한다. 시점은 가까운 3인칭 쿼터뷰(고정 피치)
- Result: 진행 방향 결정과 전장 탐색. 이동 기준축(MOVE-001)이 즉시 갱신된다
- Exception: 없음

## CAM-002 프레이밍

- Status: CONFIRMED (구조) / 수치 TEMPORARY
- Condition: 일반 플레이 상태
- Process: 플레이어를 화면 세로 기준 하단 40~45%에 두고, 부착 무기가 읽힐 만큼 가까운 거리를 유지한다
- Result: 전방 시야 확보 + 무기 부착 상태 가독성
- Exception: 속도·적 수에 따른 거리 증가는 F04 이후 (속도 단계 연동), 대시 FOV는 F03

## CAM-003 물리 보간 분리

- Status: CONFIRMED
- Condition: 항상
- Process: 카메라 갱신과 물리 업데이트의 보간을 분리한다 (Rigidbody Interpolate + LateUpdate 계열 카메라 갱신)
- Result: 고속 이동 중 카메라 떨림 없음
- Exception: 없음

---

## 충돌 피해 (DMG)

## DMG-001 충돌 피해 공식

- Status: CONFIRMED (공식 구조) / 계수 TEMPORARY
- Condition: 플레이어 본체가 적과 접촉
- Process: `피해 = 기본 충돌 피해 × 속도 배율 × 충돌 정면도 × 대시 배율 × 낙하 배율 × 패시브 배율`
  - 정면도 = 플레이어 이동 방향과 적 방향의 내적, **하한으로 클램프** (음수·0 방지, 하한값 TEMPORARY: 0.3)
  - 속도 배율에는 최소값을 두어 저속 접촉도 0이 되지 않게 한다
- Result: 적에게 피해와 피격 방향 넉백
- Exception: 낙하 배율은 DMG-004 조건에서만 1 초과

## DMG-002 중복 히트 방지

- Status: CONFIRMED
- Condition: 같은 적과 연속 프레임 접촉
- Process: 적 개체별 피해 쿨다운을 둔다
- Result: 한 번의 충돌은 한 번만 피해를 준다
- Exception: 쿨다운 경과 후 재접촉은 새 충돌로 취급

## DMG-003 대형 적 넉백 감쇠

- Status: CONFIRMED
- Condition: 대시 중 브루트 또는 보스와 충돌
- Process: 일반 적보다 낮은 넉백 배율 적용
- Result: 탱커·보스는 밀리되 날아가지 않는다
- Exception: 없음

## DMG-004 낙하 충돌 보너스

- Status: CONFIRMED (구조) / 수치 TEMPORARY
- Condition: 하강 속도가 임계치(TEMPORARY: 6 m/s) 이상인 상태로 적과 접촉
- Process: DMG-001의 낙하 배율을 적용 (TEMPORARY: 1.5배)
- Result: 점프·램프 낙하가 실제 공격 수단이 된다 (JUMP-001의 "낙하 충돌" 이행)
- Exception: 정면도는 이때도 하한 클램프 값 이상 보장 — 수직 낙하가 공식으로 무효화되지 않는다

## DMG-005 히트스톱

- Status: CONFIRMED (구조) / 수치 TEMPORARY
- Condition: 일정 피해 이상의 타격이 발생 (임계 미만은 정지하지 않는다)
- Process: 실시간(unscaled) 기준으로 짧게 시간을 정지시킨다. 지속시간은 피해에 비례하되 상한이 있다
- Result: 강한 타격이 타격으로 읽힌다 (기획서 11.2)
- Exception:
  - **동시 다발 타격은 누적되지 않는다** — 더 긴 쪽만 유지한다
  - **직전 정지로부터 최소 간격이 지나기 전에는 다시 정지하지 않는다** — 무리 전투에서 짧은 간격의 연쇄가 화면 버벅임으로 보이기 때문 (2026-08-01 플레이테스트 EXP-004에서 확인)
  - 정지는 어떤 경우에도 `Time.timeScale`을 1로 복구하며 종료된다

> **왜 최소 간격이 규칙인가**: 누적 방지(동시)만으로는 부족하다. 무리 안에서는 타격이 0.25초 쿨다운 간격으로 **순차적으로** 들어오므로, 개별 정지가 짧아도 연쇄되면 저속 재생처럼 보인다. 무기(WPN)가 추가되어 타격 빈도가 올라가면 더 심해진다.

---

## 플레이어 생존 (HP)

## HP-001 피격과 무적

- Status: CONFIRMED (구조) / 수치 TEMPORARY
- Condition: 적 접촉 피해 또는 보스 패턴에 피격
- Process: HP 감소 후 0.35~0.55초 피격 무적 부여, `PlayerDamaged` 이벤트 발생
- Result: 연속 접촉으로 즉사하지 않는다
- Exception: 무적 중 피해 무시

## HP-002 초기 피해 수치표

- Status: TEMPORARY (전부 첫 플레이테스트용 placeholder)
- 플레이어 기본 HP: 100
- 스크랩 드론 접촉: 8 / 램 비틀 돌진: 15 (일반 접촉 8) / 아이언 브루트 접촉: 15
- 보스 접촉·돌진: 25 / 원형 충격파: 20
- 검증 기준: 이 수치로 "첫 무기 획득 전(0:15) 사망 금지"(WAVE-003)와 "첫 판 60초 생존률 80%"가 성립해야 한다

---

## 승패 (WIN / LOSE)

## FP-001 First Playable 임시 승리 조건

- Status: TEMPORARY (First Playable 전용 — MVP에서 WIN-001로 교체 후 폐기)
- Condition: 90초 생존
- Process: `GameEnded` 발생, 결과 화면 표시 (Victory)
- Result: LOSE-001과 동일한 결과 정보 표시
- Exception: 없음

## LOSE-001 패배

- Status: CONFIRMED
- Condition: 플레이어 HP가 0
- Process: `GameEnded` 발생, 결과 화면 표시 (Defeat)
- Result: 생존 시간, 처치 수, 최고 연속 처치, 도달 레벨, 획득 무기 표시. 원클릭 Retry
- Exception: 플레이어 사망과 보스 사망이 같은 프레임에 발생하면 **승리가 우선**한다 (상호 파괴 = Victory, 결과는 한 번만 처리)

## WIN-001 승리

- Status: CONFIRMED
- Condition: 보스 처치 (기준 세션은 3분 30초, WIN-002 연장 포함)
- Process: `GameEnded` 발생, 결과 화면 표시 (Victory)
- Result: LOSE-001과 동일한 결과 정보 표시
- Exception: 승리는 오직 보스 처치로만 성립 — 시간 생존만으로는 승리하지 않는다

## WIN-002 시간 종료 시 보스 생존 → 광폭화 연장

- Status: CONFIRMED
- Condition: 3분 30초 시점에 보스가 살아 있음
- Process: 보스가 광폭화(강화 상태)하고 세션이 결판까지 무제한 연장된다. 타이머는 3:30 이후 광폭화 상태를 표시
- Result: 보스 처치 시 승리(WIN-001), 플레이어 HP 0 시 패배(LOSE-001). 시간에 의한 강제 패배 없음
- Exception: 광폭화는 해제되지 않는다 (연장 구간 내내 유지)

---

## 무기 공통 (WPN)

## WPN-001 무기 획득과 부착

- Status: CONFIRMED
- Condition: 전장의 무기 캡슐과 접촉
- Process: 캡슐과 접촉한 표면 위치에서 **가장 가까운 사전 정의 부착 슬롯으로 스냅**하여 무기를 배치하고, 장착 연출(시간 0.15배 감속, 1.0~1.5초, 반복 획득 시 0.6~0.8초) 재생. `WeaponAcquired` 이벤트 발생
- Result: 공 실루엣 변화, 무기 자동 발동 시작. 접촉 각도에 따라 매 판 부착 위치가 달라진다
- Exception: 이미 점유된 슬롯이면 가장 가까운 빈 슬롯으로 스냅

## WPN-001a 부착 슬롯 정의

- Status: CONFIRMED (균등 분포 방식) / 개수 TEMPORARY
- Condition: 프로젝트 초기 설정
- Process: 구체 표면에 균등 분포한 **12개 슬롯** (정20면체 꼭짓점 기반 분포 권장, 개수는 TEMPORARY — 플레이테스트로 조정 가능)
- Result: 무기 3개가 서로 겹치지 않고, 실루엣 통제와 부착 위치 다양성을 동시에 확보 (기획서 25장 리스크 대응)
- Exception: 없음

## WPN-002 부착 방향

- Status: CONFIRMED
- Condition: 무기 부착 시
- Process: 무기 기준축은 구체 중심 → 부착점 방향의 표면 노멀을 따른다
- Result: 무기는 공과 함께 회전한다
- Exception: 판정·이펙트는 가독성을 위해 일부 보정 가능

## WPN-003 무기 슬롯과 강화 상한

- Status: CONFIRMED
- Condition: MVP 전체
- Process: 최대 3개 무기, 무기당 최대 2단계
- Result: 무기 4개째 획득 상황은 발생하지 않도록 세션을 설계한다
- Exception: 없음

## WPN-004 무기 강화 — 보스 등장 시 전승급

- Status: CONFIRMED
- Condition: 보스 등장 (2:30, `BossSpawned` 이벤트)
- Process: 보스 등장 연출과 함께 보유 중인 모든 무기가 2단계로 동기화 승급. 무기별 `WeaponUpgraded` 이벤트 발생
- Result: 기존 부착 인스턴스에 강화 적용 (새 인스턴스를 만들지 않음). "전체 무기가 하나의 전투기계처럼 동기화"(기획서 7.3 보스전 단계)를 규칙으로 구현
- Exception: 보스 등장 시점에 미보유한 무기(획득 실패 케이스)는 이후 획득 시 즉시 2단계로 장착

## WPN-005 캡슐 무기 종류 결정

- Status: CONFIRMED
- Condition: 무기 캡슐 스폰 (0:15 / 1:00 / 2:00)
- Process: 무기 3종이 **매 판 랜덤 순서, 중복 없이** 등장한다
- Result: 한 판을 완주하면 3종을 전부 장착. 판마다 획득 순서와 부착 위치가 달라진다
- Exception: 없음

## WPN-006 캡슐 지속과 비컨

- Status: CONFIRMED
- Condition: 스폰된 캡슐을 플레이어가 아직 줍지 않음
- Process: 캡슐은 획득할 때까지 **영구히 전장에 남는다**. 멀리서도 보이는 수직 광선 비컨을 표시한다
- Result: 무기를 놓쳐서 세션 경험이 붕괴하는 일이 없다 ("첫 무기 획득 도달률 95%" 목표 지원). WPN-004 Exception(늦은 획득 시 즉시 2단계)과 정합
- Exception: 없음

---

## 크러셔 스파이크 (SPK)

## SPK-001 스파이크 접촉 피해

- Status: CONFIRMED
- Condition: 스파이크 콜라이더가 적과 접촉
- Process: 충돌 피해에 추가 피해를 더한다. 현재 속도·정면도에 비례, 대시 중 피해·넉백 증가
- Result: 스파크 VFX + 금속 충돌음
- Exception: DMG-002 중복 히트 방지 공유

## SPK-002 스파이크 강화 (2단계)

- Status: CONFIRMED
- Condition: WPN-004 전승급 발동
- Process: 쐐기 확대 + 넉백 배율 대폭 증가
- Result: 대시 돌파 시 적이 더 크게 날아간다 — 파괴 연출 강화
- Exception: 브루트·보스 넉백 감쇠(DMG-003)는 유지

---

## 리볼버 캐논 (CAN)

## CAN-001 발사 조건

- Status: CONFIRMED
- Condition: 가장 가까운 적 방향과 포신 방향의 각도가 허용 범위 안 + 발사 쿨다운 종료
- Process: 시안색 에너지 볼트 발사, 포신 회전·반동 연출. 약간의 조준 보정 허용
- Result: 공의 회전에 따라 발사 타이밍이 변한다
- Exception: 360도 자동 추적 포탑처럼 보이면 안 된다 (조준 보정 상한)

## CAN-002 캐논 강화 (2단계)

- Status: CONFIRMED
- Condition: WPN-004 전승급 발동
- Process: 발사 조건 충족 시 짧은 간격의 2연사
- Result: 발사 빈도 증가 — "회전하며 쏘는" 정체성 강화. 투사체는 풀링(성능 원칙 준수)
- Exception: 2연사 중에도 CAN-001 각도 조건은 첫 발 기준 1회만 판정

---

## 테슬라 링 (TES)

## TES-001 방전 공격

- Status: CONFIRMED
- Condition: 주기적 탐색에서 링 부착점 근처에 적 존재
- Process: 가장 가까운 적 1명에게 번개 연결. 부착점과 적이 가까울수록 피해 증가. 공격 순간에만 Physics 쿼리 수행
- Result: 근거리 군중 처리
- Exception: 없음

## TES-002 테슬라 강화 (2단계)

- Status: CONFIRMED
- Condition: WPN-004 전승급 발동
- Process: 인접한 적 1~2명에게 연쇄 피해
- Result: 군중 처리 확대
- Exception: 없음

---

## 경험치와 레벨 (XP / LVL)

## XP-001 경험치 오브

- Status: CONFIRMED
- Condition: 적 처치
- Process: 경험치 오브 드랍. 일정 거리 안에서 플레이어에게 흡수, `ExperienceCollected` 이벤트 발생. 오브는 풀링하고 먼 거리에서는 합산 가능
- Result: 경험치 누적
- Exception: 적 사망 후 경험치 중복 지급 금지

## XP-002 초기 경험치 곡선

- Status: TEMPORARY (전부 첫 플레이테스트용 placeholder)
- 드론 오브: 1 XP / 비틀: 3 XP / 브루트: 8 XP
- 레벨 요구량: Lv2 = 15, Lv3 = 35, Lv4 = 65, Lv5 = 105 (누적)
- 검증 기준: 기획서 4.1의 레벨업 타이밍(첫 레벨업 ~0:45, 두 번째 ~1:30)과 3:30 세션 총 3~5회 레벨업이 성립해야 한다

## LVL-001 레벨업

- Status: CONFIRMED
- Condition: 경험치가 요구량 도달
- Process: 게임 시간 일시정지 → 패시브 3종 카드 표시 → 1개 선택(키보드 1/2/3 + 마우스) → 0.2초 이내 재개. `PlayerLevelUp` 이벤트 발생
- Result: 선택한 패시브 (강화)적용
- Exception: 레벨업 중 게임 시간은 완전히 멈춘다

---

## 패시브 (PAS)

## PAS-001 강화 외피

- Status: CONFIRMED (효과 구조) / 수치 TEMPORARY
- Process: 최대 HP 증가 + 일부 즉시 회복. 강화 방향: 피해 감소 또는 추가 회복
- 스타일: 안정형

## PAS-002 고밀도 코어

- Status: CONFIRMED (효과 구조) / 수치 TEMPORARY
- Process: 이동속도·충돌 피해 증가. 강화 방향: 대시 재사용 감소
- 스타일: 고위험 돌파형

## PAS-003 자기장 증폭

- Status: CONFIRMED (효과 구조) / 수치 TEMPORARY
- Process: 획득 범위·경험치 배율 증가. 강화 방향: 흡수 시 짧은 가속
- 스타일: 빠른 성장형

## PAS-004 패시브 중복 선택과 상한

- Status: CONFIRMED
- Condition: 이미 보유한 패시브를 레벨업에서 다시 선택
- Process: 해당 패시브의 다음 단계로 강화. 상한 **3단계** (1단계: 기본 효과, 2~3단계: "강화 방향" 효과 누적)
- Result: 같은 패시브를 여러 번 고르면 누적 강화
- Exception: 3단계 도달한 패시브는 레벨업 카드에서 제외하고 나머지로 대체 (3종 전부 3단계면 레벨업 보상 없음 — 3:30 세션에서는 사실상 발생하지 않음)

---

## 적 (ENM)

## ENM-001 스크랩 드론

- Status: CONFIRMED
- 행동: 플레이어 직선 추적, 접촉 피해. 낮은 HP 물량형. 붉은/마젠타 소형 구체·다면체

## ENM-002 램 비틀

- Status: CONFIRMED
- 행동: 일정 거리에서 정지 → 방향 고정 → 예고 → 돌진. 돌진 실패 후 짧은 기절(약점). 낮고 긴 쐐기형

## ENM-003 아이언 브루트

- Status: CONFIRMED
- 행동: 느린 추적, 근거리 압박. 높은 HP·높은 넉백 저항. 큰 다면체/장갑 구체

## ENM-004 적 공통 규칙

- Status: CONFIRMED
- 모든 위험 행동은 색·바닥 표시·준비 동작 중 최소 2가지로 예고
- 플레이어를 둘러싸되 완전히 가두지 않음 (접근 슬롯 분산)
- 사망 시 즉시 삭제하지 않고 짧은 물리 날아가기 + 디졸브. `EnemyKilled` 이벤트 발생

---

## 보스 (BOSS)

## BOSS-001 오버로드 코어 패턴

- Status: CONFIRMED
- 등장: 2:30, 등장 연출(감속·카메라 후퇴·조명 점등·HP 바) 후 전투
- 패턴: ① 추적·압박 ② 원형 충격파 ③ 3방향 연속 돌진
- 구현: 브루트의 이동·추적 코드 재사용, 크기·머티리얼·장갑만 변경

## BOSS-002 보스 2페이즈

- Status: CONFIRMED
- Condition: 보스 HP 50% 이하
- Process: 이동속도 증가 + 드론 생성 추가
- Result: 후반 압박 상승
- Exception: 없음

---

## 웨이브 (WAVE)

## WAVE-001 스폰 디렉터

- Status: CONFIRMED (구조) / 구간별 수치 TEMPORARY
- Process: `WaveDirector`가 경과 시간·현재 적 수·플레이어 레벨 기준으로 생성 예산 관리. 구간표는 원본 기획서 9.1
- Exception: 적 상한 도달 시 개체 수 대신 HP·속도·엘리트 비율 상승

## WAVE-002 스폰 위치

- Status: CONFIRMED
- Process: 카메라 화면 밖이되 플레이어와 너무 가깝지 않은 링에서 생성. 진행 방향 정면에만 집중 생성 금지

## WAVE-003 세션 스크립트 이벤트

- Status: CONFIRMED
- Process: 무기 캡슐 0:15 / 1:00 / 2:00, 보스 2:30 등 고정 타임라인은 원본 기획서 4.1을 따른다
- Exception: 첫 무기 획득 전 사망이 발생하지 않도록 난이도 설계 (기획서 4.4)
