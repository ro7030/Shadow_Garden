# Cursor 작업 브리프 템플릿

이 파일을 복사해 작업별 브리프를 작성한다. 발행 브리프에는 `{PLACEHOLDER}`를 하나도 남기지 않는다.

## 1. 작업 식별

- 작업 ID: `{TASK_ID}`
- 제목: `{TITLE}`
- 발행자: Codex
- 승인자: 인간 관리자
- 대상 브랜치: `{BRANCH}`
- 선행 작업: `{PREREQUISITES}`

## 2. 목적과 플레이어 결과

`{PURPOSE_AND_PLAYER_OUTCOME}`

## 3. 적용 계약

- 기획서 요구사항 ID: `{REQUIREMENT_IDS}`
- 공개 타입·인터페이스: `{PUBLIC_CONTRACTS}`
- 기본값과 허용 범위: `{BALANCE_VALUES}`
- 입력 데이터 흐름: `{INPUT_DATA_FLOW}`
- 출력·이벤트 흐름: `{OUTPUT_EVENT_FLOW}`

## 4. 허용 범위

`{ALLOWED_SCOPE}`

허용 파일 또는 폴더:

- `{ALLOWED_PATH_1}`
- `{ALLOWED_PATH_2}`

## 5. 금지 범위

- 기획서와 공개 계약 변경
- Package Manifest와 ProjectSettings 변경
- 브리프에 없는 다른 시스템 수정
- `git commit`, `git push`, `git merge`
- `{TASK_SPECIFIC_FORBIDDEN_SCOPE}`

## 6. 구현 계약

1. `{IMPLEMENTATION_STEP_1}`
2. `{IMPLEMENTATION_STEP_2}`
3. `{IMPLEMENTATION_STEP_3}`

예외와 실패 처리:

- `{EDGE_CASE_1}`
- `{EDGE_CASE_2}`

## 7. 필수 검증

EditMode:

- `{EDITMODE_TEST_1}`
- `{EDITMODE_TEST_2}`

PlayMode:

- `{PLAYMODE_TEST_1}`
- `{PLAYMODE_TEST_2}`

시각·에셋·사운드:

- `{PRESENTATION_CHECK_1}`
- `{PRESENTATION_CHECK_2}`

합격 기준:

- Unity Console Error 0건
- `{ACCEPTANCE_CRITERION_1}`
- `{ACCEPTANCE_CRITERION_2}`

## 8. 제출 증거

- 변경·생성 파일 전체 목록
- 구현한 요구사항 ID별 설명
- 테스트 이름, 실행 환경, 통과·실패 결과
- Console Error·Warning 요약
- `{REQUIRED_SCREENSHOT_LOG_OR_ASSET_EVIDENCE}`
- 잔여 위험과 알려진 제한
- commit, push, merge를 하지 않았다는 확인

## 9. 완료 후 행동

결과를 보고한 뒤 추가 수정이나 다음 작업을 시작하지 말고 Codex 검수를 기다린다.
