# Commit Message Template
## Repository Structure Migration

Use this template for the migration commit:

```
refactor: migrate to enterprise repository structure

BREAKING CHANGE: Repository structure reorganized per DevOps audit

Migration Summary:
- Moved backend services: solution/backend/* → src/Backend/*
- Moved frontend: solution/frontend → src/Frontend
- Moved shared libraries: solution/shared → src/Shared
- Moved tests: solution/MagiDesk.Tests → tests/MagiDesk.Tests
- Moved docs: solution/docs → docs/
- Created infrastructure: infra/terraform/
- Removed archive: solution/archive/ (deprecated code)

New Structure:
├── src/
│   ├── Backend/          # 9 microservices (TablesApi, OrdersApi, etc.)
│   ├── Frontend/         # WinUI3 desktop application
│   └── Shared/           # Shared DTOs and libraries
├── tests/                # Test projects
├── docs/                 # Documentation
├── infra/                # Infrastructure as Code
│   └── terraform/        # GCP Cloud Run + Cloud SQL
└── scripts/              # Deployment and utility scripts

Changes:
- Updated MagiDesk.sln with new project paths
- Added Directory.Build.props for shared MSBuild properties
- Added .editorconfig for consistent code formatting
- Generated Terraform IaC for GCP infrastructure
- Updated .gitignore for new structure

Build Status: ✅ All projects build successfully
Tests: ✅ All tests pass

Backup Branch: backup/pre-migration-YYYYMMDD-HHMMSS

Closes #[issue-number]
```

## Alternative: Conventional Commits Format

```
refactor(repo): migrate to enterprise repository structure

BREAKING CHANGE: Repository structure reorganized

- Backend: solution/backend → src/Backend
- Frontend: solution/frontend → src/Frontend
- Shared: solution/shared → src/Shared
- Tests: solution/MagiDesk.Tests → tests/MagiDesk.Tests
- Docs: solution/docs → docs/
- Infrastructure: infra/terraform/ (new)
- Removed: solution/archive/

Related: DevOps Audit Report
Backup: backup/pre-migration-YYYYMMDD-HHMMSS
```

## Short Version

```
refactor: migrate to enterprise repo structure (solution/* → src/)

BREAKING CHANGE: All project paths updated in solution file

- Backend services → src/Backend/
- Frontend → src/Frontend/
- Tests → tests/
- Docs → docs/
- Added infra/terraform/ for GCP IaC

Backup branch: backup/pre-migration-YYYYMMDD-HHMMSS
```

