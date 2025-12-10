# Code Quality Issues - To Fix After Migration

**Generated**: December 10, 2025  
**Branch**: `feature/revamp-2025-enterprise-ui`  
**Status**: Documented, deferred until after DigitalOcean migration

## Issues Found During Cleanup

With `TreatWarningsAsErrors=true`, the following code quality issues were detected:

### Unreachable Code (CS0162)
- **File**: `src/Frontend/frontend/Services/ReceiptService.cs:528`
- **Issue**: Code after return statement or conditional that always returns
- **Fix**: Remove unreachable code or refactor logic

### Unawaited Async Calls (CS4014)
- **File**: `src/Frontend/frontend/Views/TablesPage.xaml.cs:917`
- **Issue**: Async method called without await
- **Fix**: Add `await` or use `_ =` to explicitly discard result

### Unused Variables (CS0168)
- **Files**: 
  - `src/Frontend/frontend/Views/TablesPage.xaml.cs:961` (variable `ex`)
  - `src/Frontend/frontend/Views/TablesPage.xaml.cs:978` (variable `ex`)
- **Issue**: Exception variable declared but never used
- **Fix**: Use variable (e.g., log it) or remove declaration

### Unused Field (CS0414)
- **File**: `src/Frontend/frontend/Views/EnhancedMenuManagementPage.xaml.cs:33`
- **Issue**: Field `_selectedAvailability` assigned but never read
- **Fix**: Remove if truly unused, or use it if needed

## Action Plan

1. ✅ **After Migration**: Fix these issues in a separate commit
2. ✅ **Re-enable Warnings-as-Errors**: Update `Directory.Build.props` to set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
3. ✅ **Verify**: Ensure build passes with all warnings treated as errors

## Priority

**Medium** - These are code quality issues, not blocking bugs. Safe to defer until after migration.

