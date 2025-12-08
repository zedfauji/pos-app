# Archive Notes - Test Scripts

This directory contains test and diagnostic scripts that are no longer in active use.

## Archived Scripts

These PowerShell scripts were used for testing and diagnostics:

- **test-*.ps1**: Various test scripts for APIs, RBAC, payments, etc.
- **test-db-connection.ps1**: Database connection testing
- **test-health-endpoints.ps1**: Health endpoint testing
- **Other test scripts**: Various diagnostic and testing scripts

## Why Archived

These scripts were archived because:
- They are ad-hoc test scripts from development phases
- They are superseded by automated tests in the test projects
- They are no longer needed for regular operations
- They may contain outdated endpoints or configurations

## Active Testing

For current testing:
- Use the test projects in `solution/MagiDesk.Tests/`
- Use the API test projects in `solution/backend/{ApiName}.Tests/`
- See the [Testing Documentation](../../docs/docs/testing/overview.md)

## Restoration

If you need any of these scripts:
1. Review them for current relevance
2. Update endpoints and configurations
3. Consider integrating into test projects if useful
4. Move back to root if needed for specific use cases
