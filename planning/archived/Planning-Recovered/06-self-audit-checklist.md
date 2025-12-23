# Anti-Drift Self-Audit Checklist

**Mandate**: Run this checklist mentally or physically at each stage of a task.

## 1. Pre-Task Launch (The "Why" Check)
- [ ] **WBS Alignment**: Does this task exist in `01-master-wbs.md`?
- [ ] **Dependency Check**: Are the Prerequisites (e.g., Swagger, Assets) actually available?
- **STOP Condition**: If the task is "Fixing X" but X isn't in WBS, create an Issue first. Do not fix forward without a plan update.

## 2. In-Flight Architecture Scan (The "How" Check)
- [ ] **Database Ban**: Did I just type `using Npgsql` or `new SqlConnection`? (STOP if yes).
- [ ] **Logic Trap**: Did I just write `if (total > 100)` in a ViewModel? (Move to Backend if it's business logic).
- [ ] **Service Locator**: Did I write `App.Services...`? (Inject it instead).
- [ ] **Async Void**: Did I write `async void` outside of an Event Handler? (Change to `async Task`).

## 3. Pre-Merge Quality Gate (The "Done" Check)
- [ ] **Architecture Tests**: Did I run `dotnet test MagiDesk.Client.ArchTests`?
- [ ] **Compilation**: Does the solution build without warnings?
- [ ] **Visuals**: Did I verify the UI matches the mockup (or at least looks decent)?
- [ ] **Logic**: Did I verify the data is coming from the API (not hardcoded)?

## 4. Emergency Procedures
- **"I'm Stuck"**: If a task takes > 2 hours longer than expected -> Stop -> update WBS -> Notify.
- **"Requirement Changed"**: If code contradicts `04-product-and-ux-flows.md` -> Update the Doc -> Notify.
