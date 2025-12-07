# System Outage Playbook

## Scenario

The entire MagiDesk POS system is down or unavailable. Users cannot log in, process orders, or handle payments.

## Impact Assessment

**Severity:** 🔴 **CRITICAL**

**Business Impact:**
- Cannot process orders
- Cannot accept payments
- Cannot manage tables
- Revenue loss
- Customer dissatisfaction

**Estimated Downtime Cost:** $X per hour (calculate based on average revenue)

## Immediate Actions (First 5 Minutes)

### Step 1: Confirm the Outage

**Check:**
1. Can users log in? ❌
2. Are APIs responding? ❌
3. Is the database accessible? ❓
4. Is this affecting all users or just some? ❓

**Document:**
- Time outage started
- What was working before
- What stopped working
- Error messages seen

### Step 2: Notify Stakeholders

**Who to notify:**
- Store manager
- IT support team
- Operations team
- Management (if extended outage)

**Message template:**
```
URGENT: MagiDesk POS System Outage
Time: [Current Time]
Status: System completely unavailable
Impact: Cannot process orders or payments
Action: Investigating root cause
ETA: [Estimate if available]
```

### Step 3: Activate Manual Procedures

**Immediate workarounds:**
- Switch to manual order taking (paper)
- Accept cash payments only
- Record all transactions for later entry
- Use backup payment terminal if available

## Detailed Procedures

### Phase 1: Diagnosis (Minutes 5-15)

#### Check Infrastructure Status

**1. Check Google Cloud Console**
- Log into Google Cloud Console
- Navigate to Cloud Run services
- Check service status for all APIs
- Look for error indicators

**2. Check Database Status**
- Navigate to Cloud SQL
- Verify database instance is running
- Check connection count
- Review recent errors

**3. Check Network Connectivity**
```powershell
# Test internet connectivity
Test-NetConnection -ComputerName google.com -Port 443

# Test API endpoints
Invoke-WebRequest -Uri "https://magidesk-order-904541739138.northamerica-south1.run.app/health"
```

**4. Review Recent Changes**
- Check recent deployments
- Review configuration changes
- Check for scheduled maintenance
- Review recent code deployments

#### Identify Root Cause

**Common Causes:**
1. **Database Down** - Most common
   - Check Cloud SQL status
   - Verify database instance is running
   - Check connection limits

2. **Service Crash** - API services crashed
   - Check Cloud Run service status
   - Review service logs
   - Check for out-of-memory errors

3. **Network Issues** - Connectivity problems
   - Check internet connection
   - Verify DNS resolution
   - Check firewall rules

4. **Configuration Error** - Bad config deployed
   - Review recent config changes
   - Check environment variables
   - Verify connection strings

5. **DDoS Attack** - System overloaded
   - Check traffic patterns
   - Review Cloud Run metrics
   - Check for unusual requests

### Phase 2: Recovery (Minutes 15-60)

#### If Database is Down

**Steps:**
1. Check Cloud SQL console for instance status
2. If stopped, start the instance
3. Wait for instance to be ready (5-10 minutes)
4. Verify connectivity
5. Test API health endpoints

**If database is corrupted:**
- See [Database Corruption Playbook](./database-corruption.md)

#### If Services are Down

**Steps:**
1. Check Cloud Run service status
2. Review service logs for errors
3. Restart affected services
4. Wait for services to be ready
5. Test health endpoints

**Restart Service:**
```powershell
# Via Google Cloud Console or gcloud CLI
gcloud run services update magidesk-order \
  --region northamerica-south1 \
  --project bola8pos
```

#### If Network Issues

**Steps:**
1. Check local network connectivity
2. Verify DNS resolution
3. Check firewall rules
4. Test from different network
5. Contact network administrator if needed

#### If Configuration Error

**Steps:**
1. Identify the bad configuration
2. Revert to previous known-good config
3. Redeploy services
4. Verify services start correctly
5. Test functionality

### Phase 3: Verification (Minutes 60-90)

#### System Health Check

**Run comprehensive health check:**
```powershell
.\test-health-endpoints.ps1
.\test-db-connection.ps1
.\test-payment-flow.ps1
```

**Verify:**
- [ ] All APIs responding
- [ ] Database accessible
- [ ] Payment flow working
- [ ] Users can log in
- [ ] Orders can be created
- [ ] Payments can be processed

#### Functional Testing

**Test critical functions:**
1. **Login Test**
   - Try logging in with test user
   - Verify session is created
   - Check permissions work

2. **Order Test**
   - Create a test order
   - Verify order is saved
   - Check inventory is updated

3. **Payment Test**
   - Process a test payment
   - Verify payment is recorded
   - Check bill is updated

4. **Table Test**
   - Start a test table session
   - Verify session is created
   - Check table status updates

### Phase 4: Data Recovery (If Needed)

#### Recover Lost Transactions

**If transactions were lost during outage:**
1. Review manual records (paper orders)
2. Enter transactions into system
3. Verify all transactions are recorded
4. Reconcile with manual records
5. Generate reports to verify

#### Verify Data Integrity

**Check:**
- No duplicate orders
- All payments recorded
- Inventory counts correct
- Table sessions accurate
- Financial totals match

## Post-Incident Actions

### Immediate (Within 24 Hours)

1. **Document the Incident**
   - Root cause
   - Timeline of events
   - Actions taken
   - Resolution time
   - Impact assessment

2. **Notify Stakeholders**
   - System is restored
   - Brief summary of cause
   - Steps taken to resolve
   - Prevention measures

3. **Review Logs**
   - Analyze what happened
   - Identify warning signs
   - Document lessons learned

### Short-Term (Within 1 Week)

1. **Post-Mortem Meeting**
   - Review incident timeline
   - Identify root cause
   - Discuss improvements
   - Assign action items

2. **Implement Fixes**
   - Address root cause
   - Improve monitoring
   - Add alerts
   - Update procedures

3. **Update Documentation**
   - Update runbooks
   - Add new procedures
   - Document new learnings

### Long-Term (Within 1 Month)

1. **Prevention Measures**
   - Implement monitoring improvements
   - Add automated alerts
   - Improve redundancy
   - Update disaster recovery plan

2. **Training**
   - Train team on new procedures
   - Conduct drills
   - Update playbooks

## Prevention Measures

### Monitoring Improvements

**Add alerts for:**
- Database connection failures
- Service health degradation
- High error rates
- Unusual traffic patterns
- Resource exhaustion

### Redundancy

**Consider:**
- Database read replicas
- Multi-region deployment
- Backup services
- Failover mechanisms

### Regular Testing

**Schedule:**
- Monthly disaster recovery drills
- Quarterly failover tests
- Annual full system tests

## Escalation

### When to Escalate

**Escalate if:**
- Outage lasts > 1 hour
- Cannot identify root cause
- Database is corrupted
- Data loss is suspected
- Security breach suspected

### Escalation Contacts

1. **Level 1:** Operations Team
2. **Level 2:** Development Team Lead
3. **Level 3:** CTO / Technical Director
4. **Level 4:** Google Cloud Support (for infrastructure)

## Related Playbooks

- [Database Corruption Playbook](./database-corruption.md)
- [Disaster Recovery Playbook](./disaster-recovery.md)
- [Payment Processing Outage Playbook](./payment-outage.md)

## Related Runbooks

- [Health Check Runbook](../runbooks/health-check.md)
- [Service Restart Runbook](../runbooks/service-restart.md)
- [Database Backup Runbook](../runbooks/database-backup.md)

---

**Last Updated:** 2025-01-27  
**Maintained By:** Operations Team  
**Review Frequency:** Quarterly
