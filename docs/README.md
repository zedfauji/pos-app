# Developer Portal Documentation

This directory contains the complete developer portal documentation for MagiDesk POS, built with Docusaurus.

## Structure

```
docs/
├── docs/              # Documentation content (Markdown files)
│   ├── getting-started/
│   ├── architecture/
│   ├── operations/
│   ├── backend/
│   ├── frontend/
│   ├── api/
│   ├── security/
│   ├── testing/
│   ├── contributing/
│   └── faq/
├── src/               # Custom React components and CSS
├── static/            # Static assets (images, etc.)
├── docusaurus.config.js  # Docusaurus configuration
├── sidebars.js        # Sidebar navigation configuration
└── package.json       # Node.js dependencies
```

## Local Development

### Prerequisites

- Node.js 18+
- npm or yarn

### Setup

```powershell
# Install dependencies
npm install

# Start development server
npm start

# Build for production
npm run build

# Serve production build locally
npm run serve
```

## Documentation Sections

### Getting Started
- Overview
- Prerequisites
- Installation
- Quick Start

### Architecture
- System Architecture
- Frontend Architecture
- Backend Architecture
- Database Architecture
- Deployment Architecture
- RBAC Architecture

### Operations & Support
- Logging
- Monitoring
- Alerting
- Service Management
- Backup & Restore
- Disaster Recovery
- Performance Tuning

### Backend APIs
- UsersApi
- MenuApi
- OrderApi
- PaymentApi
- InventoryApi
- SettingsApi
- CustomerApi
- DiscountApi
- TablesApi

### Frontend
- Overview
- ViewModels
- Views & Pages
- Services
- Navigation
- Data Binding

### API Reference
- API Overview
- Authentication
- OpenAPI Specification
- API v1 (Legacy)
- API v2 (RBAC-enabled)

### Security
- Security Overview
- RBAC Documentation
- Authentication
- Best Practices

### Testing
- Testing Overview
- Unit Tests
- Integration Tests
- E2E Tests

### Contributing
- Contributing Guide
- Development Workflow
- Coding Standards
- Commit Guidelines
- Pull Request Process

### FAQ
- General Questions
- Development Questions
- API Questions
- Troubleshooting

## Deployment

Documentation is automatically deployed to GitHub Pages when changes are pushed to the repository.

### Manual Deployment

```powershell
# Build documentation
npm run build

# Deploy (if configured)
npm run deploy
```

## Adding New Documentation

1. Create Markdown file in appropriate directory under `docs/docs/`
2. Add entry to `sidebars.js` for navigation
3. Follow existing documentation style
4. Include Mermaid diagrams where helpful
5. Test locally before committing

## Documentation Standards

- Use clear, concise language
- Include code examples
- Add diagrams (Mermaid) for complex concepts
- Keep documentation up to date with code
- Link related sections
- Include troubleshooting information

## Maintenance

- Review documentation quarterly
- Update with each major release
- Keep examples current
- Remove outdated information
- Add new sections as needed
