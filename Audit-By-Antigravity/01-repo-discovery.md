# Audit Phase 1: Repository Discovery

## 1. Project Structure Overview

The repository follows a standard .NET solution structure with a clear separation between Frontend and Backend at the root level, but with significant implementation leakage.

### Folder Tree (Simplified)

```text
Solution/
├── info/ (Documentation & specs)
├── solution/
│   ├── backend/ (Microservices/API)
│   │   ├── CustomerApi/
│   │   ├── OrderApi/
│   │   ├── PaymentApi/
│   │   └── ... (6+ other services)
│   └── frontend/ (WinUI 3 Desktop App)
│       ├── Services/ (API Clients + Logic)
│       ├── ViewModels/ (MVVM State)
│       ├── Views/ (XAML)
│       └── Assets/
└── src/ (Mobile/Maui experiments - ignored for this audit)
```

## 2. Identified Layers & Responsibilities

| Layer | Technology | Actual Responsibility (Observed) | Clean Arch Violation? |
|-------|------------|----------------------------------|-----------------------|
| **UI** | WinUI 3 (XAML) | Rendering, Layout, Styling | No (Mostly) |
| **ViewModel** | CommunityToolkit.Mvvm | State, UI Logic, *Business Calculation* | **YES** |
| **Service (Frontend)** | C# Classes | API Communication, *Direct DB Access*, *Transaction Management* | **CRITICAL** |
| **Backend** | ASP.NET Core | API Endpoints, Domain Logic (Duplicated) | N/A (Source of Truth) |

## 3. Critical Architectural Violations

### A. The "Fat Client" Hybrid Model
While the repository has a Backend API, the Frontend is designed as a **"Smart Client"** that operates in a dual mode (Online/Offline). It does not purely consume APIs. Instead, it maintains its own direct connection to the database (`Npgsql`) and executes complex SQL transactions locally.

### B. Dependency Injection Pollution
The Frontend project (`MagiDesk.Frontend.csproj`) has a direct dependency on **`Npgsql`**. In a Clean Architecture / API-First system, the UI client should NEVER know what database engine is being used, let alone reference the driver.

### C. Logic Duplication
Business rules (e.g., creating a billing record, starting a session) exist in the Backend (`InventoryApi`, `OrderApi`) but are **duplicated** in the Frontend `BillingService.cs` using raw SQL. This creates a "Split Brain" problem where logic changes must be synchronized across two codebases.

## 4. Coupling Analysis

- **High Coupling**: Frontend `Services` <-> Database Schema. Changes to the DB schema require recompiling and deploying the Desktop App.
- **Medium Coupling**: ViewModels <-> specific Service implementation details (Offline vs Online switching logic).

## 5. Conclusion
The application is **NOT** API-First. It is a **Sync-Capable Fat Client** with a heavy reliance on local logic and direct database access. Migration to a pure API-First Thin Client will require significant "Hollowing Out" of the Frontend Services.
