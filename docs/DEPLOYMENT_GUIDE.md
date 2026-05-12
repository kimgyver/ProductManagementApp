# Complete Deployment Guide

This guide covers deploying the E-Commerce Application to cloud platforms (AWS, Azure) and local Docker setup.

## Quick Start - Local Docker Deployment

### Prerequisites
- Docker installed
- Docker Compose installed

### Steps

1. **Clone/Navigate to project**
```bash
cd ProductManagementApp
```

2. **Update environment variables**
Edit `docker-compose.yml` and set:
- `Jwt__Secret`: Your secure JWT secret key
- `Email__ResendApiKey`: Your Resend email API key
- Database password: `POSTGRES_PASSWORD`

3. **Start services**
```bash
docker-compose up -d
```

4. **Verify services**
```bash
# Check running containers
docker-compose ps

# View API logs
docker-compose logs api

# View Frontend logs
docker-compose logs frontend
```

5. **Access application**
- Frontend: http://localhost:3000
- API: http://localhost:5000
- Database: localhost:5432

6. **Stop services**
```bash
docker-compose down
```

## Deployment Options

### Option 1: AWS (Recommended for scalability)

**Best for:**
- High-traffic applications
- Need for auto-scaling
- Complex infrastructure requirements
- Budget: $90-160/month

**Key Services:**
- ECS Fargate (containerized API and Frontend)
- RDS PostgreSQL (managed database)
- Application Load Balancer (traffic distribution)
- CloudFront (CDN for static assets)
- Route 53 (DNS management)

**See:** [AWS_DEPLOYMENT.md](./AWS_DEPLOYMENT.md)

### Option 2: Azure (Recommended for Microsoft Stack)

**Best for:**
- Organizations using Azure ecosystem
- Integration with Microsoft services
- Simpler setup for smaller apps
- Budget: $82-145/month

**Key Services:**
- App Service (API hosting)
- Static Web Apps (Frontend hosting)
- Azure SQL Database (managed PostgreSQL)
- Application Gateway (load balancing)
- Application Insights (monitoring)

**See:** [AZURE_DEPLOYMENT.md](./AZURE_DEPLOYMENT.md)

### Option 3: Heroku (Fastest to deploy)

**Best for:**
- Quick prototyping
- Small-scale applications
- Minimal DevOps knowledge needed
- Budget: $20-50/month

**Steps:**
```bash
# Login
heroku login

# Create apps
heroku create ecommerce-api
heroku create ecommerce-frontend

# Set config
heroku config:set -a ecommerce-api ASPNETCORE_ENVIRONMENT=Production

# Deploy
git push heroku main
```

## Pre-Deployment Checklist

### Backend Configuration
- [ ] Update `appsettings.Production.json` with production secrets
- [ ] Configure CORS settings for frontend URL
- [ ] Set JWT expiration times
- [ ] Configure email service credentials
- [ ] Review database connection pooling settings
- [ ] Enable HTTPS/TLS
- [ ] Setup logging and monitoring
- [ ] Configure backup strategy

### Frontend Configuration
- [ ] Update `.env.production` with API URL
- [ ] Run `npm run build` to create production build
- [ ] Test in production build mode locally
- [ ] Configure CDN caching headers
- [ ] Setup error monitoring (Sentry, etc.)
- [ ] Configure analytics (optional)

### Database
- [ ] Create database backup
- [ ] Run migrations on target database
- [ ] Verify connection string format
- [ ] Setup automated backups
- [ ] Configure read replicas (optional)

### Security
- [ ] Change default passwords
- [ ] Generate new JWT secrets
- [ ] Setup SSL certificates
- [ ] Configure security headers
- [ ] Enable CORS with specific origins
- [ ] Setup rate limiting
- [ ] Configure WAF rules
- [ ] Enable audit logging

## Build Process

### Backend Build

```bash
cd API

# Restore packages
dotnet restore

# Build
dotnet build -c Release

# Run tests (if available)
dotnet test

# Publish
dotnet publish -c Release -o ../publish/api
```

### Frontend Build

```bash
cd frontend

# Install dependencies
npm ci

# Build
npm run build

# Output is in `dist/` directory
```

## Database Migrations

### Using Entity Framework Core

```bash
# Create migration
dotnet ef migrations add MigrationName

# Apply migration
dotnet ef database update

# For specific target database:
dotnet ef database update --connection "connection_string"
```

### Using SQL Scripts

```bash
psql -h hostname -U username -d database_name -f migration.sql
```

## Environment Variables Reference

### API Configuration

```env
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000

# Database
ConnectionStrings__DefaultConnection=Host=db-host;Port=5432;Database=ecommerce;Username=postgres;Password=password;

# JWT
Jwt__Secret=your-super-secret-key-minimum-32-characters
Jwt__Issuer=ecommerce-api
Jwt__Audience=ecommerce-client
Jwt__ExpirationMinutes=1440

# Email
Email__ResendApiKey=your-resend-api-key
Email__FromAddress=noreply@yourdomain.com
Email__FromName=E-Commerce

# CORS
Cors__AllowedOrigins=https://yourdomain.com,https://www.yourdomain.com
```

### Frontend Configuration

```env
# API Endpoint
VITE_API_URL=https://api.yourdomain.com/api

# Analytics (optional)
VITE_ANALYTICS_KEY=your-analytics-key

# Monitoring (optional)
VITE_SENTRY_DSN=your-sentry-dsn
```

## Monitoring & Maintenance

### Health Checks

```bash
# API health
curl https://api.yourdomain.com/health

# Database connectivity
curl https://api.yourdomain.com/api/products
```

### Log Collection

```bash
# AWS CloudWatch
aws logs tail /ecs/ecommerce-api --follow

# Azure
az webapp log tail -n ecommerce-api -g ecommerce-rg
```

### Performance Monitoring

- Setup Application Insights / DataDog / New Relic
- Monitor CPU, memory, disk usage
- Track API response times
- Monitor database performance
- Setup alerts for critical metrics

## Rollback Procedure

### AWS ECS

```bash
# List previous task definitions
aws ecs describe-task-definition \
  --task-definition ecommerce-api:1

# Update service to previous task definition
aws ecs update-service \
  --cluster ecommerce-cluster \
  --service ecommerce-api-service \
  --task-definition ecommerce-api:1 \
  --force-new-deployment
```

### Azure App Service

```bash
# List deployment slots
az webapp deployment slot list \
  -g ecommerce-rg \
  -n ecommerce-api

# Swap slots
az webapp deployment slot swap \
  -g ecommerce-rg \
  -n ecommerce-api \
  --slot staging
```

## Troubleshooting

### Connection Issues

**Problem:** Cannot connect to database
```bash
# Check connection string
# Verify security groups/firewall rules allow inbound 5432
# Test connection:
psql -h hostname -U username -d database_name
```

**Problem:** API returns 502 Bad Gateway
```bash
# Check logs
docker-compose logs api

# Restart services
docker-compose restart api
```

### Performance Issues

**Problem:** Slow response times
- Check database query performance
- Enable caching
- Scale horizontally (add more instances)
- Optimize frontend bundle size

**Problem:** High memory usage
- Check for memory leaks
- Increase allocated memory
- Optimize database queries

## Cost Optimization

### AWS
- Use Reserved Instances for predictable load
- Setup CloudFront distribution
- Use S3 for static content
- Enable auto-scaling policies

### Azure
- Use Reserved Instances
- Setup auto-scale based on metrics
- Use CDN for global distribution
- Monitor and optimize resource usage

## Backup & Recovery

### Database Backups

```bash
# PostgreSQL manual backup
pg_dump -h hostname -U username database_name > backup.sql

# Restore from backup
psql -h hostname -U username database_name < backup.sql
```

### Automated Backups

**AWS:**
- Enable RDS automated backups (7-35 days retention)
- Setup automated snapshots

**Azure:**
- Enable automatic backups
- Configure geo-redundancy

## Next Steps

1. Choose deployment platform (AWS or Azure)
2. Follow platform-specific deployment guide
3. Set up monitoring and alerting
4. Configure automated backups
5. Setup CI/CD pipeline (GitHub Actions, Azure Pipelines)
6. Configure domain name and SSL certificate
7. Test all functionality in production
8. Setup error tracking (Sentry, Rollbar)

## Support & Resources

- [AWS Documentation](https://docs.aws.amazon.com/)
- [Azure Documentation](https://docs.microsoft.com/azure/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/dotnet/core/)
- [React Documentation](https://react.dev/)

## Deployment Checklist

### Pre-Deployment
- [ ] All tests passing
- [ ] Code reviewed
- [ ] Dependencies updated
- [ ] Security scanning done
- [ ] Database migrations tested
- [ ] Environment variables configured
- [ ] SSL certificates installed
- [ ] Monitoring setup complete

### Deployment Day
- [ ] Backup database
- [ ] Deploy to staging first
- [ ] Run smoke tests
- [ ] Monitor logs closely
- [ ] Check metrics
- [ ] Verify all endpoints working
- [ ] Test user workflows

### Post-Deployment
- [ ] Monitor error rates
- [ ] Check performance metrics
- [ ] Verify all integrations working
- [ ] Test payment flow
- [ ] Check email notifications
- [ ] Monitor database performance
- [ ] Verify backups working
