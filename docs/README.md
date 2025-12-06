# MagiDesk POS Developer Portal

This directory contains the Docusaurus-based Developer Portal documentation for MagiDesk POS.

## Quick Start

### Prerequisites

- Node.js 18+ and npm/yarn
- Git

### Installation

```bash
# Install dependencies
npm install

# Start development server
npm start
```

The documentation will be available at `http://localhost:3000`.

### Build

```bash
# Build for production
npm run build

# Serve production build locally
npm run serve
```

## Project Structure

```
docs/
├── docs/              # Documentation markdown files
├── src/               # React components and pages
├── static/            # Static assets (images, etc.)
├── docusaurus.config.js  # Docusaurus configuration
├── sidebars.js        # Sidebar navigation
└── package.json       # Dependencies
```

## Documentation Structure

- **Getting Started** - Installation and setup guides
- **Architecture** - System design and architecture
- **Frontend** - WinUI 3 development documentation
- **Backend** - Microservices API documentation
- **Database** - Schema and migration guides
- **Features** - Feature-specific documentation
- **API Reference** - Complete API documentation
- **Configuration** - App settings and environment variables
- **Deployment** - Deployment guides
- **Security** - Security and RBAC documentation
- **Developer Guide** - Coding standards and best practices
- **Troubleshooting** - Common issues and solutions

## Contributing

1. Edit markdown files in `docs/`
2. Test locally: `npm start`
3. Submit a pull request

## Deployment

The documentation is automatically deployed to GitHub Pages via GitHub Actions when changes are pushed to the `main` branch.

### Manual Deployment

```bash
npm run build
npm run deploy
```

## Customization

- **Branding:** Edit `docusaurus.config.js`
- **Navigation:** Edit `sidebars.js`
- **Styling:** Edit `src/css/custom.css`
- **Homepage:** Edit `src/pages/index.js`

## Resources

- [Docusaurus Documentation](https://docusaurus.io/docs)
- [MagiDesk POS Repository](https://github.com/your-username/Order-Tracking-By-GPT)
