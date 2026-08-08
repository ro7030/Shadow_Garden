# 그림자 정원 (Shadow Garden)

태양등을 90° 회전해 기둥의 그림자를 길로 만들고, 제한 시간 안에 출구 또는 밤꽃에 도달하는 **비전투 2D 탑다운 그리드 퍼즐**입니다.

## 바로 확인하기

- [WebGL 플레이](https://ro7030.github.io/Shadow_Garden/)
- [실제 플레이 영상 — 32초](https://ro7030.github.io/Shadow_Garden/media/shadow-garden-nan2026-gameplay.mp4)
- [전체 소스와 커밋 기록](https://github.com/ro7030/Shadow_Garden)

WebGL은 데스크톱 Chrome 또는 macOS Safari, 1280×720 이상의 화면과 키보드·마우스를 권장합니다. 로딩 완료 후 `플레이 시작`을 클릭해야 캔버스 포커스와 오디오가 활성화됩니다.

## 게임 구성

- 3개 월드 × 4개 스테이지, 총 12개 독립 퍼즐 보드
- 월드 1·2는 12×6, 월드 3은 최대 18×8의 가변 격자
- x-1~x-3은 출구 문 도달, x-4는 밤꽃 도달이 목표
- 출구 스테이지 120초, 밤꽃 스테이지 150초
- 점수·별·체력·전투 없이 완료 여부와 최고 기록만 저장

기둥의 높이에 따라 그림자는 낮음 2칸, 중간 3칸, 높음 4칸으로 생성됩니다. 같은 채널(`○ △ ☆ ◇`)의 기둥은 하나의 태양등에 함께 반응합니다. 공백의 그림자가 한 겹이면 길, 두 겹 이상이면 중첩 위험, 그림자가 없으면 절벽입니다.

## 조작

| 입력 | 기능 |
|---|---|
| `WASD` | 상하좌우 한 칸 이동 |
| `Q` / `E` | 태양등 칸에서 90° 회전 |
| `R` | 현재 스테이지 즉시 초기화 |
| 방향키 / `WASD` | 메뉴 포커스 이동 |
| `Enter` / `Space` | 현재 선택 확인 |

## 개발 환경

- Unity `6000.3.11f1`
- Universal Render Pipeline `17.3.0`
- Input System `1.19.0`
- uGUI `2.0.0`
- Test Framework `1.6.0`
- 대상 플랫폼: 데스크톱 WebGL

Unity Hub에서 이 저장소를 Unity 6000.3.11f1 프로젝트로 열고 `Assets/Scenes/Main.unity`를 실행하면 됩니다. 최종 빌드에는 Main 씬만 포함되며 `TestField`와 `GrayboxStages`는 기술 검증용으로 보존되어 있습니다.

## 구조

- `ShadowGarden.Core`: UnityEngine을 참조하지 않는 그림자 계산·이동 판정·상태 복원
- `ShadowGarden.Runtime`: 입력, 앱 상태, 타이머, 저장과 Unity 수명주기 연결
- `ShadowGarden.Presentation`: 보드·캐릭터·HUD·VFX·오디오 표현
- `Assets/Content/Stages`: 본편 12개 스테이지 데이터
- `Assets/Game`: 런타임 아트·UI·오디오
- `Tools/ArtSources`: 에셋 출처·후처리·동결 해시 기록

## AI 활용과 라이선스

- ChatGPT/Codex: 기획 구조화, 에셋 제작·통합, 검수와 제출 준비
- Cursor + Grok 4.5: Unity 구현, 테스트 반복, WebGL QA
- Gemini: 로비·월드별 BGM 후보 제작
- OpenAI 이미지 생성: 캐릭터·월드·오브젝트 원본 후보 제작

AI 결과는 사람이 선별·수정·후처리·검증했습니다. 세부 제작 출처는 [`Tools/ArtSources/ASSET_PROVENANCE.md`](Tools/ArtSources/ASSET_PROVENANCE.md), 동결 기준과 무결성은 [`Tools/ArtSources/ASSET_FREEZE_v1.0.md`](Tools/ArtSources/ASSET_FREEZE_v1.0.md)에서 확인할 수 있습니다.

게임 UI는 Noto Sans KR Regular/Bold를 사용하며 [SIL Open Font License 1.1](https://openfontlicense.org/) 조건을 따릅니다. Unity 패키지 버전은 [`Packages/manifest.json`](Packages/manifest.json)에 기록되어 있습니다.

## NAN 2026 제출 안내

이 저장소는 NAN 2026 사전 과제의 전체 Unity 소스와 변경 이력을 공개하기 위해 유지됩니다. `docs/`는 GitHub Pages용 최종 WebGL Release이며, 75초 예상 플레이 시뮬레이션은 제출 영상에 포함하지 않았습니다.
