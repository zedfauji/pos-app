# Changelog Template

This is a template for creating changelog entries. Copy this template when creating new release notes.

## [Version] - YYYY-MM-DD

### Added
- New feature 1
- New feature 2

### Changed
- Changed behavior 1
- Changed behavior 2

### Deprecated
- Feature being deprecated 1
- Feature being deprecated 2

### Removed
- Removed feature 1
- Removed feature 2

### Fixed
- Bug fix 1
- Bug fix 2

### Security
- Security fix 1
- Security fix 2

---

## Release Notes Format

### Version Numbering

Follow [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Categories

- **Added**: New features
- **Changed**: Changes in existing functionality
- **Deprecated**: Soon-to-be removed features
- **Removed**: Removed features
- **Fixed**: Bug fixes
- **Security**: Security vulnerabilities fixed

### Example

```markdown
## [2.1.0] - 2025-01-27

### Added
- RBAC v2 API endpoints
- Permission-based UI visibility
- Comprehensive API documentation

### Changed
- Updated authentication flow to return permissions
- Improved error messages

### Fixed
- Fixed payment processing timeout issue
- Resolved database connection pool exhaustion

### Security
- Enhanced RBAC permission enforcement
- Fixed authentication token validation
```
