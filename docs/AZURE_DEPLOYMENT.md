# Azure Deployment Guide

## Prerequisites

- Azure Account with active subscription
- Azure CLI installed and logged in (`az login`)
- Docker installed (for building container images)
- Service Principal for CI/CD (optional but recommended)

## Architecture

```
┌──────────────────────────────────────────────────────┐
│            Azure Front Door (CDN/WAF)                │
└────────────────────┬─────────────────────────────────┘
                     │
┌────────────────────▼─────────────────────────────────┐
│          Application Gateway (Load Balancer)         │
└──────────┬───────────────────────────┬───────────────┘
           │                           │
    ┌──────▼─────────┐        ┌────────▼────────┐
    │ Static Web App │        │  App Service    │
    │   (Frontend)   │        │   (API Backend) │
    │  3 instances   │        │  3 instances    │
    └────────────────┘        └────────┬────────┘
                                       │
                              ┌────────▼──────────┐
                              │  Azure SQL DB     │
                              │  PostgreSQL       │
                              │  Geo-Redundant    │
                              └───────────────────┘
```

## Deployment Steps

### 1. Create Resource Group

```bash
az group create \
  --name ecommerce-rg \
  --location eastus
```

### 2. Create Azure Container Registry

```bash
az acr create \
  --resource-group ecommerce-rg \
  --name ecommerceacr \
  --sku Standard \
  --admin-enabled true

# Get credentials
az acr credential show \
  --resource-group ecommerce-rg \
  --name ecommerceacr
```

### 3. Build and Push Docker Images

```bash
# Login to ACR
az acr login --name ecommerceacr

# Build and push API
az acr build \
  --registry ecommerceacr \
  --image ecommerce-api:latest \
  --file Dockerfile.api .

# Build and push Frontend
az acr build \
  --registry ecommerceacr \
  --image ecommerce-frontend:latest \
  --file Dockerfile.frontend .
```

### 4. Create Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --resource-group ecommerce-rg \
  --name ecommerce-server \
  --location eastus \
  --admin-user sqladmin \
  --admin-password YourSecurePassword123

# Create Database
az sql db create \
  --resource-group ecommerce-rg \
  --server ecommerce-server \
  --name ecommerce \
  --edition Standard \
  --compute-model Serverless

# Configure Firewall
az sql server firewall-rule create \
  --resource-group ecommerce-rg \
  --server ecommerce-server \
  --name AllowAzure \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# For local development (optional)
az sql server firewall-rule create \
  --resource-group ecommerce-rg \
  --server ecommerce-server \
  --name AllowMyIP \
  --start-ip-address YOUR_IP \
  --end-ip-address YOUR_IP
```

### 5. Create App Service Plan

```bash
az appservice plan create \
  --resource-group ecommerce-rg \
  --name ecommerce-plan \
  --sku B2 \
  --is-linux
```

### 6. Create App Service for API

```bash
az webapp create \
  --resource-group ecommerce-rg \
  --plan ecommerce-plan \
  --name ecommerce-api \
  --deployment-container-image-name ecommerceacr.azurecr.io/ecommerce-api:latest

# Configure container settings
az webapp config container set \
  --name ecommerce-api \
  --resource-group ecommerce-rg \
  --docker-custom-image-name ecommerceacr.azurecr.io/ecommerce-api:latest \
  --docker-registry-server-url https://ecommerceacr.azurecr.io \
  --docker-registry-server-user <USERNAME> \
  --docker-registry-server-password <PASSWORD>

# Set environment variables
az webapp config appsettings set \
  --resource-group ecommerce-rg \
  --name ecommerce-api \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    "ConnectionStrings__DefaultConnection=Server=tcp:ecommerce-server.database.windows.net,1433;Initial Catalog=ecommerce;Persist Security Info=False;User ID=sqladmin;Password=YourSecurePassword123;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
    Email__ResendApiKey=<YOUR_RESEND_API_KEY> \
    Email__FromAddress=noreply@yourdomain.com \
    Jwt__Secret=<YOUR_JWT_SECRET> \
    Jwt__Issuer=ecommerce-api \
    Jwt__Audience=ecommerce-client

# Configure startup command
az webapp config set \
  --resource-group ecommerce-rg \
  --name ecommerce-api \
  --startup-file "dotnet API.dll"
```

### 7. Create Static Web App for Frontend

```bash
az staticwebapp create \
  --resource-group ecommerce-rg \
  --name ecommerce-frontend \
  --source https://github.com/YOUR_GITHUB/ProductManagementApp \
  --location eastus \
  --branch main \
  --app-location frontend \
  --output-location dist \
  --sku Standard
```

Or manual deployment:

```bash
# Create Static Web App
az staticwebapp create \
  --resource-group ecommerce-rg \
  --name ecommerce-frontend \
  --location eastus \
  --sku Standard

# Deploy frontend
az staticwebapp upload \
  --resource-group ecommerce-rg \
  --name ecommerce-frontend \
  --source-path ./frontend/dist
```

### 8. Configure Static Web App Routing

Create `frontend/staticwebapp.config.json`:

```json
{
  "routes": [
    {
      "route": "/api/*",
      "allowedRoles": ["authenticated", "anonymous"],
      "rewrite": "https://ecommerce-api.azurewebsites.net/api/*"
    },
    {
      "route": "/*",
      "serve": "/index.html",
      "statusCode": 200
    }
  ],
  "navigationFallback": {
    "rewrite": "/index.html",
    "exclude": ["/images/*.{svg,png,jpg,gif}", "/css/*"]
  }
}
```

### 9. Apply Database Migrations

```bash
# Get connection string
CONNECTION_STRING=$(az sql db show-connection-string \
  --client dotnet \
  --server ecommerce-server \
  --name ecommerce \
  --username sqladmin \
  --password YourSecurePassword123)

# Run migrations
dotnet ef database update --connection "$CONNECTION_STRING"
```

### 10. Setup Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --resource-group ecommerce-rg \
  --app ecommerce-insights \
  --location eastus

# Link to App Service
az monitor app-insights web update \
  --resource-group ecommerce-rg \
  --app ecommerce-api \
  --instrumentation-key <INSTRUMENTATION_KEY>
```

## Environment Configuration

Set environment variables in Azure:

### App Service Configuration

```bash
az webapp config appsettings set \
  --resource-group ecommerce-rg \
  --name ecommerce-api \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    "ConnectionStrings__DefaultConnection=..." \
    Email__ResendApiKey="..." \
    Jwt__Secret="..."
```

## Auto-Scaling

```bash
# Create auto-scale settings
az monitor autoscale create \
  --resource-group ecommerce-rg \
  --resource ecommerce-plan \
  --resource-type "microsoft.web/serverfarms" \
  --name ecommerce-autoscale \
  --min-count 2 \
  --max-count 10 \
  --count 3

# Add scaling rule
az monitor autoscale rule create \
  --resource-group ecommerce-rg \
  --autoscale-name ecommerce-autoscale \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1
```

## Custom Domain & SSL

```bash
# Add custom domain
az staticwebapp custom-domain add \
  --resource-group ecommerce-rg \
  --name ecommerce-frontend \
  --domain-name yourdomain.com

# For App Service
az webapp config ssl bind \
  --resource-group ecommerce-rg \
  --name ecommerce-api \
  --certificate-thumbprint <THUMBPRINT>
```

## Security Best Practices

1. Use Azure Key Vault for secrets

```bash
az keyvault create \
  --resource-group ecommerce-rg \
  --name ecommerce-kv

az keyvault secret set \
  --vault-name ecommerce-kv \
  --name DbConnectionString \
  --value "..."
```

2. Enable Managed Identity for App Service
3. Configure VNet integration
4. Use Azure WAF on Application Gateway
5. Enable SQL Database encryption
6. Configure SQL Auditing
7. Use Azure Policy for compliance

## Monitoring

```bash
# View logs
az webapp log tail \
  --resource-group ecommerce-rg \
  --name ecommerce-api

# View metrics
az monitor metrics list \
  --resource ecommerce-api \
  --resource-group ecommerce-rg \
  --resource-type "microsoft.web/sites"
```

## Cost Estimation

- Static Web App: ~$10-15/month
- App Service (B2 plan): ~$50-100/month
- SQL Database (Standard): ~$15-20/month
- Application Insights: ~$2-5/month
- Container Registry: ~$5/month
- **Total: ~$82-145/month**

## Deployment Script

Create `deploy.sh`:

```bash
#!/bin/bash

RESOURCE_GROUP="ecommerce-rg"
REGISTRY="ecommerceacr"
API_NAME="ecommerce-api"

# Build images
docker build -f Dockerfile.api -t ecommerce-api:latest .
docker build -f Dockerfile.frontend -t ecommerce-frontend:latest .

# Tag images
docker tag ecommerce-api:latest ${REGISTRY}.azurecr.io/ecommerce-api:latest
docker tag ecommerce-frontend:latest ${REGISTRY}.azurecr.io/ecommerce-frontend:latest

# Push to ACR
az acr login --name ${REGISTRY}
docker push ${REGISTRY}.azurecr.io/ecommerce-api:latest
docker push ${REGISTRY}.azurecr.io/ecommerce-frontend:latest

# Update App Service
az webapp config container set \
  --name ${API_NAME} \
  --resource-group ${RESOURCE_GROUP} \
  --docker-custom-image-name ${REGISTRY}.azurecr.io/ecommerce-api:latest \
  --docker-registry-server-url https://${REGISTRY}.azurecr.io

echo "Deployment complete!"
```

## References

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure Static Web Apps](https://docs.microsoft.com/azure/static-web-apps/)
- [Azure SQL Database](https://docs.microsoft.com/azure/azure-sql/database/)
- [Azure Container Registry](https://docs.microsoft.com/azure/container-registry/)
