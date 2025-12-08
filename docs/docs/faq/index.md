# Frequently Asked Questions (FAQ)

## General

### What is MagiDesk POS?

MagiDesk POS is a comprehensive Point of Sale system designed for restaurants, bars, and hospitality businesses. It includes order management, payment processing, inventory management, customer management, and more.

### What technologies does it use?

- **Frontend**: WinUI 3, .NET 8, C#
- **Backend**: ASP.NET Core 8, C#
- **Database**: PostgreSQL 17
- **Deployment**: Google Cloud Run, Cloud SQL
- **Architecture**: Microservices

### Is it open source?

Yes, MagiDesk POS is open source. See the [LICENSE](../../LICENSE) file for details.

## Development

### How do I set up the development environment?

See the [Installation Guide](../getting-started/installation.md) for complete setup instructions.

### What are the system requirements?

- **Windows 10/11** (for frontend development)
- **.NET 8 SDK**
- **Visual Studio 2022** (recommended)
- **PostgreSQL 17** (or Cloud SQL access)
- **Node.js 18+** (for documentation)

### How do I run the application locally?

1. Start the database (Cloud SQL or local PostgreSQL)
2. Start backend APIs (see [Backend Overview](../backend/overview.md))
3. Start frontend (see [Frontend Overview](../frontend/overview.md))

### How do I add a new API endpoint?

1. Create controller in appropriate API project
2. Add route and HTTP method attributes
3. Implement endpoint logic
4. Add RBAC permissions (for v2 endpoints)
5. Add tests
6. Update API documentation

## API

### What's the difference between v1 and v2 APIs?

- **v1**: Legacy endpoints, no RBAC enforcement (backward compatible)
- **v2**: RBAC-enabled endpoints, requires permissions

### How do I authenticate API requests?

For v2 endpoints, include `X-User-Id` header with the user ID. For production, JWT tokens will be used.

### What permissions do I need?

See the [RBAC Documentation](../security/rbac.md) for complete permission list.

### How do I handle API errors?

APIs return standard HTTP status codes:
- `200`: Success
- `400`: Bad Request
- `401`: Unauthorized
- `403`: Forbidden (missing permission)
- `404`: Not Found
- `500`: Server Error

## Database

### What database does it use?

PostgreSQL 17 on Google Cloud SQL.

### How do I access the database?

Use Cloud SQL Proxy for local development or connect directly to Cloud SQL instance.

### How do I run migrations?

Migrations are handled via SQL scripts. See [Database Migrations](../database/migrations.md).

## Deployment

### How do I deploy to production?

See the [Deployment Guide](../deployment/production.md).

### How do I monitor services?

See the [Monitoring Guide](../operations/monitoring.md).

### How do I view logs?

See the [Logging Guide](../operations/logging.md).

## Troubleshooting

### Service won't start

1. Check logs for errors
2. Verify database connectivity
3. Check configuration (appsettings.json)
4. Verify dependencies are installed

### API returns 403 Forbidden

1. Check user has required permissions
2. Verify `X-User-Id` header is present
3. Check role assignments
4. Review [RBAC Documentation](../security/rbac.md)

### Database connection fails

1. Verify Cloud SQL instance is running
2. Check connection string
3. Verify network connectivity
4. Check firewall rules

### Frontend won't connect to APIs

1. Verify API URLs in appsettings.json
2. Check API services are running
3. Verify network connectivity
4. Check CORS configuration (if applicable)

## Support

### Where can I get help?

- **Documentation**: This developer portal
- **GitHub Issues**: [Open an issue](https://github.com/zedfauji/pos-app/issues)
- **GitHub Discussions**: [Start a discussion](https://github.com/zedfauji/pos-app/discussions)

### How do I report a bug?

Open a GitHub issue with:
- Description of the bug
- Steps to reproduce
- Expected vs actual behavior
- Environment details
- Logs (if applicable)

### How do I request a feature?

Open a GitHub issue with:
- Feature description
- Use case
- Proposed implementation (if any)
- Benefits

## Contributing

### How do I contribute?

See the [Contributing Guide](../contributing/overview.md).

### What should I work on?

Check GitHub Issues for:
- Good first issues (labeled `good-first-issue`)
- Help wanted (labeled `help-wanted`)
- Bugs (labeled `bug`)
- Features (labeled `enhancement`)

### How do I submit a pull request?

See the [Pull Request Process](../contributing/pull-request-process.md).

## Security

### How is security handled?

- RBAC for authorization
- Secure authentication
- Encrypted database connections
- Secure API communication
- Regular security audits

### How do I report a security vulnerability?

Please email security issues directly to maintainers (do not open public issues).

## Performance

### What are the performance characteristics?

- API response times: < 200ms (p95)
- Database query times: < 100ms (p95)
- Frontend load time: < 2 seconds

### How do I optimize performance?

See the [Performance Tuning Guide](../operations/performance-tuning.md).
