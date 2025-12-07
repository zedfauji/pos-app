# RBAC Frontend Testing Guide

**Date:** 2025-01-27

## Overview

This guide explains how to test RBAC (Role-Based Access Control) from the frontend application.

## What Was Added

### 1. RBAC Test Page
- **Location**: `solution/frontend/Views/RbacTestPage.xaml` and `.xaml.cs`
- **Access**: Navigate to "RBAC Test" in the Management section of the navigation menu

### 2. UserIdHeaderHandler
- **Location**: `solution/frontend/Services/UserIdHeaderHandler.cs`
- **Purpose**: Automatically adds `X-User-Id` header to all API requests
- **Applied to**: UsersApi, MenuApi, PaymentApi, OrderApi, SettingsApi

### 3. Login Page Updates
- **File**: `solution/frontend/Views/LoginPage.xaml.cs`
- **Change**: Now saves permissions from `LoginResponse` to `SessionDto`

## How to Test RBAC

### Step 1: Login
1. Open the application
2. Login with your credentials (e.g., `admin` / `123456`)
3. The login response now includes permissions which are saved to the session

### Step 2: Navigate to RBAC Test Page
1. In the main navigation menu, go to **Management** section
2. Click on **"RBAC Test"**
3. The page will display:
   - Current user information (User ID, Username, Role)
   - List of all permissions the user has
   - API endpoint testing interface
   - Permission checking tool

### Step 3: Test API Endpoints
1. **Select an endpoint** from the dropdown:
   - SettingsApi - GET /api/v2/settings/frontend
   - MenuApi - GET /api/v2/menu/items
   - OrderApi - GET /api/v2/orders
   - InventoryApi - GET /api/v2/inventory/items
   - PaymentApi - POST /api/v2/payments

2. **Toggle "Include X-User-Id Header"**:
   - ✅ **Checked**: Request includes user ID → Should return 200 (if user has permission) or 403 (if user doesn't have permission)
   - ❌ **Unchecked**: Request without user ID → Should return 403 Forbidden

3. **Click "Test Endpoint"** to see the result

### Step 4: Check Permissions
1. Enter a permission name in the "Permission to Check" field (e.g., `menu:view`, `order:create`)
2. Click "Check Permission"
3. The result will show:
   - ✅ Green: User HAS the permission
   - ❌ Red: User does NOT have the permission

## Expected Results

### With User ID Header (Logged In User)
- **Admin user**: Should return 200 OK for most endpoints (admin has all permissions)
- **Employee user**: Should return 403 Forbidden for restricted endpoints
- **Response**: Shows full API response with data

### Without User ID Header
- **All requests**: Should return 403 Forbidden
- **Response**: Shows error message indicating insufficient permissions

## Testing Different Scenarios

### Scenario 1: Test as Admin
1. Login as `admin` / `123456`
2. Navigate to RBAC Test page
3. You should see ~53 permissions
4. Test endpoints with X-User-Id header - should get 200 OK

### Scenario 2: Test Without User ID
1. Navigate to RBAC Test page (while logged in)
2. Uncheck "Include X-User-Id Header"
3. Test any endpoint - should get 403 Forbidden

### Scenario 3: Test Permission Check
1. Enter `menu:view` in permission field
2. Click "Check Permission"
3. Admin should have this permission (green checkmark)

## Troubleshooting

### Permissions Not Showing
- **Issue**: Permissions list is empty
- **Solution**: 
  1. Logout and login again (to refresh session with permissions)
  2. Check that you're using v2 login endpoint (`api/v2/auth/login`)
  3. Check that UsersApi is returning permissions in LoginResponse

### API Calls Returning 500 Instead of 403
- **Issue**: APIs returning 500 Internal Server Error
- **Solution**: 
  1. Verify APIs are deployed with latest RBAC fixes
  2. Check Cloud Run logs for errors
  3. Ensure authentication middleware is properly configured

### X-User-Id Header Not Being Sent
- **Issue**: Header not included in requests
- **Solution**:
  1. Verify `UserIdHeaderHandler` is registered for the API service
  2. Check that `SessionService.Current` has a valid `UserId`
  3. Verify the handler is in the HTTP client pipeline

## Files Modified

1. ✅ `solution/frontend/Views/LoginPage.xaml.cs` - Save permissions to session
2. ✅ `solution/frontend/Views/RbacTestPage.xaml` - Test page UI
3. ✅ `solution/frontend/Views/RbacTestPage.xaml.cs` - Test page logic
4. ✅ `solution/frontend/Services/UserIdHeaderHandler.cs` - Auto-add X-User-Id header
5. ✅ `solution/frontend/App.xaml.cs` - Register UserIdHeaderHandler for APIs
6. ✅ `solution/frontend/Views/MainPage.xaml` - Add navigation item
7. ✅ `solution/frontend/Views/MainPage.xaml.cs` - Add navigation case

## Next Steps

1. Build the frontend to verify no compilation errors
2. Run the application and test RBAC functionality
3. Verify that all API calls include X-User-Id header automatically
4. Test with different user roles to verify permission enforcement

