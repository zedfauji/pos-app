using Microsoft.VisualStudio.TestTools.UnitTesting;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using System.Diagnostics;
using System.Text.Json;
using FluentAssertions;

namespace MagiDesk.Tests.VisualFlowDemo
{
    [TestClass]
    public class VisualFlowDemoTests
    {
        private FlaUI.Core.Application? _application;
        private Window? _mainWindow;
        private UIA3Automation? _automation;

        [TestInitialize]
        public async Task Setup()
        {
            Console.WriteLine("🎬 STARTING VISUAL FLOW DEMO SETUP");
            Console.WriteLine("==================================");
            Console.WriteLine($"🕐 Setup Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                // Launch the MagiDesk application
                Console.WriteLine("🚀 Launching MagiDesk Application for Visual Demo...");
                await LaunchMagiDeskApplication();
                
                Console.WriteLine("⏳ Waiting for application to fully load...");
                await Task.Delay(3000);
                
                // Find the main window
                Console.WriteLine("🔍 Finding main window...");
                await FindMainWindow();
                
                if (_mainWindow == null)
                {
                    throw new InvalidOperationException("Could not find main application window");
                }
                
                Console.WriteLine($"✅ Main window found: '{_mainWindow.Title}'");
                Console.WriteLine($"   Window Bounds: {_mainWindow.BoundingRectangle}");
                Console.WriteLine();

                Console.WriteLine("🎉 VISUAL FLOW DEMO SETUP COMPLETED!");
                Console.WriteLine($"🕐 Setup Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR IN SETUP: {ex.Message}");
                throw;
            }
        }

        [TestCleanup]
        public void Cleanup()
        {
            Console.WriteLine("🧹 CLEANUP STARTED");
            Console.WriteLine("==================");
            
            try
            {
                // Force kill any MagiDesk processes
                var processes = Process.GetProcessesByName("MagiDesk.Frontend");
                foreach (var process in processes)
                {
                    Console.WriteLine($"🔪 Killing process: {process.Id}");
                    process.Kill();
                }
                
                // Close FlaUI application
                if (_application != null)
                {
                    _application.Close();
                    _application.Dispose();
                }
                
                _automation?.Dispose();
                
                Console.WriteLine("✅ CLEANUP COMPLETED");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CLEANUP ERROR: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task VisualFlowDemo_CompleteUserJourney_ShouldShowAllFlows()
        {
            Console.WriteLine("🎬 STARTING COMPLETE VISUAL FLOW DEMO");
            Console.WriteLine("=====================================");
            Console.WriteLine($"🕐 Demo Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                // Step 1: Dashboard Overview
                await ShowDashboardOverview();
                
                // Step 2: Table Management Flow
                await ShowTableManagementFlow();
                
                // Step 3: Order Management Flow
                await ShowOrderManagementFlow();
                
                // Step 4: Payment Flow
                await ShowPaymentFlow();
                
                // Step 5: Settings and Configuration
                await ShowSettingsFlow();
                
                Console.WriteLine("🎉 COMPLETE VISUAL FLOW DEMO FINISHED!");
                Console.WriteLine($"🕐 Demo Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DEMO ERROR: {ex.Message}");
                throw;
            }
        }

        private async Task ShowDashboardOverview()
        {
            Console.WriteLine("📊 STEP 1: DASHBOARD OVERVIEW");
            Console.WriteLine("=============================");
            Console.WriteLine("👀 WATCH: We'll explore the main dashboard...");
            
            // Wait for user to see the dashboard
            await Task.Delay(2000);
            
            // Highlight the main window
            if (_mainWindow != null)
            {
                _mainWindow.DrawHighlight();
                Console.WriteLine("✅ Dashboard is visible and highlighted");
            }
            
            // Show available navigation elements
            await ShowNavigationElements();
            
            Console.WriteLine("✅ Dashboard overview completed");
            Console.WriteLine();
        }

        private async Task ShowNavigationElements()
        {
            Console.WriteLine("🧭 EXPLORING NAVIGATION ELEMENTS");
            Console.WriteLine("================================");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                var navigationElements = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.ListItem));
                Console.WriteLine($"🎯 Found {navigationElements.Length} navigation elements");
                
                foreach (var element in navigationElements.Take(5))
                {
                    if (!string.IsNullOrEmpty(element.Name))
                    {
                        Console.WriteLine($"   📍 {element.Name} at {element.BoundingRectangle}");
                        element.DrawHighlight();
                        await Task.Delay(1000);
                    }
                }
                
                Console.WriteLine("✅ Navigation exploration completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Navigation exploration error: {ex.Message}");
            }
        }

        private async Task ShowTableManagementFlow()
        {
            Console.WriteLine("🪑 STEP 2: TABLE MANAGEMENT FLOW");
            Console.WriteLine("================================");
            Console.WriteLine("👀 WATCH: We'll demonstrate table management...");
            
            // Look for table-related buttons
            await FindAndClickButton("Table", "table management");
            
            // Wait to see the table management interface
            await Task.Delay(3000);
            
            // Show table operations
            await ShowTableOperations();
            
            Console.WriteLine("✅ Table management flow completed");
            Console.WriteLine();
        }

        private async Task ShowTableOperations()
        {
            Console.WriteLine("🪑 DEMONSTRATING TABLE OPERATIONS");
            Console.WriteLine("==================================");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Find buttons related to table operations
                var buttons = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.Button));
                
                var tableButtons = buttons.Where(b => 
                    !string.IsNullOrEmpty(b.Name) && 
                    (b.Name.Contains("Add", StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase))
                ).Take(3);
                
                foreach (var button in tableButtons)
                {
                    Console.WriteLine($"🎯 Found table button: {button.Name}");
                    button.DrawHighlight();
                    await Task.Delay(1500);
                    
                    // Click the button
                    Console.WriteLine($"👆 Clicking {button.Name}...");
                    button.Click();
                    await Task.Delay(2000);
                    
                    Console.WriteLine($"✅ {button.Name} clicked successfully");
                }
                
                Console.WriteLine("✅ Table operations demonstrated");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Table operations error: {ex.Message}");
            }
        }

        private async Task ShowOrderManagementFlow()
        {
            Console.WriteLine("📋 STEP 3: ORDER MANAGEMENT FLOW");
            Console.WriteLine("================================");
            Console.WriteLine("👀 WATCH: We'll demonstrate order management...");
            
            // Look for order-related navigation
            await FindAndClickNavigation("Order", "order management");
            
            // Wait to see the order management interface
            await Task.Delay(3000);
            
            // Show order operations
            await ShowOrderOperations();
            
            Console.WriteLine("✅ Order management flow completed");
            Console.WriteLine();
        }

        private async Task ShowOrderOperations()
        {
            Console.WriteLine("📋 DEMONSTRATING ORDER OPERATIONS");
            Console.WriteLine("==================================");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Find order-related buttons
                var buttons = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.Button));
                
                var orderButtons = buttons.Where(b => 
                    !string.IsNullOrEmpty(b.Name) && 
                    (b.Name.Contains("Add", StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("Edit", StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("Refresh", StringComparison.OrdinalIgnoreCase))
                ).Take(4);
                
                foreach (var button in orderButtons)
                {
                    Console.WriteLine($"🎯 Found order button: {button.Name}");
                    button.DrawHighlight();
                    await Task.Delay(1500);
                    
                    // Click the button
                    Console.WriteLine($"👆 Clicking {button.Name}...");
                    button.Click();
                    await Task.Delay(2000);
                    
                    Console.WriteLine($"✅ {button.Name} clicked successfully");
                }
                
                Console.WriteLine("✅ Order operations demonstrated");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Order operations error: {ex.Message}");
            }
        }

        private async Task ShowPaymentFlow()
        {
            Console.WriteLine("💳 STEP 4: PAYMENT FLOW");
            Console.WriteLine("=======================");
            Console.WriteLine("👀 WATCH: We'll demonstrate payment processing...");
            
            // Look for payment-related navigation
            await FindAndClickNavigation("Payment", "payment processing");
            
            // Wait to see the payment interface
            await Task.Delay(3000);
            
            // Show payment operations
            await ShowPaymentOperations();
            
            Console.WriteLine("✅ Payment flow completed");
            Console.WriteLine();
        }

        private async Task ShowPaymentOperations()
        {
            Console.WriteLine("💳 DEMONSTRATING PAYMENT OPERATIONS");
            Console.WriteLine("====================================");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Find payment-related buttons
                var buttons = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.Button));
                
                var paymentButtons = buttons.Where(b => 
                    !string.IsNullOrEmpty(b.Name) && 
                    (b.Name.Contains("Payment", StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("Process", StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("Refund", StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("Cash", StringComparison.OrdinalIgnoreCase) ||
                     b.Name.Contains("Card", StringComparison.OrdinalIgnoreCase))
                ).Take(3);
                
                foreach (var button in paymentButtons)
                {
                    Console.WriteLine($"🎯 Found payment button: {button.Name}");
                    button.DrawHighlight();
                    await Task.Delay(1500);
                    
                    // Click the button
                    Console.WriteLine($"👆 Clicking {button.Name}...");
                    button.Click();
                    await Task.Delay(2000);
                    
                    Console.WriteLine($"✅ {button.Name} clicked successfully");
                }
                
                Console.WriteLine("✅ Payment operations demonstrated");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Payment operations error: {ex.Message}");
            }
        }

        private async Task ShowSettingsFlow()
        {
            Console.WriteLine("⚙️ STEP 5: SETTINGS AND CONFIGURATION");
            Console.WriteLine("======================================");
            Console.WriteLine("👀 WATCH: We'll demonstrate settings management...");
            
            // Look for settings-related elements
            await FindAndClickButton("Settings", "settings management");
            
            // Wait to see the settings interface
            await Task.Delay(3000);
            
            // Show settings operations
            await ShowSettingsOperations();
            
            Console.WriteLine("✅ Settings flow completed");
            Console.WriteLine();
        }

        private async Task ShowSettingsOperations()
        {
            Console.WriteLine("⚙️ DEMONSTRATING SETTINGS OPERATIONS");
            Console.WriteLine("=====================================");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Look for dark mode toggle
                var darkModeButton = _mainWindow.FindFirstDescendant(_automation.ConditionFactory.ByAutomationId("DarkToggle"));
                if (darkModeButton != null)
                {
                    Console.WriteLine("🎯 Found Dark Mode toggle");
                    darkModeButton.DrawHighlight();
                    await Task.Delay(1500);
                    
                    Console.WriteLine("👆 Clicking Dark Mode toggle...");
                    darkModeButton.Click();
                    await Task.Delay(2000);
                    
                    Console.WriteLine("✅ Dark Mode toggle clicked successfully");
                }
                
                // Look for logout button
                var logoutButton = _mainWindow.FindFirstDescendant(_automation.ConditionFactory.ByAutomationId("LogoutBtn"));
                if (logoutButton != null)
                {
                    Console.WriteLine("🎯 Found Logout button");
                    logoutButton.DrawHighlight();
                    await Task.Delay(1500);
                    
                    Console.WriteLine("👆 Clicking Logout button...");
                    logoutButton.Click();
                    await Task.Delay(2000);
                    
                    Console.WriteLine("✅ Logout button clicked successfully");
                }
                
                Console.WriteLine("✅ Settings operations demonstrated");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Settings operations error: {ex.Message}");
            }
        }

        private async Task FindAndClickButton(string buttonName, string description)
        {
            Console.WriteLine($"🔍 Looking for {description}...");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                var buttons = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.Button));
                var targetButton = buttons.FirstOrDefault(b => 
                    !string.IsNullOrEmpty(b.Name) && 
                    b.Name.Contains(buttonName, StringComparison.OrdinalIgnoreCase));
                
                if (targetButton != null)
                {
                    Console.WriteLine($"🎯 Found {buttonName} button at {targetButton.BoundingRectangle}");
                    targetButton.DrawHighlight();
                    await Task.Delay(1500);
                    
                    Console.WriteLine($"👆 Clicking {buttonName}...");
                    targetButton.Click();
                    await Task.Delay(2000);
                    
                    Console.WriteLine($"✅ {buttonName} clicked successfully");
                }
                else
                {
                    Console.WriteLine($"⚠️ {buttonName} button not found, continuing...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error finding {buttonName}: {ex.Message}");
            }
        }

        private async Task FindAndClickNavigation(string navName, string description)
        {
            Console.WriteLine($"🔍 Looking for {description} navigation...");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                var navItems = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.ListItem));
                var targetNav = navItems.FirstOrDefault(n => 
                    !string.IsNullOrEmpty(n.Name) && 
                    n.Name.Contains(navName, StringComparison.OrdinalIgnoreCase));
                
                if (targetNav != null)
                {
                    Console.WriteLine($"🎯 Found {navName} navigation at {targetNav.BoundingRectangle}");
                    targetNav.DrawHighlight();
                    await Task.Delay(1500);
                    
                    Console.WriteLine($"👆 Clicking {navName}...");
                    targetNav.Click();
                    await Task.Delay(2000);
                    
                    Console.WriteLine($"✅ {navName} navigation clicked successfully");
                }
                else
                {
                    Console.WriteLine($"⚠️ {navName} navigation not found, continuing...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error finding {navName}: {ex.Message}");
            }
        }

        private async Task LaunchMagiDeskApplication()
        {
            Console.WriteLine("🚀 Launching MagiDesk Application...");
            
            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "frontend", "bin", "Release", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "frontend", "bin", "Release", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "frontend", "bin", "Release", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "frontend", "bin", "Debug", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "frontend", "bin", "Debug", "net8.0-windows10.0.19041.0", "MagiDesk.Frontend.exe")
            };

            string? appPath = null;
            Console.WriteLine($"🔍 Current Directory: {Directory.GetCurrentDirectory()}");
            foreach (var path in possiblePaths)
            {
                Console.WriteLine($"🔍 Checking path: {path}");
                Console.WriteLine($"🔍 File exists: {File.Exists(path)}");
                if (File.Exists(path))
                {
                    appPath = path;
                    Console.WriteLine($"✅ Found application at: {path}");
                    break;
                }
            }

            if (appPath == null)
            {
                throw new FileNotFoundException("Could not find MagiDesk.Frontend.exe");
            }

            Console.WriteLine($"📱 Application Path: {appPath}");
            
            _automation = new UIA3Automation();
            _application = FlaUI.Core.Application.Launch(appPath);
            
            Console.WriteLine($"✅ Application launched successfully!");
            Console.WriteLine($"📱 Application Handle: {_application.ProcessId}");
        }

        private async Task FindMainWindow()
        {
            Console.WriteLine("🔍 Finding main application window...");
            
            if (_application == null || _automation == null)
            {
                throw new InvalidOperationException("Application or automation not initialized");
            }

            // Wait for the main window to appear
            var attempts = 0;
            while (attempts < 10)
            {
                try
                {
                    _mainWindow = _application.GetMainWindow(_automation);
                    if (_mainWindow != null && !string.IsNullOrEmpty(_mainWindow.Title))
                    {
                        Console.WriteLine($"✅ Found main window: {_mainWindow.Title}");
                        break;
                    }
                }
                catch
                {
                    // Window not ready yet
                }
                
                await Task.Delay(1000);
                attempts++;
            }

            if (_mainWindow == null)
            {
                throw new InvalidOperationException("Could not find main window within timeout");
            }
        }
    }
}
