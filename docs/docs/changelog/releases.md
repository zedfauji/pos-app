# Release Notes

This document tracks all releases and changes to MagiDesk POS.

## Versioning Strategy

MagiDesk POS follows [Semantic Versioning](https://semver.org/) (SemVer):

- **MAJOR.MINOR.PATCH** (e.g., 1.2.3)
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

## Release History

### Version 1.0.0 (2025-01-02)

**Initial Release**

#### Features
- ✅ Complete RBAC system with 47 permissions
- ✅ 9 microservices (UsersApi, MenuApi, OrderApi, PaymentApi, InventoryApi, SettingsApi, CustomerApi, DiscountApi, TablesApi)
- ✅ WinUI 3 desktop frontend with MVVM pattern
- ✅ API versioning (v1 legacy, v2 RBAC-enabled)
- ✅ PostgreSQL database with 9 schemas
- ✅ Google Cloud Run deployment
- ✅ Self-hosted GitHub Actions runner
- ✅ Comprehensive documentation

#### Backend
- UsersApi with authentication and RBAC
- MenuApi with menu item management
- OrderApi with order processing
- PaymentApi with payment processing and refunds
- InventoryApi with inventory management
- SettingsApi with hierarchical settings
- CustomerApi with customer management
- DiscountApi with discount management
- TablesApi with table/session management

#### Frontend
- 70+ views/pages
- 27 ViewModels
- 51+ services
- 15+ converters
- 24+ dialogs

#### Security
- Role-Based Access Control (RBAC)
- 6 system roles (Owner, Admin, Manager, Server, Cashier, Host)
- Permission-based authorization
- Backend-enforced security

#### Documentation
- Complete developer portal
- Architecture documentation
- API reference
- Troubleshooting guides
- Coding standards

---

## Upcoming Releases

### Version 1.1.0 (Planned)

#### Planned Features
- JWT authentication
- Real-time updates via WebSocket
- Enhanced reporting
- Mobile app support
- Multi-language support

---

## Migration Guides

### Migrating from v1 to v2 APIs

See [API Migration Guide](../api/v2/overview#migration-from-v1) for details.

### Database Migrations

See [Database Migrations](../database/migrations) for migration procedures.

---

**Last Updated**: 2025-01-02

