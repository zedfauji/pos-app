# Project Brief: MagiDesk POS System

## Overview

MagiDesk is a modern Point of Sale (POS) system designed for restaurant and hospitality operations. The system manages table sessions, orders, payments, inventory, menu items, and shift operations. It is built as a **WinUI 3 desktop application** with a **microservices backend architecture** using ASP.NET Core APIs.

The project represents a complete rewrite from a legacy application that suffered from high coupling, business logic embedded in UI, and direct database dependencies. The new architecture enforces strict **Client-Server separation** with zero business logic in the client.

## Core Requirements

- **Thin Client Architecture**: WinUI 3 client must contain zero business logic and zero database references
- **API-First Design**: All backend interaction through Refit interfaces and shared DTOs
- **MVVM Pattern**: Strict MVVM using CommunityToolkit.Mvvm with ObservableObject and RelayCommand
- **Microservices Backend**: Separate APIs for Tables, Orders, Payments, Menu, Inventory, Settings, Users, Customers, Discounts, Reporting
- **PostgreSQL Database**: Centralized database with schema separation (ord, pay, public schemas)
- **Shift Management**: Hard gating - no operations without an open shift
- **Financial Safety**: Backend is authoritative for all calculations, validations, and business rules
- **Printing Support**: PDFSharp for receipt generation, ESCPOS.NET for thermal printing

## Goals

- **Maintainability**: Clean architecture with testable, decoupled components
- **Scalability**: Support for web/mobile expansion through API-first design
- **Reliability**: Robust error handling, retry policies, and offline resilience
- **Performance**: Efficient data binding with x:Bind, async/await patterns
- **Security**: Token-based authentication, shift-based access control
- **User Experience**: Modern WinUI 3 interface with responsive, touch-optimized controls

## Project Scope

### In Scope
- Table management and session tracking
- Order creation and management
- Payment processing (Cash, Card, Split payments)
- Menu browsing and item selection
- Inventory management
- Shift open/close operations
- Receipt printing
- User authentication and authorization
- Settings management
- Reporting and analytics
- Customer management and loyalty programs
- Vendor order management

### Out of Scope (Current Phase)
- Web/mobile clients (architecture supports future expansion)
- Real-time notifications (WebSocket support)
- Advanced reporting dashboards
- Multi-location support
- Offline mode with sync (future enhancement)

## Key Architectural Principles

1. **Backend is Authoritative**: All business logic, calculations, and validations happen server-side
2. **Client is Display Only**: UI displays data and captures user intent, no calculations
3. **Zero Database in Client**: No Npgsql, no direct SQL, no DbContext in client
4. **Dependency Injection**: All services and ViewModels use constructor injection
5. **Library-First**: Use established libraries (Refit, Polly, Serilog) instead of custom implementations
6. **Architecture Tests**: Automated tests enforce architectural boundaries

