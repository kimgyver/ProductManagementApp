# Day 1~2 실행 결과 (실전 유지 루틴)

작성일: 2026-03-11

**[최신 상태]**
2026-03-19 기준, 아래 Day 1~2의 모든 정리/수정/테스트 작업이 실제 코드에 반영 및 완료되었습니다.
모든 테스트가 통과하였으며, 문서의 모든 항목이 구현 완료되었습니다.

## 목적

- Day 1: 코드 정리 대상을 식별하고 우선순위를 정한다.
- Day 2: 인증 설정의 불일치를 제거해 런타임 오류 가능성을 낮춘다.

---

## Day 1 — 정리 대상 목록

### 1) 네이밍/오타

- `API/src/Services/UserQeuryService.cs`
  - 파일명 오타(`Qeury`) → `UserQueryService.cs`로 정리 필요
  - 관련 인터페이스/DI 참조와 함께 일괄 정리 권장

### 2) 비동기 호출 일관성

- `API/src/Controllers/UsersController.cs`
  - `Index()`에서 `_userQueryService.GetAllUsersAsync()` 반환 사용 방식 점검 필요
- `API/src/Services/UserQeuryService.cs`
  - `_sessionService.GenerateSessionAsync(...)` 호출 시 `await` 누락 점검 필요

### 3) 테스트 코드 정리

- `API.Test/ProductsControllerTests.cs`
  - `[Fact]` 누락 테스트 존재(실행되지 않는 테스트)
  - 주석 처리된 테스트 블록 정리 필요
- `API.Test/UnitTest1.cs`
  - 빈 테스트 템플릿 정리 필요

### 4) 기타 코드 품질

- `BackgroundProcessor/Services/EmailFailureBackgroundWorker.cs`
  - 미사용 using 존재 여부 점검(이번 Day 2 작업에서 1건 정리 완료)

---

## Day 2 — 인증 설정 일관화 결과

### 인증 키 기준

- 표준 키 경로: `Jwt:Secret`
- 적용 이유: `appsettings.json` 두 프로젝트 모두 `Jwt` 섹션 사용

### 수정된 파일

- `API/src/Services/JwtService.cs`
  - `GetClientToken()` 비교 키를 `JWT:Secret` → `Jwt:Secret`으로 변경
- `BackgroundProcessor/Services/EmailFailureBackgroundWorker.cs`
  - `GetClientJwtTokenAsync()`에서 읽는 키를 `JWT:Secret` → `Jwt:Secret`으로 변경
  - 미사용 using 1건 제거

### 인증 설정 표 (현재 기준)

| 구분         | 키             | 사용 위치                                          |
| ------------ | -------------- | -------------------------------------------------- |
| JWT Issuer   | `Jwt:Issuer`   | API 토큰 발급                                      |
| JWT Audience | `Jwt:Audience` | API 토큰 검증                                      |
| JWT Secret   | `Jwt:Secret`   | API 토큰 발급/검증, Worker client 로그인 요청 생성 |

---


## 다음 권장 작업 (Day 3~4 연계)

- Day 3~4 실행 계획 및 API Key 인증 도입 관련 내용은 docs/DAY3_DAY4_EXECUTION.md 문서로 분리되었습니다.
