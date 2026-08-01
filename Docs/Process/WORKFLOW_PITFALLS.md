# Workflow Pitfalls — FirstPlayable + Unity MCP 운영 기록

> 이 기록은 [FirstPlayable](https://github.com/WhorideChicken/first-playable)
> **v0.3.0**의 근거 자료다. 여기 적힌 A1~D3가 그대로 플러그인 수정으로 반영됐고,
> 영문 요약은 [field report](https://github.com/WhorideChicken/first-playable/blob/main/examples/field-report-unity-6.md)에 공개돼 있다.

- 작성: 2026-08-01 (F01~F07 진행 중 실제로 겪은 것만)
- 목적 두 가지:
  1. **우리 프로젝트의 운영 규칙** — 같은 함정을 반복하지 않는다
  2. **first-playable 플러그인(0.2.1)에 대한 피드백** — 구조로 예방 가능한 것들을 파일 단위로 지목한다

> 원칙: 여기 적힌 건 전부 **실제로 발생한 사건**이다. 가정이나 예상 위험은 넣지 않는다.

---

## 요약

| # | 문제 | 발생 | 심각도 | 구조로 예방 가능? |
|---|---|---:|---|---|
| A1 | 플레이 모드 중 스크립트 수정 → **에디터 교착** | 2회 | **치명** | ✅ 훅으로 차단 가능 |
| A2 | Run In Background 꺼짐 → 플레이 모드 프레임 정지 | 1회 | 높음 (**잘못된 검증 결론 유발**) | ✅ bootstrap 프리플라이트 |
| A3 | 도메인 리로드 중 MCP 응답 공백 | 수십 회 | 중 (시간 낭비) | ✅ 재시도 규약 |
| B1 | **계측 조건이 실제 플레이와 달라 결론이 정반대** | 1회 | **치명** | ✅ verify 규칙 |
| B2 | **공허하게 통과하는 단언** (발동 안 해도 통과) | 2건 | 높음 | ✅ unity-testing 규칙 |
| B3 | 문자열은 맞고 **렌더링이 틀린** 경우 | 1건 | 중 | ✅ verify 체크리스트 |
| C1 | 재컴파일로 **씬 인스펙터 참조 유실** | 1회 | 높음 | ✅ feature 순서 규약 |
| C2 | **진단용 임시 수치를 되돌리지 못함** | 1회 | 중 | ✅ verify 종료 게이트 |
| D1 | 테스트 결과 폴링으로 왕복 낭비 | 상시 | 낮음 | ✅ 규약 정리 |
| D2 | MCP에서 모달 다이얼로그 → 명령 실패 | 1회 | 낮음 | ✅ 레퍼런스 주석 |

---

## A. 에디터 생명주기

### A1. 플레이 모드 중 스크립트 수정 → 교착 (치명, 2회)

**상황**
검증 중 런타임 계측을 하다가 코드를 고쳤다. 한 번은 F06 검증 중, 한 번은 F07 준비 중 계측기 스크립트를 새로 만들었을 때. 두 번째는 **사용자가 26분간 방치된 뒤 직접 강제 종료**했다.

**증상 (순환 교착)**

```text
플레이 모드 실행 중  →  스크립트 수정
        ↓
Unity: "플레이 중에는 재컴파일 보류" (에디터 기본 동작)
        ↓
MCP:   isCompiling = true  →  모든 명령 거부 (COMPILATION_IN_PROGRESS)
        ↓
에이전트: 플레이 모드를 멈출 수단이 없음
        ↓
사람이 Stop을 눌러야만 풀림
```

**근본 원인**
Unity의 "Script Changes While Playing" 정책과 MCP의 `isCompiling` 가드가 **서로를 기다린다.** 어느 쪽도 잘못되지 않았는데 조합이 데드락이다.

**이번에 든 비용**
2회 × 수십 분. 두 번째는 사용자가 직접 개입.

**플러그인 예방책 (P0)**

플러그인에는 이미 `hooks/hooks.json` + `scripts/unity-guard.sh`(PreToolUse)가 있다. **여기에 플레이 모드 가드를 추가하면 구조적으로 막힌다.**

1. `bootstrap`이 설치하는 에디터 스크립트에 플레이 모드 마커를 추가:

   ```csharp
   [InitializeOnLoad]
   internal static class PlayModeMarker
   {
       private const string Path = "Temp/firstplayable_playmode";
       static PlayModeMarker()
       {
           EditorApplication.playModeStateChanged += s =>
           {
               if (s == PlayModeStateChange.EnteredPlayMode) File.WriteAllText(Path, "1");
               else if (s == PlayModeStateChange.ExitingPlayMode) File.Delete(Path);
           };
       }
   }
   ```

   > 플러그인은 이미 `Temp/firstplayable_test_result.txt` 규약을 쓰고 있으므로 **같은 패턴**이다.

2. `unity-guard.sh`에 케이스 추가 — `Assets/**/*.cs` 대상 `Edit|Write|MultiEdit`인데 마커 파일이 있으면 `ask`:

   > "FirstPlayable: Unity가 플레이 모드입니다. 지금 스크립트를 수정하면 재컴파일이 보류되고 MCP가 잠겨 **플레이 모드를 멈출 수단이 사라집니다**(사람이 직접 Stop을 눌러야 함). 먼저 플레이 모드를 종료하세요."

3. `skills/verify/SKILL.md`의 사다리에 명시: **런타임 검증 중에는 코드를 수정하지 않는다.** 결함을 발견하면 → 플레이 모드 종료 → 수정 → 재진입.

**우리 프로젝트 규칙 (이미 적용 중)**
Unity 명령을 보내기 전에 `EditorApplication.isPlaying`을 먼저 확인하고, 상태를 바꾸는 명령에는 가드를 넣는다:

```csharp
if (EditorApplication.isPlaying) { result.LogError("play mode is running — aborting"); return; }
```

---

### A2. Run In Background 꺼짐 → 프레임 정지 (높음, 1회)

**상황**
F03 검증에서 플레이 모드 상태를 샘플링했는데 `Time.frameCount`가 여러 번 측정해도 **완전히 동일**했다.

**근본 원인**
에디터가 백그라운드(포커스 없음)일 때 플레이 모드가 프레임을 진행하지 않는다. MCP로 조작하면 Unity는 항상 백그라운드다 — **MCP 워크플로에서는 기본값이 곧 고장**이다.

**파급**
이 문제를 모른 채 작성한 **F02 검증 보고서의 카메라 추적 항목을 `VERIFIED` → `UNVERIFIED`로 정정**해야 했다. 즉 **잘못된 검증 결론을 만들어냈다.**

**플러그인 예방책 (P0)**
`skills/bootstrap/SKILL.md`의 프리플라이트에 추가하고, **위반 시 경고가 아니라 차단**할 것:

- `PlayerSettings.runInBackground = true` 확인/설정
- 근거를 `Docs/PROJECT_STATUS.md`에 기록

`references/unity-mcp.md` §1 표에 한 줄: *"MCP로 플레이 모드를 검증하려면 Run In Background가 반드시 켜져 있어야 한다. 꺼져 있으면 프레임이 진행되지 않아 모든 런타임 관측이 조용히 거짓이 된다."*

---

### A3. 도메인 리로드 중 MCP 응답 공백 (중, 상시)

**증상**
`Unity not detected (no fresh discovery files found)` 또는 `COMPILATION_IN_PROGRESS`. 스크립트를 고칠 때마다 20~60초.

**플러그인 예방책 (P1)**
`references/unity-mcp.md`에 **재시도 규약**을 명문화:

| 응답 | 의미 | 대응 |
|---|---|---|
| `Unity not detected` | 도메인 리로드 중 | 30~45초 대기 후 재시도. **실패로 보고하지 말 것** |
| `COMPILATION_IN_PROGRESS` | 컴파일 중 | 동일. 단 **플레이 모드면 A1 교착을 의심**할 것 |

> 특히 마지막 줄이 중요하다. 이번에 나는 `COMPILATION_IN_PROGRESS`를 계속 "곧 끝나겠지"로 해석해서 교착을 늦게 알아챘다.

---

## B. 검증 방법론 — 가장 값비싼 실수들

### B1. 계측 조건이 실제 플레이와 달라 결론이 정반대 (치명, 1회)

**상황**
F06 검증에서 히트스톱 발동을 계측했다. 190타 동안 `FreezeCount = 0`. 검증 보고서에 이렇게 적었다:

> "히트스톱이 정상 플레이에서 발동하지 않을 수 있음 — 무리 안에서 공 속도가 1.7~2.7 m/s로 떨어져 피해가 임계값 5를 못 넘는다"

**실제로는 정반대였다.**
사용자가 플레이하자 히트스톱이 **과도하게** 걸려 "두두두 버벅"으로 나타났다. 결국 규칙(DMG-005)을 신설해야 했다.

**근본 원인**
계측할 때 **입력이 없었다.** 입력이 없으면 `BallMotor`가 속도를 감쇠시켜 공이 멈춘다. 사람은 계속 입력을 넣으므로 속도가 유지되고 피해가 임계값을 넘는다.
**계측 조건이 실제 플레이와 달랐고, 나는 그 차이를 인지하지 못한 채 결론을 냈다.**

**플러그인 예방책 (P0)**
`skills/verify/SKILL.md`에 라벨을 하나 추가하거나, 최소한 규칙을 명시:

> **런타임 계측은 "실제 플레이와 조건이 같은가"를 먼저 진술하라.**
> 사람의 지속적 입력, 카메라 조작, 연속 충돌 등이 빠진 계측은 `VERIFIED`가 아니라 **`INFERRED (계측 조건: 입력 없음)`** 이다.
> 특히 **"X가 발생하지 않는다"는 부정형 결론은 계측만으로 `VERIFIED`가 될 수 없다** — 발생 조건을 못 만든 것과 구분이 불가능하다.

이건 `references/game-feel.md`의 "수치가 아니라 설계를 의심하라"와 짝을 이룬다. 잘못된 계측은 잘못된 튜닝으로 직결된다.

---

### B2. 공허하게 통과하는 단언 (높음, 2건)

**사례 1 — 히트스톱**

```csharp
// 정지가 한 번도 안 걸려도 통과한다
while (stop.IsFrozen && ...) yield return null;
Assert.IsFalse(stop.IsFrozen);              // 처음부터 false면 무조건 통과
Assert.That(Time.timeScale, Is.EqualTo(1f)); // 아무 일도 없었어도 1
```

**사례 2 — 풀 반환**
`Enm004_DeadDrone_LingersThenDespawns`는 이름과 달리 **"시체가 남는가"만 확인**하고 풀 반환·재사용(B11/B14)은 전혀 검증하지 않았다. 스펙 표에는 검증된 것으로 적혀 있었다.

**공통 구조**
둘 다 **"끝난 상태"를 단언했고, "시작했는가"를 단언하지 않았다.**

**플러그인 예방책 (P0)**
`references/unity-testing.md` §3에 절 추가:

> **일시적 상태(transient state)를 테스트할 때는 "일어났다"를 먼저 단언하라.**
> 히트스톱·무적·쿨다운·넉백처럼 짧게 켜졌다 꺼지는 것은, 종료만 단언하면 **한 번도 켜지지 않은 경우와 구분되지 않는다.**
> 관측 가능한 카운터(`FreezeCount`, `HitCount`)를 노출하고 `> 0`을 먼저 단언한 뒤 종료를 단언할 것.

그리고 §5(커버리지 계약)에 한 줄:

> 스펙의 규칙 ID와 테스트를 **이름이 아니라 단언 내용으로** 대조하라. `unity-qa` 에이전트의 감사 항목에 "이 테스트가 규칙이 깨졌을 때 실제로 실패하는가"를 포함할 것.

---

### B3. 문자열은 맞고 렌더링이 틀린 경우 (중, 1건)

**상황**
F07 결과 화면. 테스트는 통과:

```csharp
Assert.IsTrue(stats.text.Contains("생존"));   // 통과
```

화면에는 **`□□ 11.6□ | □□ 1`** 이 떴다. TMP 기본 폰트에 한글 글리프가 없다.

**잡아낸 경로**
테스트가 아니라 **콘솔 경고**였다: `The character with Unicode value 생 was not found in the [LiberationSans SDF] font asset`.

**플러그인 예방책 (P1)**
`skills/verify/SKILL.md`의 사다리 4단계(경고 확인)를 **선택이 아니라 필수**로 격상하고, UI가 포함된 기능에 체크 항목 추가:

> UI 텍스트를 추가한 기능은 **문자열 단언만으로 `VERIFIED` 불가**. 콘솔에 폰트/글리프 경고가 없는지 확인하고, 캡처로 육안 확인할 것.

`references/unity-testing.md`에도: *"`Contains("...")`는 문자열을 검증하지 렌더링을 검증하지 않는다."*

---

## C. 상태 오염

### C1. 재컴파일로 씬 인스펙터 참조 유실 (높음, 1회)

**상황 (F05)**
씬 배선 직후 스크립트를 수정했더니, 재컴파일 후 `PlayerHealth.config`와 `DroneSpawner.dronePrefab`이 **NULL**이 되어 있었다. 플레이하면 스포너가 아무것도 안 했다.

**플러그인 예방책 (P1)**
`skills/feature/SKILL.md`의 작업 순서를 명시적으로:

```text
스크립트 수정 → 컴파일 완료 확인 → 씬 배선 → 저장 → 저장 직후 참조 재확인
```

그리고 **씬 배선 스크립트는 항상 `Verify()` 함수를 함께 만들 것** — 배선 결과를 즉시 되읽어 로그로 남긴다. (이번 프로젝트의 `F02CameraSetup.Verify()`, `F07SessionSetup.Verify()`가 이 패턴이고, 실제로 잘 작동했다.)

---

### C2. 진단용 임시 수치를 되돌리지 못함 (중, 1회)

**상황 (F06)**
히트스톱을 눈으로 보려고 `hitStopMinDuration/MaxDuration`을 **0.05/0.09 → 30f**로 바꿨다. 그 직후 세션이 끊기고 **여러 차례 복구에 실패**했다(MCP 교착·리로드). 하마터면 30초짜리 정지가 커밋될 뻔했다.

**플러그인 예방책 (P1)**
`skills/verify/SKILL.md`의 Completion 절에 **종료 게이트** 추가:

> 검증 중 진단 목적으로 바꾼 설정값·씬 상태는 **보고서를 쓰기 전에 원복하고, 원복을 되읽어 확인한 로그를 보고서에 남긴다.**
> 원복하지 못한 항목이 있으면 보고서 최상단에 `⚠ 미복구` 로 명시할 것 — 조용히 넘어가지 않는다.

플레이테스트 쪽(`skills/playtest/SKILL.md`)에도 동일하게: 진단 실험(EXP)에서 바꾼 값은 **판정 직후 원복**을 실험 정의에 포함시킨다. (이번 EXP-004에서 이 규칙을 미리 적어둔 덕에 원복을 놓치지 않았다.)

---

## D. 워크플로 마찰

### D1. 테스트 결과 폴링 (낮음, 상시)

`Temp/firstplayable_test_result.txt`가 생길 때까지 반복 확인하느라 왕복이 많이 낭비됐다.

**제안 (P2)** — `references/unity-testing.md`에 대기 규약을 명시. 파일이 생길 때까지 **한 번의 블로킹 대기**로 처리하고, EditMode는 ~30초 / PlayMode는 ~2~4분을 기준으로 안내.

### D2. MCP에서 모달 다이얼로그 (낮음, 1회)

`AssetDatabase.DeleteAsset()`이 확인 다이얼로그를 띄워 `User interactions are not supported for MCP tool calls`로 실패했다.

**제안 (P2)** — `references/unity-mcp.md` §2 Caveats에 추가: *"사용자 상호작용을 유발하는 Editor API(`DeleteAsset`, `EditorUtility.DisplayDialog`, `ImportPackage(interactive:true)` 등)는 MCP에서 실패한다. 비대화형 오버로드를 쓰거나 파일 시스템으로 우회할 것."*

### D3. 스크린샷이 잘못된 창을 캡처 (낮음, 1회)

`Tools/capture-unity-editor.ps1`이 Unity가 아니라 Claude Code 창을 잡았다. Unity가 포그라운드가 아니면 발생한다.

**우리 규칙** — 캡처 후 **반드시 이미지를 되읽어 Unity 창이 맞는지 확인**한다. (이번에 확인했기 때문에 잘못된 증빙이 보고서에 들어가지 않았다.)

---

## 플러그인 제안 요약 (우선순위)

| 우선 | 제안 | 대상 파일 |
|---|---|---|
| **P0** | 플레이 모드 마커 + 가드 훅 (A1) | `scripts/unity-guard.sh`, `hooks/hooks.json`, `skills/bootstrap/SKILL.md` |
| **P0** | Run In Background 프리플라이트 (A2) | `skills/bootstrap/SKILL.md`, `references/unity-mcp.md` |
| **P0** | 계측 조건 진술 의무 + 부정형 결론 금지 (B1) | `skills/verify/SKILL.md` |
| **P0** | 일시적 상태는 "일어났다"를 먼저 단언 (B2) | `references/unity-testing.md` |
| **P1** | MCP 재시도 규약 (A3) | `references/unity-mcp.md` |
| **P1** | UI 텍스트는 경고·육안 확인 필수 (B3) | `skills/verify/SKILL.md` |
| **P1** | 배선 순서 + `Verify()` 동반 (C1) | `skills/feature/SKILL.md` |
| **P1** | 검증 종료 게이트: 진단값 원복 확인 (C2) | `skills/verify/SKILL.md`, `skills/playtest/SKILL.md` |
| **P2** | 대기 규약, 비대화형 API 주의 (D1, D2) | `references/unity-testing.md`, `references/unity-mcp.md` |

---

## 잘 작동한 것 (바꾸지 말 것)

공정하게 적어둔다. 이번 세션에서 **실제로 결함을 잡아낸** 장치들이다.

- **규칙 ID 추적성** — "규칙 변경은 코드가 아니라 GAME_RULES에서 시작한다"가 DMG-005 신설로 이어졌다. 수치만 만졌으면 무기 추가 후 같은 문제가 재발했을 것이다
- **Core/Gameplay/Presentation 분리** — B6(히트스톱이 세션 정지를 덮어쓰는 문제)에서 "Gameplay가 Presentation을 참조할 수 없다"는 제약이 **더 나은 설계**(구독 방식)를 강제했다
- **한 번에 한 변수만** — EXP-003~006에서 원인을 정확히 분리해냈다. 특히 EXP-004(히트스톱 끄고 대조)는 계측기를 만드는 것보다 빠르고 정확했다
- **성공 기준을 변경 전에 작성** — EXP-005가 "실패"로 판정될 수 있었던 건 기준을 미리 적어뒀기 때문이다. 사후 판단이었으면 "좀 나아졌네"로 넘어갔을 것이다
- **증거 라벨(VERIFIED/INFERRED/UNVERIFIED)** — 히트스톱 런타임 관측을 `UNVERIFIED`로 남긴 덕에, 나중에 그 항목이 실제로 뒤집혔을 때 보고서를 정정할 근거가 남아 있었다
- **`unity-guard.sh` 훅** — YAML 직접 편집을 실제로 막았다. A1 가드도 **같은 자리에 넣으면 된다**
