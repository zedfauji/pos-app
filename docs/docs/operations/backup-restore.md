# Backup & Restore

## Overview

MagiDesk POS uses Google Cloud SQL for database hosting, which provides automated backups. This guide covers backup and restore procedures.

## Automated Backups

### Cloud SQL Automated Backups

Cloud SQL automatically creates backups:

- **Frequency**: Daily
- **Retention**: 7 days (configurable)
- **Time**: During maintenance window (configurable)
- **Point-in-Time Recovery**: Available for last 7 days

### Backup Configuration

Configure backups in Cloud SQL Console:

1. Navigate to [Cloud SQL Instances](https://console.cloud.google.com/sql/instances)
2. Select instance: `pos-app-1`
3. Click **Backups** tab
4. Configure:
   - **Backup Window**: Choose maintenance window
   - **Backup Retention**: 7-35 days
   - **Point-in-Time Recovery**: Enable

### Verify Backups

```powershell
# List backups
gcloud sql backups list --instance=pos-app-1
```

## Manual Backups

### Full Database Backup

```powershell
# Create manual backup
gcloud sql backups create \
    --instance=pos-app-1 \
    --description="Manual backup before deployment"
```

### Export Specific Schema

```powershell
# Export users schema
gcloud sql export sql pos-app-1 \
    gs://bola8pos-backups/users-schema-$(Get-Date -Format "yyyyMMdd").sql \
    --database=postgres \
    --schema=users
```

### pg_dump (Local)

```powershell
# Install Cloud SQL Proxy
# Connect via proxy, then:
pg_dump -h 127.0.0.1 -p 5432 -U posapp -d postgres -n users > users_backup.sql
```

## Restore Procedures

### Restore from Automated Backup

```powershell
# List available backups
gcloud sql backups list --instance=pos-app-1

# Restore from backup
gcloud sql backups restore BACKUP_ID \
    --backup-instance=pos-app-1 \
    --restore-instance=pos-app-1
```

### Point-in-Time Recovery

```powershell
# Restore to specific point in time
gcloud sql backups restore BACKUP_ID \
    --backup-instance=pos-app-1 \
    --restore-instance=pos-app-1 \
    --point-in-time="2025-01-27T10:30:00Z"
```

### Restore Specific Schema

```powershell
# Import schema from backup
psql -h 127.0.0.1 -p 5432 -U posapp -d postgres < users_backup.sql
```

## Backup Verification

### Verify Backup Integrity

```sql
-- Connect to restored database
-- Verify table counts
SELECT 
    schemaname,
    COUNT(*) as table_count
FROM pg_tables
WHERE schemaname IN ('users', 'menu', 'ord', 'payments', 'inventory')
GROUP BY schemaname;

-- Verify data integrity
SELECT COUNT(*) FROM users.users;
SELECT COUNT(*) FROM menu.menu_items;
SELECT COUNT(*) FROM ord.orders;
```

### Test Restore Procedure

Regularly test restore procedures:

1. Create test instance
2. Restore backup to test instance
3. Verify data integrity
4. Document any issues

## Backup Storage

### Backup Locations

- **Automated Backups**: Stored in Cloud SQL
- **Manual Exports**: Stored in Cloud Storage bucket
- **Local Backups**: Stored on local machine (development only)

### Backup Retention Policy

| Backup Type | Retention | Location |
|-------------|-----------|----------|
| Automated Daily | 7 days | Cloud SQL |
| Manual | 30 days | Cloud Storage |
| Point-in-Time | 7 days | Cloud SQL |
| Local Exports | 90 days | Local/Cloud Storage |

## Disaster Recovery

See [Disaster Recovery Guide](./disaster-recovery.md) for complete DR procedures.

### Recovery Time Objectives (RTO)

- **RTO**: 4 hours (time to restore service)
- **RPO**: 1 hour (maximum data loss)

### Recovery Procedures

1. **Assess Damage**: Determine scope of data loss
2. **Choose Restore Point**: Select appropriate backup
3. **Restore Database**: Execute restore procedure
4. **Verify Data**: Validate restored data
5. **Resume Services**: Restart affected services
6. **Monitor**: Watch for issues

## Best Practices

1. **Regular Backups**: Ensure automated backups are enabled
2. **Test Restores**: Regularly test restore procedures
3. **Document Procedures**: Keep restore procedures up to date
4. **Monitor Backup Status**: Check backup completion daily
5. **Offsite Backups**: Consider additional backup locations
6. **Encryption**: Ensure backups are encrypted
7. **Access Control**: Limit backup access to authorized personnel

## Troubleshooting

### Backup Failures

1. **Check Cloud SQL Status**: Verify instance is running
2. **Check Storage**: Ensure sufficient storage quota
3. **Check Permissions**: Verify service account permissions
4. **Review Logs**: Check Cloud SQL logs for errors

### Restore Failures

1. **Verify Backup**: Ensure backup is valid
2. **Check Resources**: Ensure sufficient resources for restore
3. **Check Permissions**: Verify restore permissions
4. **Review Logs**: Check restore operation logs
