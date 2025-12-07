# RBAC Frontend Testing - Complete Setup ✅

**Date:** 2025-01-27  
**Status:** ✅ Ready for Testing

## What Was Created

### 1. RBAC Test Page ✅
- **File**: `solution/frontend/Views/RbacTestPage.xaml` and `.xaml.cs`
- **Location**: Management section → "RBAC Test" in navigation menu
- **Features**:
  - Display current user info (User ID, Username, Role)
  - Show all user permissions (from session or API)
  - Test API endpoints with/without X-User-Id header
  - Check if user has specific permissions

### 2. UserIdHeaderHandler ✅
- **File**: `solution/frontend/Services/UserIdHeaderHandler.cs`
- **Purpose**: Automatically adds `X-User-Id` header to all API requests
- **Applied to**: UsersApi, MenuApi, PaymentApi, OrderApi, SettingsApi, CustomerApi

### 3. Login Page Updates ✅
- **File**: `solution/frontend/Views/LoginPage.xaml.cs`
- **Change**: Now saves permissions from `LoginResponse` to `SessionDto`

## How to Test

### Step 1: Build and Run
```powershell
cd solution/frontend
dotnet build
# Run the application
```

### Step 2: Login
1. Login with credentials (e.g., `admin` / `123456`)
2. Permissions are automatically saved to session

### Step 3: Navigate to RBAC Test Page
1. In main navigation, go to **Management** section
2. Click **"RBAC Test"**

### Step 4: Test Scenarios

#### Test 1: View Permissions
- Page automatically loads and displays:
  - User ID, Username, Role
  - List of all permissions (should show ~53 for admin)

#### Test 2: Test API Endpoint WITH User ID
1. Select an endpoint (e.g., "SettingsApi - GET /api/v2/settings/frontend")
2. Ensure "Include X-User-Id Header" is ✅ checked
3. Click "Test Endpoint"
4. **Expected**: 200 OK (admin has permission)

#### Test 3: Test API Endpoint WITHOUT User ID
1. Select an endpoint
2. Uncheck "Include X-User-Id Header" ❌
3. Click "Test Endpoint"
4. **Expected**: 403 Forbidden

#### Test 4: Check Specific Permission
1. Enter permission name (e.g., `menu:view`)
2. Click "Check Permission"
3. **Expected**: ✅ Green checkmark (admin has this permission)

## Expected Results

### Admin User (admin/123456)
- **Permissions**: ~53 permissions
- **API Tests with X-User-Id**: 200 OK
- **API Tests without X-User-Id**: 403 Forbidden
- **Permission Check**: Most permissions should return ✅

### Employee User (if you create one)
- **Permissions**: Limited permissions based on role
- **API Tests with X-User-Id**: 403 Forbidden for restricted endpoints
- **API Tests without X-User-Id**: 403 Forbidden
- **Permission Check**: Only role-specific permissions return ✅

## Files Created/Modified

### Created
1. ✅ `solution/frontend/Views/RbacTestPage.xaml`
2. ✅ `solution/frontend/Views/RbacTestPage.xaml.cs`
3. ✅ `solution/frontend/Services/UserIdHeaderHandler.cs`

### Modified
1. ✅ `solution/frontend/Views/LoginPage.xaml.cs` - Save permissions
2. ✅ `solution/frontend/Views/MainPage.xaml` - Add navigation item
3. ✅ `solution/frontend/Views/MainPage.xaml.cs` - Add navigation case
4. ✅ `solution/frontend/App.xaml.cs` - Register UserIdHeaderHandler for all APIs

## Build Status

✅ **Frontend builds successfully** (only warnings, no errors)

## Next Steps

1. ✅ Run the application
2. ✅ Login and navigate to RBAC Test page
3. ✅ Test all scenarios
4. ✅ Verify X-User-Id header is being sent automatically
5. ✅ Test with different user roles

## Troubleshooting

### If permissions are empty:
- Logout and login again (to refresh session)
- Check that you're using v2 login endpoint
- Verify UsersApi is returning permissions

### If API tests fail:
- Check that APIs are deployed with RBAC fixes
- Verify UserIdHeaderHandler is registered
- Check that SessionService.Current has valid UserId

### If navigation doesn't work:
- Verify MainPage.xaml has the navigation item
- Check that MainPage.xaml.cs has the navigation case
- Ensure RbacTestPage.xaml compiles without errors

