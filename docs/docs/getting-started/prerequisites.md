# Prerequisites

Before you begin developing with MagiDesk POS, ensure you have the following prerequisites installed and configured.

## Required Software

### Windows Development

- **Windows 10/11** (64-bit)
- **Visual Studio 2022** (Community, Professional, or Enterprise)
  - Workload: **.NET desktop development**
  - Workload: **Desktop development with C++**
  - Component: **Windows 11 SDK (10.0.22621.0 or later)**
  - Component: **Windows App SDK C# Templates**

### .NET SDK

- **.NET 8.0 SDK** or later
  - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
  - Verify installation: `dotnet --version`

### Database

- **PostgreSQL 17** (for local development)
  - Download from: https://www.postgresql.org/download/windows/
  - Or use Docker: `docker run -p 5432:5432 -e POSTGRES_PASSWORD=yourpassword postgres:17`

### Google Cloud (for deployment)

- **Google Cloud SDK (gcloud CLI)**
  - Download from: https://cloud.google.com/sdk/docs/install
  - Authenticate: `gcloud auth login`
  - Set project: `gcloud config set project YOUR_PROJECT_ID`

### Version Control

- **Git** (latest version)
  - Download from: https://git-scm.com/download/win

## Optional Tools

### Database Management

- **pgAdmin 4** - PostgreSQL administration tool
- **DBeaver** - Universal database tool
- **Azure Data Studio** - Cross-platform database tool

### API Testing

- **Postman** - API testing and documentation
- **Insomnia** - REST API client
- **Swagger UI** - Available at `/swagger` when running APIs locally

### Code Quality

- **ReSharper** or **Rider** - Code analysis and refactoring
- **SonarLint** - Code quality and security analysis

## Development Environment Setup

### 1. Clone the Repository

```powershell
git clone https://github.com/your-username/Order-Tracking-By-GPT.git
cd Order-Tracking-By-GPT
```

### 2. Verify .NET SDK

```powershell
dotnet --version
# Should output: 8.0.x or later
```

### 3. Restore Dependencies

```powershell
cd solution
dotnet restore
```

### 4. Build the Solution

```powershell
dotnet build MagiDesk.sln -c Debug
```

### 5. Set Up Local Database

Create a local PostgreSQL database:

```sql
CREATE DATABASE magidesk_dev;
```

Update connection strings in `appsettings.json` files.

## System Requirements

### Minimum Requirements

- **CPU:** 2 cores
- **RAM:** 4 GB
- **Disk:** 10 GB free space
- **OS:** Windows 10 version 1809 or later

### Recommended Requirements

- **CPU:** 4+ cores
- **RAM:** 8+ GB
- **Disk:** 20+ GB free space (SSD recommended)
- **OS:** Windows 11

## Account Setup

### Google Cloud Platform

1. Create a GCP account: https://cloud.google.com/
2. Create a new project
3. Enable required APIs:
   - Cloud Run API
   - Cloud SQL Admin API
   - Cloud Build API
4. Create a Cloud SQL instance (PostgreSQL)
5. Set up service account with appropriate permissions

### Database Access

- **Local Development:** Use local PostgreSQL instance
- **Cloud Development:** Use Cloud SQL instance
- **Connection String:** Configure in `appsettings.json`

## Verification Checklist

Before proceeding, verify:

- [ ] Visual Studio 2022 installed with required workloads
- [ ] .NET 8.0 SDK installed and verified
- [ ] PostgreSQL installed and running locally
- [ ] Git installed and configured
- [ ] Repository cloned successfully
- [ ] Solution builds without errors
- [ ] Database connection works
- [ ] Google Cloud SDK installed (for deployment)

## Troubleshooting

### Common Issues

**Issue:** "Windows App SDK not found"
- **Solution:** Install Windows App SDK from Visual Studio Installer

**Issue:** "PostgreSQL connection failed"
- **Solution:** Verify PostgreSQL service is running and connection string is correct

**Issue:** "Build errors related to WinUI 3"
- **Solution:** Ensure Windows 11 SDK is installed and project targets correct version

**Issue:** "gcloud command not found"
- **Solution:** Add Google Cloud SDK to PATH or reinstall with PATH option enabled

## Next Steps

Once prerequisites are met:

1. [Installation Guide](./installation.md) - Complete setup instructions
2. [Quick Start](./quick-start.md) - Run the application
3. [Architecture Overview](../architecture/overview.md) - Understand the system

## Getting Help

If you encounter issues:

- Check [Troubleshooting Guide](../troubleshooting/common-issues.md)
- Review [FAQ](../faq/index.md)
- Open a [GitHub Issue](https://github.com/your-username/Order-Tracking-By-GPT/issues)
