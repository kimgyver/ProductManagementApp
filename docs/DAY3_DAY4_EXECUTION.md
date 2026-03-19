# Day 3~4 실행 계획 및 API Key 인증 도입

작성일: 2026-03-19

## Day 3~4 권장 작업

- 비동기 정확성 정리:
  - `UsersController.Index()` 반환 로직 정리
  - `UserQueryService.AuthenticateUserAsync()` 내부 `GenerateSessionAsync` await 적용
- Day 2 변경 반영 후 테스트 실행:
  - 인증/로그인 관련 테스트 케이스 우선 추가 후 실행

## Clean Architecture(온ion Architecture) 계층 분리 및 의존성 방향 준수 점검/적용

- Controller → Application(Service) → Domain → Infrastructure 흐름이 실제 코드에 맞게 적용되어 있는지 점검 및 리팩터링
- 의존성(컴파일 타임) 방향이 Infrastructure → Application → Domain만 허용되는지 확인
- 각 계층별 책임(Controller: 입출력, Application: 비즈니스 로직, Domain: 엔티티/도메인 서비스, Infrastructure: 외부 연동/저장소) 명확화

---

## API Key 인증 도입 계획

- **API Key 발급/관리 주체:** API 서버
- **API Key를 받아 사용하는 주체:** 클라이언트/워커

### API Key 발급 및 사용 방식 예시

1. 관리자가 서버에 등록(생성) 요청 → 서버가 고유 API Key를 생성/저장
2. 클라이언트/워커에 해당 Key를 전달(환경변수, 설정파일 등)
3. 클라이언트/워커는 API 호출 시 X-API-KEY 헤더에 Key를 포함
4. 서버는 요청마다 Key의 유효성 검증

> 참고: API Key는 노출/유출에 취약하므로, 키 관리와 접근 제한(예: IP 제한, 만료 등)도 함께 고려해야 함
