# Database Backup Runbook

## Purpose

This runbook guides you through backing up the MagiDesk POS database to ensure data safety and enable recovery if needed.

## Prerequisites

- Access to Google Cloud Console
- Permissions to manage Cloud SQL instances
- Basic knowledge of database operations

## Estimated Time

10-30 minutes (depending on database size)

## When to Use This Runbook

**Use this runbook when:**
- Performing scheduled backups
- Before major changes/deployments
- Before database migrations
- Manual backup is needed (automated backups run every 6 hours)

## Steps

### Step 1: Verify Automated Backups

**Check if automated backups are enabled:**

**Via Google Cloud Console:**
1. Log into Google Cloud Console
2. Navigate to **SQL** (Cloud SQL)
3. Select instance: `pos-app-1`
4. Go to **"Backups"** tab
5. Verify backups are scheduled

**Expected:**
- Automated backups enabled
- Backups run every 6 hours
- Backups retained for 7 days (default)

### Step 2: Create Manual Backup

**Method 1: Via Google Cloud Console (Recommended)**

1. Log into Google Cloud Console
2. Navigate to **SQL** (Cloud SQL)
3. Select instance: `bola8pos:northamerica-south1:pos-app-1`
4. Click **"BACKUPS"** tab
5. Click **"CREATE BACKUP"**
6. Enter backup description (e.g., "Pre-deployment backup 2025-01-27")
7. Click **"CREATE"**
8. Wait for backup to complete (10-30 minutes)

**Method 2: Via Command Line**

```powershell
# Create manual backup
gcloud sql backups create \
  --instance=pos-app-1 \
  --description="Manual backup $(Get-Date -Format 'yyyy-MM-dd HH:mm')" \
  --project=bola8pos
```

**Monitor backup progress:**
- Backup status shows in Cloud Console
- Usually takes 10-30 minutes
- Larger databases take longer

### Step 3: Verify Backup Completion

**Check backup status:**

**Via Google Cloud Console:**
1. Go to **"Backups"** tab
2. Find your backup
3. Verify status is **"SUCCESSFUL"**
4. Note backup ID and timestamp

**Via Command Line:**
```powershell
# List recent backups
gcloud sql backups list \
  --instance=pos-app-1 \
  --project=bola8pos \
  --limit=5
```

**Expected Result:**
- Backup status: SUCCESSFUL
- Backup size: Reasonable (depends on database size)
- Backup timestamp: Recent

### Step 4: Verify Backup Integrity (Optional but Recommended)

**Test backup can be restored:**

**Note:** This creates a test instance, which costs money. Only do this if critical.

1. Create a test Cloud SQL instance from backup
2. Verify instance starts successfully
3. Test database connectivity
4. Verify data is present
5. Delete test instance when done

**Command:**
```powershell
# Create test instance from backup (example - adjust backup ID)
gcloud sql instances clone pos-app-1 pos-app-1-test \
  --backup-id=BACKUP_ID \
  --project=bola8pos
```

## Verification

### Backup Checklist

- [ ] Backup created successfully
- [ ] Backup status is "SUCCESSFUL"
- [ ] Backup size is reasonable
- [ ] Backup timestamp is correct
- [ ] Backup description is clear
- [ ] Backup location is noted (for restore)

### Success Criteria

✅ **Backup Created Successfully**
- Backup exists in backups list
- Status is SUCCESSFUL
- Can be used for restore if needed

⚠️ **Backup Created with Warnings**
- Backup exists but may have issues
- Review backup details
- May need to create another backup

🔴 **Backup Failed**
- Backup creation failed
- Review error message
- Check database status
- Retry backup

## Troubleshooting

### Issue: Backup Takes Too Long

**Possible Causes:**
- Large database size
- High database activity
- Network issues
- Resource constraints

**Solutions:**
1. Wait longer (large databases can take 30+ minutes)
2. Check database activity (may need to reduce load)
3. Verify network connectivity
4. Check Cloud SQL instance resources

### Issue: Backup Fails

**Possible Causes:**
- Database is corrupted
- Insufficient storage
- Permission issues
- Instance is down

**Solutions:**
1. Check database status
2. Verify instance is running
3. Check available storage
4. Review error logs
5. Contact Google Cloud support if needed

### Issue: Backup Size is Unexpected

**Possible Causes:**
- Database grew significantly
- Backup includes unnecessary data
- Compression issues

**Solutions:**
1. Verify database size is expected
2. Check for large tables
3. Review backup compression
4. Consider excluding certain tables if needed

## Backup Retention

### Automated Backups
- **Frequency:** Every 6 hours
- **Retention:** 7 days (default)
- **Location:** Same region as instance

### Manual Backups
- **Retention:** Until manually deleted
- **Location:** Same region as instance
- **Cost:** Storage costs apply

### Best Practices
- Keep manual backups before major changes
- Delete old manual backups to save costs
- Verify backups periodically
- Test restore procedures quarterly

## Related Runbooks

- [Database Restore Runbook](./database-restore.md)
- [Health Check Runbook](./health-check.md)

## Related Playbooks

- [Disaster Recovery Playbook](../playbooks/disaster-recovery.md)
- [Database Corruption Playbook](../playbooks/database-corruption.md)

## Additional Resources

- [Google Cloud SQL Backup Documentation](https://cloud.google.com/sql/docs/postgres/backup-recovery/backing-up)
- [Backup & Restore Guide](../backup-restore.md)

---

**Last Updated:** 2025-01-27  
**Maintained By:** Operations Team
