# Process Compose - Troubleshooting Guide

## Viewing Logs

### Method 1: Terminal UI (TUI) - Recommended

When Process Compose is running:

1. **Navigate to OrderApi**:
   - Use **Arrow Keys** to navigate to `orderapi` in the process list
   - Press **Space** to select it

2. **View Logs**:
   - Press **L** to view logs for the selected process
   - Use **Arrow Keys** to scroll through logs
   - Press **Q** to exit log view and return to main screen

3. **Restart Service**:
   - Select the process
   - Press **R** to restart it

### Method 2: Command Line

#### View Logs for Specific Service
```powershell
# View logs for OrderApi
process-compose logs orderapi

# Follow logs (like tail -f)
process-compose logs -f orderapi

# View last N lines
process-compose logs --tail 100 orderapi
```

#### View All Logs
```powershell
# View logs for all services
process-compose logs

# Follow all logs
process-compose logs -f
```

#### View Logs from File
Logs are also saved to files in the `./logs/` directory:
```powershell
# View OrderApi log file
Get-Content .\logs\orderapi.log -Tail 50

# Follow log file
Get-Content .\logs\orderapi.log -Wait
```

### Method 3: REST API

Process Compose exposes a REST API (default: http://localhost:8080):

```powershell
# Get process status
Invoke-RestMethod http://localhost:8080/api/v1/processes/orderapi

# Get logs (if available via API)
Invoke-RestMethod http://localhost:8080/api/v1/processes/orderapi/logs
```

## Common Issues

### Service Failing to Start

1. **Check Logs**:
   ```powershell
   process-compose logs orderapi
   ```

2. **Check Process Status**:
   - In TUI, look for red status indicators
   - Check exit codes

3. **Common Causes**:
   - Port already in use
   - Missing dependencies
   - Configuration errors
   - Database connection issues

### Port Conflicts

```powershell
# Check if port is in use
netstat -ano | findstr :5256

# Kill process using port (if needed)
# Find PID from netstat output, then:
taskkill /PID <PID> /F
```

### Build Errors

If OrderApi fails to build:
```powershell
# Try building manually
cd solution/backend/OrderApi
dotnet build

# Check for errors
dotnet build --verbosity detailed
```

### Dependency Issues

OrderApi depends on InventoryApi. Check:
1. Is InventoryApi running?
2. Is InventoryApi healthy?
3. Can OrderApi reach InventoryApi?

```powershell
# Check InventoryApi status
process-compose logs inventoryapi

# Test InventoryApi health
Invoke-WebRequest http://localhost:5117/health
```

## Debugging Steps

### Step 1: Check Service Status
In TUI, verify OrderApi status (should show as "Running" or error)

### Step 2: View Logs
```powershell
process-compose logs orderapi
```

### Step 3: Check Health Endpoint
```powershell
# Test health endpoint
Invoke-WebRequest http://localhost:5256/health

# Test Swagger
Start-Process http://localhost:5256/swagger
```

### Step 4: Check Dependencies
```powershell
# Verify InventoryApi is running
Invoke-WebRequest http://localhost:5117/health

# Check if OrderApi can reach InventoryApi
# (Check OrderApi logs for connection errors)
```

### Step 5: Restart Service
```powershell
# Restart OrderApi
process-compose restart orderapi

# Or in TUI: Select OrderApi, press R
```

## TUI Keyboard Shortcuts

- **Arrow Keys** - Navigate
- **Space** - Select/deselect process
- **L** - View logs
- **R** - Restart process
- **S** - Stop process
- **Q** - Quit
- **?** - Show help (if available)

## Getting More Information

### Verbose Logging
You can add environment variables for more verbose .NET logging:

```yaml
orderapi:
  command: dotnet run --project solution/backend/OrderApi/OrderApi.csproj --launch-profile http
  environment:
    - ASPNETCORE_ENVIRONMENT=Development
    - Logging__LogLevel__Default=Debug
```

### Check Process Compose Logs
Process Compose itself logs to:
- Console output
- May have its own log file (check Process Compose documentation)

## Quick Commands Reference

```powershell
# View logs
process-compose logs orderapi

# Follow logs
process-compose logs -f orderapi

# Restart service
process-compose restart orderapi

# Stop service
process-compose stop orderapi

# Start service
process-compose start orderapi

# Get service status
process-compose ps

# View all logs
process-compose logs
```

---

**For OrderApi specifically, start with**: `process-compose logs orderapi`

