# 그림자 정원 에셋·연출 동결 기준 v1.0

- 동결일: 2026-08-06
- Unity: 6000.3.11f1
- 상태: **APPROVED / FROZEN**
- 범위: 런타임 아트·오디오·Sprite Atlas·PresentationData·Resources 바인딩

## 동결 목적

현재 Main 플레이 흐름에 연결된 표현 자산을 제출 기준선으로 고정한다. 이후 7단계 오프닝·엔딩·WebGL 셸과 8단계 최종 QA는 이 자산을 소비만 하며, 명백한 출시 차단 결함 외에는 원본·메타데이터·GUID·바인딩을 변경하지 않는다.

Core 규칙, 12개 스테이지 좌표, 타이머, 저장과 해금 데이터는 이 동결 범위에 포함되지 않으며 이번 패스에서도 변경하지 않았다.

## 동결 수량

| 분류 | 수량 | 위치 |
|---|---:|---|
| 런타임 PNG | 106 | `Assets/Game/Art/Common`, `Assets/Game/Art/Worlds` |
| 오디오 WAV | 22 | `Assets/Game/Audio` |
| Sprite Atlas | 6 | `Assets/Game/Atlases` |
| 월드 아트 세트 | 3 | `Assets/Resources/Presentation/Worlds` |
| 스테이지 바인딩 | 12 | `Assets/Resources/Presentation/Bindings` |
| 공용 카탈로그 | 1 | `Assets/Resources/Presentation/InGameAssetCatalog.asset` |
| 모아 애니메이션 세트 | 1 | `Assets/Game/PresentationData/MoaAnimationSet.asset` |
| VFX 세트 | 1 | `Assets/Game/PresentationData/GameplayFxSet.asset` |
| 오디오 세트 | 1 | `Assets/Game/PresentationData/AudioSet.asset` |

## 제작 출처

- 회화풍 래스터 자산: 이 프로젝트 전용 OpenAI 이미지 생성 결과를 로컬에서 크로마키 제거, 디스필, 알파 정리, 크롭, 크기 정규화하여 제작
- UI 원본: 프로젝트 전용 결정론적 SVG·벡터 원본
- 오디오: 프로젝트 전용으로 직접 합성한 WAV
- 외부 게임 에셋·타사 아티스트 명시 화풍·워터마크 자산 미사용
- 세부 출처와 후처리는 `Tools/ArtSources/ASSET_PROVENANCE.md` 참조

## 감사 결과

### 모아와 공용 게임플레이

- 이동 6프레임, 표정 6종, 행동 포즈 6종의 얼굴·남색 망토·황동 장식·접지선 일관성 확인
- 낮음·중간·높음 기둥은 동일 지름과 공통 접지선을 사용하고 높이만 명확하게 구분
- `○ △ ☆ ◇` 문양은 기둥 상단 원과 태양등 중앙에 고정
- 태양등 화살표는 채널 문양과 겹치지 않으며 방향 판독 가능
- 생성 문자·워터마크·크로마 잔색·캐릭터 정체성 드리프트 없음

### 세 월드

- 노을 과수원·바람종 협곡·별뿌리 온실이 배경, 타일, 장식, 목표 자산만으로 구분됨
- 타일 이음새, 배경 크롭, 앞·뒤 장식 분리, 문 닫힘·열림과 밤꽃 닫힘·개화 상태 확인
- 12개 StagePresentationBinding이 각 스테이지의 정확한 월드 세트를 선택

### 그림자·VFX·오디오

- 단일 그림자, 중첩 위험, 절벽의 알파 경계와 상태 판독 확인
- 회전 스윕, 위험 맥동, 침몰, 낙하 먼지, 시간 흡입, 문 빛, 꽃잎, 개화광, 완료광의 독립 참조 확인
- 연출 계약: 회전 0.18초, 중첩 0.55초, 절벽 0.35칸+0.5초, 시간 흡입 0.65초, 문 0.45초+통과 0.35초, 밤꽃 1.5초
- BGM 4개, 환경음 3개, 효과음 15개 연결 확인
- 측정 피크: 음악 약 -1.51 dBFS, 환경음 약 -6.25 dBFS, 효과음 약 -2.02~-2.05 dBFS
- 루프 경계 최대 샘플 차: 음악 0.008118, 환경음 0.004456

## Unity 연결·임포트 계약

- `stageId → StagePresentationBindingAsset → WorldArtSetAsset` 연결 완료
- Final Asset Library 빌더를 연속 두 번 실행한 결과 동결 대상 338개 파일의 해시가 동일함
- Sprite: 128 PPU, Bilinear, Mip Map 비활성화, Clamp, 지정 피벗
- Atlas: 최대 2048×2048, 패딩 4px, 회전 패킹 비활성화
- 장시간 오디오: Compressed In Memory / Vorbis
- 짧은 효과음: Decompress On Load / PCM / Preload
- Production Main의 카탈로그·모아·VFX·오디오·월드·스테이지 참조가 모두 채워짐
- 누락 참조용 폴백은 유지하지만 12개 본편 스테이지 캡처에서 실제 사용 0건

## 시각 검수 증거

- 12개 스테이지 1280×720:
  `Temp/ShadowGardenQA/Freeze_Stage_{stageId}_1280x720.png`
- 1-1, 1-4, 2-2, 2-4, 3-1, 3-4의 1920×1080:
  `Temp/ShadowGardenQA/Freeze_Stage_{stageId}_1920x1080.png`
- 전체 접촉 시트:
  `Temp/ShadowGardenQA/Freeze_Stages_1280_Contact.png`
  `Temp/ShadowGardenQA/Freeze_Stages_1920_Contact.png`
- UI 상태:
  `6_1_Title.png`, `6_1_Opening.png`, `6_1_GameOver.png`,
  `UI_WorldMap_*.png`, `UI_Pause.png`,
  `SG_Settings_Final_Approved.png`, `SG_Cleared_Door_Final.png`

12×6 최상단의 높은 기둥이 잘리던 문제는 레벨 데이터를 바꾸지 않고 Main 카메라의 상단 표현 여백 1.5셀로 보완했다. 12개 보드 재캡처에서 높은 기둥·문양·HUD가 모두 프레임 안에 있음을 확인했다.

## 자동 검증

- 런타임 수량, 참조 완전성, Sprite 임포트, 오디오 임포트, Atlas, 바인딩 검사를 EditMode 계약으로 고정
- 12개 본편 보드 캡처를 PlayMode 계약으로 고정
- Main 씬 Missing Script·Missing Reference 없음
- 전체 EditMode 105/105, PlayMode 36/36 통과
- `git diff --check` 통과

## 변경 허용 범위

동결 후 허용:

1. 재현 가능한 출시 차단 결함의 최소 수정
2. 수정 자산 단위의 시각·자동 회귀 검증
3. 이 문서에 원인·파일·검증 결과 추가
4. `ASSET_FREEZE_v1.0.sha256` 재생성 및 전체 검증

동결 후 금지:

- 새 자산 종류·새 캐릭터·새 규칙 추가
- 단순 취향에 따른 재생성·팔레트 교체
- GUID 변경, 무단 이동·이름 변경, 바인딩 재생성
- 해시와 기록을 갱신하지 않은 자산 수정

## 출시 차단 결함 보완 기록

- `2026-08-08` — 로비·세 월드의 최종 Gemini BGM을 `AudioSet.asset`에 연결한 후 해당 바인딩 해시를 갱신했다. 트랙 수, 게임 규칙, 스테이지 데이터와 다른 동결 자산은 변경하지 않았다.
- 같은 날 WebGL 오프닝 진행률의 `%` 글리프 누락을 막기 위해 TMP 폰트 코퍼스와 자동 검증을 보강했다. 폰트 SDF는 이 문서의 338개 런타임 아트·오디오·바인딩 해시 범위 밖이며, EditMode 109/109와 PlayMode 44/44로 최종 회귀를 확인했다.

## 무결성 확인

프로젝트 루트에서 다음 명령으로 검사한다.

```bash
shasum -a 256 -c Tools/ArtSources/ASSET_FREEZE_v1.0.sha256
```

한 항목이라도 실패하면 7·8단계 작업을 시작하지 않고 원인을 먼저 보고한다.
