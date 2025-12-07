# Contributing Guide

Welcome to the MagiDesk POS project! This guide will help you contribute effectively.

## Table of Contents

- [Code of Conduct](./code-of-conduct.md)
- [Getting Started](./getting-started.md)
- [Development Workflow](./development-workflow.md)
- [Coding Standards](./coding-standards.md)
- [Commit Guidelines](./commit-guidelines.md)
- [Pull Request Process](./pull-request-process.md)
- [Testing Requirements](./testing-requirements.md)

## Quick Start

1. **Fork the Repository**
2. **Clone Your Fork**
   ```powershell
   git clone https://github.com/your-username/pos-app.git
   ```
3. **Create a Branch**
   ```powershell
   git checkout -b feature/your-feature-name
   ```
4. **Make Changes**
5. **Test Your Changes**
   ```powershell
   dotnet test solution/MagiDesk.sln
   ```
6. **Commit and Push**
   ```powershell
   git commit -m "feat: add your feature"
   git push origin feature/your-feature-name
   ```
7. **Create Pull Request**

## Development Workflow

### Branch Strategy

- **main**: Production-ready code
- **develop**: Integration branch for features
- **feature/***: New features
- **bugfix/***: Bug fixes
- **hotfix/***: Critical production fixes

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `style`: Code style changes
- `refactor`: Code refactoring
- `test`: Test changes
- `chore`: Maintenance tasks

**Example:**
```
feat(users): add user search functionality

- Add search by username
- Add search by email
- Add pagination support

Closes #123
```

## Coding Standards

### C# Code Style

- Use **PascalCase** for classes, methods, properties
- Use **camelCase** for variables, parameters
- Use **UPPER_CASE** for constants
- Use meaningful names
- Keep methods small (< 50 lines)
- Use async/await for I/O operations

### XAML Style

- Use **PascalCase** for element names
- Use meaningful x:Name values
- Keep XAML files organized
- Use resource dictionaries for styles

### Documentation

- Document public APIs
- Include XML comments for classes and methods
- Update README for significant changes
- Keep documentation up to date

## Testing Requirements

- All new features must include tests
- Maintain > 75% code coverage
- Tests must pass before PR merge
- Include integration tests for APIs

## Pull Request Process

1. **Update Documentation**: Update relevant docs
2. **Add Tests**: Include tests for new features
3. **Update Changelog**: Add entry to CHANGELOG.md
4. **Request Review**: Request review from maintainers
5. **Address Feedback**: Respond to review comments
6. **Merge**: Maintainer will merge after approval

## Code Review Guidelines

### For Authors

- Keep PRs small and focused
- Provide clear description
- Link related issues
- Respond to feedback promptly

### For Reviewers

- Be constructive and respectful
- Focus on code, not person
- Provide specific feedback
- Approve when ready

## Getting Help

- **Documentation**: Check this developer portal
- **Issues**: Open an issue on GitHub
- **Discussions**: Use GitHub Discussions
- **Email**: Contact maintainers

## Recognition

Contributors will be:
- Listed in CONTRIBUTORS.md
- Credited in release notes
- Thanked in project documentation

Thank you for contributing to MagiDesk POS!
