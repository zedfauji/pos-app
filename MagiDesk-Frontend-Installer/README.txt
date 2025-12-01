# MagiDesk POS System - Frontend Installer

## Installation Instructions

1. **Run Install.bat** as Administrator for system-wide installation, or as regular user for user-only installation.

2. **Prerequisites**: 
   - Windows 10/11
   - .NET 8.0 Runtime (if not self-contained)
   - Backend services must be running and accessible

## What's Included

- ✅ **Complete Settings Management**: Comprehensive settings for all aspects of the POS system
- ✅ **Business/Receipt Settings**: Paper width, tax rates, business info, receipt formatting
- ✅ **API Connections**: All backend API endpoints configuration
- ✅ **Notification Settings**: Toast notifications, sound alerts
- ✅ **Locale Settings**: Language and regional preferences
- ✅ **Table Management**: Session timers, auto-stop settings
- ✅ **Print Settings**: Receipt printer configuration

## Backend Requirements

This frontend application requires the following backend services to be running:

- **Settings API**: For managing all application settings
- **Menu API**: For menu management
- **Order API**: For order processing
- **Payment API**: For payment processing
- **Tables API**: For table management
- **Inventory API**: For inventory management
- **Users API**: For user management

## Configuration

After installation, configure the API endpoints in Settings:
1. Open MagiDesk POS
2. Go to Settings page
3. Configure API Connections section
4. Set the correct URLs for your backend services

## Features

### Enhanced Settings System
- **General Settings**: Theme, locale, notifications
- **API Connections**: All backend service URLs
- **Business Settings**: Company info, tax rates, receipt formatting
- **Table Settings**: Session management, timers
- **Print Settings**: Receipt printer configuration

### MVVM Architecture
- Clean separation of concerns
- Proper data binding
- Async/await patterns
- Error handling and validation

## Support

For support or issues, please contact your system administrator.

---
**Version**: 1.0.0
**Build Date**: 2025-09-17 13:55:18
**Target**: Windows 10/11 x64
