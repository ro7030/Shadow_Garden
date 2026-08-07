# Cursor Grok 4.5 인계 — 7단계·8단계

이 문서는 에셋 동결 뒤 Cursor Grok 4.5에 순서대로 전달할 실행 명령문이다. 7단계가 완료되고 사용자가 결과를 확인한 뒤에만 8단계를 전달한다.

## 7단계 — 오프닝·엔딩·WebGL 셸

```text
이번 작업은 《그림자 정원》의 오프닝·엔딩·WebGL 외곽 셸을 제출 가능한 상태로 완성하는 7단계 작업이다.

프로젝트:
 <프로젝트_루트>

기획서:
 <기획서_폴더>

먼저 반드시 읽을 자료:
- 그림자_정원_컨셉 기획서.docx
- 그림자_정원_UI·UX 기획서.docx
- 그림자_정원_아키텍처 기획서.docx
- Tools/ArtSources/ASSET_FREEZE_v1.0.md
- Tools/ArtSources/ASSET_FREEZE_v1.0.sha256
- AGENTS.md가 있으면 그 지침

운영 원칙:
- 계획만 제안하지 말고 완료 조건까지 분석·구현·테스트·수정을 반복하라.
- 사용자에게 중간 승인을 요청하지 말고 외부 차단 문제가 있을 때만 멈춰라.
- 별도 브랜치 생성, 자동 커밋, Push를 하지 마라.
- 기존 사용자 변경을 reset, restore, checkout, revert하지 마라.
- 동결된 아트·오디오·Atlas·PresentationData를 수정하지 마라.
- Core 규칙, 12개 레벨 좌표, 타이머, 저장·해금 구조를 변경하지 마라.
- 새로운 게임 규칙·캐릭터·전투·수집물을 추가하지 마라.
- TestField와 GrayboxStages는 보존하라.
- Main에서 OnGUI를 사용하지 마라.

1. 시작 전 검증
- git status와 현재 diff를 읽고 사용자 변경을 보존하라.
- ASSET_FREEZE_v1.0.sha256을 검증하고 불일치가 있으면 작업하지 말고 보고하라.
- 현재 EditMode·PlayMode 테스트를 실행해 기준선을 확인하라.
- 현재 오프닝·엔딩 상태 흐름과 WebGL 빌드 설정을 전부 읽어라.

2. 오프닝 완성
- 첫 실행에서만 자동 진입하는 기존 6페이지 정원사 작업 노트 구성을 유지하라.
- 짧은 말풍선과 행동·환경 반응 중심으로 표현하고 음성을 추가하지 마라.
- 전체 비플레이 시간이 45~60초를 넘지 않게 하라.
- Enter, Space 또는 마우스 1초 유지로 건너뛰게 하라.
- 건너뛰기 진행 상태를 명확하게 표시하라.
- 건너뛰기와 정상 완료 모두 WorldMap으로 정확히 전환하고 openingSeen을 저장하라.
- 타이틀의 ‘오프닝 다시 보기’는 저장 진행도를 손상시키지 않아야 한다.
- 패널·본문·버튼은 모든 재진입에서 정원사 노트 패널 내부에 유지하라.

3. 엔딩 완성
- 3-4 밤꽃 개화 뒤 8~12초의 세 월드 회복 연출을 구성하라.
- 동결된 배경·모아 포즈·밤꽃·VFX만 사용하라.
- 엔딩 종료 후 ‘타이틀’과 ‘월드 선택’ 모두 정상 동작해야 한다.
- Ending → Title → Opening/WorldMap 반복 진입에서도 UI 좌표·리스너·포커스가 중복되지 않아야 한다.

4. WebGL 전용 셸
- Unity 6000.3.11f1의 기본 WebGL 템플릿 구조를 실제 설치본 또는 현재 빌드에서 확인한 뒤 Assets/WebGLTemplates/ShadowGarden에 전용 템플릿을 제작하라. 플레이스홀더 문법을 추측하지 마라.
- 게임명, 실제 로딩 진행률, ‘플레이 시작’, 오류 안내, ‘다시 불러오기’, 권장 브라우저를 제공하라.
- 모바일 또는 세로 화면에는 데스크톱 실행 안내를 표시하라.
- Canvas를 16:9 중앙 정렬하고 검은 여백 대신 게임 팔레트 배경을 사용하라.
- matchWebGLToCanvasSize=true, devicePixelRatio=1을 적용하라.
- WebGLInput.captureAllKeyboardInput=false를 유지하라.
- 플레이 시작 클릭으로 캔버스 포커스와 오디오를 활성화하라.
- 포커스 이탈 시 게임 타이머를 정지하고 ‘게임 화면을 클릭해 계속’을 표시하라.
- GitHub Pages에서 동작하도록 Unity WebGL Decompression Fallback을 실제 PlayerSettings/빌드 설정으로 적용하라.
- Main 씬만 최종 빌드에 포함하라.

5. 테스트
- 첫 저장, 기존 저장, 오프닝 정상 완료, 건너뛰기, 다시 보기
- 3-4 완료, 엔딩 정상 완료, 엔딩 후 타이틀·월드맵 복귀
- 새로고침, 첫 클릭 전후 오디오, 포커스 이탈·복귀
- 1280×720, 1366×768, 1440×900, 1920×1080
- Chrome과 macOS Safari
- EditMode·PlayMode 전체 회귀
- Console Error·Exception, Missing Reference 0건
- 작업 후 ASSET_FREEZE_v1.0.sha256 재검증

Development WebGL 후보를 Builds/WebGL-Stage7-Candidate에 생성하라.

완료 보고:
- 변경 파일과 구현 기능
- 실행한 자동 테스트 결과
- Chrome·Safari 확인 결과
- 수동 확인이 필요한 항목
- ASSET_FREEZE 해시 동일 여부
- 알려진 위험
을 정리하고 8단계는 시작하지 말고 멈춰라.
```

## 8단계 — 최종 QA·Release

```text
이번 작업은 《그림자 정원》의 기능을 추가하지 않고 제출용 WebGL Release가 안정될 때까지 검증하고 수정하는 최종 8단계다.

프로젝트:
 <프로젝트_루트>

기준:
- Unity 6000.3.11f1
- 데스크톱 WebGL
- Chrome와 macOS Safari 필수
- 최소 1280×720
- 압축 Release 80MB 이하
- 전체 첫 플레이 15~25분
- Tools/ArtSources/ASSET_FREEZE_v1.0.md
- Tools/ArtSources/ASSET_FREEZE_v1.0.sha256

운영 원칙:
- 계획만 작성하지 말고 실패 원인 수정과 재검증을 반복하라.
- 별도 브랜치, 자동 커밋, Push를 하지 마라.
- 기존 사용자 변경을 삭제하거나 되돌리지 마라.
- 새 기능, 새 규칙, 새 레벨, 새 에셋을 추가하지 마라.
- 테스트를 삭제하거나 검증 강도를 낮추지 마라.
- 동결 자산은 출시 차단 결함이 아닌 이상 수정하지 마라.
- 자동화할 수 없는 항목을 임의로 통과 처리하지 마라.

1. 기준선 검사
- git status, diff, Build Settings, PlayerSettings를 확인하라.
- ASSET_FREEZE SHA-256을 검사하라.
- 전체 EditMode·PlayMode 테스트를 실행하라.
- 모든 StageDefinitionAsset과 12개 Presentation Binding을 검사하라.
- Main 씬의 Missing Script, Missing Reference, TMP 글리프, Atlas, AudioClip 설정을 검사하라.

2. 전체 기능 회귀
- 신규 저장으로 Title → Opening → WorldMap → 1-1부터 3-4 → Ending을 진행하라.
- 각 스테이지의 안전 해답이 제한 시간 안에 완료되는지 자동 검증하라.
- 중첩 그림자·절벽·시간 초과를 각각 최소 한 번 발생시켜 정확한 사망 문구와 복구 버튼을 검사하라.
- R, 다시 도전, 레벨 선택, 일시정지, 포커스 이탈, 완료, 해금, 최고 기록을 검사하라.
- 골 도달과 0초가 같은 프레임이면 골 우선인지 확인하라.
- 저장 후 종료·재실행·새로고침에서도 진행도와 설정이 유지되는지 확인하라.

3. 화면·접근성 QA
- 1280×720, 1366×768, 1440×900, 1920×1080과 16:10을 검사하라.
- 12×6과 18×8 보드에서 HUD가 채널·기둥·그림자·목표를 가리지 않아야 한다.
- 타이틀, 오프닝 6페이지, 월드맵, HUD, 일시정지, 설정, 세 게임 오버, 두 완료 화면, 엔딩을 캡처하라.
- 텍스트 잘림, 패널 이탈, 중복 UI, 포커스 누락, 마우스 클릭 영역을 검사하라.
- WASD·방향키로 하이라이트가 이동하고 Enter·Space가 현재 하이라이트 버튼만 실행해야 한다.
- 최소 텍스트 16px, 버튼 높이 44px, 포커스 윤곽 3px를 유지하라.
- 모션 완화 시 점멸·맥동이 없어야 한다.

4. WebGL QA
- Development Build를 로컬 HTTP 서버에서 실행하라. file://로 검사하지 마라.
- Chrome과 Safari에서 로딩, 플레이 시작, 오디오, 키보드, 마우스, 저장, 새로고침, 포커스, 전체화면을 확인하라.
- 브라우저 Console Error와 Unity Exception이 없어야 한다.
- 첫 사용자 입력 전에 BGM·환경음이 재생되지 않아야 한다.
- GitHub Pages 조건에서 압축 해제 폴백이 동작해야 한다.
- 지속적인 프레임 저하, 반복 GC 스파이크, 오디오 누수와 오브젝트 누적을 검사하라.

5. Release 제작
- 실패를 발견하면 같은 단계에서 수정한 후 관련 테스트와 전체 회귀 테스트를 다시 실행하라.
- Development 옵션, 디버그 HUD, TestField, GrayboxStages를 최종 빌드에서 제외하라.
- Main만 Build Settings에 포함하라.
- 미참조 에셋과 불필요한 디버그 출력을 검사하라.
- 최종 Release를 Builds/WebGL-Final-Release에 생성하라.
- 압축 빌드가 80MB 이하인지 기록하라.
- index.html부터 data, framework, wasm까지 전체 파일 존재와 로딩을 확인하라.
- ASSET_FREEZE SHA-256을 마지막으로 다시 검증하라.

완료 보고:
- EditMode·PlayMode 통과 수
- 12개 스테이지 해답 검증 결과
- 전체 플레이 시간
- 세 게임 오버·저장·엔딩 결과
- Chrome·Safari와 해상도별 결과
- 브라우저 Console·Unity Console 결과
- Release 크기와 실행 경로
- ASSET_FREEZE 해시 결과
- 자동화하지 못한 사람 초견 테스트 목록
- 제출 전 사용자가 직접 확인할 최종 체크리스트

첫 Release 생성만으로 종료하지 말고, 발견된 오류를 수정한 뒤 전체 회귀를 재실행하라.
```
