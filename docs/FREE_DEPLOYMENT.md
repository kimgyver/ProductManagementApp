# 무료 배포 가이드 - 최적화된 방법

## 요약: 가장 좋은 무료 배포 방식

```
프론트엔드 (React)   →   Vercel (무료)
         ↓
백엔드 (.NET API)    →   Railway (월 $5 크레딧 무료)
         ↓
데이터베이스 (PostgreSQL) → Supabase 무료 (또는 기존 DB 연결)
```

**총 비용: 0원~무료**

---

## 🎯 전략 1: 무료 PaaS 조합 (가장 추천!)

### 장점
✅ 배포 매우 간단 (Git push만으로 배포)
✅ DB 마이그레이션 불필요
✅ 모두 무료 또는 거의 무료
✅ Docker 빌드 자동화
✅ 자동 스케일링

### 비용 분석
- **Vercel** (프론트): $0 (무료)
- **Railway** (백엔드): $0 (월 $5 크레딧 무료)
- **Supabase PostgreSQL** (DB): $0 (무료 플랜)
- **총합**: $0

---

## 📋 방법 1: Vercel + Railway + Supabase (무료)

### 1단계: 프론트엔드 - Vercel에 배포

```bash
# 1. Vercel CLI 설치
npm install -g vercel

# 2. 프론트엔드 디렉토리 이동
cd frontend

# 3. Vercel로 배포
vercel

# 4. 환경 변수 설정 (Vercel Dashboard)
# VITE_API_URL=https://your-railway-app.up.railway.app/api
```

**배포 결과**: `https://your-app.vercel.app`

### 2단계: 백엔드 - Railway에 배포

```bash
# 1. Railway 회원가입 (GitHub 로그인)
# https://railway.app

# 2. 새 프로젝트 생성
# GitHub repo 연결

# 3. 환경 변수 설정
# - ConnectionStrings__DefaultConnection
# - Jwt__Secret
# - Email__ResendApiKey
```

**배포 결과**: `https://your-api.up.railway.app`

### 3단계: 데이터베이스 - Supabase 연결

```bash
# 1. Supabase 회원가입
# https://supabase.com

# 2. 새 프로젝트 생성
# - PostgreSQL 자동 생성 (무료)
# - Connection string 복사

# 3. EF Core 마이그레이션 실행
cd API
dotnet ef database update --connection "Supabase_Connection_String"

# 4. Railway에서 ConnectionString 업데이트
```

**배포 결과**: Supabase PostgreSQL 사용

### 4단계: 프론트엔드 환경 변수 업데이트

```env
# frontend/.env.production
VITE_API_URL=https://your-api.up.railway.app/api
```

**배포 후 접속**: `https://your-app.vercel.app`

---

## 📋 방법 2: 기존 로컬 DB 유지 + Railway (백엔드만 배포)

기존 PostgreSQL을 계속 사용하고 싶은 경우:

### 단계

```bash
# 1. 로컬 DB를 클라우드에서 접근 가능하게 설정
# 또는 Supabase로 마이그레이션

# 2. Railway에서 환경 변수 설정
ConnectionStrings__DefaultConnection=Host=your-db-host;Port=5432;Database=ecommerce;Username=postgres;Password=xxx;

# 3. 프론트엔드는 Vercel에 배포
cd frontend && vercel

# 4. 백엔드는 Railway에 배포
# GitHub 연결로 자동 배포
```

**주의**: 로컬 DB를 공개할 수 없으므로, Supabase나 AWS RDS 무료 티어 사용 권장

---

## 📋 방법 3: AWS Free Tier (12개월 무료)

기존 AWS_DEPLOYMENT.md를 활용하되, 무료 옵션만 선택:

```bash
# 1. RDS PostgreSQL - db.t3.micro (무료 12개월)
# 2. EC2 - t3.micro (무료 12개월, 750시간/월)
# 3. CloudFront - 1TB 무료 데이터 전송
# 4. S3 - 5GB 무료 스토리지
```

**주의**: 12개월 이후 요금 발생

---

## 📋 방법 4: Azure Free Tier (12개월 무료)

기존 AZURE_DEPLOYMENT.md를 활용하되, 무료 옵션만 선택:

```bash
# 1. App Service - F1 Free (무료)
# 2. Static Web App - Free (무료)
# 3. SQL Database - 5GB (무료)
# 4. Application Insights - 1GB (무료)
```

**12개월 후 요금 발생**

---

## 🎯 배포 형태 비교

| 방식 | 장점 | 단점 | 비용 |
|------|------|------|------|
| **Vercel + Railway + Supabase** | 매우 간단, 매우 빠름, 자동 배포 | 소규모 프로젝트용 | $0 |
| **AWS Free Tier** | 강력한 리소스 | 12개월 제한, 설정 복잡 | $0 (12개월) |
| **Azure Free Tier** | 강력한 리소스 | 12개월 제한, 설정 복잡 | $0 (12개월) |
| **Docker + AWS/Azure** | 프로덕션급 | 설정 복잡, 비용 높음 | $80+/월 |

**추천**: Vercel + Railway + Supabase (가장 간단, 가장 무료) ⭐

---

## 🚀 빠른 배포 (Vercel + Railway + Supabase)

### 총 소요 시간: 약 15분

```bash
# Step 1: GitHub에 코드 푸시 (2분)
git add .
git commit -m "Ready for production"
git push origin main

# Step 2: Vercel에서 프론트엔드 배포 (3분)
# - vercel.com 접속
# - GitHub repo 연결
# - 자동 배포

# Step 3: Railway에서 백엔드 배포 (5분)
# - railway.app 접속
# - GitHub repo 연결
# - Environment 변수 추가
# - 자동 배포

# Step 4: Supabase 설정 (5분)
# - supabase.com 접속
# - PostgreSQL 생성
# - Connection string 복사

# Step 5: 연결 및 테스트 (중요한 부분이 이미 완료됨)
# frontend .env 업데이트
# API health check
```

---

## 🔧 각 플랫폼별 상세 가이드

### Vercel 프론트엔드 배포

```bash
# 1. 프로젝트 루트에서 (frontend 안 아님)
cd ProductManagementApp
vercel --prod

# 또는 Vercel Dashboard에서:
# 1. Sign in with GitHub
# 2. Import project
# 3. Select "frontend" folder
# 4. Add environment variables:
#    VITE_API_URL=https://your-api.railway.app/api
# 5. Deploy
```

**결과**: `https://your-project.vercel.app`

### Railway 백엔드 배포

```bash
# 1. railway.app에서 GitHub 연결
# 2. ProductManagementApp 리포지토리 선택
# 3. "Add Variables" 탭에서:
#    - ASPNETCORE_ENVIRONMENT=Production
#    - ConnectionStrings__DefaultConnection=postgres://...
#    - Jwt__Secret=your-secret
#    - Email__ResendApiKey=your-key
# 4. 배포 자동 시작

# 5. Dockerfile 확인 (ProductManagementApp/Dockerfile.api)
```

**결과**: `https://your-api.up.railway.app`

### Supabase 데이터베이스 설정

```bash
# 1. supabase.com/dashboard
# 2. "New Project" 클릭
# 3. PostgreSQL 자동 생성
# 4. "Project Settings" > "Database"에서 URI 복사:
#    postgresql://user:password@host:5432/postgres
# 5. Railway에 설정
```

---

## 💾 기존 DB 마이그레이션 (선택)

### 로컬 DB를 Supabase로 옮기기

```bash
# 1. 로컬 DB 백업
pg_dump -U postgres ecommerce > backup.sql

# 2. Supabase에 복원
# a) Supabase SQL Editor에서 backup.sql 실행
# 또는
# b) psql 명령으로 복원
psql -h db.supabase.co -U postgres -d postgres < backup.sql
```

### 또는 마이그레이션 도구 사용

```bash
# AWS DMS 또는 Supabase 마이그레이션 도구 사용
# (가장 안전한 방법)
```

---

## 🌍 도메인 연결 (선택)

### Vercel에 도메인 추가

```bash
# Vercel Dashboard > Settings > Domains
# 1. your-domain.com 입력
# 2. DNS 레코드 추가 (자동 가이드)
# 3. 20분 후 활성화
```

### Railway 커스텀 도메인

```bash
# Railway Dashboard > Settings > Custom Domain
# 1. CNAME 레코드 추가
# 2. DNS 설정
```

---

## ✅ 배포 체크리스트

- [ ] GitHub 리포지토리 생성 및 코드 푸시
- [ ] Vercel 회원가입 (프론트)
- [ ] Railway 회원가입 (백엔드)
- [ ] Supabase 회원가입 (DB) 또는 기존 DB 사용
- [ ] Vercel에서 frontend 폴더 배포
- [ ] Railway에서 Dockerfile.api 배포
- [ ] 환경 변수 설정
- [ ] Supabase 마이그레이션 (또는 DB 연결)
- [ ] API health check 테스트
- [ ] 프론트엔드 환경 변수 업데이트
- [ ] 프론트엔드 재배포
- [ ] E2E 테스트 (로그인 → 상품 → 주문 → 확인)

---

## 🆚 비용 비교

| 옵션 | 프론트 | 백엔드 | DB | 총 월비용 |
|------|--------|--------|-----|----------|
| Vercel + Railway + Supabase | $0 | $0 | $0 | **$0** ⭐ |
| AWS Free (12개월) | $0 | $0 | $0 | **$0** (12개월 후 $50+) |
| Azure Free (12개월) | $0 | $0 | $0 | **$0** (12개월 후 $40+) |
| AWS Production | $10 | $50 | $20 | **$80+** |

**가장 추천**: Vercel + Railway + Supabase (영구 무료) ⭐⭐⭐

---

## 📝 각 플랫폼 프리 티어 한계

### Vercel Free
- ✅ 무료 배포
- ✅ 자동 SSL
- ✅ CDN (전 세계)
- ⚠️ 함수 호출 시간 10초 제한
- ⚠️ 함수 메모리 1024MB

### Railway Free
- ✅ $5/월 크레딧 (자동)
- ✅ Docker 지원
- ✅ PostgreSQL 연결 가능
- ⚠️ 크레딧 소진 시 일시 중단
- ℹ️ 일반적으로 $5로 충분

### Supabase Free
- ✅ PostgreSQL 1GB
- ✅ 프로젝트 1개
- ✅ API 자동 생성
- ⚠️ 1주 미사용시 일시 중단
- ⚠️ 대역폭 2GB/월

---

## 🎓 권장 배포 순서

```
1단계: GitHub에 코드 푸시
   ↓
2단계: Vercel에 프론트엔드 배포 (3분)
   ↓
3단계: Supabase 설정 (5분)
   ↓
4단계: Railway에 백엔드 배포 (5분)
   ↓
5단계: 환경 변수 연결 (2분)
   ↓
6단계: 테스트 및 검증 (3분)
   
총 소요 시간: 약 20분
```

---

## 🆘 문제 해결

### Railway에서 .NET 앱이 시작 안 됨
```bash
# 확인사항:
# 1. Dockerfile.api 확인
# 2. ConnectionString 올바른지 확인
# 3. 로그 확인: Railway Dashboard > Logs
```

### Vercel에서 API 연결 안 됨
```bash
# 확인사항:
# 1. VITE_API_URL 올바른지 확인
# 2. CORS 설정 확인 (backend appsettings.json)
# 3. Railway API 주소 공개 되었는지 확인
```

### Supabase 연결 타임아웃
```bash
# 확인사항:
# 1. Connection string 올바른지 확인
# 2. 방화벽 규칙 확인 (Supabase > Settings > Database)
# 3. IP 화이트리스트 설정
```

---

## 💡 최종 추천안

**완벽한 무료 배포 솔루션:**

```yaml
프로젝트명: MyEcommerce

프론트엔드:
  플랫폼: Vercel
  배포: GitHub 연동 (git push 자동 배포)
  주소: https://myecommerce.vercel.app
  비용: $0

백엔드:
  플랫폼: Railway
  배포: Docker 자동 빌드
  주소: https://myecommerce-api.up.railway.app
  비용: $0 (월 $5 크레딧)

데이터베이스:
  플랫폼: Supabase PostgreSQL
  크기: 1GB 무료
  비용: $0

총 비용: $0/월 (영구 무료)
배포 시간: 20분
유지보수: 자동 (Git push만 하면 됨)
확장성: ⭐⭐⭐⭐⭐ (쉽게 업그레이드 가능)
```

이 방법이 가장 간단하고 빠르며 무료입니다! 🎉
