# MagiDesk POS - Enterprise Billiard Point of Sale System

[![CI/CD Pipeline](https://github.com/zedfauji/pos-app/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/zedfauji/pos-app/actions/workflows/ci-cd.yml)

Enterprise-grade WinUI 3 Point of Sale system for billiard parlors with real-time table management, order processing, and payment handling.

## 🏗️ Architecture

- **Frontend**: WinUI 3 Desktop App (MVVM pattern)
- **Backend**: 9 ASP.NET Core 8.0 Microservices
- **Database**: PostgreSQL 17
- **Infrastructure**: Docker containers deployed on Render
- **CI/CD**: GitHub Actions with automated testing and deployment

## 📁 Repository Structure

```
├── src/
│   ├── Backend/          # 9 Microservices (TablesApi, OrderApi, etc.)
│   ├── Frontend/         # WinUI 3 Desktop Application
│   └── Shared/           # Shared DTOs and Models
├── tests/                # xUnit Test Suite
├── infra/                # Terraform Infrastructure as Code
├── scripts/              # Automation scripts
└── solution/             # .NET Solution File
```

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or Rider
- PostgreSQL 17 (local or Cloud SQL)
- Docker Desktop (for containerization)

### Local Development

```powershell
# Restore dependencies
dotnet restore solution/MagiDesk.sln

# Build solution
dotnet build solution/MagiDesk.sln --configuration Release

# Run tests
dotnet test solution/MagiDesk.sln --configuration Release

# Run frontend
dotnet run --project src/Frontend/frontend/MagiDesk.Frontend.csproj
```

## 🌿 Branch Strategy

**IMPORTANT**: `main` branch is **protected** and requires pull requests for all changes.

### Branch Workflow

1. **`main`** - Production-ready code only
   - ✅ Protected: Requires PR, status checks, no force push
   - ✅ Deploys to Render production automatically
   - ❌ **Never commit directly to `main`**

2. **`revamp/ci-cd-and-cleanup`** - Primary development branch
   - ✅ All feature work happens here
   - ✅ Automatic CI/CD checks on every push
   - ✅ Creates Render preview environments for PRs

3. **`revamp/baseline-2025-12-09`** - Production snapshot
   - 🔒 Read-only baseline (never modify)
   - 📸 Exact copy of production state as of Dec 9, 2025

### Feature Branch Naming

- `feature/description` - New features
- `fix/description` - Bug fixes
- `refactor/description` - Code refactoring
- `docs/description` - Documentation updates

## 🔄 CI/CD Pipeline

The pipeline runs automatically on every push (except `main`) and includes:

### ✅ Automated Checks

1. **Restore → Build → Test**
   - Restores NuGet packages
   - Builds entire solution
   - Runs unit tests with code coverage

2. **Code Format Verification**
   - Enforces code style via `dotnet format --verify-no-changes`
   - Fails if code is not formatted correctly

3. **Docker Image Builds**
   - Builds all 9 microservice Docker images
   - Caches layers for faster builds
   - Pushes to GitHub Container Registry

4. **Security Scanning**
   - Trivy vulnerability scanner
   - Uploads results to GitHub Security tab

5. **Render Preview Environments**
   - Automatically deploys PR branches to Render
   - Provides preview URLs in PR comments

### 🚀 Production Deployment

Production deployment to Render **only** happens when:
- ✅ PR is merged to `main`
- ✅ All CI checks pass
- ✅ Manual approval (if configured)

## 🛡️ Security

- Secrets managed via GitHub Secrets
- No credentials in code
- Regular security scans via Trivy
- Branch protection prevents accidental production deploys

## 📊 Development Standards

### Code Quality

- **Warnings as Errors**: Enabled in `Directory.Build.props`
- **Code Style**: Enforced via `.editorconfig`
- **Formatting**: Auto-verified in CI/CD
- **Nullability**: `Nullable` enabled project-wide

### Testing

- All tests must pass before merge
- Code coverage reports generated
- Test results published to GitHub Actions

## 🔧 Configuration

### Environment Variables

Backend services require:
- `ConnectionStrings__Postgres` - PostgreSQL connection string
- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Production)
- `ASPNETCORE_URLS` - Server binding URL

### Database

- PostgreSQL 17
- Connection string format: `Host=...;Port=5432;Database=...;Username=...;Password=...`
- Schema migrations via Entity Framework Core

## 📚 Additional Documentation

- [Migration Guide](MIGRATION_GUIDE.md) - Repository structure migration
- [Render Deployment](scripts/migration/RENDER_DEPLOYMENT_TROUBLESHOOTING.md) - Deployment troubleshooting
- [Branch Workflow](BRANCH_WORKFLOW_README.md) - Detailed branch strategy

## 🤝 Contributing

1. Create a feature branch from `revamp/ci-cd-and-cleanup`
2. Make your changes
3. Ensure all tests pass and code is formatted
4. Create a pull request to `revamp/ci-cd-and-cleanup`
5. Wait for CI/CD checks to pass
6. Request review and merge

## 📝 License

Proprietary - All rights reserved

---

**Last Updated**: December 10, 2025  
**Production Baseline**: `revamp/baseline-2025-12-09`  
**Active Development**: `revamp/ci-cd-and-cleanup`

