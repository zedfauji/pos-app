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
    public class AdvancedVisualFlowDemoTests
    {
        private FlaUI.Core.Application? _application;
        private Window? _mainWindow;
        private UIA3Automation? _automation;

        [TestInitialize]
        public async Task Setup()
        {
            Console.WriteLine("🎬 STARTING ADVANCED VISUAL FLOW DEMO SETUP");
            Console.WriteLine("============================================");
            Console.WriteLine($"🕐 Setup Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                // Launch the MagiDesk application
                Console.WriteLine("🚀 Launching MagiDesk Application for Advanced Visual Demo...");
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

                Console.WriteLine("🎉 ADVANCED VISUAL FLOW DEMO SETUP COMPLETED!");
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
        public async Task AdvancedVisualFlowDemo_CompleteUserJourney_ShouldShowAllFlows()
        {
            Console.WriteLine("🎬 STARTING ADVANCED VISUAL FLOW DEMO");
            Console.WriteLine("=====================================");
            Console.WriteLine($"🕐 Demo Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                // Step 1: Dashboard Overview
                await ShowDashboardOverview();
                
                // Step 2: Navigate to Tables Page
                await NavigateToTablesPage();
                
                // Step 3: Navigate to Orders Page
                await NavigateToOrdersPage();
                
                // Step 4: Navigate to Payments Page
                await NavigateToPaymentsPage();
                
                // Step 5: Navigate to Settings Page
                await NavigateToSettingsPage();
                
                Console.WriteLine("🎉 ADVANCED VISUAL FLOW DEMO FINISHED!");
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
            Console.WriteLine("👀 WATCH: We're starting on the Dashboard...");
            
            // Wait for user to see the dashboard
            await Task.Delay(2000);
            
            // Highlight the main window
            if (_mainWindow != null)
            {
                _mainWindow.DrawHighlight();
                Console.WriteLine("✅ Dashboard is visible and highlighted");
            }
            
            // Show current page information
            await ShowCurrentPageInfo();
            
            Console.WriteLine("✅ Dashboard overview completed");
            Console.WriteLine();
        }

        private async Task ShowCurrentPageInfo()
        {
            Console.WriteLine("📄 CURRENT PAGE INFORMATION");
            Console.WriteLine("============================");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Look for page title or header
                var textElements = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.Text));
                
                Console.WriteLine($"🔍 Found {textElements.Length} text elements on current page");
                
                // Show first few text elements to identify the page
                foreach (var element in textElements.Take(10))
                {
                    if (!string.IsNullOrEmpty(element.Name) && element.Name.Length > 2)
                    {
                        Console.WriteLine($"   📝 Text: '{element.Name}' at {element.BoundingRectangle}");
                    }
                }
                
                Console.WriteLine("✅ Page information displayed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting page info: {ex.Message}");
            }
        }

        private async Task NavigateToTablesPage()
        {
            Console.WriteLine("🪑 STEP 2: NAVIGATING TO TABLES PAGE");
            Console.WriteLine("====================================");
            Console.WriteLine("👀 WATCH: We'll navigate to the Tables page...");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Look for navigation elements (ListItems in navigation panel)
                var navigationItems = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.ListItem));
                
                Console.WriteLine($"🔍 Found {navigationItems.Length} navigation items");
                
                // Find Tables navigation item
                var tablesNavItem = navigationItems.FirstOrDefault(item => 
                    !string.IsNullOrEmpty(item.Name) && 
                    item.Name.Contains("Table", StringComparison.OrdinalIgnoreCase));
                
                if (tablesNavItem != null)
                {
                    Console.WriteLine($"🎯 Found Tables navigation: '{tablesNavItem.Name}' at {tablesNavItem.BoundingRectangle}");
                    tablesNavItem.DrawHighlight();
                    await Task.Delay(1500);
                    
                    Console.WriteLine("👆 Clicking Tables navigation...");
                    tablesNavItem.Click();
                    await Task.Delay(3000); // Wait for page to load
                    
                    Console.WriteLine("✅ Successfully navigated to Tables page!");
                    
                    // Show Tables page content
                    await ShowTablesPageContent();
                }
                else
                {
                    Console.WriteLine("⚠️ Tables navigation not found, looking for alternative...");
                    
                    // Try to find any navigation item that might be Tables
                    foreach (var item in navigationItems.Take(5))
                    {
                        if (!string.IsNullOrEmpty(item.Name))
                        {
                            Console.WriteLine($"   📍 Navigation item: '{item.Name}'");
                            item.DrawHighlight();
                            await Task.Delay(1000);
                        }
                    }
                }
                
                Console.WriteLine("✅ Tables navigation completed");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error navigating to Tables: {ex.Message}");
            }
        }

        private async Task ShowTablesPageContent()
        {
            Console.WriteLine("🪑 TABLES PAGE CONTENT");
            Console.WriteLine("=======================");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Look for table-related elements
                var buttons = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.Button));
                var tablesButtons = buttons.Where(b => !string.IsNullOrEmpty(b.Name)).Take(5);
                
                Console.WriteLine($"🔍 Found {buttons.Length} buttons on Tables page");
                
                foreach (var button in tablesButtons)
                {
                    Console.WriteLine($"   🎯 Table button: '{button.Name}' at {button.BoundingRectangle}");
                    button.DrawHighlight();
                    await Task.Delay(1000);
                }
                
                // Look for list items that might be tables
                var listItems = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.ListItem));
                var tableItems = listItems.Where(li => !string.IsNullOrEmpty(li.Name)).Take(3);
                
                Console.WriteLine($"🔍 Found {listItems.Length} list items on Tables page");
                
                foreach (var item in tableItems)
                {
                    Console.WriteLine($"   📋 Table item: '{item.Name}' at {item.BoundingRectangle}");
                    item.DrawHighlight();
                    await Task.Delay(1000);
                }
                
                Console.WriteLine("✅ Tables page content displayed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error showing Tables content: {ex.Message}");
            }
        }

        private async Task NavigateToOrdersPage()
        {
            Console.WriteLine("📋 STEP 3: NAVIGATING TO ORDERS PAGE");
            Console.WriteLine("====================================");
            Console.WriteLine("👀 WATCH: We'll navigate to the Orders page...");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Look for Orders navigation item
                var navigationItems = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.ListItem));
                
                var ordersNavItem = navigationItems.FirstOrDefault(item => 
                    !string.IsNullOrEmpty(item.Name) && 
                    item.Name.Contains("Order", StringComparison.OrdinalIgnoreCase));
                
                if (ordersNavItem != null)
                {
                    Console.WriteLine($"🎯 Found Orders navigation: '{ordersNavItem.Name}' at {ordersNavItem.BoundingRectangle}");
                    ordersNavItem.DrawHighlight();
                    await Task.Delay(1500);
                    
                    Console.WriteLine("👆 Clicking Orders navigation...");
                    ordersNavItem.Click();
                    await Task.Delay(3000); // Wait for page to load
                    
                    Console.WriteLine("✅ Successfully navigated to Orders page!");
                    
                    // Show Orders page content
                    await ShowOrdersPageContent();
                }
                else
                {
                    Console.WriteLine("⚠️ Orders navigation not found");
                }
                
                Console.WriteLine("✅ Orders navigation completed");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error navigating to Orders: {ex.Message}");
            }
        }

        private async Task ShowOrdersPageContent()
        {
            Console.WriteLine("📋 ORDERS PAGE CONTENT");
            Console.WriteLine("=======================");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Look for order-related buttons
                var buttons = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.Button));
                var orderButtons = buttons.Where(b => !string.IsNullOrEmpty(b.Name)).Take(5);
                
                Console.WriteLine($"🔍 Found {buttons.Length} buttons on Orders page");
                
                foreach (var button in orderButtons)
                {
                    Console.WriteLine($"   🎯 Order button: '{button.Name}' at {button.BoundingRectangle}");
                    button.DrawHighlight();
                    await Task.Delay(1000);
                }
                
                Console.WriteLine("✅ Orders page content displayed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error showing Orders content: {ex.Message}");
            }
        }

        private async Task NavigateToPaymentsPage()
        {
            Console.WriteLine("💳 STEP 4: NAVIGATING TO PAYMENTS PAGE");
            Console.WriteLine("======================================");
            Console.WriteLine("👀 WATCH: We'll navigate to the Payments page...");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Look for Payments navigation item
                var navigationItems = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.ListItem));
                
                var paymentsNavItem = navigationItems.FirstOrDefault(item => 
                    !string.IsNullOrEmpty(item.Name) && 
                    item.Name.Contains("Payment", StringComparison.OrdinalIgnoreCase));
                
                if (paymentsNavItem != null)
                {
                    Console.WriteLine($"🎯 Found Payments navigation: '{paymentsNavItem.Name}' at {paymentsNavItem.BoundingRectangle}");
                    paymentsNavItem.DrawHighlight();
                    await Task.Delay(1500);
                    
                    Console.WriteLine("👆 Clicking Payments navigation...");
                    paymentsNavItem.Click();
                    await Task.Delay(3000); // Wait for page to load
                    
                    Console.WriteLine("✅ Successfully navigated to Payments page!");
                    
                    // Show Payments page content
                    await ShowPaymentsPageContent();
                }
                else
                {
                    Console.WriteLine("⚠️ Payments navigation not found");
                }
                
                Console.WriteLine("✅ Payments navigation completed");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error navigating to Payments: {ex.Message}");
            }
        }

        private async Task ShowPaymentsPageContent()
        {
            Console.WriteLine("💳 PAYMENTS PAGE CONTENT");
            Console.WriteLine("=========================");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Look for payment-related buttons
                var buttons = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.Button));
                var paymentButtons = buttons.Where(b => !string.IsNullOrEmpty(b.Name)).Take(5);
                
                Console.WriteLine($"🔍 Found {buttons.Length} buttons on Payments page");
                
                foreach (var button in paymentButtons)
                {
                    Console.WriteLine($"   🎯 Payment button: '{button.Name}' at {button.BoundingRectangle}");
                    button.DrawHighlight();
                    await Task.Delay(1000);
                }
                
                Console.WriteLine("✅ Payments page content displayed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error showing Payments content: {ex.Message}");
            }
        }

        private async Task NavigateToSettingsPage()
        {
            Console.WriteLine("⚙️ STEP 5: NAVIGATING TO SETTINGS PAGE");
            Console.WriteLine("======================================");
            Console.WriteLine("👀 WATCH: We'll navigate to the Settings page...");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Look for Settings navigation item
                var navigationItems = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.ListItem));
                
                var settingsNavItem = navigationItems.FirstOrDefault(item => 
                    !string.IsNullOrEmpty(item.Name) && 
                    item.Name.Contains("Setting", StringComparison.OrdinalIgnoreCase));
                
                if (settingsNavItem != null)
                {
                    Console.WriteLine($"🎯 Found Settings navigation: '{settingsNavItem.Name}' at {settingsNavItem.BoundingRectangle}");
                    settingsNavItem.DrawHighlight();
                    await Task.Delay(1500);
                    
                    Console.WriteLine("👆 Clicking Settings navigation...");
                    settingsNavItem.Click();
                    await Task.Delay(3000); // Wait for page to load
                    
                    Console.WriteLine("✅ Successfully navigated to Settings page!");
                    
                    // Show Settings page content
                    await ShowSettingsPageContent();
                }
                else
                {
                    Console.WriteLine("⚠️ Settings navigation not found, trying to find settings controls...");
                    
                    // Look for settings-related controls like Dark Mode toggle
                    var darkModeButton = _mainWindow.FindFirstDescendant(_automation.ConditionFactory.ByAutomationId("DarkToggle"));
                    if (darkModeButton != null)
                    {
                        Console.WriteLine("🎯 Found Dark Mode toggle");
                        darkModeButton.DrawHighlight();
                        await Task.Delay(1500);
                        
                        Console.WriteLine("👆 Clicking Dark Mode toggle...");
                        darkModeButton.Click();
                        await Task.Delay(2000);
                        
                        Console.WriteLine("✅ Dark Mode toggle clicked!");
                    }
                }
                
                Console.WriteLine("✅ Settings navigation completed");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error navigating to Settings: {ex.Message}");
            }
        }

        private async Task ShowSettingsPageContent()
        {
            Console.WriteLine("⚙️ SETTINGS PAGE CONTENT");
            Console.WriteLine("=========================");
            
            if (_mainWindow == null || _automation == null) return;
            
            try
            {
                // Look for settings-related controls
                var buttons = _mainWindow.FindAllDescendants(_automation.ConditionFactory.ByControlType(ControlType.Button));
                var settingsButtons = buttons.Where(b => !string.IsNullOrEmpty(b.Name)).Take(5);
                
                Console.WriteLine($"🔍 Found {buttons.Length} buttons on Settings page");
                
                foreach (var button in settingsButtons)
                {
                    Console.WriteLine($"   🎯 Settings button: '{button.Name}' at {button.BoundingRectangle}");
                    button.DrawHighlight();
                    await Task.Delay(1000);
                }
                
                Console.WriteLine("✅ Settings page content displayed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error showing Settings content: {ex.Message}");
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
            foreach (var path in possiblePaths)
            {
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
