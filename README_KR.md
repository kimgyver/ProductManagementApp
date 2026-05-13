# User/Product/Order Management App 문서 (한국어)

ASP.NET Core(.NET 8) 백엔드와 React(TypeScript) 프론트엔드로 구성된 전자상거래 샘플 애플리케이션입니다.

## 1. 시스템 구성

- API: 인증, 상품, 장바구니, 주문, 결제, 관리자 기능을 제공하는 Web API
- BackgroundProcessor: 이메일 비동기 처리를 담당하는 워커 서비스
- Frontend: 사용자 화면(React SPA)
- API.Test: xUnit 기반 테스트 프로젝트

## 2. 아키텍처

### 2.1 논리 아키텍처

- Client Layer: React SPA
- API Layer: ASP.NET Core Controller + Service + Repository
- Data Layer: PostgreSQL (운영은 Neon)
- Async Layer: AWS SQS + SES + BackgroundProcessor

### 2.2 프로젝트별 역할

- API: 비즈니스 로직, 인증/인가, DB 접근
- BackgroundProcessor: 이메일 발송 및 실패 후속 처리
- API.Test: 컨트롤러/서비스 동작 검증

## 3. 핵심 플로우

### 3.1 로그인 플로우

1. 클라이언트가 POST /api/users/login 호출
2. API가 비밀번호 해시 검증
3. JWT 발급 및 세션 컨텍스트 설정
4. 클라이언트가 보호된 API 호출

### 3.2 회원가입 및 이메일 플로우

1. 클라이언트가 POST /api/users/register 호출
2. API가 사용자 저장
3. API가 SQS에 이메일 작업 메시지 게시
4. 워커가 메시지를 소비해 SES로 메일 발송
5. 발송 실패 시 실패 이벤트를 처리하고 인증 상태를 갱신

### 3.3 주문 조회 플로우 (안정화 반영)

1. 클라이언트가 GET /api/orders 호출 (Bearer 토큰 포함)
2. API가 클레임에서 사용자 식별
3. 주문 조회 수행
4. 레거시 스키마 불일치가 발생하면 500 대신 200 + 빈 배열 반환

## 4. 최근 변경 사항

- 배포 환경 CORS 동작을 보강해 프론트/백엔드 통신 안정성 개선
- 주문 조회 시 스키마 불일치 예외를 안전 처리해 서버 500 방지
- 장바구니 배지/장바구니 화면 동기화 UX 개선
- 홈 화면 단순화 및 주문 페이지 에러 노출 축소

## 5. 사용한 자원

### 5.1 프레임워크 및 라이브러리

- .NET 8 / ASP.NET Core
- React 19 + TypeScript
- Entity Framework Core + Npgsql
- Vite + Axios

### 5.2 인프라

- 프론트엔드 호스팅: Vercel
- 백엔드 호스팅: Railway
- 데이터베이스: Neon PostgreSQL
- 메시징: AWS SQS
- 이메일: AWS SES

### 5.3 데이터베이스(RDB) 및 핵심 테이블 스키마

- 사용 RDBMS: PostgreSQL (운영은 Neon PostgreSQL)
- 데이터 접근 계층: Entity Framework Core + Npgsql
- 아래 스키마는 핵심 테이블(`User`, `Product`, `Order`) 기준 요약입니다.

#### User 테이블

| 컬럼           | 타입         | 제약 / 설명              |
| -------------- | ------------ | ------------------------ |
| Id             | int          | PK                       |
| Username       | varchar(100) | 필수                     |
| Email          | varchar      | 필수, Unique 인덱스      |
| HashedPassword | varchar      | 필수                     |
| IsAdmin        | boolean      | 기본값 false             |
| Verified       | boolean      | 플로우에 따라 true/false |
| Role           | varchar      | 기본값 `customer`        |
| CreatedAt      | timestamp    | UTC 현재시각 기본값      |
| UpdatedAt      | timestamp    | UTC 현재시각 기본값      |

#### Product 테이블

| 컬럼        | 타입          | 제약 / 설명                   |
| ----------- | ------------- | ----------------------------- |
| Id          | int           | PK                            |
| Sku         | varchar(100)  | 필수, Unique 인덱스           |
| Name        | varchar(100)  | 필수                          |
| Description | text          | 선택                          |
| Status      | enum/text     | `draft`, `active`, `archived` |
| Price       | decimal(18,2) | 필수                          |
| Category    | varchar(50)   | 선택                          |
| Stock       | int           | 재고 수량                     |
| CreatedAt   | timestamp     | UTC 현재시각 기본값           |
| UpdatedAt   | timestamp     | UTC 현재시각 기본값           |

#### Order 테이블

| 컬럼            | 타입      | 제약 / 설명             |
| --------------- | --------- | ----------------------- |
| Id              | int       | PK                      |
| UserId          | int       | FK -> User.Id           |
| Status          | varchar   | 주문 상태               |
| PaymentMethod   | varchar   | `card` 또는 `po`        |
| TotalPrice      | decimal   | 주문 총액               |
| PaymentIntentId | varchar   | Unique 인덱스(Nullable) |
| PoNumber        | varchar   | Unique 인덱스(Nullable) |
| RefundStatus    | varchar   | 기본값 `none`           |
| CreatedAt       | timestamp | UTC 현재시각 기본값     |
| UpdatedAt       | timestamp | UTC 현재시각 기본값     |

## 6. 현재 배포 형태

- 프론트엔드 URL 형식: https://user-product-oder-management-app.vercel.app
- API URL 형식: https://<api>.up.railway.app
- DB: Neon PostgreSQL (기존 데이터 유지)

## 7. 테스트 가능한 사용자 계정

- 고객 계정
  - 이메일: customer@test.com
  - 비밀번호: cust123

이 계정으로 로그인, 상품 조회, 장바구니, 주문 목록 시나리오를 검증할 수 있습니다.

## 8. 로컬 실행 방법

### 8.1 API

```bash
cd API
dotnet restore
dotnet build
dotnet run
```

### 8.2 BackgroundProcessor

```bash
cd BackgroundProcessor
dotnet restore
dotnet run
```

### 8.3 Frontend

```bash
cd frontend
npm install
npm run dev
```

## 9. 테스트 방법

### 9.1 자동 테스트

```bash
cd API.Test
dotnet test
```

### 9.2 수동 스모크 테스트

1. customer@test.com / cust123 으로 로그인
2. 상품 목록 페이지 진입
3. 상품을 장바구니에 추가
4. 장바구니 배지와 장바구니 페이지 반영 확인
5. My Orders 페이지에서 서버 에러 없이 화면 표시 확인

## 10. 추가 문서

- docs/PROJECT_DEEP_DIVE_KR.md
- docs/DAY1_DAY2_EXECUTION.md
- docs/DAY3_DAY4_EXECUTION.md
