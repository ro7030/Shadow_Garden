# 《그림자 정원》 Cursor 작업 브리프

문서 상태: Draft — 인간 관리자 실행 승인 전

발행일: 2026-07-28

발행자: Codex

승인자: 인간 관리자

## 1. 작업 식별

- 작업 ID: `SG-PROT-G1`
- 제목: 공개 계약·Bootstrap·Input·`Dev_SystemSandbox` 기반
- 대상 브랜치: `prototype/system-sandbox-w1`
- 선행 작업:
  1. 인간 관리자가 현재 기준선 변경을 승인한다.
  2. 인간 관리자 요청에 따라 Codex가 기준선을 커밋한다.
  3. Codex가 대상 브랜치를 만들고 해당 브랜치로 전환한다.
  4. Unity 6000.3.11f1에서 기준선 프로젝트가 Console Error 0건인 상태를 다시 확인한다.
- 실행 조건: 위 선행 작업과 인간 관리자의 이 브리프 실행 승인이 모두 확인되기 전에는 파일을 수정하거나 Cursor의 `Build Locally`를 시작하지 않는다.

## 2. 목적과 플레이어 결과

이 작업은 후속 게임플레이 시스템이 의존할 공개 타입, 어셈블리 경계, 공용 밸런스 데이터, 입력 컨텍스트와 지속형 Bootstrap을 먼저 고정한다. 실행 결과는 `Bootstrap`이 앱 수명 동안 하나만 유지되고, `Dev_SystemSandbox`가 유일한 Additive 콘텐츠 씬으로 한 번만 로드되는 최소 개발 환경이다.

플레이어 이동·태양등 조작·빛 판정은 이 작업에서 구현하지 않는다. 플레이 화면에는 고정 직교 카메라와 XZ 평면의 구분 가능한 회색 상자 바닥만 보이면 된다. 이 단계의 플레이어 결과는 “조작 가능한 퍼즐”이 아니라 다음 단계의 이동·선택·빛 시스템을 안전하게 붙일 수 있는 실행 기반이다.

## 3. 적용 계약

### 3.1 권위 문서와 요구사항

- 정본: `SG-GSA-001 v0.1 Draft`, 2026-07-27
- 기획서 요구사항 ID:
  - `REQ-SPACE-001`
  - `REQ-SPACE-002`
  - `REQ-CAMERA-001`
  - `REQ-INPUT-001`
  - `REQ-INPUT-002`
  - `REQ-INPUT-003`
  - `REQ-SCENE-002`
- 구조 계약:
  - `SG-GSA-001 §8.1`의 지속형 `Bootstrap` + 한 번에 하나의 Additive 콘텐츠 씬
  - `SG-GSA-001 §9.1`의 어셈블리·네임스페이스
  - `SG-GSA-001 §9.2`의 `GameBootstrap`, `InputRouter`, `SceneFlowService` 책임
  - `SG-GSA-001 §10.1–10.3`의 공개 enum·값 타입·인터페이스
  - `SG-GSA-001 §11.1`의 `GameBalanceConfig`
  - `SG-GSA-001 §16`의 Editor/Development Build 전용 Sandbox

`REQ-SCENE-001`의 전체 페이드·방 초기화 계약은 G1의 완료 주장에 포함하지 않는다. G1에서는 중복 요청 방지, 현재 콘텐츠 언로드, 대상 콘텐츠 Additive 로드와 입력 `Transition` 잠금까지만 기반을 만든다. 실제 페이드 프레젠테이션과 `RoomFlowController` 초기화 연결은 후속 Flow 작업에서 `REQ-SCENE-001` 전체를 검증한다.

### 3.2 공개 타입·인터페이스

다음 선언 이름, 순서, 네임스페이스와 멤버는 정본과 일치해야 한다.

- `ShadowGarden.Core`
  - `SunLampState { Inactive, Receiving, Active, Off }`
  - `ChargeState { Empty, Charging, Complete }`
  - `RoomPhase { Entering, Playing, Resolving, Exiting }`
  - `GameInputContext { Title, Gameplay, Cinematic, Pause, Transition }`
  - `PillarHeightTier { Low, Medium, High }`
  - `IRoomResettable`
  - `IRoomGoal`
  - `ICheckpointProvider`
- `ShadowGarden.Gameplay`
  - `readonly struct LightExposure`
  - `ILightReceiver`

`LightExposure`는 `SourceLampId`, `Origin`, `Direction`, `Distance`의 네 readonly 필드와 정본에 명시된 생성자를 가진다. `ILightReceiver`는 `ReceiverId`, `IsComplete`, `ReplaceExposures(IReadOnlyList<LightExposure>)`를 가진다. 나머지 세 인터페이스도 정본 §10.3의 서명을 그대로 사용한다.

### 3.3 `GameBalanceConfig`

- 메뉴 경로: `Shadow Garden/Balance Config`
- 기본 에셋: `Assets/ShadowGarden/Config/DefaultGameBalanceConfig.asset`
- 런타임 상태 저장 금지
- 필드와 기본값:

| 필드 | 기본값 | G1 검증 범위 |
|---|---:|---:|
| `playerMoveSpeed` | 4.5 | 3.5–5.5 |
| `playerAcceleration` | 36 | 28–48 |
| `playerDeceleration` | 48 | 36–64 |
| `lampSelectionRadius` | 1.6 | 1.2–2.0 |
| `selectionUnlockDelay` | 0.15 | 고정 0.15 |
| `lampStartAngularSpeed` | 30 | 20–40 |
| `lampMaxAngularSpeed` | 90 | 75–120 |
| `lampAngularAcceleration` | 120 | 90–180 |
| `lampAngularDeceleration` | 180 | 120–240 |
| `magnetAngleDegrees` | 3 | 2–5 |
| `magnetDuration` | 0.12 | 0.08–0.20 |
| `lightRange` | 9 | 7–12 |
| `lightConeAngle` | 24 | 18–32 |
| `lampActivationSeconds` | 0.4 | 고정 0.4 |
| `receiverChargeSeconds` | 0.8 | 고정 0.8 |
| `nightFlowerChargeSeconds` | 1.0 | 고정 1.0 |
| `motherFlowerChargeSeconds` | 1.5 | 1.2–2.0 |
| `incompleteChargeDecayMultiplier` | 2 | 고정 2 |
| `lowShadowLength` | 3.5 | 3.0–4.0 |
| `mediumShadowLength` | 5.0 | 4.5–5.5 |
| `highShadowLength` | 6.5 | 6.0–7.0 |
| `shadowWidth` | 1.2 | 1.0–1.6 |
| `shadowStableSeconds` | 0.15 | 고정 0.15 |

정본에 조정 범위가 명시된 필드는 `OnValidate`에서 범위 밖 값을 가장 가까운 경계값으로 보정하고 오류를 기록한다. 정본에 조정 범위가 없는 고정 필드는 다른 유한값, `NaN`, 무한대 또는 음수가 들어오면 표의 고정값으로 복원하고 오류를 기록한다. 이 검증은 Editor에서만 실행하며 런타임에 매 프레임 검사하지 않는다.

### 3.4 입력 계약

`Assets/InputSystem_Actions.inputactions`를 다음 계약으로 교체한다. 생성 C# 래퍼는 만들지 않고 `InputRouter`가 직렬화된 `InputActionAsset`을 사용한다.

| Action Map | Action | 타입 | 바인딩 |
|---|---|---|---|
| `Gameplay` | `Move` | Value / Vector2 | WASD 2D Vector Composite |
| `Gameplay` | `RotateLamp` | Value / Axis | Q = -1, E = +1 |
| `Gameplay` | `ToggleLampOff` | Button | SPACE |
| `Gameplay` | `ResetRoom` | Button | R |
| `Gameplay` | `Pause` | Button | ESC |
| `Menu` | `Submit` | Button | SPACE, ENTER |
| `Menu` | `Cancel` | Button | ESC |

`InputRouter`의 컨텍스트 동작은 다음으로 고정한다.

- `Gameplay`: `Gameplay` Action Map만 활성화한다.
- `Title`, `Pause`: `Menu` Action Map만 활성화한다.
- `Cinematic`, `Transition`: 모든 Action Map을 비활성화한다.
- 컨텍스트 전환 시 기존 Action Map을 먼저 비활성화한 후 대상 하나만 활성화한다.
- 같은 컨텍스트의 중복 요청은 상태와 이벤트를 중복 변경하지 않는다.
- 앱 또는 브라우저 포커스를 잃었을 때 현재 컨텍스트가 `Gameplay`이면 `Pause`로 전환한다. 포커스 복귀 시 자동으로 `Gameplay`로 돌아가지 않는다.
- Move와 RotateLamp는 현재 값을 읽을 수 있는 typed read-only 접근을 제공한다.
- ToggleLampOff, ResetRoom, Pause, Submit, Cancel은 같은 프레임의 동기 C# event로 전달한다.
- `InputRouter`는 입력을 전달할 뿐 플레이어·램프·방 상태를 직접 변경하지 않는다.

### 3.5 데이터 흐름

입력 데이터 흐름:

```text
Keyboard
  → Input System Action
  → InputRouter
  → 현재 GameInputContext에 허용된 값 또는 동기 이벤트
```

씬과 상태 흐름:

```text
Bootstrap scene
  → GameBootstrap 중복 검사·DontDestroyOnLoad
  → InputRouter를 Transition으로 설정
  → SceneFlowService가 Dev_SystemSandbox를 Additive 로드
  → 유일한 콘텐츠 씬 확인
  → InputRouter를 Gameplay로 설정
```

## 4. 허용 범위

### 4.1 허용 작업

- 아래 5개 어셈블리 정의와 단방향 참조 구성
  - `ShadowGarden.Core`
  - `ShadowGarden.Gameplay`
  - `ShadowGarden.Flow`
  - `ShadowGarden.Presentation`
  - `ShadowGarden.Tests`
- §3.2의 공개 계약과 §3.3의 `GameBalanceConfig` 구현
- §3.4의 Input Actions와 `InputRouter` 구현
- 중복 Bootstrap을 방지하고 앱 수명 서비스를 연결하는 `GameBootstrap` 구현
- 현재 콘텐츠 씬을 하나로 제한하고 중복 로드 요청을 거부하는 최소 `SceneFlowService` 구현
- `Bootstrap.unity`, `Dev_SystemSandbox.unity` 생성
- Sandbox에 회색 상자 바닥, 고정 직교 카메라, 방향·스케일 확인용 표식 배치
- 요구 Layer와 Build Settings 씬 순서 설정
- 이 브리프의 EditMode·PlayMode 테스트 작성

### 4.2 허용 파일 또는 폴더

- `Assets/ShadowGarden/**`
- `Assets/InputSystem_Actions.inputactions`
- `ProjectSettings/TagManager.asset`
- `ProjectSettings/EditorBuildSettings.asset`

Unity가 위 허용 경로 안에서 생성하는 `.meta` 파일도 허용한다.

### 4.3 ProjectSettings의 제한적 예외

`ProjectSettings/TagManager.asset`에는 다음 8개 Layer만 빈 사용자 Layer 슬롯에 추가한다.

1. `Walkable`
2. `SafeGround`
3. `Void`
4. `ShadowWalkable`
5. `LightReceiver`
6. `Pillar`
7. `LightObstacle`
8. `ShadowObstacle`

기존 Layer·Tag·Sorting Layer·Rendering Layer는 이름 변경, 이동 또는 삭제하지 않는다.

`ProjectSettings/EditorBuildSettings.asset`의 씬 순서는 G1에서 다음 두 개만 활성화한다.

1. `Assets/ShadowGarden/Scenes/System/Bootstrap.unity`
2. `Assets/ShadowGarden/Scenes/Development/Dev_SystemSandbox.unity`

기존 `Assets/Scenes/SampleScene.unity` 파일은 수정하거나 삭제하지 않고 Build Settings에서만 제외한다.

## 5. 금지 범위

- 기획서, 팀 헌장, Cursor 규칙과 이 브리프 변경
- 공개 계약의 이름·멤버·순서·네임스페이스 변경
- `Package.json`, `Packages/manifest.json`, `Packages/packages-lock.json` 변경
- §4.2에 없는 ProjectSettings 또는 기존 URP 설정 변경
- `Assets/Scenes/SampleScene.unity` 수정 또는 삭제
- `PlayerMotor`, `LampSelectionController`, `SunLampController`, `LightProjectionSystem` 구현
- 태양등 상태 머신, 회전, 자동 선택, 빛 노출·차폐 판정 구현
- 그림자, 수신기, 문, 밤꽃, 추락, 저장, Pause UI, Hub, WORLD 1 구현
- Cinemachine, glTFast, VFX Graph 또는 외부 DI 프레임워크를 런타임 의존성으로 사용
- 런타임 상태를 ScriptableObject에 저장
- 의미 없는 marker MonoBehaviour, 빈 프리팹 또는 향후 사용을 추측한 추상화 추가
- 승인되지 않은 아트·오디오·외부 에셋 추가
- `git branch`, `git commit`, `git push`, `git merge`, `git reset`, `git clean`, 사용자 변경 되돌리기

허용 범위를 벗어나는 변경이 필요하다고 판단되면 구현하지 말고 이유, 영향 파일과 대안을 완료 보고의 잔여 위험에 기록한다.

## 6. 구현 계약

1. 작업 시작 전 현재 브랜치가 `prototype/system-sandbox-w1`인지, 브리프 선행 작업이 충족됐는지, Git 상태에 기존 기준선 외 예상하지 못한 변경이 없는지 읽기 전용으로 확인한다.
2. `Assets/ShadowGarden` 아래에 런타임·테스트 폴더와 5개 asmdef를 만든다. `Core`는 순환 참조가 없어야 하고, `Gameplay`와 `Flow`는 `Core`를 참조할 수 있으며, `Presentation`은 `Core`와 `Gameplay`를 참조할 수 있다. `Gameplay`는 `Presentation`을 참조하지 않는다.
3. 공개 enum, 값 타입, 인터페이스와 `GameBalanceConfig`를 정본 서명 그대로 구현한다. 기본 에셋을 생성하고 모든 값이 표의 기본값인지 검사한다.
4. 기본 Input Actions를 §3.4 계약으로 교체하고 `InputRouter`를 구현한다. 입력 콜백은 활성화·비활성화 때 정확히 한 번만 등록·해제해 재진입 시 중복 이벤트가 생기지 않게 한다.
5. `GameBootstrap`은 중복 인스턴스를 거부하고 자신과 앱 수명 서비스를 `DontDestroyOnLoad`로 유지한다. 퍼즐 상태를 보유하지 않는다.
6. 최소 `SceneFlowService`는 동시에 하나의 콘텐츠 전환만 허용한다. 로드 요청 시 입력을 `Transition`으로 바꾸고, 기존 콘텐츠 씬이 있으면 언로드한 뒤 대상을 Additive로 로드하고, 성공 시 `Gameplay`로 바꾼다.
7. 초기 콘텐츠 로드는 Editor 또는 Development Build에서만 `Dev_SystemSandbox`를 대상으로 한다. 일반 제출 빌드의 진입 경로를 Sandbox에 영구 결합하는 코드는 만들지 않는다.
8. `Bootstrap` 씬에는 앱 수명 루트만 둔다. `Dev_SystemSandbox`에는 1m 기준을 읽을 수 있는 XZ 바닥, 고정 직교 카메라, 월드 +Z 방향 표식과 원점 표식을 둔다. 게임플레이 시스템과 퍼즐 장치는 두지 않는다.
9. §4.3의 Layer와 Build Settings만 변경한다.
10. 모든 필수 자동 테스트를 실행하고 Editor Game View에서 두 씬 구조와 고정 카메라 프레임을 확인한다.

예외와 실패 처리:

- `GameBalanceConfig`, `InputActionAsset`, 필수 Action Map 또는 Action이 누락되면 명확한 객체명과 필드명을 포함한 Error를 한 번 기록하고 Gameplay 진입을 막는다.
- 중복 `GameBootstrap`은 기존 앱 서비스를 보존하고 새 중복 객체만 제거한다.
- 같은 콘텐츠 씬의 중복 요청 또는 전환 중 추가 요청은 최초 요청만 수락한다.
- Additive 로드 실패 시 `Transition` 입력 잠금을 유지하고 Error를 기록한다. 임의의 다른 씬을 대신 로드하지 않는다.
- 입력 포커스 상실 처리와 이벤트 구독 해제는 Editor Play Mode 재진입 후 중복 호출을 만들지 않는다.

## 7. 필수 검증

### 7.1 EditMode

- `G1-EDIT-001 PublicContracts_MatchCanonicalSignatures`
  - enum 이름·순서·값, `LightExposure` 필드·생성자, 네 인터페이스의 공개 멤버를 검사한다.
- `G1-EDIT-002 GameBalanceConfig_DefaultAssetMatchesCanonicalValues`
  - 기본 에셋의 모든 필드가 §3.3의 기본값과 일치하는지 검사한다.
- `G1-EDIT-003 GameBalanceConfig_OnValidateClampsAndReports`
  - 범위형 값은 양쪽 경계로 보정되고, 고정형 값은 기본값으로 복구되는지 검사한다.
- `G1-EDIT-004 AssemblyDefinitions_HaveNoForbiddenCycle`
  - 5개 asmdef 이름과 참조 방향을 검사한다.
- `G1-EDIT-005 InputActions_ContainOnlyApprovedKeyboardBindings`
  - Action Map, Action 타입과 키보드 바인딩을 검사하고 게임패드 바인딩이 없음을 확인한다.

### 7.2 PlayMode

- `G1-PLAY-001 Bootstrap_LoadsSandboxAdditivelyExactlyOnce`
  - `Bootstrap`이 유지되고 `Dev_SystemSandbox`가 한 번만 Additive로 로드되며, 다른 콘텐츠 씬은 동시에 로드되지 않는다.
- `G1-PLAY-002 DuplicateBootstrap_PreservesSingleServiceSet`
  - 중복 Bootstrap을 생성해도 `GameBootstrap`, `InputRouter`, `SceneFlowService`가 각각 하나만 남는다.
- `G1-PLAY-003 InputRouter_EnablesOnlyCurrentContext`
  - Gameplay는 Gameplay Map만, Title·Pause는 Menu Map만, Cinematic·Transition은 어떤 Map도 활성화하지 않는다.
- `G1-PLAY-004 InputRouter_DoesNotDuplicateCallbacksAfterReenable`
  - 비활성화·재활성화 후 Button 입력 한 번에 이벤트가 정확히 한 번 발생한다.
- `G1-PLAY-005 FocusLoss_FromGameplayEntersPauseOnly`
  - Gameplay에서 포커스를 잃으면 Pause가 되고 포커스 복귀만으로 Gameplay가 되지 않는다.
- `G1-PLAY-006 SceneFlow_RejectsConcurrentAndDuplicateRequests`
  - 전환 중 추가 요청과 동일 콘텐츠 중복 요청이 두 번째 로드·언로드를 만들지 않는다.

### 7.3 시각·씬·플랫폼

- Hierarchy에서 `Bootstrap`과 `Dev_SystemSandbox`만 로드되고 전자는 앱 수명 루트, 후자는 콘텐츠 루트임을 확인한다.
- 1920×1080 Game View에서 고정 직교 카메라가 XZ 바닥, 원점과 +Z 표식을 한 화면에 잘림 없이 보여준다.
- 플레이 중 카메라는 이동·회전·자동 줌하지 않는다.
- Unity Console Error 0건을 확인한다.
- 새 Warning이 있으면 원인과 유지 사유를 보고한다. 원인 불명의 새 Warning은 합격하지 않는다.
- macOS Editor에서 EditMode·PlayMode 전체 테스트를 실행한다.
- Desktop WebGL Development Build가 성공하고 `Bootstrap`에서 Sandbox가 열리는지 Chrome 1920×1080으로 스모크 테스트한다.

### 7.4 합격 기준

- 모든 G1 자동 테스트 통과
- Unity Console Error 0건
- 원인 불명의 신규 Warning 0건
- `Bootstrap`이 Build Settings 첫 씬
- 앱 수명 서비스가 각각 정확히 1개
- 로드된 Additive 콘텐츠 씬이 정확히 1개
- 활성 Input Action Map이 현재 컨텍스트 계약과 일치
- `GameBalanceConfig` 기본 에셋과 검증 동작이 정본과 일치
- 허용 목록 밖 파일 변경 0건
- WebGL Development Build 성공

## 8. 제출 증거

- 변경·생성 파일 전체 목록
- 구현한 요구사항 ID와 구조 계약별 설명
- 작업 전후 `git status --short`와 `git diff --stat`
- 각 테스트 이름, 실행 환경, 통과·실패 결과
- Test Runner 전체 결과 캡처
- Unity Console Error·Warning 개수와 메시지 요약
- Play Mode Hierarchy 캡처
- 1920×1080 Game View 캡처
- Build Settings 씬 순서 캡처
- Layer 목록 캡처 또는 설정 diff
- WebGL Development Build 경로, 빌드 시간, 성공 로그와 Chrome 스모크 결과
- 잔여 위험과 알려진 제한
- 브리프 허용 목록 밖 파일을 변경하지 않았다는 확인
- branch, commit, push, merge, reset, clean을 실행하지 않았다는 확인

“파일을 만들었다”, “테스트가 통과했다”는 서술만으로 완료로 인정하지 않는다. 실제 저장소 파일, Unity Test Runner, Console, Game View와 빌드 로그가 검수 기준이다.

## 9. 완료 후 보고 형식

Cursor는 아래 순서로 한 번만 보고한 뒤 추가 수정이나 다음 작업을 시작하지 않고 Codex 검수를 기다린다.

1. 작업 ID와 한 문장 결과
2. 구현한 요구사항·구조 계약
3. 변경·생성 파일 전체 목록
4. 구현 동작 요약
5. EditMode 테스트 결과
6. PlayMode 테스트 결과
7. Console과 WebGL 빌드 결과
8. 캡처·로그·빌드 산출물 경로
9. 알려진 제한과 잔여 위험
10. 금지된 Git 작업을 하지 않았다는 확인

Codex 검수와 인간 관리자 플레이 승인이 끝나기 전에는 G2, 리팩터링, 아트·사운드 추가 또는 Git 작업으로 넘어가지 않는다.
