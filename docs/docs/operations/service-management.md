# Service Management

## Overview

This guide covers starting, stopping, restarting, and managing MagiDesk POS services in different environments.

## Service Architecture

MagiDesk POS consists of:

- **9 Backend APIs** (microservices)
- **1 Frontend Application** (WinUI 3 desktop app)
- **1 Database** (PostgreSQL on Cloud SQL)

## Local Development

### Starting Services

#### Backend APIs

Each API can be started individually:

```powershell
# Navigate to API directory
cd solution/backend/UsersApi

# Run the API
dotnet run
```

**Default Ports:**
- UsersApi: `http://localhost:5001`
- MenuApi: `http://localhost:5003`
- OrderApi: `http://localhost:5004`
- PaymentApi: `http://localhost:5005`
- InventoryApi: `http://localhost:5006`
- SettingsApi: `http://localhost:5007`
- CustomerApi: `http://localhost:5008`
- DiscountApi: `http://localhost:5009`
- TablesApi: `http://localhost:5010`

#### Frontend

```powershell
# Navigate to frontend directory
cd solution/frontend

# Run the application
dotnet run
```

Or use Visual Studio:
1. Open `solution/MagiDesk.sln`
2. Set `MagiDesk.Frontend` as startup project
3. Press F5

### Stopping Services

**Backend APIs:**
- Press `Ctrl+C` in the terminal
- Or close the terminal window

**Frontend:**
- Close the application window
- Or press `Ctrl+C` if running from terminal

### Restarting Services

Simply stop and start again using the methods above.

## Production (Cloud Run)

### Service Status

Check service status:

```powershell
# List all services
gcloud run services list --region northamerica-south1

# Get service details
gcloud run services describe magidesk-users --region northamerica-south1
```

### Restarting Services

#### Rolling Restart

```powershell
# Deploy new revision (triggers rolling restart)
gcloud run deploy magidesk-users \
    --image gcr.io/bola8pos/magidesk-users:latest \
    --region northamerica-south1
```

#### Force Restart (All Instances)

```powershell
# Update environment variable to force restart
gcloud run services update magidesk-users \
    --update-env-vars RESTART_TRIGGER=$(Get-Date -Format "yyyyMMddHHmmss") \
    --region northamerica-south1
```

### Scaling

#### Manual Scaling

```powershell
# Set minimum instances
gcloud run services update magidesk-users \
    --min-instances 2 \
    --max-instances 10 \
    --region northamerica-south1
```

#### Auto-scaling

Cloud Run automatically scales based on:
- Request rate
- CPU utilization
- Memory usage

Configure in Cloud Run console or via gcloud:

```powershell
gcloud run services update magidesk-users \
    --cpu-throttle \
    --concurrency 80 \
    --max-instances 10 \
    --region northamerica-south1
```

## Health Checks

### Manual Health Check

```powershell
# Check health endpoint
Invoke-WebRequest -Uri "https://magidesk-users-904541739138.northamerica-south1.run.app/health"
```

### Automated Health Monitoring

Use monitoring tools (see [Monitoring Guide](./monitoring.md)) to track service health.

## Service Dependencies

### Startup Order

Services should start in this order:

1. **Database** (Cloud SQL) - Must be available first
2. **UsersApi** - Other services may depend on it
3. **SettingsApi** - Provides configuration
4. **Other APIs** - Can start in parallel
5. **Frontend** - Requires all APIs to be running

### Dependency Check Script

```powershell
# Check all service dependencies
$services = @(
    "https://magidesk-users-904541739138.northamerica-south1.run.app/health",
    "https://magidesk-menu-904541739138.northamerica-south1.run.app/health",
    "https://magidesk-order-904541739138.northamerica-south1.run.app/health"
    # ... add all services
)

foreach ($service in $services) {
    try {
        $response = Invoke-WebRequest -Uri $service -TimeoutSec 5
        Write-Host "$service - OK" -ForegroundColor Green
    } catch {
        Write-Host "$service - FAILED" -ForegroundColor Red
    }
}
```

## Troubleshooting

### Service Won't Start

1. **Check Logs**: Review application logs
2. **Check Ports**: Ensure ports are not in use
3. **Check Database**: Verify database connectivity
4. **Check Configuration**: Review appsettings.json
5. **Check Dependencies**: Ensure all dependencies are installed

### Service Crashes

1. **Check Logs**: Review crash logs
2. **Check Resources**: CPU/Memory usage
3. **Check Dependencies**: External service availability
4. **Review Recent Changes**: Recent deployments or config changes

### High Resource Usage

1. **Scale Up**: Increase instance count
2. **Optimize Code**: Review performance bottlenecks
3. **Check Database**: Database query performance
4. **Review Monitoring**: Identify resource-intensive operations

## Best Practices

1. **Graceful Shutdown**: Services handle shutdown signals properly
2. **Health Checks**: Implement comprehensive health checks
3. **Logging**: Log all important events
4. **Monitoring**: Monitor service health continuously
5. **Documentation**: Document service-specific procedures
