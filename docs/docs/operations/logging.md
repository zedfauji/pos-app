# Logging

## Overview

MagiDesk POS uses structured logging across all services. Logs are written to:
- **Local Development**: Console output and log files
- **Production**: Google Cloud Logging (Cloud Run)

## Log Levels

| Level | Usage | Example |
|-------|-------|---------|
| **Trace** | Very detailed diagnostic information | Method entry/exit |
| **Debug** | Diagnostic information for debugging | Variable values, flow control |
| **Information** | General informational messages | Service started, request processed |
| **Warning** | Warning messages for potential issues | Deprecated API usage, retry attempts |
| **Error** | Error messages for failures | Exception caught, operation failed |
| **Critical** | Critical failures requiring immediate attention | Service crash, data corruption |

## Log Format

### Structured Logging

All logs use structured JSON format:

```json
{
  "timestamp": "2025-01-27T10:30:00Z",
  "level": "Information",
  "service": "UsersApi",
  "message": "User login successful",
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "username": "admin",
  "requestId": "req-abc123"
}
```

### Log Categories

- **Authentication**: Login, logout, session management
- **Authorization**: Permission checks, RBAC operations
- **API**: Request/response logging
- **Database**: Query execution, connection management
- **Business Logic**: Order processing, payment processing
- **System**: Service startup, configuration, health checks

## Log Locations

### Local Development

**Backend APIs:**
```
solution/backend/{ApiName}/logs/
  - {ApiName}-{date}.log
```

**Frontend:**
```
%LocalAppData%\MagiDesk\Logs\
  - MagiDesk-{date}.log
```

### Production (Cloud Run)

Logs are available in **Google Cloud Logging**:

1. Navigate to [Cloud Logging Console](https://console.cloud.google.com/logs)
2. Filter by service name (e.g., `magidesk-users`)
3. View real-time and historical logs

## Logging Configuration

### Backend (ASP.NET Core)

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Frontend (WinUI 3)

Logging is configured in `App.xaml.cs`:

```csharp
// Logs written to:
// %LocalAppData%\MagiDesk\Logs\MagiDesk-{date}.log
```

## Log Analysis

### Common Log Queries

**Find all errors in the last hour:**
```
severity>=ERROR
timestamp>="2025-01-27T09:00:00Z"
```

**Find authentication failures:**
```
jsonPayload.message=~"Login failed"
severity>=WARNING
```

**Find slow requests (>1 second):**
```
jsonPayload.duration>1000
```

### Log Retention

- **Local Development**: 30 days
- **Production (Cloud Logging)**: 30 days (configurable)

## Best Practices

1. **Use Structured Logging**: Include context (userId, requestId, etc.)
2. **Log Levels**: Use appropriate log levels
3. **Sensitive Data**: Never log passwords, tokens, or PII
4. **Performance**: Avoid logging in hot paths
5. **Correlation**: Include request IDs for tracing

## Troubleshooting

### Logs Not Appearing

1. Check log level configuration
2. Verify file permissions (local)
3. Check Cloud Logging permissions (production)
4. Verify service is running

### Too Many Logs

1. Increase log level threshold
2. Filter by category
3. Review logging statements in code
