# 03 - Library & Tooling Decisions

## 1. Reporting Engine
- **Decision**: **No new library**.
- **Implementation**: logic in C# (`LINQ` over `Dapper` or Entity Framework projections).
- **Reasoning**: Reports are simple aggregations. No need for Crystal Reports or heavy BI tools. The "View" is the receipt printer.

## 2. Receipt Generation
- **Decision**: **ESCPOS.NET** (Existing).
- **Implementation**: String building with ESC commands (`Initialize`, `Center`, `Bold`).
- **Reasoning**: Thermal printers require raw commands for speed and correct cutting/drawer kick. Generating PDF and printing it is slow and driver-dependent.

## 3. Database Access (Reporting)
- **Decision**: **Dapper** (if raw SQL needed for speed) or **EF Core** (if logic is simple).
- **Recommendation**: Reuse existing backend pattern. If backend uses EF Core + Specification, use that. If raw SQL, use Dapper. **Do not mix patterns.**

## 4. API Communication
- **Decision**: **Refit** (Existing).
- **Reasoning**: Standard for this project.

## 5. Resilience
- **Decision**: **Polly** (Existing).
- **Reasoning**: Retry reporting calls if DB is locked.
