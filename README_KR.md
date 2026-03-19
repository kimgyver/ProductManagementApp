# 프로젝트 구성

- API: WebAPI 프로젝트
- BackgroundProcessor: 백그라운드 워커 프로젝트
- API.Test: 단위 테스트(xUnit) 프로젝트

# 사용 AWS 리소스

- ~~Lambda 함수: LambdaEmailSender.js (Node.js)~~ → 백그라운드 워커로 전환
- SQS: SendEmailQueue, EmailFailureQueue
- SES

# 데이터베이스 엔티티

- **User**
- **Product**

# 주요 기능

### User

- **CRUD**: 생성, 조회, 수정, 삭제
- **회원가입**: Users 테이블에 신규 사용자 등록, SQS → ~~Lambda~~ 워커 → SES를 통한 환영 이메일 발송
- **비밀번호 해싱**: 안전하게 저장
- **로그인**: 인증(JWT & 세션)
- **로그아웃**: 세션 제거

### Product

- **CRUD**: 생성, 조회, 수정, 삭제
  - **권한 필요**: 인증된 사용자만 가능

# 회원가입 플로우

- WebAPI가 사용자 정보를 "Users" 테이블에 저장
- WebAPI가 이메일 정보를 SQS(SendEmailQueue)에 메시지로 전송
- ~~SQS가 Lambda(LambdaEmailSender)를 트리거~~
- 백그라운드 워커(EmailBackgroundWorker)가 SQS(SendEmailQueue)에서 메시지 확인
- 메시지가 있으면 SES를 통해 환영 이메일 발송
- 이메일 발송 실패 시, 워커가 실패 알림 메시지를 SQS(EmailFailureQueue)에 전송
- 백그라운드 워커(EmailFailureBackgroundWorker)가 SQS(EmailFailureQueue)에서 메시지 확인
- 메시지가 있으면 WebAPI를 호출하여 "Users" 테이블의 Verified 플래그를 false로 변경
  (API 호출 시 클라이언트 로그인 및 JWT 토큰 검증 선행)

# 사용 기술

- .NET WebAPI & Worker 프로젝트
- 입력 검증 및 커스텀 검증기
- 해싱: 비밀번호 해싱 및 검증
- 인증(JWT + 세션)
- CQRS(명령/조회 분리)
- Fluent API
- 커스텀 예외 처리 / 글로벌 예외 핸들러
- AWS SQS, SES, ~~Lambda~~
- xUnit 단위 테스트

# JWT 서비스: 주요 역할 및 호출 시점

- 사용자가 로그인할 때(API 접근용 토큰 발급)
- 백그라운드 워커/클라이언트가 로그인할 때(API 호출용 토큰 발급)
- 인증/권한이 필요한 모든 API 엔드포인트

# UserCommandService / UserQueryService: 주요 역할 및 호출 시점

- UserCommandService: 회원가입, 정보 수정/삭제, 이메일 인증 실패 시 Verified=false 처리 담당
- UserQueryService: 사용자 데이터 조회 및 로그인(비밀번호 검증) 담당
- 호출 시점:
  - 회원가입(데이터 저장, 환영 이메일 트리거)
  - 사용자 정보 수정/삭제
  - 이메일 인증 실패(Verified 플래그 false 처리)
  - 로그인(입력 정보 검증)
  - 사용자 데이터 조회(프로필, 목록)

# ProductCommandService / ProductQueryService: 주요 역할 및 호출 시점

- 인증된 사용자가 상품 데이터 조회
- 관리자가 상품 생성/수정/삭제

# PasswordHasherService / SessionService: 주요 역할

- PasswordHasherService: 회원가입 시 비밀번호 해싱, 로그인 시 비밀번호 검증 담당
- SessionService: 로그인/로그아웃 시 세션 데이터 저장 및 쿠키 인증 처리 담당
- 사용자 인증 및 자격 관리가 필요한 모든 상황에서 사용됨

# JWT vs Session 인증 구조

- 이 코드베이스는 JWT와 Session/Cookie 인증을 병행 사용함
- JWT: API 토큰 발급 및 클라이언트 인증에 사용(무상태, API 호출용)
- Session/Cookie: 사용자 세션 데이터 저장 및 로그인 상태 유지에 사용(상태 기반, 웹 시나리오용)
- 로그인 시 JWT 토큰 발급과 세션/쿠키 데이터 설정이 동시에 이루어짐
- API 클라이언트와 웹 사용자 모두를 위한 유연한 인증 구조를 제공함
