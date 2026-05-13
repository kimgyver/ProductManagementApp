# User/Product/Order Management App

Production-oriented full stack e-commerce sample built with ASP.NET Core (.NET 8), React (TypeScript), and PostgreSQL.

## 1. System

This repository contains three runtime components:

- API: ASP.NET Core Web API for authentication, products, cart, orders, admin, and payments.
- BackgroundProcessor: .NET Worker service for asynchronous email processing.
- Frontend: React + TypeScript + Vite client app.

## 2. Architecture

### 2.1 Logical Architecture

- Client layer: React SPA (browser)
- API layer: ASP.NET Core controllers + services + repositories
- Data layer: PostgreSQL (Neon in production)
- Async layer: AWS SQS + SES handled by BackgroundProcessor workers

### 2.2 Main Projects

- API: REST endpoints, auth, business logic, EF Core data access
- BackgroundProcessor: email send and email failure handling workers
- API.Test: xUnit tests for API behavior

## 3. End-to-End Flows

### 3.1 User Login Flow

1. Client sends credentials to POST /api/users/login
2. API validates password hash and user status
3. API returns JWT and sets session context
4. Client stores token and calls protected endpoints

### 3.2 Registration and Email Flow

1. Client calls POST /api/users/register
2. API saves user in database
3. API publishes email task to SQS
4. Background worker consumes queue and sends email via SES
5. On failure, worker publishes failure event and API updates verification state

### 3.3 Order Retrieval Flow (Stabilized)

1. Client calls GET /api/orders with Bearer token
2. API resolves user identity from claims
3. API queries order data
4. If legacy schema mismatch occurs, API now returns HTTP 200 with empty list instead of HTTP 500

## 4. Recently Applied Changes

- Improved CORS behavior for deployed frontend/backend communication.
- Added schema mismatch-safe behavior for order retrieval to avoid runtime 500 on legacy Neon schema.
- Updated frontend UX for cart badge sync and cart local fallback behavior.
- Simplified landing page and reduced noisy error UI on order page.

## 5. Resources Used

### 5.1 Runtime and Frameworks

- .NET 8 / ASP.NET Core
- React 19 + TypeScript
- Entity Framework Core + Npgsql
- Vite + Axios

### 5.2 Infrastructure

- Frontend hosting: Vercel
- Backend hosting: Railway
- Database: Neon PostgreSQL
- Messaging: AWS SQS
- Email: AWS SES

### 5.3 Database (RDB) and Core Table Schema

- RDBMS: PostgreSQL (managed on Neon in production)
- Access stack: Entity Framework Core + Npgsql
- Scope below: core business tables only (`User`, `Product`, `Order`)

#### User table

| Column         | Type         | Constraint / Note          |
| -------------- | ------------ | -------------------------- |
| Id             | int          | PK                         |
| Username       | varchar(100) | Required                   |
| Email          | varchar      | Required, Unique index     |
| HashedPassword | varchar      | Required                   |
| IsAdmin        | boolean      | Default false              |
| Verified       | boolean      | Default false/true by flow |
| Role           | varchar      | Default `customer`         |
| CreatedAt      | timestamp    | Default UTC now            |
| UpdatedAt      | timestamp    | Default UTC now            |

#### Product table

| Column      | Type          | Constraint / Note             |
| ----------- | ------------- | ----------------------------- |
| Id          | int           | PK                            |
| Sku         | varchar(100)  | Required, Unique index        |
| Name        | varchar(100)  | Required                      |
| Description | text          | Optional                      |
| Status      | enum/text     | `draft`, `active`, `archived` |
| Price       | decimal(18,2) | Required                      |
| Category    | varchar(50)   | Optional                      |
| Stock       | int           | Inventory count               |
| CreatedAt   | timestamp     | Default UTC now               |
| UpdatedAt   | timestamp     | Default UTC now               |

#### Order table

| Column          | Type      | Constraint / Note       |
| --------------- | --------- | ----------------------- |
| Id              | int       | PK                      |
| UserId          | int       | FK -> User.Id           |
| Status          | varchar   | Order state             |
| PaymentMethod   | varchar   | `card` or `po`          |
| TotalPrice      | decimal   | Total amount            |
| PaymentIntentId | varchar   | Unique index (nullable) |
| PoNumber        | varchar   | Unique index (nullable) |
| RefundStatus    | varchar   | Default `none`          |
| CreatedAt       | timestamp | Default UTC now         |
| UpdatedAt       | timestamp | Default UTC now         |

## 6. Deploy Topology (Current)

- Frontend URL pattern: https://user-product-oder-management-app.vercel.app
- API URL pattern: https://<api>.up.railway.app
- DB provider: Neon PostgreSQL (existing DB reused)

## 7. Testable Accounts

- Customer
  - Email: customer@test.com
  - Password: cust123

Use this account to validate login, product listing, cart, and order history screens.

## 8. Local Run

### 8.1 API

```bash
cd API
dotnet restore
dotnet build
dotnet run
```

### 8.2 Background Worker

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

## 9. Verification

### 9.1 Automated Tests

```bash
cd API.Test
dotnet test
```

### 9.2 Manual Smoke Test Checklist

1. Login with customer@test.com / cust123
2. Open product list
3. Add product to cart
4. Confirm cart badge and cart page updates
5. Open My Orders and verify no server error is shown

## 10. Additional Docs

- docs/PROJECT_DEEP_DIVE_KR.md
- docs/DAY1_DAY2_EXECUTION.md
- docs/DAY3_DAY4_EXECUTION.md
