# Active Context: MagiDesk POS System

## Current Work Focus

The project is in **active development** with core features implemented. The system has:
- ✅ Complete client-server architecture with WinUI 3 frontend
- ✅ Multiple microservices backend (Tables, Orders, Payments, Menu, etc.)
- ✅ Authentication and authorization
- ✅ Table management and session tracking
- ✅ Order creation and management
- ✅ Payment processing with split payment support
- ✅ Shift management with hard gating
- ✅ Receipt printing (PDFSharp and ESCPOS.NET)

**Current Phase**: Production hardening and feature completion

## Recent Changes

### Architecture Decisions
- **Payment Flow**: Moved from ContentDialog to Page-based architecture (PaymentWorkspacePage)
- **Type Compatibility**: Changed payment properties from `decimal` to `double` for WinUI NumberBox binding
- **Route Standardization**: Client APIs updated to match backend routing (`/tables/active` instead of `/sessions/active`)
- **Data Persistence**: Database initializer no longer drops schemas on startup (prevents data loss)
- **Financial Safety**: Enforced full payment model - rejects partial payments for Cash method
- **Shift Gating**: All financial operations require open shift (enforced at API level with `[RequireOpenShift]`)

### Implementation Status
- ✅ ViewModel template registration system
- ✅ Navigation service with ViewModel-based routing
- ✅ API clients using Refit with Polly retry policies
- ✅ Serilog logging throughout application
- ✅ Dependency injection container setup
- ✅ Architecture tests enforcing boundaries

## Next Steps

### Immediate Priorities
1. **Complete Payment Flow Testing**: Verify full payment, split payment, and partial payment scenarios
2. **Shift Controller UI**: Implement frontend for shift open/close operations
3. **Error Handling**: Improve user-facing error messages for API failures
4. **Receipt Printing**: Verify thermal printer integration with physical hardware

### Short-Term Goals
1. **Menu Editor**: Complete menu item management UI
2. **Inventory Management**: Finish inventory tracking and vendor order workflows
3. **Reporting**: Implement reporting dashboards
4. **Settings Management**: Complete system settings UI

### Long-Term Goals
1. **Offline Mode**: Implement offline capability with sync
2. **Real-time Updates**: WebSocket support for live table status
3. **Multi-location**: Support for multiple restaurant locations
4. **Mobile App**: Expand to mobile platform using same APIs

## Active Decisions and Considerations

### Financial Safety
- **Decision**: Backend calculates all totals and validates payments
- **Rationale**: Prevents financial discrepancies from UI rounding errors
- **Status**: Implemented and enforced

### Type Handling
- **Decision**: UI uses `double` for NumberBox binding, casts to `decimal` for API calls
- **Rationale**: WinUI NumberBox requires double, backend uses decimal for precision
- **Status**: Implemented with explicit casting

### Shift Management
- **Decision**: Shift is immutable once closed, only one open shift allowed
- **Rationale**: Financial audit trail integrity
- **Status**: Implemented with database constraints

### Architecture Enforcement
- **Decision**: Automated architecture tests block violations
- **Rationale**: Prevents regression to legacy patterns
- **Status**: Active, runs on every build

## Important Patterns and Preferences

### MVVM Patterns
- **Always use `[ObservableProperty]`**: Never manual `OnPropertyChanged()` calls
- **Always use `[RelayCommand]`**: Never manual command implementations
- **Dependency Injection**: All ViewModels use constructor injection
- **x:Bind preferred**: Use x:Bind over {Binding} when possible for performance

### Code Organization
- **No Code-Behind Logic**: Except UI event routing (OnItemInvoked, etc.)
- **No ViewModel → View References**: ViewModels must not reference UI types
- **No UI Math**: Business calculations belong in backend
- **No Silent Failures**: All binding errors and exceptions must be logged

### API Patterns
- **Refit Interfaces**: All API calls through Refit interfaces
- **Polly Retry**: Automatic retry with exponential backoff
- **Serilog Logging**: Structured logging to file and console
- **Shared DTOs**: All data contracts in MagiDesk.Shared project

### Database Patterns
- **Schema Separation**: ord (orders), pay (payments), public (tables, shifts)
- **Immutable Records**: Closed shifts and finalized orders cannot be modified
- **Foreign Keys**: shift_id links all financial transactions

## Learnings and Project Insights

### WinUI 3 Specifics
- **NumberBox requires double**: Cannot bind decimal directly, must cast
- **DatePicker**: Use `DatePickerValueChangedEventArgs`, not `CalendarDateChangedEventArgs`
- **x:Bind is compile-time**: Better performance than {Binding}
- **XAML Compilation**: Some issues require Visual Studio debugging, not simple fallbacks

### Architecture Insights
- **Thin Client Works**: Zero business logic in client is achievable and beneficial
- **API-First Design**: Defining contracts first enables parallel development
- **Library-First Approach**: Using established libraries (Refit, Polly) saves time
- **Architecture Tests**: Automated enforcement prevents regression

### Development Workflow
- **Build Before Proceeding**: Always verify build succeeds before moving on
- **No Fallback Implementations**: Ask user for help with XAML issues rather than creating simple versions
- **PowerShell Commands**: Use PowerShell-compatible commands (Windows environment)
- **Visual Studio Debugging**: User prefers VS 2022 Community for XAML debugging

### Financial Safety
- **Backend Authority**: All financial calculations must happen server-side
- **Validation at API Level**: `[RequireOpenShift]` attribute enforces shift gating
- **Immutable Financial Records**: Once closed, shifts and payments cannot be modified
- **Precision Handling**: Use decimal for financial calculations, double only for UI binding

