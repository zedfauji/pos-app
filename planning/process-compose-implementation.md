# Process Compose - Implementation Guide

**Date**: 2025-12-23  
**Status**: IMPLEMENTATION GUIDE

---

## Prerequisites

1. **Windows 10/11** (already have ✅)
2. **.NET 8 SDK** (already have ✅)
3. **Process Compose Binary** (download required)

---

## Step 1: Download Process Compose (2 minutes)

### Option A: Direct Download
1. Visit: https://github.com/F1bonacc1/process-compose/releases
2. Download `process-compose-windows-amd64.exe` (or appropriate for your system)
3. Rename to `process-compose.exe`
4. Place in a directory in your PATH, or in project root

### Option B: Using Package Manager (if available)
```powershell
# Using winget (if available)
winget install process-compose

# Using scoop (if installed)
scoop install process-compose
```

### Verify Installation
```powershell
process-compose --version
```

---

## Step 2: Create Configuration File (10 minutes)

### Create `process-compose.yml` in Project Root

Copy the example from `process-compose-example.yml` and customize:

1. **Verify Paths** - Ensure all project paths are correct
2. **Check Ports** - Verify all ports match your services
3. **Health Endpoints** - Ensure all APIs have `/health` endpoints
4. **Launch Profiles** - Match your `launchSettings.json` profiles

### Key Configuration Sections

#### Basic Service
```yaml
processes:
  usersapi:
    command: dotnet
    args:
      - run
      - --project
      - solution/backend/UsersApi/UsersApi.csproj
      - --launch-profile
      - UsersApi
    working_dir: .
    healthcheck:
      http:
        path: /health
        port: 55162
```

#### With Dependencies
```yaml
  orderapi:
    command: dotnet
    args:
      - run
      - --project
      - solution/backend/OrderApi/OrderApi.csproj
    depends_on:
      inventoryapi:
        condition: process_healthy
```

---

## Step 3: Test Configuration (5 minutes)

### Dry Run
```powershell
process-compose up --dry-run
```

This validates the configuration without starting services.

### Start All Services
```powershell
process-compose up
```

This will:
1. Start all 10 APIs
2. Open TUI (Terminal User Interface)
3. Show service status, logs, and health

### TUI Controls
- **Arrow Keys** - Navigate
- **Space** - Select process
- **L** - View logs
- **R** - Restart process
- **S** - Stop process
- **Q** - Quit

---

## Step 4: Verify Services (5 minutes)

### In TUI
1. Check all services show as "Running" (green)
2. Verify health checks pass
3. View logs for any errors

### In Browser
1. Open Swagger UIs:
   - UsersApi: http://localhost:55162/swagger
   - SettingsApi: http://localhost:53504/swagger
   - etc.

### Test Service Communication
1. Test OrderApi → InventoryApi communication
2. Verify all endpoints work

---

## Step 5: Customize Configuration (Optional)

### Add Environment Variables
```yaml
processes:
  usersapi:
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Postgres=Host=localhost;Port=5432;...
```

### Add Logging
```yaml
processes:
  usersapi:
    log_location: ./logs/usersapi.log
```

### Add Replicas
```yaml
processes:
  usersapi:
    replicas: 2  # Run 2 instances
```

---

## Step 6: Integration with Development Workflow

### Start Services
```powershell
# From project root
process-compose up
```

### Stop Services
```powershell
# Press 'Q' in TUI, or:
process-compose down
```

### Restart Single Service
```powershell
# In TUI: Select service, press 'R'
# Or via CLI:
process-compose restart usersapi
```

### View Logs
```powershell
# In TUI: Select service, press 'L'
# Or via CLI:
process-compose logs usersapi
```

---

## Step 7: Advanced Features

### REST API
Process Compose exposes a REST API (default: http://localhost:8080)

```powershell
# Get all processes
Invoke-RestMethod http://localhost:8080/api/v1/processes

# Get process status
Invoke-RestMethod http://localhost:8080/api/v1/processes/usersapi
```

### Multiple Configurations
```powershell
# Use different config file
process-compose -f process-compose.dev.yml up
```

### Namespaces
Organize processes into namespaces:
```yaml
processes:
  usersapi:
    namespace: apis
  postgres:
    namespace: infrastructure
```

---

## Troubleshooting

### Services Not Starting
1. Check TUI for error messages
2. Verify paths in `process-compose.yml`
3. Check launch profiles exist
4. Verify ports aren't in use

### Health Checks Failing
1. Ensure `/health` endpoints exist
2. Check port numbers match
3. Verify services are actually running

### Port Conflicts
1. Check if ports are already in use:
   ```powershell
   netstat -ano | findstr :55162
   ```
2. Update ports in `process-compose.yml` if needed

### Configuration Errors
1. Validate YAML syntax
2. Use `--dry-run` to check
3. Check Process Compose logs

---

## Migration Checklist

- [ ] Download Process Compose binary
- [ ] Create `process-compose.yml`
- [ ] Test with 2-3 services first
- [ ] Verify health checks work
- [ ] Test service-to-service communication
- [ ] Migrate all 10 services
- [ ] Test restart functionality
- [ ] Update team documentation
- [ ] Remove PowerShell script (after validation)

---

## Benefits After Migration

1. **Single Command**: `process-compose up` starts everything
2. **Unified TUI**: All services in one interface
3. **Automatic Recovery**: Services restart on failure
4. **Health Checks**: Automatic monitoring
5. **Dependency Management**: Services start in correct order
6. **Better Logs**: Unified log viewing
7. **Easy Restart**: One command to restart all

---

## Next Steps After Implementation

1. **Add More Features**:
   - Environment-specific configs
   - Process replication
   - Custom health checks

2. **Integrate with CI/CD**:
   - Use in build pipelines
   - Test orchestration

3. **Team Adoption**:
   - Share configuration
   - Document workflows
   - Train team members

---

**END OF IMPLEMENTATION GUIDE**

