# MagiDesk POS System - Frontend Application

## 🚀 Quick Start

1. **Run the Application**: Double-click `MagiDesk.Frontend.exe` to start the POS system
2. **Configure Settings**: Go to Settings page to configure your backend API endpoints
3. **Start Using**: The application is ready to use with all improved settings functionality

## ✨ What's New - Enhanced Settings System

This version includes a completely improved settings system with:

### 🎛️ **General Settings**
- Theme selection (Light/Dark)
- Language and locale settings
- Notification preferences
- Host key configuration

### 🔗 **API Connections**
- Backend API URL configuration
- Menu API settings
- Order API settings  
- Payment API settings
- Tables API settings
- Inventory API settings
- Users API settings
- Settings API configuration

### 🏢 **Business Settings**
- Company information
- Tax rate configuration
- Receipt formatting options
- Business address and contact details
- Receipt header/footer customization

### 🖨️ **Print Settings**
- Paper width selection (58mm/80mm)
- Printer configuration
- Receipt formatting
- Print test functionality

### 🪑 **Table Management Settings**
- Session timer configuration
- Auto-stop settings
- Warning notifications
- Rate management

## 🔧 Backend Requirements

This frontend requires the following backend services to be running:

- **Settings API**: `https://magidesk-settings-904541739138.us-central1.run.app`
- **Menu API**: `https://magidesk-menu-904541739138.northamerica-south1.run.app`
- **Order API**: `https://magidesk-order-904541739138.northamerica-south1.run.app`
- **Payment API**: `https://magidesk-payment-904541739138.northamerica-south1.run.app`
- **Tables API**: `https://magidesk-tables-904541739138.northamerica-south1.run.app`
- **Inventory API**: `http://localhost:5001` (or your deployed URL)
- **Users API**: `https://magidesk-backend-904541739138.us-central1.run.app`

## 📋 Configuration Steps

1. **Launch the application**
2. **Navigate to Settings** (gear icon in the navigation)
3. **Configure API Connections**:
   - Update the URLs to match your backend deployment
   - Test connectivity using the "Test Connections" button
4. **Set Business Information**:
   - Enter your company details
   - Configure tax rates
   - Set receipt formatting preferences
5. **Configure Print Settings**:
   - Select paper width
   - Test printer functionality
6. **Save Settings**: Click "Save All Settings" to persist your configuration

## 🎯 Key Features

- ✅ **MVVM Architecture**: Clean, maintainable code structure
- ✅ **Comprehensive Settings**: All aspects of the POS system configurable
- ✅ **Real-time Validation**: Input validation and error handling
- ✅ **Async Operations**: Non-blocking UI with proper async/await patterns
- ✅ **Error Handling**: Robust error handling and user feedback
- ✅ **Modern UI**: WinUI 3 with beautiful, responsive design

## 🛠️ Technical Details

- **Framework**: .NET 8.0 with WinUI 3
- **Architecture**: MVVM pattern with dependency injection
- **Self-Contained**: No additional .NET installation required
- **Platform**: Windows 10/11 x64
- **Dependencies**: All included in this package

## 📞 Support

For technical support or questions about the enhanced settings system, please contact your system administrator.

---
**Version**: 1.0.0 with Enhanced Settings  
**Build Date**: September 13, 2025  
**Target**: Windows 10/11 x64  
**Package Type**: Self-contained portable application
