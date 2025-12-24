# Process Compose - Quick Start Guide

## What is Process Compose?

Process Compose is a lightweight orchestrator that manages all 10 of your .NET microservices with a single command. It provides:
- ✅ Unified Terminal UI (TUI) for monitoring
- ✅ Automatic health checks
- ✅ Dependency management
- ✅ Automatic recovery on failure
- ✅ Unified log viewing

## Installation

### Download Process Compose

1. Visit: https://github.com/F1bonacc1/process-compose/releases
2. Download `process-compose-windows-amd64.exe` (or appropriate for your system)
3. Rename to `process-compose.exe`
4. Place in a directory in your PATH, or in this project root

### Verify Installation

```powershell
process-compose --version
```

## Usage

### Start All Services

From the project root:

```powershell
process-compose up
```

This will:
- Start all 10 APIs automatically
- Open Terminal UI (TUI) for monitoring
- Show service status, logs, and health

### Stop All Services

Press `Q` in the TUI, or:

```powershell
process-compose down
```

### TUI Controls

- **Arrow Keys** - Navigate between processes
- **Space** - Select/deselect process
- **L** - View logs for selected process
- **R** - Restart selected process
- **S** - Stop selected process
- **Q** - Quit Process Compose

### View Logs

In TUI: Select a process and press `L`

Or via command line:
```powershell
process-compose logs usersapi
```

### Restart a Service

In TUI: Select a process and press `R`

Or via command line:
```powershell
process-compose restart usersapi
```

## Configuration

The configuration is in `process-compose.yml` in the project root.

### Services Configured

1. **UsersApi** (port 55162)
2. **SettingsApi** (port 53504)
3. **InventoryApi** (port 5117)
4. **MenuApi** (port 5227)
5. **DiscountApi** (port 5229)
6. **CustomerApi** (port 51372)
7. **TablesApi** (port 53505)
8. **OrderApi** (port 5256) - Depends on InventoryApi
9. **PaymentApi** (port 55463)
10. **ReportingApi** (port 5228)

### Dependencies

- **OrderApi** waits for **InventoryApi** to be healthy before starting

## Health Checks

All services have HTTP health checks configured at `/health` endpoint.

If a service fails its health check, Process Compose will automatically restart it (up to 3 times).

## Logs

Logs are stored in the `./logs` directory, with separate files per service.

## Troubleshooting

### Services Not Starting

1. Check TUI for error messages
2. Verify paths in `process-compose.yml` are correct
3. Check launch profiles exist in `Properties/launchSettings.json`
4. Verify ports aren't already in use:
   ```powershell
   netstat -ano | findstr :55162
   ```

### Health Checks Failing

1. Ensure all APIs have `/health` endpoints
2. Check port numbers match your services
3. Verify services are actually running

### Port Conflicts

If a port is already in use, either:
1. Stop the conflicting service
2. Update the port in `process-compose.yml` and your API's `launchSettings.json`

## Migration from PowerShell Script

The PowerShell script (`start-backend.ps1`) can still be used, but Process Compose provides:
- Better monitoring (TUI)
- Automatic recovery
- Dependency management
- Unified logs

You can run both in parallel during migration.

## Additional Resources

- **GitHub**: https://github.com/F1bonacc1/process-compose
- **Documentation**: https://f1bonacc1.github.io/process-compose/
- **Planning Docs**: See `planning/process-compose-*.md` files

---

**Ready to use!** Just download Process Compose and run `process-compose up`

