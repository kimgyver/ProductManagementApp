# AWS Deployment Guide

## Prerequisites

- AWS Account with appropriate IAM permissions
- AWS CLI installed and configured
- Docker installed (for building container images)
- GitHub account for CI/CD (optional but recommended)

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Route 53 (DNS)                       │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                    CloudFront (CDN)                         │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                     ALB (Load Balancer)                     │
└────────────┬─────────────────────────────┬──────────────────┘
             │                             │
    ┌────────▼────────┐         ┌──────────▼─────────┐
    │  ECS (Frontend) │         │  ECS (API Backend) │
    │   3 instances   │         │   3 instances      │
    └─────────────────┘         └────────┬───────────┘
                                         │
                                ┌────────▼──────────┐
                                │  RDS PostgreSQL   │
                                │  Multi-AZ         │
                                └───────────────────┘
```

## Deployment Steps

### 1. Create AWS Resources

#### 1.1 Create RDS PostgreSQL Database

```bash
# Create security group
aws ec2 create-security-group \
  --group-name ecommerce-db-sg \
  --description "Security group for RDS database"

# Create RDS instance
aws rds create-db-instance \
  --db-instance-identifier ecommerce-db \
  --db-instance-class db.t3.micro \
  --engine postgres \
  --engine-version 16.1 \
  --master-username postgres \
  --master-user-password YourSecurePassword123 \
  --allocated-storage 20 \
  --storage-type gp3 \
  --multi-az \
  --publicly-accessible false \
  --db-name ecommerce
```

#### 1.2 Create ECR Repositories

```bash
# Backend repository
aws ecr create-repository \
  --repository-name ecommerce-api \
  --region us-east-1

# Frontend repository
aws ecr create-repository \
  --repository-name ecommerce-frontend \
  --region us-east-1
```

#### 1.3 Push Docker Images

```bash
# Get login token
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com

# Build and push API
docker build -f Dockerfile.api -t ecommerce-api:latest .
docker tag ecommerce-api:latest <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/ecommerce-api:latest
docker push <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/ecommerce-api:latest

# Build and push Frontend
docker build -f Dockerfile.frontend -t ecommerce-frontend:latest .
docker tag ecommerce-frontend:latest <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/ecommerce-frontend:latest
docker push <ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/ecommerce-frontend:latest
```

### 2. Create ECS Cluster

```bash
# Create ECS cluster
aws ecs create-cluster --cluster-name ecommerce-cluster

# Create Application Load Balancer
aws elbv2 create-load-balancer \
  --name ecommerce-alb \
  --subnets subnet-xxx subnet-yyy \
  --security-groups sg-xxx

# Create target groups
aws elbv2 create-target-group \
  --name ecommerce-api-tg \
  --protocol HTTP \
  --port 5000 \
  --vpc-id vpc-xxx

aws elbv2 create-target-group \
  --name ecommerce-frontend-tg \
  --protocol HTTP \
  --port 3000 \
  --vpc-id vpc-xxx
```

### 3. Create ECS Task Definitions

#### 3.1 API Task Definition

```bash
aws ecs register-task-definition \
  --family ecommerce-api \
  --network-mode awsvpc \
  --requires-compatibilities FARGATE \
  --cpu 256 \
  --memory 512 \
  --container-definitions '[
    {
      "name": "ecommerce-api",
      "image": "<ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/ecommerce-api:latest",
      "portMappings": [
        {
          "containerPort": 5000,
          "hostPort": 5000,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        },
        {
          "name": "ConnectionStrings__DefaultConnection",
          "value": "Host=<RDS-ENDPOINT>;Port=5432;Database=ecommerce;Username=postgres;Password=YourSecurePassword123;"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/ecommerce-api",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ]'
```

#### 3.2 Frontend Task Definition

```bash
aws ecs register-task-definition \
  --family ecommerce-frontend \
  --network-mode awsvpc \
  --requires-compatibilities FARGATE \
  --cpu 256 \
  --memory 512 \
  --container-definitions '[
    {
      "name": "ecommerce-frontend",
      "image": "<ACCOUNT_ID>.dkr.ecr.us-east-1.amazonaws.com/ecommerce-frontend:latest",
      "portMappings": [
        {
          "containerPort": 3000,
          "hostPort": 3000,
          "protocol": "tcp"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/ecommerce-frontend",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      }
    }
  ]'
```

### 4. Create ECS Services

```bash
# API Service
aws ecs create-service \
  --cluster ecommerce-cluster \
  --service-name ecommerce-api-service \
  --task-definition ecommerce-api:1 \
  --desired-count 3 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-xxx,subnet-yyy],securityGroups=[sg-xxx],assignPublicIp=DISABLED}" \
  --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:us-east-1:<ACCOUNT_ID>:targetgroup/ecommerce-api-tg/xxx,containerName=ecommerce-api,containerPort=5000"

# Frontend Service
aws ecs create-service \
  --cluster ecommerce-cluster \
  --service-name ecommerce-frontend-service \
  --task-definition ecommerce-frontend:1 \
  --desired-count 3 \
  --launch-type FARGATE \
  --network-configuration "awsvpcConfiguration={subnets=[subnet-xxx,subnet-yyy],securityGroups=[sg-xxx],assignPublicIp=DISABLED}" \
  --load-balancers "targetGroupArn=arn:aws:elasticloadbalancing:us-east-1:<ACCOUNT_ID>:targetgroup/ecommerce-frontend-tg/yyy,containerName=ecommerce-frontend,containerPort=3000"
```

### 5. Apply Database Migrations

```bash
# Connect to the database and run migrations
export RDS_ENDPOINT="<your-rds-endpoint>"
export PGPASSWORD="YourSecurePassword123"

psql -h $RDS_ENDPOINT -U postgres -d ecommerce < migration.sql
```

Or use EF Core:

```bash
# From API project directory
dotnet ef database update --connection "Host=$RDS_ENDPOINT;Port=5432;Database=ecommerce;Username=postgres;Password=YourSecurePassword123;"
```

### 6. Setup Auto-Scaling

```bash
# Register scalable target
aws application-autoscaling register-scalable-target \
  --service-namespace ecs \
  --resource-id service/ecommerce-cluster/ecommerce-api-service \
  --scalable-dimension ecs:service:DesiredCount \
  --min-capacity 2 \
  --max-capacity 10

# Create scaling policy
aws application-autoscaling put-scaling-policy \
  --policy-name cpu-scaling \
  --service-namespace ecs \
  --resource-id service/ecommerce-cluster/ecommerce-api-service \
  --scalable-dimension ecs:service:DesiredCount \
  --policy-type TargetTrackingScaling \
  --target-tracking-scaling-policy-configuration "TargetValue=70.0,PredefinedMetricSpecification={PredefinedMetricType=ECSServiceAverageCPUUtilization}"
```

## Environment Variables

Create a `.env.production` file:

```env
# API
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=Host=<RDS-ENDPOINT>;Port=5432;Database=ecommerce;Username=postgres;Password=<PASSWORD>;
Email__ResendApiKey=<YOUR_RESEND_API_KEY>
Email__FromAddress=noreply@yourdomain.com
Jwt__Secret=<YOUR_JWT_SECRET_KEY>
Jwt__Issuer=ecommerce-api
Jwt__Audience=ecommerce-client

# Frontend
VITE_API_URL=https://api.yourdomain.com/api
```

## Monitoring & Logging

- CloudWatch: Monitor ECS tasks, logs, and metrics
- X-Ray: Trace API requests
- Application Insights: (Use CloudWatch instead)

## Security Best Practices

1. Use AWS Secrets Manager for sensitive data
2. Enable VPC security groups with least privilege
3. Use HTTPS with ACM certificates
4. Enable WAF on ALB
5. Enable RDS encryption
6. Use IAM roles for ECS tasks
7. Enable CloudTrail for audit logs
8. Regular security patching

## Cost Estimation

- ECS Fargate: ~$50-100/month
- RDS PostgreSQL: ~$20-30/month
- ALB: ~$15-20/month
- Data Transfer: ~$5-10/month
- **Total: ~$90-160/month**

## Rollback Procedure

```bash
# Revert to previous task definition
aws ecs update-service \
  --cluster ecommerce-cluster \
  --service ecommerce-api-service \
  --task-definition ecommerce-api:1 \
  --force-new-deployment
```

## References

- [AWS ECS Documentation](https://docs.aws.amazon.com/ecs/)
- [AWS RDS PostgreSQL](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_PostgreSQL.html)
- [AWS ECR](https://docs.aws.amazon.com/AmazonECR/latest/userguide/)
