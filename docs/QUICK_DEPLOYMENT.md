# 기존 DB 마이그레이션 및 배포 (5단계)

## 🎯 당신의 상황

✅ Neon에 PostgreSQL DB 이미 있음
❌ DB 마이그레이션 불필요 (Neon 그대로 사용)
✅ 무료 배포 원함

**해결책**: 기존 Neon DB를 그대로 사용하고 Railway에 배포

---

## 📋 단계별 배포 (15분)

### **1단계: GitHub 준비 (2분)**

```bash
cd /Users/jinyoungkim/net-repo/ProductManagementApp

# 1. Git 초기화 (이미 되어있으면 스킵)
git init

# 2. .gitignore 확인 (민감한 정보 제외)
cat .gitignore
# 포함되어야 함:
# - appsettings.Production.json
# - .env
# - node_modules/
# - bin/
# - obj/

# 3. 커밋
git add .
git commit -m "E-commerce platform - ready for deployment"

# 4. GitHub 푸시
git remote add origin https://github.com/YOUR_USERNAME/ProductManagementApp.git
git branch -M main
git push -u origin main
```

---

### **2단계: Neon Connection String 준비 (1분)**

```bash
# 기존 레포에서 Neon Connection String 복사
cat /Users/jinyoungkim/nextjs-repo/ecommerce-store/.env | grep DATABASE_URL

# 결과:
# DATABASE_URL="postgresql://neondb_owner:npg_j7vdfwi8tJFa@ep-weathered-wind-a4fflxo8-pooler.us-east-1.aws.neon.tech/neondb?sslmode=require&options=endpoint%3Dep-weathered-wind-a4fflxo8-pooler"

# 이 연결 문자열을 복사해두기 (3단계에서 사용)
```

**Neon Connection String 포맷:**
```
postgresql://USERNAME:PASSWORD@HOST/DATABASE?sslmode=require&options=...
```

---

### **3단계: Railway 배포 - 백엔드 (5분)**

#### 3-1. Railway 프로젝트 생성

```
1. https://railway.app 접속
2. "Login with GitHub" 클릭
3. GitHub 권한 승인
4. "New Project" 클릭
5. "GitHub Repo" 선택
6. ProductManagementApp 리포지토리 선택
7. 배포 자동 시작
```

#### 3-2. 환경 변수 설정 (Neon 사용)

```
Railway Dashboard에서:
1. Project 이름 클릭
2. "Variables" 탭
3. 다음 변수 추가:

ASPNETCORE_ENVIRONMENT=Production

ConnectionStrings__DefaultConnection=
postgresql://neondb_owner:npg_j7vdfwi8tJFa@ep-weathered-wind-a4fflxo8-pooler.us-east-1.aws.neon.tech/neondb?sslmode=require

Jwt__Secret=your-very-long-secret-key-minimum-32-characters-here

Jwt__Issuer=ecommerce-api

Jwt__Audience=ecommerce-client

Email__ResendApiKey=your-resend-api-key-here

Email__FromAddress=noreply@yourdomain.com

Cors__AllowedOrigins=https://your-app.vercel.app
```

**중요**: ConnectionStrings__DefaultConnection에 2단계의 Neon Connection String을 그대로 붙여넣기

#### 3-3. 배포 설정

```
Railway Dashboard에서:
1. "Settings" 탭
2. "Root Directory": API (또는 비워두기)
3. "Dockerfile": Dockerfile.api
4. "Save"
5. 배포 자동 시작 (3~5분)
```

#### 3-4. 배포 확인 및 공개 URL

```
Railway Dashboard에서:
1. "Settings" > "Networking"
2. "Generate Domain" 클릭
3. Domain 복사: https://your-api-xxxxx.up.railway.app

API 테스트:
curl https://your-api-xxxxx.up.railway.app/api/products

결과: JSON 배열 또는 401 (인증 필요) - OK!
```

---

### **4단계: Vercel 배포 - 프론트엔드 (5분)**

#### 4-1. Vercel 프로젝트 생성

```
1. https://vercel.com 접속
2. "Sign up with GitHub" 클릭
3. GitHub 권한 승인
4. "Import Project" 클릭
5. ProductManagementApp 리포지토리 선택
6. Framework: Vite
7. Root Directory: frontend
8. 다음으로 진행
```

#### 4-2. 환경 변수 설정

```
Vercel Dashboard에서:
1. "Settings" > "Environment Variables"
2. 다음 추가:

VITE_API_URL=https://your-api-xxxxx.up.railway.app/api
```

#### 4-3. 배포

```
Vercel Dashboard에서:
1. "Deploy" 클릭
2. 배포 시작 (2~3분)
3. 완료되면 URL 제공: https://your-app-xxxxx.vercel.app
```

#### 4-4. 테스트

```
배포된 앱에서:
1. https://your-app-xxxxx.vercel.app 접속
2. 회원가입 테스트
3. 로그인 테스트
4. 상품 조회 테스트 (API 호출 확인)
5. 장바구니 테스트
6. 모든 기존 데이터 Neon에서 로드됨 ✅
```

---

## 🔄 전체 흐름도

```
Neon PostgreSQL DB (기존)
        ↓
  Connection String 복사
        ↓
Railway (API 배포) ← Neon Connection String
        ↓
공개 URL (https://xxx.up.railway.app)
        ↓
Vercel (프론트 배포) ← 환경변수로 설정
        ↓
최종 앱 (https://xxx.vercel.app)

✅ 기존 Neon DB 그대로 사용
✅ 마이그레이션 불필요
✅ 배포만 하면 됨
```

---

## ✅ 배포 완료 체크리스트

- [ ] 1단계: GitHub에 코드 푸시
- [ ] 2단계: Neon Connection String 복사
- [ ] 3단계: Railway 프로젝트 생성
- [ ] 3단계: Neon Connection String을 환경 변수에 설정
- [ ] 3단계: 배포 성공 확인
- [ ] 3단계: 공개 URL 확인
- [ ] 4단계: Vercel 프로젝트 생성
- [ ] 4단계: 환경 변수 설정 (API_URL)
- [ ] 4단계: 배포 성공 확인
- [ ] 4단계: 앱 테스트

---

## 🚀 배포 시간 단축

**이전 방식 (Supabase)**: 
- Supabase 프로젝트 생성: 5분
- DB 마이그레이션: 3~5분
- 총 배포: 20분

**새로운 방식 (Neon 직접)**:
- Neon 프로젝트 준비: 0분 (이미 있음!)
- Connection String 복사: 1분
- 총 배포: **15분** ⚡ (5분 단축!)


---

## 🆘 배포 중 문제 해결

### Railway API 배포 안 됨

```bash
# 1. 로그 확인
# Railway Dashboard > Project > Logs

# 2. 일반적인 문제:
# - Neon Connection String 오류 → .env에서 다시 복사
# - 환경 변수 누락 → 모두 추가 확인
# - Dockerfile.api 없음 → ProductManagementApp 루트에 있는지 확인

# 3. Neon 연결 테스트 (로컬)
# Connection String이 작동하는지 확인:
export CONN="postgresql://neondb_owner:npg_j7vdfwi8tJFa@..."
psql "$CONN" -c "SELECT version();"

# 연결 성공 메시지 나와야 함
```

### Neon Connection String 못 찾음

```bash
# NextJS 레포에서 Connection String 확인:
cat /Users/jinyoungkim/nextjs-repo/ecommerce-store/.env | grep DATABASE_URL

# 또는 Railway 대시보드에서:
# 1. Neon을 데이터베이스로 추가 (optional)
# 2. 자동으로 CONNECTION_STRING 환경 변수 생성됨
```

### Vercel 프론트 배포 안 됨

```bash
# 1. 로그 확인
# Vercel Dashboard > Deployments > Logs

# 2. 일반적인 문제:
# - Root Directory 오류 → "frontend" 설정 확인
# - npm 빌드 실패 → npm install 확인
# - VITE_API_URL 누락 → 환경 변수 추가

# 3. 수동 테스트 (로컬)
cd frontend
npm run build
npm run preview
# http://localhost:4173 접속
```

### API 연결 안 됨

```bash
# 1. Vercel에서 확인
# - DevTools 콘솔 확인 (CORS 에러?)
# - VITE_API_URL 올바른지 확인

# 2. Railway API 공개 확인
# - Railway > Settings > Networking
# - Domain 생성되었나?
# - Health check: curl https://your-api-xxxxx.up.railway.app/api/health

# 3. Neon 연결 확인 (Railway)
# - Railway Logs 확인
# - Connection String 포맷 올바른가?
# - Neon 프로젝트 Active 상태인가?

# 4. CORS 설정 확인 (API)
# backend appsettings.json:
"Cors": {
  "AllowedOrigins": "https://your-app.vercel.app"
}
```

### 기존 Neon 데이터 안 나옴

```bash
# 1. Neon 연결 문자열 확인
# - Railway의 ConnectionStrings__DefaultConnection
# - 정확히 복사되었는가?

# 2. Neon 웹 대시보드에서 데이터 확인
# - https://console.neon.tech
# - 프로젝트 > SQL Editor
# - SELECT * FROM "Products"; 로 데이터 확인

# 3. EF Core 마이그레이션 불필요
# - Neon의 기존 스키마 그대로 사용
# - dotnet ef database update 실행 금지 (오류 발생 가능)
```

---

## 📊 배포 비용 최종 확인

| 서비스 | 프리 티어 | 비용 |
|--------|----------|------|
| Vercel | 무제한 배포 | $0 |
| Railway | $5/월 크레딧 | $0 (크레딧으로 충당) |
| Neon | 기존 DB (프리 플랜) | $0 |
| **총계** | | **$0** |

**Neon 프리 플랜:**
- ✅ 1 프로젝트
- ✅ 3GB 스토리지
- ✅ 700 컴퓨트 시간/월
- ✅ 1주 미사용 시 자동 대기 (언제든 깨울 수 있음)
- ✅ 개발/테스트용 완벽

---

## 🎓 다음: 커스텀 도메인 (선택)

```bash
# Domain이 있다면:
# 1. Vercel Dashboard > Settings > Domains
# 2. Domain 입력
# 3. DNS 레코드 추가 (자동 가이드)
# 4. 20분 후 활성화

# 예: https://myecommerce.com
```

---

## 🚀 배포 완료!

축하합니다! 이제 무료로 배포된 앱이 있습니다:

```
📱 프론트엔드: https://your-app.vercel.app
🔌 백엔드 API: https://your-api.up.railway.app
💾 데이터베이스: Neon PostgreSQL (기존 데이터 유지)
📊 모니터링: Railway & Vercel Dashboard
💰 비용: $0
⚡ 배포시간: 15분
```

**기존 Neon DB의 모든 데이터 그대로 사용!** ✅

---

## 📞 지원

문제가 있으면:
1. Railway 로그 확인
2. Vercel 로그 확인
3. Neon Connection String 확인
4. 환경 변수 다시 확인
