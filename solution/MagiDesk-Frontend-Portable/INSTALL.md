# MagiDesk POS - Installation Guide

## 📦 Installation Options

### Option 1: Portable (Recommended)
1. **Extract** this folder to any location on your computer
2. **Run** `MagiDesk.Frontend.exe` directly
3. **No installation required** - completely portable!

### Option 2: System Installation
1. **Create a folder** in Program Files: `C:\Program Files\MagiDesk\`
2. **Copy all files** from this package to that folder
3. **Create a shortcut** to `MagiDesk.Frontend.exe` on your desktop
4. **Run from shortcut** or Start Menu

## 🚀 First Run Setup

1. **Launch** MagiDesk.Frontend.exe
2. **Go to Settings** (gear icon in navigation)
3. **Configure API URLs** in the "API Connections" section
4. **Set your business information** in "Business Settings"
5. **Configure printer** in "Print Settings"
6. **Save all settings**

## ⚙️ System Requirements

- **OS**: Windows 10 version 1903 or later, Windows 11
- **Architecture**: x64 (64-bit)
- **RAM**: 4GB minimum, 8GB recommended
- **Storage**: 500MB free space
- **Network**: Internet connection for backend API access

## 🔧 Backend Configuration

The frontend needs these backend services running:

```
Settings API: https://magidesk-settings-904541739138.us-central1.run.app
Menu API: https://magidesk-menu-904541739138.northamerica-south1.run.app
Order API: https://magidesk-order-904541739138.northamerica-south1.run.app
Payment API: https://magidesk-payment-904541739138.northamerica-south1.run.app
Tables API: https://magidesk-tables-904541739138.northamerica-south1.run.app
Users API: https://magidesk-backend-904541739138.us-central1.run.app
Inventory API: http://localhost:5001 (configure as needed)
```

## 🎯 Enhanced Settings Features

This version includes a comprehensive settings system:

### General Settings
- Theme selection (Light/Dark)
- Language and locale
- Notification preferences

### API Management
- All backend service URLs
- Connection testing
- Status monitoring

### Business Configuration
- Company information
- Tax rates
- Receipt formatting
- Business address

### Print Management
- Paper width selection
- Printer configuration
- Receipt formatting
- Test printing

### Table Management
- Session timers
- Auto-stop settings
- Warning notifications

## 🚨 Troubleshooting

### Application Won't Start
- Ensure you're running Windows 10/11 x64
- Check if antivirus is blocking the application
- Try running as Administrator

### Backend Connection Issues
- Verify backend services are running
- Check network connectivity
- Update API URLs in Settings if needed

### Printer Issues
- Ensure printer is installed and accessible
- Check printer settings in the application
- Try the "Test Print" function

## 📞 Support

For technical support, contact your system administrator or IT department.

---
**Ready to use!** Just run `MagiDesk.Frontend.exe` and configure your settings.
