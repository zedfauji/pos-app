# Sync Overview

- Projects:
  - `solution/sync/SyncService/`
  - `solution/sync/SyncWorker/`
- Purpose: Background processing for long-running and scheduled tasks (e.g., inventory sync, order processing jobs), using shared DTOs and interoperating with the backend services.

## Key Points

- Uses `MagiDesk.Shared` DTOs for consistent contracts.
- Likely communicates with Firestore and/or backend APIs to track `JobStatusDto` and recent jobs.
- `Program.cs` in `SyncWorker` defines the worker entry point.

## Build

```powershell
# from solution/
dotnet build sync/SyncService/SyncService.csproj -c Debug
dotnet build sync/SyncWorker/MagiDesk.SyncWorker.csproj -c Debug
```

## Notes

- Ensure configuration and credentials (if any) are aligned with the backend's Firestore setup.
- Consider using hosted services or worker templates for scheduling and resiliency.
