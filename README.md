# E-Commerce Platform - Full Stack Application

A complete, production-ready e-commerce platform built with .NET 8.0 backend and React 19 frontend.

## 🚀 Quick Start

### Prerequisites

- Docker & Docker Compose
- Node.js 20+ (for local frontend development)
- .NET 8.0 SDK (for local backend development)

### Local Development with Docker

```bash
# Clone/navigate to project
cd ProductManagementApp

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f
```

Services will be available at:

- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5000
- **Database**: localhost:5432

## 🏗️ Architecture Overview

### Projects in Solution

- **API**: ASP.NET Core WebAPI (20+ endpoints)
- **BackgroundProcessor**: Background Worker for email notifications
- **API.Test**: xUnit test project

### Key Features

**User Features**:

- User registration with validation
- Secure login/logout with JWT + Session
- Product browsing with search & filters
- Shopping cart management
- Order placement & tracking
- Payment processing (mock)
- Order history

**Admin Features**:

- Dashboard with statistics
- User management
- Order management & tracking
- Product management
- Revenue tracking
- Inventory monitoring

## 🏃 Running Locally

### Backend

```bash
cd API
dotnet restore
dotnet build
dotnet run
# API runs on http://localhost:5000
```

### Frontend

```bash
cd frontend
npm install
npm run dev
# Frontend runs on http://localhost:5173
```

### Background Worker

```bash
cd BackgroundProcessor
dotnet restore
dotnet run
# Worker processes background jobs
```

## 📦 API Endpoints (20+)

### Authentication

- `POST /api/users/register` - Register new user
- `POST /api/users/login` - User login
- `POST /api/users/logout` - User logout
- `GET /api/users/profile` - Get profile (protected)

### Products

- `GET /api/products` - List products
- `GET /api/products/{id}` - Get product details
- `POST /api/products` - Create product (admin)
- `PUT /api/products/{id}` - Update product (admin)
- `DELETE /api/products/{id}` - Delete product (admin)

### Cart

- `GET /api/cart` - Get user's cart
- `POST /api/cart/items` - Add item to cart
- `PUT /api/cart/items/{id}` - Update item quantity
- `DELETE /api/cart/items/{id}` - Remove item
- `DELETE /api/cart` - Clear cart

### Orders

- `POST /api/orders` - Create order
- `GET /api/orders` - List user's orders
- `GET /api/orders/{id}` - Get order details
- `PUT /api/orders/{id}/cancel` - Cancel order
- `POST /api/orders/{id}/refund` - Request refund

### Payments

- `POST /api/payments/process` - Process payment (mock)
- `GET /api/payments/{id}/status` - Get payment status
- `POST /api/payments/{id}/refund` - Refund payment

### Admin (Admin only)

- `GET /api/admin/dashboard` - Dashboard stats
- `GET /api/admin/users` - List users
- `GET /api/admin/orders` - List all orders
- `GET /api/admin/orders/{id}` - Get order details

## 🧬 Tech Stack

| Component        | Technology                |
| ---------------- | ------------------------- |
| Backend          | .NET 8.0 / ASP.NET Core   |
| Frontend         | React 19 + TypeScript     |
| Build Tool       | Vite                      |
| Database         | PostgreSQL 16             |
| ORM              | Entity Framework Core 8.0 |
| Authentication   | JWT + Session             |
| HTTP Client      | Axios                     |
| Styling          | Tailwind CSS v3           |
| Containerization | Docker                    |

## 🔐 Authentication & Authorization

- **JWT Tokens**: Stateless API authentication
- **Session Cookies**: Stateful web authentication
- **Role-Based Access**: Customer, Admin, Distributor roles
- **Protected Routes**: Frontend routes require authentication
- **Password Security**: BCrypt hashing with salts

## ☁️ 배포

### 무료 배포 (권장) ⭐

**Vercel + Railway + Neon 조합 - $0/월, 15분**

기존 Neon DB 그대로 사용, 마이그레이션 불필요!

```bash
1. GitHub에 코드 푸시
2. Vercel에 프론트엔드 배포 (5분)
3. Railway에 백엔드 배포 (5분, Neon Connection String 설정)
4. 테스트 (5분)

총 15분 → 배포 완료!
```

결과:

- 프론트엔드: https://your-app.vercel.app
- 백엔드: https://your-api.up.railway.app
- 데이터베이스: Neon PostgreSQL (기존 데이터 유지)
- 비용: **$0**

See [QUICK_DEPLOYMENT.md](docs/QUICK_DEPLOYMENT.md) for detailed 4-step guide

### 배포 형태 비교

See [DEPLOYMENT_COMPARISON.md](docs/DEPLOYMENT_COMPARISON.md)

### AWS 배포 (프로덕션급)

See [AWS_DEPLOYMENT.md](docs/AWS_DEPLOYMENT.md)

- Cost: ~$90-160/month

### Azure 배포 (프로덕션급)

See [AZURE_DEPLOYMENT.md](docs/AZURE_DEPLOYMENT.md)

- Cost: ~$82-145/month

## 📚 Documentation

- [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md) - Project overview & features
- [docs/DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md) - General deployment
- [docs/AWS_DEPLOYMENT.md](docs/AWS_DEPLOYMENT.md) - AWS specific
- [docs/AZURE_DEPLOYMENT.md](docs/AZURE_DEPLOYMENT.md) - Azure specific
- [docs/PROJECT_DEEP_DIVE_KR.md](docs/PROJECT_DEEP_DIVE_KR.md) - Detailed Korean docs

## 🧪 Testing

### Backend Tests

```bash
cd API.Test
dotnet test
```

### Manual Frontend Testing

1. Register new account
2. Browse products
3. Add items to cart
4. Proceed to checkout
5. Place order
6. View order history

## 🔒 Security

- Password hashing with BCrypt
- JWT token signing with RS256
- CORS configuration
- SQL injection prevention (parameterized queries)
- XSS protection (React auto-escaping)
- Input validation on all endpoints
- Error handling without info leakage

## 📈 Performance

- API response caching
- Database query optimization
- Frontend code splitting
- CDN ready for static assets
- Auto-scaling configuration

## 📊 Project Status

✅ **PRODUCTION READY**

- Complete backend API (20+ endpoints)
- Complete React frontend (8 pages)
- PostgreSQL database schema
- Docker setup
- AWS deployment guide
- Azure deployment guide
- Comprehensive documentation

## 🎯 Next Steps

1. Choose deployment platform (AWS or Azure)
2. Follow platform-specific deployment guide
3. Configure domain & SSL
4. Setup monitoring & backups
5. Deploy to production

## 📞 Support

For deployment questions:

1. Check [DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md)
2. Review platform-specific guide (AWS/Azure)
3. Check Docker logs: `docker-compose logs`

---

**Version**: 1.0.0 (Production Ready)
