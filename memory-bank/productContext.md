# Product Context: MagiDesk POS System

## Problem Statement

The legacy Order Tracking application had critical architectural flaws:
- **High Coupling**: Business logic embedded directly in UI code-behind
- **Direct Database Access**: Client application connected directly to PostgreSQL, preventing scalability
- **Mixed Concerns**: UI, business logic, and data access were tightly intertwined
- **Lack of Testability**: Impossible to unit test business logic without UI dependencies
- **Rigid Architecture**: Changes required touching multiple layers simultaneously
- **No Scalability Path**: Could not expand to web or mobile without complete rewrite

The system needed a clean break to enforce proper separation of concerns and enable future expansion.

## User Experience Goals

### For Wait Staff
- **Quick Table Management**: Visual table map showing status (Open, Paused, Paying) at a glance
- **Fast Order Entry**: Touch-optimized menu browsing with quick add to cart
- **Session Awareness**: Clear visibility of what's been ordered, by whom, and when
- **Payment Workflow**: Streamlined payment processing with split payment support
- **Receipt Printing**: Reliable thermal and PDF receipt generation

### For Managers
- **Shift Control**: Open/close shifts with cash reconciliation
- **Operational Visibility**: Real-time view of all active sessions and orders
- **Financial Safety**: Hard gating prevents operations without open shift
- **Reporting**: Access to sales data, shift reports, and analytics

### For Administrators
- **Menu Management**: Edit menu items, categories, modifiers, and combos
- **Inventory Control**: Track stock levels, manage vendors, handle orders
- **Settings Configuration**: System-wide settings management
- **User Management**: Authentication and authorization control

## Success Metrics

### Technical Metrics
- **Zero Business Logic in Client**: Architecture tests enforce this boundary
- **100% API Coverage**: All data operations go through defined APIs
- **Build Success Rate**: All modules compile without errors
- **Test Coverage**: Architecture tests pass on every build

### Functional Metrics
- **Order-to-Payment Flow**: Complete workflow from table selection to payment completion
- **Shift Operations**: Reliable shift open/close with proper validation
- **Payment Accuracy**: Backend validates all financial transactions
- **Print Reliability**: Receipts generate correctly for all payment types

### User Experience Metrics
- **Response Time**: UI remains responsive during API calls
- **Error Handling**: Clear error messages, no silent failures
- **Offline Resilience**: Graceful degradation when backend unavailable
- **Touch Optimization**: All controls accessible via touch interface

## Business Context

MagiDesk serves restaurant and hospitality operations, specifically:
- **Pool Club La Calma** (Bola 8) - Primary use case
- Table-based service model (Bar, Billiards, Dining tables)
- Multiple payment methods (Cash, Card, Split)
- Shift-based operations with cash reconciliation
- Menu items with modifiers, combos, and categories
- Inventory tracking and vendor management
- Customer loyalty and membership programs

## Critical User Flows

1. **Start Shift**: Manager opens shift → System gates all operations
2. **Start Session**: Server selects table → Session begins with timer
3. **Place Order**: Browse menu → Add items → Send to kitchen
4. **Process Payment**: View bill → Select payment method → Process → Print receipt
5. **Close Shift**: Verify all sessions closed → Declare cash → Close shift → Print Z-report

