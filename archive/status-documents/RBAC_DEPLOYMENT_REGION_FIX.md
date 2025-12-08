# Deployment Region Fix

**Date:** 2025-01-27  
**Issue:** APIs were being deployed to `us-central1` instead of `northamerica-south1`

## Fixed Deployment Scripts

All deployment scripts have been updated to use the correct region:

1. ✅ `deploy-menu-cloudrun.ps1` - Changed default from `us-central1` to `northamerica-south1`
2. ✅ `deploy-order-cloudrun.ps1` - Changed default from `us-central1` to `northamerica-south1`
3. ✅ `deploy-inventory-cloudrun.ps1` - Changed default from `us-central1` to `northamerica-south1`
4. ✅ `deploy-tables-cloudrun.ps1` - Changed default from `us-central1` to `northamerica-south1`
5. ✅ `deploy-cloudrun.ps1` - Changed default from `us-central1` to `northamerica-south1`

## Correct Region

- **Region**: `northamerica-south1`
- **Database Region**: `northamerica-south-1`
- **Cloud Run Region**: `northamerica-south1`

## Next Steps

Redeploy all APIs to the correct region:
- MenuApi
- OrderApi
- InventoryApi
- TablesApi

All other APIs (SettingsApi, PaymentApi, CustomerApi, DiscountApi, UsersApi) were already using the correct region.

