# Tech Context: MagiDesk POS System

## Technologies Used

### Frontend
- **WinUI 3** (Windows App SDK): Modern Windows desktop UI framework
- **.NET 8**: Latest .NET runtime
- **C# 12**: Modern C# language features
- **XAML**: UI markup language
- **CommunityToolkit.Mvvm**: MVVM framework with source generators
- **Refit**: Type-safe REST API client generation
- **Polly**: Resilience and transient-fault-handling library
- **Serilog**: Structured logging framework
- **PDFSharp**: PDF generation for receipts
- **ESCPOS.NET**: Thermal printer abstraction

### Backend
- **ASP.NET Core 8**: Web API framework
- **.NET 8**: Runtime
- **C# 12**: Language
- **PostgreSQL**: Relational database
- **Dapper**: Lightweight ORM for data access
- **Npgsql**: PostgreSQL .NET driver
- **Refit**: API client generation (for inter-service communication)
- **Serilog**: Structured logging

### Infrastructure
- **Google Cloud Run**: Container hosting for APIs
- **Google Cloud SQL**: Managed PostgreSQL instance
- **Docker**: Containerization
- **PowerShell**: Build and deployment scripts
- **Process Compose**: Local development orchestration

### Testing
- **xUnit**: Unit testing framework
- **NetArchTest**: Architecture testing library
- **Moq**: Mocking framework (where needed)

### Development Tools
- **Visual Studio 2022 Community**: Primary IDE
- **Git**: Version control
- **PowerShell 7**: Scripting and automation

## Development Setup

### Prerequisites
- Windows 11
- .NET 8 SDK
- Visual Studio 2022 Community
- PostgreSQL (local or Cloud SQL)
- PowerShell 7

### Local Development

#### Backend Services
- **Start Script**: `start-backend.ps1`
- **Ports**:
  - TablesApi: 53503 (HTTPS)
  - OrderApi: 53504
  - PaymentApi: 5002 (HTTP)
  - MenuApi: 5227 (HTTP)
  - UsersApi: 55162 (HTTP)
  - InventoryApi: 5117 (HTTP)
  - SettingsApi: (varies)
- **Database**: PostgreSQL connection string in appsettings.json
- **Process**: Script kills existing processes, builds sequentially, starts services

#### Frontend Client
- **Project**: `solution/MagiDesk.Client/MagiDesk.Client.csproj`
- **Run**: F5 in Visual Studio or `dotnet run`
- **Configuration**: API base URLs in `App.xaml.cs` (DI configuration)

#### Database
- **Connection**: PostgreSQL instance (local or Cloud SQL)
- **Schemas**: `public`, `ord`, `pay`, `audit`
- **Migrations**: SQL scripts in `backend/migrations/`
- **MCP Tool**: PostgreSQL MCP server for database interaction

### Build Process

#### Client Build
```powershell
cd solution/MagiDesk.Client
dotnet build
```

#### Backend Build
```powershell
cd solution/backend
dotnet build
```

#### Solution Build
```powershell
cd solution
dotnet build MagiDesk.sln
```

**Note**: DO NOT run `dotnet build MagiDesk.sln -c Debug -nologo` (blocked by rules)

### Deployment

#### Cloud Run Deployment
- **Project ID**: `bola8pos`
- **Region**: `northamerica-south1`
- **Service Names**: `magidesk-menu`, `magidesk-order`, `magidesk-payment`, etc.
- **Cloud SQL Instance**: `bola8pos:northamerica-south1:pos-app-1`
- **Scripts**: `deploy-*-cloudrun.ps1` in `backend/` directory

#### Database Credentials
- **Username**: `posapp`
- **Password**: `Campus_66`
- **Connection**: Cloud SQL Unix socket (`/cloudsql/...`)

### Configuration Files

#### Client Configuration
- **App.xaml.cs**: Service registration, API client configuration
- **appsettings.json**: (if needed for local overrides)

#### Backend Configuration
- **appsettings.json**: Database connection strings, API URLs
- **appsettings.Development.json**: Local development overrides

#### API Base URLs (Production)
```json
{
  "Api": {
    "BaseUrl": "https://magidesk-backend-904541739138.us-central1.run.app"
  },
  "SettingsApi": {
    "BaseUrl": "https://magidesk-settings-904541739138.us-central1.run.app"
  },
  "MenuApi": {
    "BaseUrl": "https://magidesk-menu-904541739138.northamerica-south1.run.app"
  },
  "OrderApi": {
    "BaseUrl": "https://magidesk-order-904541739138.northamerica-south1.run.app"
  },
  "PaymentApi": {
    "BaseUrl": "https://magidesk-payment-904541739138.northamerica-south1.run.app"
  },
  "TablesApi": {
    "BaseUrl": "https://magidesk-tables-904541739138.northamerica-south1.run.app"
  }
}
```

## Technical Constraints

### Platform Constraints
- **Windows Only**: WinUI 3 requires Windows 10/11
- **.NET 8**: Minimum runtime version
- **PowerShell**: All scripts must be PowerShell-compatible (not bash)

### Architecture Constraints
- **No Database in Client**: Zero tolerance for Npgsql or System.Data.SqlClient
- **No Business Logic in UI**: All logic in backend
- **API-First**: All data operations through APIs
- **MVVM Required**: No code-behind logic (except UI events)

### WinUI 3 Constraints
- **NumberBox requires double**: Cannot bind decimal directly
- **DatePicker**: Must use `DatePickerValueChangedEventArgs` (not WPF/UWP types)
- **x:Bind preferred**: Better performance than {Binding}
- **XAML Compilation**: Some issues require Visual Studio debugging

### Financial Constraints
- **Backend Authority**: All calculations server-side
- **Decimal Precision**: Use decimal for financial, double only for UI binding
- **Immutable Records**: Closed shifts and finalized payments cannot be modified

### Build Constraints
- **Always Build**: Must build affected modules after code changes
- **No Build Skipping**: Build must pass before proceeding
- **Architecture Tests**: Must pass on every build

## Dependencies

### Client Dependencies (MagiDesk.Client)
```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.5.240627000" />
<PackageReference Include="Refit" Version="8.0.0" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
<PackageReference Include="Serilog.Sinks.Debug" Version="5.0.0" />
<PackageReference Include="PdfSharp" Version="6.1.0" />
<PackageReference Include="ESCPOS.NET" Version="1.0.0" />
```

### Backend Dependencies (Common)
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
<PackageReference Include="Npgsql" Version="8.0.0" />
<PackageReference Include="Dapper" Version="2.1.35" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Refit" Version="8.0.0" />
```

### Shared Dependencies (MagiDesk.Shared)
```xml
<PackageReference Include="System.Text.Json" Version="8.0.0" />
```

### Testing Dependencies
```xml
<PackageReference Include="xunit" Version="2.6.2" />
<PackageReference Include="NetArchTest.Rules" Version="1.3.2" />
```

## Tool Usage Patterns

### Visual Studio 2022
- **Primary IDE**: All development in VS 2022 Community
- **XAML Debugging**: User prefers VS for XAML compilation issues
- **Build**: F6 or Build menu
- **Run**: F5 for debugging

### PowerShell Scripts
- **Backend Startup**: `start-backend.ps1` - Kills processes, builds, starts services
- **Database Queries**: `query-db.ps1` - Execute SQL queries
- **Deployment**: `deploy-*-cloudrun.ps1` - Deploy to Cloud Run
- **Testing**: `test-*.ps1` - Various test scripts

### Process Compose
- **Local Orchestration**: `process-compose.yml` for multi-service development
- **Alternative**: Manual startup via PowerShell scripts

### MCP Tools
- **PostgreSQL MCP**: Database interaction and querying
- **File System MCP**: File operations
- **Context7 MCP**: Library documentation lookup

### Git
- **Version Control**: Standard Git workflow
- **Branch**: `rewrite/ui-thin-client` (current development branch)

### Logging
- **Serilog**: Structured logging to file and console
- **Log Location**: `%LocalAppData%\MagiDesk\logs\log-{date}.txt`
- **Levels**: Information, Warning, Error, Fatal

### Error Handling
- **Unhandled Exceptions**: Logged to Serilog, shown in Debug builds
- **API Errors**: Retry with Polly, then show user-friendly message
- **Binding Errors**: Logged with context in Debug builds

